using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.RenderTree;
using CellBorder = DocumentEditor.Engine.Model.Properties.CellBorder;
using CellBorders = DocumentEditor.Engine.Model.Properties.CellBorders;
using CellPadding = DocumentEditor.Engine.Model.Properties.CellPadding;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using ModelParagraph = DocumentEditor.Engine.Model.Paragraph;
using ModelRun = DocumentEditor.Engine.Model.Run;
using ModelTable = DocumentEditor.Engine.Model.Table;
using ModelHyperlink = DocumentEditor.Engine.Model.Hyperlink;
using OxParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OxRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using OxTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using OxHyperlink = DocumentFormat.OpenXml.Wordprocessing.Hyperlink;
using OxRunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using OxTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using OxTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using ModelRunProperties = DocumentEditor.Engine.Model.Properties.RunProperties;
using ModelParagraphProperties = DocumentEditor.Engine.Model.Properties.ParagraphProperties;
using ModelDocumentProperties = DocumentEditor.Engine.Model.Properties.DocumentProperties;
using ModelSectionProperties = DocumentEditor.Engine.Model.Properties.SectionProperties;
using OxSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;
using OxSectionType = DocumentFormat.OpenXml.Wordprocessing.SectionType;

namespace DocumentEditor.Engine.Serialization;

public class DocxImporter
{
    // Effective conditional format properties for one table style "band"
    private record TableStyleCondFmt(
        string? CellShading,
        CellBorders? CellBorders,
        bool? RunBold,
        bool? RunItalic,
        string? RunColor,
        int? RunFontSize,
        string? RunFontFamily
    );

    // All conditional format slots for a resolved table style
    private record TableStyleDef(
        TableStyleCondFmt? WholeTable,
        TableStyleCondFmt? FirstRow,
        TableStyleCondFmt? LastRow,
        TableStyleCondFmt? Band1Row,
        TableStyleCondFmt? Band2Row,
        TableStyleCondFmt? FirstCol,
        TableStyleCondFmt? LastCol,
        CellPadding? DefaultCellPadding  // w:tblCellMar inside the style's w:tblPr
    );

    public DocxDocument Import(byte[] docxBytes)
    {
        using var stream = new MemoryStream(docxBytes);
        using var wordDoc = WordprocessingDocument.Open(stream, false);

        var mainPart = wordDoc.MainDocumentPart
            ?? throw new InvalidOperationException("Document has no main part");
        var body = mainPart.Document?.Body
            ?? throw new InvalidOperationException("Document has no body");

        var doc = new DocxDocument();
        doc.Properties = ExtractDocumentProperties(body, mainPart);

        var docDefaults  = ReadDocDefaults(mainPart);
        var styleSpacing = BuildStyleSpacingTable(mainPart);
        var styleRunProps = BuildStyleRunPropertiesTable(mainPart);
        var numFormatLookup = BuildNumFormatLookup(mainPart);

        foreach (var element in body.ChildElements)
        {
            if (element is OxParagraph oxPara)
                doc.Children.Add(ConvertParagraph(oxPara, mainPart, docDefaults, styleSpacing, numFormatLookup, styleRunProps));
            else if (element is OxTable oxTable)
                doc.Children.Add(ConvertTable(oxTable, mainPart, docDefaults, styleSpacing, numFormatLookup, styleRunProps));
        }

        // Absorb empty section-break holder paragraphs into the preceding paragraph.
        // In OOXML, a mid-document section break lives in an empty paragraph's w:pPr/w:sectPr.
        // Keeping that empty paragraph as a standalone DOM block causes pagination to push it
        // onto the next physical page. Merging it into the preceding paragraph's properties
        // places the section-break indicator at the correct visual location.
        for (int i = doc.Children.Count - 1; i > 0; i--)
        {
            if (doc.Children[i] is ModelParagraph holder
                && holder.Properties.SectionBreak is not null
                && holder.Children.All(c => c is ModelRun r && string.IsNullOrWhiteSpace(r.Text))
                && doc.Children[i - 1] is ModelParagraph preceding)
            {
                preceding.Properties.SectionBreak = holder.Properties.SectionBreak;
                doc.Children.RemoveAt(i);
            }
        }

        // Strip the mandatory OOXML trailing empty paragraph.
        // Word never displays it; WaveEditor's exporter appends a body-level <w:sectPr>
        // directly, so removing it here is safe and round-trip export is unaffected.
        while (doc.Children.Count > 1
            && doc.Children[^1] is ModelParagraph lastPara
            && lastPara.Properties.SectionBreak is null      // never strip a section-break holder
            && IsTrailingEmptyParagraph(lastPara))
        {
            doc.Children.RemoveAt(doc.Children.Count - 1);
        }

        return doc;
    }

    private static bool IsTrailingEmptyParagraph(ModelParagraph para) =>
        para.Children.All(c => c is ModelRun r && string.IsNullOrWhiteSpace(r.Text));

    private static ModelDocumentProperties ExtractDocumentProperties(Body body, MainDocumentPart mainPart)
    {
        var props = new ModelDocumentProperties();
        var sectPr = body.Elements<OxSectionProperties>().FirstOrDefault();
        if (sectPr is null) return props;

        var pageSize = sectPr.Elements<PageSize>().FirstOrDefault();
        if (pageSize is not null)
        {
            if (pageSize.Width is not null) props.PageWidth = (int)(uint)pageSize.Width;
            if (pageSize.Height is not null) props.PageHeight = (int)(uint)pageSize.Height;
            if (pageSize.Orient?.HasValue == true && pageSize.Orient.Value == PageOrientationValues.Landscape)
                props.Orientation = Orientation.Landscape;
        }

        var pageMargin = sectPr.Elements<PageMargin>().FirstOrDefault();
        if (pageMargin is not null)
        {
            if (pageMargin.Top is not null) props.MarginTop = pageMargin.Top;
            if (pageMargin.Bottom is not null) props.MarginBottom = pageMargin.Bottom;
            if (pageMargin.Left is not null) props.MarginLeft = (int)(uint)pageMargin.Left;
            if (pageMargin.Right is not null) props.MarginRight = (int)(uint)pageMargin.Right;
            if (pageMargin.Header is not null) props.HeaderDistance = (int)(uint)pageMargin.Header;
            if (pageMargin.Footer is not null) props.FooterDistance = (int)(uint)pageMargin.Footer;
        }

        // Columns (w:cols)
        var columns = sectPr.Elements<Columns>().FirstOrDefault();
        if (columns is not null)
        {
            if (columns.ColumnCount?.HasValue == true)
                props.ColumnCount = (int)(short)columns.ColumnCount;
            if (columns.Space?.Value is not null && int.TryParse(columns.Space.Value, out var colSpace))
                props.ColumnSpacing = colSpace;
            if (columns.Separator?.HasValue == true && columns.Separator.Value)
                props.ColumnSeparator = true;
        }

        // Title page (different first-page header/footer)
        if (sectPr.Elements<TitlePage>().Any())
            props.TitlePage = true;

        // Headers and footers
        ExtractHeadersAndFooters(sectPr, mainPart, props.Headers = new(), props.Footers = new(),
            out var rawParts);
        props.HeaderFooterParts = rawParts;

        // Clean up empty dicts
        if (props.Headers.Count == 0) props.Headers = null;
        if (props.Footers.Count == 0) props.Footers = null;

        return props;
    }

    /// <summary>
    /// Shared logic for reading section properties from any w:sectPr element.
    /// Used for both mid-document and body-level section properties.
    /// </summary>
    private static ModelSectionProperties ExtractSectionProperties(OxSectionProperties sectPr, MainDocumentPart mainPart)
    {
        var sp = new ModelSectionProperties();

        var sectionType = sectPr.Elements<OxSectionType>().FirstOrDefault();
        if (sectionType?.Val?.HasValue == true)
        {
            var val = sectionType.Val.Value;
            if (val == SectionMarkValues.Continuous) sp.BreakType = SectionBreakType.Continuous;
            else if (val == SectionMarkValues.EvenPage) sp.BreakType = SectionBreakType.EvenPage;
            else if (val == SectionMarkValues.OddPage) sp.BreakType = SectionBreakType.OddPage;
            else sp.BreakType = SectionBreakType.NextPage;
        }

        var pageSize = sectPr.Elements<PageSize>().FirstOrDefault();
        if (pageSize is not null)
        {
            if (pageSize.Width is not null) sp.PageWidth = (int)(uint)pageSize.Width;
            if (pageSize.Height is not null) sp.PageHeight = (int)(uint)pageSize.Height;
            if (pageSize.Orient?.HasValue == true && pageSize.Orient.Value == PageOrientationValues.Landscape)
                sp.Orientation = Orientation.Landscape;
        }

        var pageMargin = sectPr.Elements<PageMargin>().FirstOrDefault();
        if (pageMargin is not null)
        {
            if (pageMargin.Top is not null) sp.MarginTop = pageMargin.Top;
            if (pageMargin.Bottom is not null) sp.MarginBottom = pageMargin.Bottom;
            if (pageMargin.Left is not null) sp.MarginLeft = (int)(uint)pageMargin.Left;
            if (pageMargin.Right is not null) sp.MarginRight = (int)(uint)pageMargin.Right;
            if (pageMargin.Header is not null) sp.HeaderDistance = (int)(uint)pageMargin.Header;
            if (pageMargin.Footer is not null) sp.FooterDistance = (int)(uint)pageMargin.Footer;
        }

        // Columns (w:cols)
        var spColumns = sectPr.Elements<Columns>().FirstOrDefault();
        if (spColumns is not null)
        {
            if (spColumns.ColumnCount?.HasValue == true)
                sp.ColumnCount = (int)(short)spColumns.ColumnCount;
            if (spColumns.Space?.Value is not null && int.TryParse(spColumns.Space.Value, out var spColSpace))
                sp.ColumnSpacing = spColSpace;
            if (spColumns.Separator?.HasValue == true && spColumns.Separator.Value)
                sp.ColumnSeparator = true;
        }

        // Title page (different first-page header/footer)
        if (sectPr.Elements<TitlePage>().Any())
            sp.TitlePage = true;

        // Headers and footers
        ExtractHeadersAndFooters(sectPr, mainPart, sp.Headers = new(), sp.Footers = new(),
            out var rawParts);
        sp.HeaderFooterParts = rawParts;

        // Clean up empty dicts
        if (sp.Headers.Count == 0) sp.Headers = null;
        if (sp.Footers.Count == 0) sp.Footers = null;

        return sp;
    }

    private record struct StyleSpacing(
        int? SpaceBefore,
        int? SpaceAfter,
        int? LineSpacing,
        string? LineSpacingRule,
        bool? ContextualSpacing);

    private record struct StyleRunProps(
        bool? Bold, bool? Italic, string? FontFamily, int? FontSize, string? Color);

    private record struct NumLevelInfo(string Format, int? IndentLeft, int? IndentHanging);

    private static Dictionary<string, StyleSpacing> BuildStyleSpacingTable(MainDocumentPart mainPart)
    {
        var table = new Dictionary<string, StyleSpacing>(StringComparer.OrdinalIgnoreCase);
        var stylesPart = mainPart.StyleDefinitionsPart;
        if (stylesPart?.Styles is null) return table;

        foreach (var style in stylesPart.Styles.Elements<Style>())
        {
            if (style.StyleId?.Value is null) continue;
            if (style.Type?.Value != StyleValues.Paragraph) continue;

            int? before = null, after = null, line = null;
            string? rule = null;
            var spacing = style.StyleParagraphProperties?.SpacingBetweenLines;
            if (spacing is not null)
            {
                if (spacing.Before?.Value is not null && int.TryParse(spacing.Before.Value, out var b)) before = b;
                if (spacing.After?.Value is not null && int.TryParse(spacing.After.Value, out var a)) after = a;
                if (spacing.Line?.Value is not null && int.TryParse(spacing.Line.Value, out var l)) line = l;
                if (spacing.LineRule?.HasValue == true)
                    rule = spacing.LineRule.Value.ToString().ToLowerInvariant();
            }

            bool? ctxSpacing = null;
            var ctxEl = style.StyleParagraphProperties?.ContextualSpacing;
            if (ctxEl is not null)
                ctxSpacing = ctxEl.Val?.Value != false;

            if (spacing is null && ctxSpacing is null) continue;
            table[style.StyleId.Value] = new StyleSpacing(before, after, line, rule, ctxSpacing);
        }
        return table;
    }

    private static Dictionary<string, StyleRunProps> BuildStyleRunPropertiesTable(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.StyleDefinitionsPart;
        if (stylesPart?.Styles is null)
            return new Dictionary<string, StyleRunProps>(StringComparer.OrdinalIgnoreCase);

        // Step 1: collect raw (un-inherited) run props and basedOn references for each paragraph style
        var rawProps  = new Dictionary<string, StyleRunProps?>(StringComparer.OrdinalIgnoreCase);
        var basedOnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var style in stylesPart.Styles.Elements<Style>())
        {
            if (style.StyleId?.Value is null) continue;
            if (style.Type?.Value != StyleValues.Paragraph) continue;

            var styleId = style.StyleId.Value;
            if (style.BasedOn?.Val?.Value is string parent)
                basedOnMap[styleId] = parent;

            var rPr = style.StyleRunProperties;
            if (rPr is null)
            {
                rawProps[styleId] = null;
                continue;
            }

            // Bold/Italic: null = element absent (not set), true/false = explicitly set
            bool? bold   = rPr.Bold   is not null ? (bool?)(rPr.Bold.Val   is null || rPr.Bold.Val)   : null;
            bool? italic = rPr.Italic is not null ? (bool?)(rPr.Italic.Val is null || rPr.Italic.Val) : null;

            string? fontFamily = rPr.RunFonts?.Ascii?.Value;

            int? fontSize = null;
            if (rPr.FontSize?.Val?.Value is string szStr && int.TryParse(szStr, out var sz))
                fontSize = sz;

            string? color = null;
            if (rPr.Color?.Val?.Value is string col && col != "auto")
                color = col;

            rawProps[styleId] = new StyleRunProps(bold, italic, fontFamily, fontSize, color);
        }

        // Step 2: resolve basedOn inheritance — child properties take precedence over parent
        var resolved = new Dictionary<string, StyleRunProps>(StringComparer.OrdinalIgnoreCase);

        foreach (var startId in rawProps.Keys)
        {
            bool? bold = null, italic = null;
            string? fontFamily = null, color = null;
            int? fontSize = null;

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = startId;
            var depth   = 0;

            while (current is not null && depth < 10 && visited.Add(current))
            {
                if (rawProps.TryGetValue(current, out var props) && props.HasValue)
                {
                    bold       ??= props.Value.Bold;
                    italic     ??= props.Value.Italic;
                    fontFamily ??= props.Value.FontFamily;
                    fontSize   ??= props.Value.FontSize;
                    color      ??= props.Value.Color;
                }
                basedOnMap.TryGetValue(current, out current!);
                depth++;
            }

            if (bold.HasValue || italic.HasValue || fontFamily is not null || fontSize.HasValue || color is not null)
                resolved[startId] = new StyleRunProps(bold, italic, fontFamily, fontSize, color);
        }

        return resolved;
    }

    private static Dictionary<(int, int), NumLevelInfo> BuildNumFormatLookup(MainDocumentPart mainPart)
    {
        var lookup = new Dictionary<(int, int), NumLevelInfo>();
        var numPart = mainPart.NumberingDefinitionsPart;
        if (numPart?.Numbering is null) return lookup;

        var abstractNums = numPart.Numbering
            .Elements<AbstractNum>()
            .ToDictionary(a => (int)a.AbstractNumberId!.Value);

        foreach (var numInst in numPart.Numbering.Elements<NumberingInstance>())
        {
            var numId = (int)numInst.NumberID!.Value;
            var abstractNumId = (int)numInst.GetFirstChild<AbstractNumId>()!.Val!.Value;
            if (!abstractNums.TryGetValue(abstractNumId, out var abstractNum)) continue;

            foreach (var level in abstractNum.Elements<Level>())
            {
                var ilvl = (int)level.LevelIndex!.Value;

                // Determine bullet vs. numbered:
                // - numFmt=bullet → always bullet
                // - lvlText with no %N placeholder → static glyph (e.g. "•", "-") → visual bullet
                // - anything else → numbered
                var fmtEnum = level.NumberingFormat?.Val?.Value;
                var lvlText = level.LevelText?.Val?.Value ?? "";
                string fmt;
                if (fmtEnum == NumberFormatValues.Bullet)
                    fmt = "bullet";
                else if (!string.IsNullOrEmpty(lvlText) && !lvlText.Contains('%'))
                    fmt = "bullet"; // static marker like "•", "-", "○"
                else
                    fmt = fmtEnum?.ToString() ?? "bullet";

                // Read indent from the level's w:pPr/w:ind
                int? indLeft = null, indHanging = null;
                var levelInd = level.GetFirstChild<ParagraphProperties>()?.GetFirstChild<Indentation>();
                if (levelInd is not null)
                {
                    if (levelInd.Left?.Value is not null && int.TryParse(levelInd.Left.Value, out var l))
                        indLeft = l;
                    if (levelInd.Hanging?.Value is not null && int.TryParse(levelInd.Hanging.Value, out var h))
                        indHanging = h;
                }

                lookup[(numId, ilvl)] = new NumLevelInfo(fmt, indLeft, indHanging);
            }
        }
        return lookup;
    }

    private static StyleSpacing ReadDocDefaults(MainDocumentPart mainPart)
    {
        var spacing = mainPart.StyleDefinitionsPart?.Styles?
            .Elements<DocDefaults>().FirstOrDefault()?
            .ParagraphPropertiesDefault?
            .Descendants<SpacingBetweenLines>().FirstOrDefault();

        if (spacing is null)
            // Word's standard fallback: 8pt after, line=1.08x auto
            return new StyleSpacing(null, 160, 259, "auto", null);

        int? before = null, after = null, line = null; string? rule = null;
        if (spacing.Before?.Value is not null && int.TryParse(spacing.Before.Value, out var b)) before = b;
        if (spacing.After?.Value is not null && int.TryParse(spacing.After.Value, out var a)) after = a;
        if (spacing.Line?.Value is not null && int.TryParse(spacing.Line.Value, out var l)) line = l;
        if (spacing.LineRule?.HasValue == true) rule = spacing.LineRule.Value.ToString().ToLowerInvariant();
        return new StyleSpacing(before, after, line, rule, null);
    }

    private static ModelParagraph ConvertParagraph(
        OxParagraph oxPara,
        MainDocumentPart mainPart,
        StyleSpacing docDefaults = default,
        Dictionary<string, StyleSpacing>? styleSpacing = null,
        Dictionary<(int, int), NumLevelInfo>? numFormatLookup = null,
        Dictionary<string, StyleRunProps>? styleRunProps = null)
    {
        var para = new ModelParagraph();
        para.Children.Clear();

        para.Properties = ConvertParagraphProperties(oxPara.ParagraphProperties, mainPart, docDefaults, styleSpacing, numFormatLookup, styleRunProps);

        var fieldState = 0; // 0=none, 1=instruction, 2=result
        var fieldInstr = new System.Text.StringBuilder();

        foreach (var child in oxPara.ChildElements)
        {
            if (child is OxRun oxRun)
            {
                // Check for structural field-char run
                // Note: FieldCharValues is a struct in SDK 3.x — use == not switch case
                var fldChar = oxRun.GetFirstChild<FieldChar>();
                if (fldChar != null)
                {
                    var charType = fldChar.FieldCharType?.Value;
                    if (charType == FieldCharValues.Begin)
                    {
                        fieldState = 1;
                        fieldInstr.Clear();
                    }
                    else if (charType == FieldCharValues.Separate)
                    {
                        fieldState = 2;
                    }
                    else if (charType == FieldCharValues.End)
                    {
                        fieldState = 0;
                        fieldInstr.Clear();
                    }
                    continue; // structural run — no visible content
                }

                // Accumulate instruction text (between begin and separate)
                // w:instrText maps to FieldCode in DocumentFormat.OpenXml SDK 3.x
                var instrText = oxRun.GetFirstChild<FieldCode>();
                if (instrText != null)
                {
                    if (fieldState == 1) fieldInstr.Append(instrText.Text ?? "");
                    continue; // instruction run — no visible content
                }

                // In result phase — suppress for invisible fields (SET, BOOKMARK, etc.)
                if (fieldState == 2 && IsInvisibleField(fieldInstr.ToString()))
                    continue;

                // Tag dynamic field result runs (PAGE, NUMPAGES) so the renderer
                // can substitute the correct page number per page.
                var modelRun = ConvertRun(oxRun, mainPart);
                if (fieldState == 2)
                {
                    var instrName = GetDynamicFieldName(fieldInstr.ToString());
                    if (instrName != null)
                        modelRun.FieldType = instrName;
                }
                para.Children.Add(modelRun);
            }
            else if (child is OxHyperlink oxLink)
                para.Children.Add(ConvertHyperlink(oxLink, mainPart));
        }

        if (para.Children.Count == 0)
            para.Children.Add(DocFactory.CreateRun(""));

        return para;
    }

    private static ModelParagraphProperties ConvertParagraphProperties(
        ParagraphProperties? pPr,
        MainDocumentPart mainPart,
        StyleSpacing docDefaults = default,
        Dictionary<string, StyleSpacing>? styleSpacing = null,
        Dictionary<(int, int), NumLevelInfo>? numFormatLookup = null,
        Dictionary<string, StyleRunProps>? styleRunProps = null)
    {
        var props = new ModelParagraphProperties();
        if (pPr is null)
        {
            // No inline props at all — fall back to doc defaults
            props.SpaceBefore     = docDefaults.SpaceBefore;
            props.SpaceAfter      = docDefaults.SpaceAfter;
            props.LineSpacing     = docDefaults.LineSpacing;
            props.LineSpacingRule = docDefaults.LineSpacingRule;
            return props;
        }

        var styleId = pPr.ParagraphStyleId?.Val?.Value;
        if (styleId is not null) props.Style = styleId;

        var jc = pPr.Justification;
        if (jc?.Val?.HasValue == true)
            props.Alignment = MapAlignment(jc.Val.Value);

        var indent = pPr.Indentation;
        if (indent is not null)
        {
            if (indent.Left?.Value is not null && int.TryParse(indent.Left.Value, out var left))
                props.IndentLeft = left;
            if (indent.FirstLine?.Value is not null && int.TryParse(indent.FirstLine.Value, out var firstLine))
                props.IndentFirstLine = firstLine;
            if (indent.Hanging?.Value is not null && int.TryParse(indent.Hanging.Value, out var hanging))
                props.IndentHanging = hanging;
        }

        // ── 3-layer spacing resolution ──────────────────────────────────
        // Layer 1: doc defaults
        int? resolvedBefore   = docDefaults.SpaceBefore;
        int? resolvedAfter    = docDefaults.SpaceAfter;
        int? resolvedLine     = docDefaults.LineSpacing;
        string? resolvedRule  = docDefaults.LineSpacingRule;
        bool? resolvedContextualSpacing = null;

        // Layer 2: named style overrides
        if (styleId is not null && styleSpacing?.TryGetValue(styleId, out var sp) == true)
        {
            if (sp.SpaceBefore        is not null) resolvedBefore           = sp.SpaceBefore;
            if (sp.SpaceAfter         is not null) resolvedAfter            = sp.SpaceAfter;
            if (sp.LineSpacing        is not null) resolvedLine             = sp.LineSpacing;
            if (sp.LineSpacingRule    is not null) resolvedRule             = sp.LineSpacingRule;
            if (sp.ContextualSpacing  is not null) resolvedContextualSpacing = sp.ContextualSpacing;
        }

        // Layer 3: inline paragraph spacing (highest priority)
        var spacing = pPr.SpacingBetweenLines;
        if (spacing is not null)
        {
            if (spacing.Before?.Value is not null && int.TryParse(spacing.Before.Value, out var before))
                resolvedBefore = before;
            if (spacing.After?.Value is not null && int.TryParse(spacing.After.Value, out var after))
                resolvedAfter = after;
            if (spacing.Line?.Value is not null && int.TryParse(spacing.Line.Value, out var line))
                resolvedLine = line;
            if (spacing.LineRule?.HasValue == true)
                resolvedRule = spacing.LineRule.Value.ToString().ToLowerInvariant();
        }

        // Layer 3: inline contextual spacing override
        if (pPr.ContextualSpacing is not null)
            resolvedContextualSpacing = pPr.ContextualSpacing.Val?.Value != false;

        props.SpaceBefore        = resolvedBefore;
        props.SpaceAfter         = resolvedAfter;
        props.LineSpacing        = resolvedLine;
        props.LineSpacingRule    = resolvedRule;
        props.ContextualSpacing  = resolvedContextualSpacing;

        var numPr = pPr.NumberingProperties;
        if (numPr is not null)
        {
            if (numPr.NumberingId?.Val is not null)
                props.NumberingId = numPr.NumberingId.Val;
            if (numPr.NumberingLevelReference?.Val is not null)
                props.NumberingLevel = numPr.NumberingLevelReference.Val;

            if (numFormatLookup is not null && props.NumberingId is int nid)
            {
                var ilvl = props.NumberingLevel ?? 0;
                if (numFormatLookup.TryGetValue((nid, ilvl), out var levelInfo))
                {
                    props.NumberingFormat = levelInfo.Format;
                    // Apply numbering level's indent as fallback when paragraph's own pPr omits them
                    if (props.IndentLeft is null && levelInfo.IndentLeft.HasValue)
                        props.IndentLeft = levelInfo.IndentLeft.Value;
                    if (props.IndentHanging is null && levelInfo.IndentHanging.HasValue)
                        props.IndentHanging = levelInfo.IndentHanging.Value;
                }
            }
        }

        if (pPr.KeepNext is not null)
            props.KeepNext = true;

        if (pPr.Elements<PageBreakBefore>().Any())
            props.PageBreakBefore = true;

        // Mid-document section break: w:sectPr inside w:pPr
        var sectPr = pPr.Elements<OxSectionProperties>().FirstOrDefault();
        if (sectPr is not null)
            props.SectionBreak = ExtractSectionProperties(sectPr, mainPart);

        // Layer: named style run properties (font styling from style definition)
        if (styleId is not null && styleRunProps?.TryGetValue(styleId, out var srp) == true)
        {
            if (srp.Bold       is not null) props.StyleBold       = srp.Bold;
            if (srp.Italic     is not null) props.StyleItalic     = srp.Italic;
            if (srp.FontSize   is not null) props.StyleFontSize   = srp.FontSize;
            if (srp.FontFamily is not null) props.StyleFontFamily = srp.FontFamily;
            if (srp.Color      is not null) props.StyleColor      = srp.Color;
        }

        return props;
    }

    private static ModelRun ConvertRun(OxRun oxRun, MainDocumentPart mainPart)
    {
        var run = new ModelRun();
        run.Content.Clear();

        run.Properties = ConvertRunProperties(oxRun.RunProperties);

        foreach (var child in oxRun.ChildElements)
        {
            if (child is Text text)
            {
                run.Content.Add(new TextPiece { Text = text.Text ?? "" });
            }
            else if (child is TabChar)
            {
                run.Content.Add(new TabContent());
            }
            else if (child is Break br)
            {
                var breakType = BreakType.TextWrapping;
                if (br.Type?.HasValue == true)
                {
                    if (br.Type.Value == BreakValues.Page) breakType = BreakType.Page;
                    else if (br.Type.Value == BreakValues.Column) breakType = BreakType.Column;
                }
                run.Content.Add(new BreakContent { BreakType = breakType });
            }
            else if (child is Drawing drawing)
            {
                var img = ExtractImage(drawing, mainPart);
                if (img is not null) run.Content.Add(img);
            }
        }

        if (run.Content.Count == 0)
            run.Content.Add(new TextPiece { Text = "" });

        return run;
    }

    private static ModelRunProperties ConvertRunProperties(OxRunProperties? rPr)
    {
        var props = new ModelRunProperties();
        if (rPr is null) return props;

        props.Bold = rPr.Bold is not null && (rPr.Bold.Val is null || rPr.Bold.Val);
        props.Italic = rPr.Italic is not null && (rPr.Italic.Val is null || rPr.Italic.Val);
        props.Strikethrough = rPr.Strike is not null && (rPr.Strike.Val is null || rPr.Strike.Val);

        var underline = rPr.Underline;
        if (underline?.Val?.HasValue == true)
            props.Underline = MapUnderline(underline.Val.Value);

        var fonts = rPr.RunFonts;
        if (fonts?.Ascii?.Value is not null)
            props.FontFamily = fonts.Ascii.Value;

        var fontSize = rPr.FontSize;
        if (fontSize?.Val?.Value is not null && int.TryParse(fontSize.Val.Value, out var sz))
            props.FontSize = sz;

        var color = rPr.Color;
        if (color?.Val?.Value is not null)
            props.Color = color.Val.Value;

        var highlight = rPr.Highlight;
        if (highlight?.Val?.HasValue == true)
            props.Highlight = MapHighlight(highlight.Val.Value);

        var vertAlign = rPr.VerticalTextAlignment;
        if (vertAlign?.Val?.HasValue == true)
            props.VerticalAlign = MapVerticalAlign(vertAlign.Val.Value);

        var charSpacing = rPr.Spacing;
        if (charSpacing?.Val?.HasValue == true)
            props.CharacterSpacing = (int)charSpacing.Val.Value;

        return props;
    }

    private static ImageContent? ExtractImage(Drawing drawing, MainDocumentPart mainPart)
    {
        var inline = drawing.Descendants<DW.Inline>().FirstOrDefault();
        var anchor = drawing.Descendants<DW.Anchor>().FirstOrDefault();
        OpenXmlCompositeElement? container = (OpenXmlCompositeElement?)inline ?? anchor;
        if (container is null) return null;

        // Dimensions from wp:extent
        long widthEmu  = container is DW.Inline i1 ? (i1.Extent?.Cx?.Value ?? 0)
                       : (anchor?.Extent?.Cx?.Value ?? 0);
        long heightEmu = container is DW.Inline i2 ? (i2.Extent?.Cy?.Value ?? 0)
                       : (anchor?.Extent?.Cy?.Value ?? 0);

        // Alt text / name from wp:docPr
        var docPr = container.Descendants<DW.DocProperties>().FirstOrDefault();

        // Blip → relationship ID
        var blip = container.Descendants<A.Blip>().FirstOrDefault();
        if (blip?.Embed?.Value is null) return null;

        string base64, mimeType;
        try
        {
            var part = mainPart.GetPartById(blip.Embed.Value);
            if (part is not ImagePart imagePart) return null;
            using var stream = imagePart.GetStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            base64   = Convert.ToBase64String(ms.ToArray());
            mimeType = imagePart.ContentType ?? "image/png";
        }
        catch { return null; }

        var img = new ImageContent
        {
            ImageData       = base64,
            ContentMimeType = mimeType,
            WidthEmu        = widthEmu,
            HeightEmu       = heightEmu,
            AltText         = docPr?.Description?.Value,
            Name            = docPr?.Name?.Value
        };

        // Rotation from a:xfrm/@rot (applies to both inline and anchor)
        var xfrm = container.Descendants<A.Transform2D>().FirstOrDefault();
        if (xfrm?.Rotation?.Value is int rot && rot != 0)
            img.RotationDegrees = rot / 60000.0;

        // Inline: WrapMode defaults to Inline, done
        if (anchor is null) return img;

        // Anchor: extract margin distances
        img.DistTopEmu    = (long?)anchor.DistanceFromTop?.Value;
        img.DistBottomEmu = (long?)anchor.DistanceFromBottom?.Value;
        img.DistLeftEmu   = (long?)anchor.DistanceFromLeft?.Value;
        img.DistRightEmu  = (long?)anchor.DistanceFromRight?.Value;

        // Anchor: extract absolute position offsets (wp:positionH/V > wp:posOffset)
        var posH = anchor.Descendants<DW.HorizontalPosition>().FirstOrDefault();
        var posV = anchor.Descendants<DW.VerticalPosition>().FirstOrDefault();
        if (posH?.GetFirstChild<DW.PositionOffset>()?.Text is string hs && long.TryParse(hs, out var hOff))
            img.HorizontalOffsetEmu = hOff;
        if (posV?.GetFirstChild<DW.PositionOffset>()?.Text is string vs && long.TryParse(vs, out var vOff))
            img.VerticalOffsetEmu = vOff;

        img.WrapMode = ResolveWrapMode(anchor, mainPart);
        return img;
    }

    private static ImageWrapMode ResolveWrapMode(DW.Anchor anchor, MainDocumentPart mainPart)
    {
        // wp:wrapNone — layered image (behind or in front of text)
        if (anchor.Descendants<DW.WrapNone>().Any())
            return anchor.BehindDoc?.Value == true
                ? ImageWrapMode.BehindText
                : ImageWrapMode.InFrontOfText;

        // wp:wrapTopAndBtm — text only above and below
        if (anchor.Descendants<DW.WrapTopBottom>().Any())
            return ImageWrapMode.TopAndBottom;

        // wp:wrapSquare / wrapTight / wrapThrough
        // wrapText="right" → text wraps on right → image sits on LEFT  → FloatLeft
        // wrapText="left"  → text wraps on left  → image sits on RIGHT → FloatRight
        // bothSides / largest / null → fall back to wp:positionH > wp:align
        DW.WrapTextValues? wrapText =
            anchor.Descendants<DW.WrapSquare>().FirstOrDefault()?.WrapText?.Value
            ?? anchor.Descendants<DW.WrapTight>().FirstOrDefault()?.WrapText?.Value
            ?? anchor.Descendants<DW.WrapThrough>().FirstOrDefault()?.WrapText?.Value;

        if (wrapText == DW.WrapTextValues.Left)  return ImageWrapMode.FloatRight;
        if (wrapText == DW.WrapTextValues.Right) return ImageWrapMode.FloatLeft;

        // Ambiguous (bothSides / largest / null): honour horizontal position alignment
        var posH   = anchor.Descendants<DW.HorizontalPosition>().FirstOrDefault();

        // Use LocalName matching — avoids SDK class name ambiguity with wp:align element
        var hAlign = posH?.ChildElements
                          .FirstOrDefault(e => e.LocalName == "align")
                          ?.InnerText;

        if (hAlign is "right" or "outside") return ImageWrapMode.FloatRight;
        if (hAlign is "left"  or "inside")  return ImageWrapMode.FloatLeft;

        // No wp:align — Word used absolute posOffset (image dragged manually).
        // Infer side by comparing posOffset to the column half-width.
        var posOffsetText = posH?.ChildElements
                                 .FirstOrDefault(e => e.LocalName == "posOffset")
                                 ?.InnerText;

        if (posOffsetText is not null && long.TryParse(posOffsetText, out var posOffset))
        {
            long columnEmu     = ComputeColumnWidthEmu(mainPart);
            long marginLeftEmu = ComputeMarginLeftEmu(mainPart);

            // When relativeFrom="page", posOffset is from the page left edge,
            // so add marginLeft to get the same baseline as column-relative offsets.
            bool isPageRelative = posH?.RelativeFrom?.Value
                                  == DW.HorizontalRelativePositionValues.Page;
            long threshold = isPageRelative
                ? marginLeftEmu + columnEmu / 2
                : columnEmu / 2;

            return posOffset > threshold
                ? ImageWrapMode.FloatRight
                : ImageWrapMode.FloatLeft;
        }

        return ImageWrapMode.FloatLeft;
    }

    // Computes usable text column width in EMU from sectPr page size and margins.
    // Falls back to US Letter with 1" margins (6.5" = 5,943,600 EMU) if sectPr absent.
    private static long ComputeColumnWidthEmu(MainDocumentPart mainPart)
    {
        const long TwipsToEmu = 635L; // 914400 EMU/inch ÷ 1440 twips/inch
        var sectPr = mainPart.Document?.Body?.Elements<OxSectionProperties>().FirstOrDefault();
        long pageW = sectPr?.GetFirstChild<PageSize>()?.Width?.Value  is uint pw ? pw : 12240L;
        long margL = sectPr?.GetFirstChild<PageMargin>()?.Left?.Value  is uint ml ? ml : 1440L;
        long margR = sectPr?.GetFirstChild<PageMargin>()?.Right?.Value is uint mr ? mr : 1440L;
        return (pageW - margL - margR) * TwipsToEmu;
    }

    private static long ComputeMarginLeftEmu(MainDocumentPart mainPart)
    {
        const long TwipsToEmu = 635L;
        var sectPr = mainPart.Document?.Body?.Elements<OxSectionProperties>().FirstOrDefault();
        long margL = sectPr?.GetFirstChild<PageMargin>()?.Left?.Value is uint ml ? ml : 1440L;
        return margL * TwipsToEmu;
    }

    private static ModelHyperlink ConvertHyperlink(OxHyperlink oxLink, MainDocumentPart mainPart)
    {
        var link = new ModelHyperlink();

        if (oxLink.Id?.Value is not null)
        {
            var rel = mainPart.HyperlinkRelationships
                .FirstOrDefault(r => r.Id == oxLink.Id.Value);
            if (rel is not null)
                link.Url = rel.Uri.ToString();
        }

        if (oxLink.Tooltip?.Value is not null)
            link.Tooltip = oxLink.Tooltip.Value;

        foreach (var child in oxLink.ChildElements)
        {
            if (child is OxRun oxRun)
                link.Children.Add(ConvertRun(oxRun, mainPart));
        }

        return link;
    }

    private static ModelTable ConvertTable(
        OxTable oxTable,
        MainDocumentPart mainPart,
        StyleSpacing docDefaults = default,
        Dictionary<string, StyleSpacing>? styleSpacing = null,
        Dictionary<(int, int), NumLevelInfo>? numFormatLookup = null,
        Dictionary<string, StyleRunProps>? styleRunProps = null)
    {
        var table = new ModelTable();

        var tblPr = oxTable.Elements<TableProperties>().FirstOrDefault();
        if (tblPr is not null)
        {
            var style = tblPr.TableStyle;
            if (style?.Val?.Value is not null)
                table.Properties.Style = style.Val.Value;

            table.Properties.HasBorders = tblPr.Elements<TableBorders>().Any();

            var tblW = tblPr.TableWidth;
            if (tblW?.Width?.Value is not null
                && int.TryParse(tblW.Width.Value, out var tw)
                && tw > 0
                && tblW.Type?.Value == TableWidthUnitValues.Dxa)
            {
                table.Properties.Width = tw;
            }

            table.Properties.DefaultCellPadding = ParseCellPadding(
                tblPr.Elements<TableCellMarginDefault>().FirstOrDefault());

            var cellSpacing = tblPr.Elements<TableCellSpacing>().FirstOrDefault();
            if (cellSpacing?.Width?.Value is not null
                && int.TryParse(cellSpacing.Width.Value, out var cs)
                && cs > 0
                && cellSpacing.Type?.Value == TableWidthUnitValues.Dxa)
            {
                table.Properties.CellSpacing = cs;
            }
        }

        var grid = oxTable.Elements<TableGrid>().FirstOrDefault();
        if (grid is not null)
        {
            foreach (var col in grid.Elements<GridColumn>())
            {
                if (col.Width?.Value is not null && int.TryParse(col.Width.Value, out var w))
                    table.GridColumnWidths.Add(w);
            }
        }

        if (table.Properties.Width is null && table.GridColumnWidths.Count > 0)
            table.Properties.Width = table.GridColumnWidths.Sum();

        var themeColors = ReadThemeColors(mainPart);

        var styleDef = table.Properties.Style is not null
            ? ParseTableStyleDef(mainPart, table.Properties.Style, themeColors)
            : null;

        var allRows = oxTable.Elements<OxTableRow>().ToList();
        for (int ri = 0; ri < allRows.Count; ri++)
        {
            bool isFirst = ri == 0;
            bool isLast  = ri == allRows.Count - 1;
            table.Rows.Add(ConvertTableRow(allRows[ri], mainPart, styleDef, isFirst, isLast, ri, themeColors, docDefaults, styleSpacing, numFormatLookup, styleRunProps, table.Properties.DefaultCellPadding ?? styleDef?.DefaultCellPadding));
        }

        return table;
    }

    private static Model.TableRow ConvertTableRow(
        OxTableRow oxRow,
        MainDocumentPart mainPart,
        TableStyleDef? styleDef = null,
        bool isFirstRow = false,
        bool isLastRow = false,
        int rowIndex = 0,
        IReadOnlyDictionary<string, string>? themeColors = null,
        StyleSpacing docDefaults = default,
        Dictionary<string, StyleSpacing>? styleSpacing = null,
        Dictionary<(int, int), NumLevelInfo>? numFormatLookup = null,
        Dictionary<string, StyleRunProps>? styleRunProps = null,
        CellPadding? tableCellPadding = null)
    {
        var row = new Model.TableRow();

        var trPr = oxRow.Elements<TableRowProperties>().FirstOrDefault();
        if (trPr is not null)
        {
            var height = trPr.Elements<TableRowHeight>().FirstOrDefault();
            if (height?.Val is not null)
                row.Properties.Height = (int)(uint)height.Val;
            row.Properties.IsHeader = trPr.Elements<TableHeader>().Any();
        }

        TableStyleCondFmt? rowFmt = null;
        if (styleDef is not null)
        {
            TableStyleCondFmt? specific = isFirstRow ? styleDef.FirstRow
                                        : isLastRow  ? styleDef.LastRow
                                        : (rowIndex % 2 == 0) ? styleDef.Band1Row
                                                              : styleDef.Band2Row;
            rowFmt = MergeCondFmt(styleDef.WholeTable, specific);
        }

        foreach (var oxCell in oxRow.Elements<OxTableCell>())
        {
            row.Cells.Add(ConvertTableCell(oxCell, mainPart, rowFmt, themeColors, docDefaults, styleSpacing, numFormatLookup, styleRunProps, tableCellPadding));
        }

        return row;
    }

    private static Model.TableCell ConvertTableCell(
        OxTableCell oxCell,
        MainDocumentPart mainPart,
        TableStyleCondFmt? condFmt = null,
        IReadOnlyDictionary<string, string>? themeColors = null,
        StyleSpacing docDefaults = default,
        Dictionary<string, StyleSpacing>? styleSpacing = null,
        Dictionary<(int, int), NumLevelInfo>? numFormatLookup = null,
        Dictionary<string, StyleRunProps>? styleRunProps = null,
        CellPadding? tableCellPadding = null)
    {
        var cell = new Model.TableCell();

        var tcPr = oxCell.Elements<TableCellProperties>().FirstOrDefault();
        if (tcPr is not null)
        {
            var width = tcPr.Elements<TableCellWidth>().FirstOrDefault();
            if (width?.Width?.Value is not null && int.TryParse(width.Width.Value, out var w))
                cell.Properties.Width = w;

            var gridSpan = tcPr.Elements<GridSpan>().FirstOrDefault();
            if (gridSpan?.Val is not null)
                cell.Properties.GridSpan = gridSpan.Val;

            var vMerge = tcPr.Elements<VerticalMerge>().FirstOrDefault();
            if (vMerge is not null)
            {
                if (vMerge.Val?.HasValue == true && vMerge.Val.Value == MergedCellValues.Restart)
                    cell.Properties.VerticalMerge = VerticalMergeType.Restart;
                else
                    cell.Properties.VerticalMerge = VerticalMergeType.Continue;
            }

            var vAlign = tcPr.Elements<TableCellVerticalAlignment>().FirstOrDefault();
            if (vAlign?.Val?.HasValue == true)
                cell.Properties.VerticalAlignment = MapTableVerticalAlignment(vAlign.Val.Value);

            var shading = tcPr.Elements<Shading>().FirstOrDefault();
            cell.Properties.Shading = ResolveShading(shading, themeColors);

            var tcBordersEl = tcPr.Elements<TableCellBorders>().FirstOrDefault();
            if (tcBordersEl != null)
            {
                var borders = new CellBorders();
                var top    = tcBordersEl.Elements<TopBorder>().FirstOrDefault();
                var bottom = tcBordersEl.Elements<BottomBorder>().FirstOrDefault();
                var left   = tcBordersEl.Elements<LeftBorder>().FirstOrDefault();
                var right  = tcBordersEl.Elements<RightBorder>().FirstOrDefault();
                if (top    != null) borders.Top    = ImportBorder(top.Val,    top.Size,    top.Color);
                if (bottom != null) borders.Bottom = ImportBorder(bottom.Val, bottom.Size, bottom.Color);
                if (left   != null) borders.Left   = ImportBorder(left.Val,   left.Size,   left.Color);
                if (right  != null) borders.Right  = ImportBorder(right.Val,  right.Size,  right.Color);
                if (borders.Top != null || borders.Bottom != null || borders.Left != null || borders.Right != null)
                    cell.Properties.Borders = borders;
            }

            cell.Properties.Padding = ParseCellPadding(tcPr.Elements<TableCellMargin>().FirstOrDefault())
                                      ?? tableCellPadding;
        }
        else
        {
            cell.Properties.Padding = tableCellPadding;
        }

        // Apply style-based defaults where direct properties were not set
        if (condFmt is not null)
        {
            if (cell.Properties.Shading is null && condFmt.CellShading is not null)
                cell.Properties.Shading = condFmt.CellShading;
            if (cell.Properties.Borders is null && condFmt.CellBorders is not null)
                cell.Properties.Borders = condFmt.CellBorders;
        }

        foreach (var child in oxCell.ChildElements)
        {
            if (child is OxParagraph oxPara)
                cell.Children.Add(ConvertParagraph(oxPara, mainPart, docDefaults, styleSpacing, numFormatLookup, styleRunProps));
            else if (child is OxTable oxTable)
                cell.Children.Add(ConvertTable(oxTable, mainPart, docDefaults, styleSpacing, numFormatLookup, styleRunProps));
        }

        if (cell.Children.Count == 0)
            cell.Children.Add(DocFactory.CreateParagraph());

        if (condFmt is not null && HasRunProps(condFmt))
            ApplyRunCondFmt(cell, condFmt);

        return cell;
    }

    /// <summary>
    /// Extract headers and footers from a w:sectPr element, producing both
    /// pre-rendered RenderNode lists (for display) and raw XML bytes (for round-trip export).
    /// </summary>
    private static void ExtractHeadersAndFooters(
        OxSectionProperties sectPr,
        MainDocumentPart mainPart,
        Dictionary<string, List<RenderNode>> headers,
        Dictionary<string, List<RenderNode>> footers,
        out Dictionary<string, byte[]>? rawParts)
    {
        rawParts = null;

        foreach (var headerRef in sectPr.Elements<HeaderReference>())
        {
            if (headerRef.Id?.Value is null) continue;
            var typeKey = MapHeaderFooterType(headerRef.Type?.Value);

            try
            {
                var headerPart = mainPart.GetPartById(headerRef.Id.Value) as HeaderPart;
                if (headerPart?.Header is null) continue;

                // Pre-render paragraphs and tables to RenderNodes for display
                var nodes = new List<RenderNode>();
                foreach (var child in headerPart.Header.ChildElements)
                {
                    if (child is OxParagraph oxPara)
                    {
                        var modelPara = ConvertParagraph(oxPara, mainPart);
                        nodes.Add(RenderTreeBuilder.BuildParagraph(modelPara));
                    }
                    else if (child is OxTable oxTable)
                    {
                        var modelTable = ConvertTable(oxTable, mainPart);
                        nodes.Add(RenderTreeBuilder.BuildTable(modelTable));
                    }
                }
                if (nodes.Count > 0)
                    headers[typeKey] = nodes;

                // Store raw XML bytes for lossless round-trip
                rawParts ??= new Dictionary<string, byte[]>();
                using var ms = new MemoryStream();
                headerPart.Header.Save(ms);
                rawParts[$"header-{typeKey}"] = ms.ToArray();

                // Preserve hyperlink relationships so r:id refs in the XML remain valid on re-export
                var hyperlinks = headerPart.HyperlinkRelationships
                    .ToDictionary(r => r.Id, r => r.Uri.ToString());
                if (hyperlinks.Count > 0)
                    rawParts[$"header-{typeKey}-hyperlinks"] =
                        System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(hyperlinks);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: failed to extract {typeKey} header: {ex.Message}");
            }
        }

        foreach (var footerRef in sectPr.Elements<FooterReference>())
        {
            if (footerRef.Id?.Value is null) continue;
            var typeKey = MapHeaderFooterType(footerRef.Type?.Value);

            try
            {
                var footerPart = mainPart.GetPartById(footerRef.Id.Value) as FooterPart;
                if (footerPart?.Footer is null) continue;

                var nodes = new List<RenderNode>();
                foreach (var child in footerPart.Footer.ChildElements)
                {
                    if (child is OxParagraph oxPara)
                    {
                        var modelPara = ConvertParagraph(oxPara, mainPart);
                        nodes.Add(RenderTreeBuilder.BuildParagraph(modelPara));
                    }
                    else if (child is OxTable oxTable)
                    {
                        var modelTable = ConvertTable(oxTable, mainPart);
                        nodes.Add(RenderTreeBuilder.BuildTable(modelTable));
                    }
                }
                if (nodes.Count > 0)
                    footers[typeKey] = nodes;

                rawParts ??= new Dictionary<string, byte[]>();
                using var ms = new MemoryStream();
                footerPart.Footer.Save(ms);
                rawParts[$"footer-{typeKey}"] = ms.ToArray();

                // Preserve hyperlink relationships so r:id refs in the XML remain valid on re-export
                var hyperlinks = footerPart.HyperlinkRelationships
                    .ToDictionary(r => r.Id, r => r.Uri.ToString());
                if (hyperlinks.Count > 0)
                    rawParts[$"footer-{typeKey}-hyperlinks"] =
                        System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(hyperlinks);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Warning: failed to extract {typeKey} footer: {ex.Message}");
            }
        }
    }

    private static string MapHeaderFooterType(HeaderFooterValues? type)
    {
        if (type == HeaderFooterValues.First) return "first";
        if (type == HeaderFooterValues.Even) return "even";
        return "default";
    }

    private static bool IsInvisibleField(string instruction)
    {
        var first = instruction.TrimStart()
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "";
        return string.Equals(first, "SET", StringComparison.OrdinalIgnoreCase)
            || string.Equals(first, "BOOKMARK", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetDynamicFieldName(string instruction)
    {
        var first = instruction.TrimStart()
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return first?.ToUpperInvariant() switch
        {
            "PAGE" => "PAGE",
            "NUMPAGES" => "NUMPAGES",
            _ => null
        };
    }

    private static Alignment? MapAlignment(JustificationValues val)
    {
        if (val == JustificationValues.Left) return Alignment.Left;
        if (val == JustificationValues.Center) return Alignment.Center;
        if (val == JustificationValues.Right) return Alignment.Right;
        if (val == JustificationValues.Both) return Alignment.Both;
        return null;
    }

    private static UnderlineType MapUnderline(UnderlineValues val)
    {
        if (val == UnderlineValues.Single) return UnderlineType.Single;
        if (val == UnderlineValues.Double) return UnderlineType.Double;
        if (val == UnderlineValues.Dotted) return UnderlineType.Dotted;
        if (val == UnderlineValues.Dash) return UnderlineType.Dash;
        if (val == UnderlineValues.DotDash) return UnderlineType.DotDash;
        if (val == UnderlineValues.DotDotDash) return UnderlineType.DotDotDash;
        if (val == UnderlineValues.Wave) return UnderlineType.Wave;
        if (val == UnderlineValues.Thick) return UnderlineType.Thick;
        if (val == UnderlineValues.Words) return UnderlineType.Words;
        return UnderlineType.None;
    }

    private static HighlightColor MapHighlight(HighlightColorValues val)
    {
        if (val == HighlightColorValues.Black) return HighlightColor.Black;
        if (val == HighlightColorValues.Blue) return HighlightColor.Blue;
        if (val == HighlightColorValues.Cyan) return HighlightColor.Cyan;
        if (val == HighlightColorValues.DarkBlue) return HighlightColor.DarkBlue;
        if (val == HighlightColorValues.DarkCyan) return HighlightColor.DarkCyan;
        if (val == HighlightColorValues.DarkGray) return HighlightColor.DarkGray;
        if (val == HighlightColorValues.DarkGreen) return HighlightColor.DarkGreen;
        if (val == HighlightColorValues.DarkMagenta) return HighlightColor.DarkMagenta;
        if (val == HighlightColorValues.DarkRed) return HighlightColor.DarkRed;
        if (val == HighlightColorValues.DarkYellow) return HighlightColor.DarkYellow;
        if (val == HighlightColorValues.Green) return HighlightColor.Green;
        if (val == HighlightColorValues.LightGray) return HighlightColor.LightGray;
        if (val == HighlightColorValues.Magenta) return HighlightColor.Magenta;
        if (val == HighlightColorValues.Red) return HighlightColor.Red;
        if (val == HighlightColorValues.White) return HighlightColor.White;
        if (val == HighlightColorValues.Yellow) return HighlightColor.Yellow;
        return HighlightColor.None;
    }

    private static VerticalAlignType MapVerticalAlign(VerticalPositionValues val)
    {
        if (val == VerticalPositionValues.Superscript) return VerticalAlignType.Superscript;
        if (val == VerticalPositionValues.Subscript) return VerticalAlignType.Subscript;
        return VerticalAlignType.Baseline;
    }

    private static TableVerticalAlignment MapTableVerticalAlignment(TableVerticalAlignmentValues val)
    {
        if (val == TableVerticalAlignmentValues.Top) return TableVerticalAlignment.Top;
        if (val == TableVerticalAlignmentValues.Center) return TableVerticalAlignment.Center;
        if (val == TableVerticalAlignmentValues.Bottom) return TableVerticalAlignment.Bottom;
        return TableVerticalAlignment.Top;
    }

    // For top/bottom cell margins, treat explicit 0 as "use default" — Word renders
    // Table Grid cells with top/bottom breathing room even when w:w="0" is present.
    private static int DefaultTb(int? v) => v is null or 0 ? 72 : v.Value;

    private static CellPadding? ParseCellPadding(TableCellMarginDefault? el)
    {
        if (el is null) return null;
        var t = ParseMarginTw(el.Elements<TopMargin>().FirstOrDefault());
        var b = ParseMarginTw(el.Elements<BottomMargin>().FirstOrDefault());
        var l = ParseMarginTw(el.Elements<StartMargin>().FirstOrDefault())
                ?? ParseMarginTw(el.Elements<TableCellLeftMargin>().FirstOrDefault());
        var r = ParseMarginTw(el.Elements<EndMargin>().FirstOrDefault())
                ?? ParseMarginTw(el.Elements<TableCellRightMargin>().FirstOrDefault());
        if (t is null && b is null && l is null && r is null) return null;
        return new CellPadding { Top = DefaultTb(t), Bottom = DefaultTb(b), Left = l ?? 108, Right = r ?? 108 };
    }

    private static CellPadding? ParseCellPadding(TableCellMargin? el)
    {
        if (el is null) return null;
        TableWidthType? left  = (TableWidthType?)el.Elements<StartMargin>().FirstOrDefault()
                                ?? el.Elements<LeftMargin>().FirstOrDefault();
        TableWidthType? right = (TableWidthType?)el.Elements<EndMargin>().FirstOrDefault()
                                ?? el.Elements<RightMargin>().FirstOrDefault();
        return ParseCellPaddingCore(
            el.Elements<TopMargin>().FirstOrDefault(),
            el.Elements<BottomMargin>().FirstOrDefault(),
            left, right);
    }

    private static CellPadding? ParseCellPaddingCore(
        TableWidthType? top, TableWidthType? bottom, TableWidthType? left, TableWidthType? right)
    {
        var t = ParseMarginTw(top);
        var b = ParseMarginTw(bottom);
        var l = ParseMarginTw(left);
        var r = ParseMarginTw(right);
        if (t is null && b is null && l is null && r is null) return null;
        return new CellPadding { Top = DefaultTb(t), Bottom = DefaultTb(b), Left = l ?? 108, Right = r ?? 108 };
    }

    private static int? ParseMarginTw(TableWidthType? el)
    {
        if (el?.Width?.Value is null) return null;
        if (el.Type?.Value != TableWidthUnitValues.Dxa) return null;
        return int.TryParse(el.Width.Value, out var v) ? v : null;
    }

    private static int? ParseMarginTw(TableCellLeftMargin? el)
    {
        if (el?.Width is null) return null;
        if (el.Type?.Value != TableWidthValues.Dxa) return null;
        return el.Width.Value;
    }

    private static int? ParseMarginTw(TableCellRightMargin? el)
    {
        if (el?.Width is null) return null;
        if (el.Type?.Value != TableWidthValues.Dxa) return null;
        return el.Width.Value;
    }

    private static CellBorder ImportBorder(
        EnumValue<BorderValues>? val,
        UInt32Value? size,
        StringValue? color) =>
        new()
        {
            Style = val?.HasValue == true ? MapBorderStyleFromOxml(val.Value) : CellBorderStyle.Single,
            Size  = size?.HasValue == true ? (int)(uint)size.Value : 4,
            Color = color?.Value ?? "auto"
        };

    private static CellBorderStyle MapBorderStyleFromOxml(BorderValues val)
    {
        if (val == BorderValues.Nil || val == BorderValues.None) return CellBorderStyle.None;
        if (val == BorderValues.Double) return CellBorderStyle.Double;
        if (val == BorderValues.Dotted) return CellBorderStyle.Dotted;
        if (val == BorderValues.Dashed) return CellBorderStyle.Dashed;
        if (val == BorderValues.Thick)  return CellBorderStyle.Thick;
        return CellBorderStyle.Single;
    }

    private static TableStyleDef? ParseTableStyleDef(
        MainDocumentPart mainPart,
        string styleName,
        IReadOnlyDictionary<string, string>? themeColors = null)
    {
        var stylesPart = mainPart.StyleDefinitionsPart;
        if (stylesPart?.Styles is null) return null;

        var style = stylesPart.Styles.Elements<Style>()
            .FirstOrDefault(s => s.Type?.Value == StyleValues.Table
                              && s.StyleId?.Value == styleName);
        if (style is null) return null;

        TableStyleCondFmt? wholeTable = null;
        TableStyleCondFmt? firstRow = null;
        TableStyleCondFmt? lastRow = null;
        TableStyleCondFmt? band1Row = null;
        TableStyleCondFmt? band2Row = null;
        TableStyleCondFmt? firstCol = null;
        TableStyleCondFmt? lastCol = null;

        // Parse whole-table defaults (tcPr/rPr directly on the <w:style> element)
        wholeTable = ParseCondFmt(
            style.Elements<TableCellProperties>().FirstOrDefault(),
            style.GetFirstChild<StyleRunProperties>(),
            themeColors
        );

        // Parse conditional format slots
        foreach (var pr in style.Elements<TableStyleProperties>())
        {
            var fmt = ParseCondFmt(
                pr.Elements<TableCellProperties>().FirstOrDefault(),
                pr.GetFirstChild<StyleRunProperties>(),
                themeColors
            );
            var type = pr.Type?.Value;
            if (type == TableStyleOverrideValues.FirstRow)         firstRow = fmt;
            else if (type == TableStyleOverrideValues.LastRow)     lastRow  = fmt;
            else if (type == TableStyleOverrideValues.Band1Horizontal) band1Row = fmt;
            else if (type == TableStyleOverrideValues.Band2Horizontal) band2Row = fmt;
            else if (type == TableStyleOverrideValues.FirstColumn) firstCol = fmt;
            else if (type == TableStyleOverrideValues.LastColumn)  lastCol  = fmt;
            else if (type == TableStyleOverrideValues.WholeTable)  wholeTable = MergeCondFmt(wholeTable, fmt);
        }

        var styleDefaultCellPadding = ParseCellPadding(
            style.GetFirstChild<TableProperties>()?.Elements<TableCellMarginDefault>().FirstOrDefault());

        return new TableStyleDef(wholeTable, firstRow, lastRow, band1Row, band2Row, firstCol, lastCol, styleDefaultCellPadding);
    }

    private static TableStyleCondFmt? ParseCondFmt(
        TableCellProperties? tcPr,
        StyleRunProperties? rPr,
        IReadOnlyDictionary<string, string>? themeColors = null)
    {
        string? shading = null;
        CellBorders? borders = null;
        bool? runBold = null;
        bool? runItalic = null;
        string? runColor = null;
        int? runFontSize = null;
        string? runFontFamily = null;

        if (tcPr is not null)
        {
            var shd = tcPr.Elements<Shading>().FirstOrDefault();
            shading = ResolveShading(shd, themeColors);

            var tcBorders = tcPr.Elements<TableCellBorders>().FirstOrDefault();
            if (tcBorders is not null)
            {
                var b = new CellBorders();
                var top    = tcBorders.Elements<TopBorder>().FirstOrDefault();
                var bottom = tcBorders.Elements<BottomBorder>().FirstOrDefault();
                var left   = tcBorders.Elements<LeftBorder>().FirstOrDefault();
                var right  = tcBorders.Elements<RightBorder>().FirstOrDefault();
                if (top    != null) b.Top    = ImportBorder(top.Val,    top.Size,    top.Color);
                if (bottom != null) b.Bottom = ImportBorder(bottom.Val, bottom.Size, bottom.Color);
                if (left   != null) b.Left   = ImportBorder(left.Val,   left.Size,   left.Color);
                if (right  != null) b.Right  = ImportBorder(right.Val,  right.Size,  right.Color);
                if (b.Top != null || b.Bottom != null || b.Left != null || b.Right != null)
                    borders = b;
            }
        }

        if (rPr is not null)
        {
            if (rPr.Bold is not null) runBold = true;
            if (rPr.Italic is not null) runItalic = true;
            var col = rPr.Color;
            if (col?.Val?.Value is not null && col.Val.Value != "auto")
                runColor = col.Val.Value;
            if (rPr.FontSize?.Val?.Value is not null && int.TryParse(rPr.FontSize.Val.Value, out var sz))
                runFontSize = sz;
            if (rPr.RunFonts?.Ascii?.Value is not null)
                runFontFamily = rPr.RunFonts.Ascii.Value;
        }

        if (shading is null && borders is null && runBold is null && runItalic is null
            && runColor is null && runFontSize is null && runFontFamily is null)
            return null;

        return new TableStyleCondFmt(shading, borders, runBold, runItalic, runColor, runFontSize, runFontFamily);
    }

    private static TableStyleCondFmt? MergeCondFmt(
        TableStyleCondFmt? baseF, TableStyleCondFmt? overlay)
    {
        if (baseF is null) return overlay;
        if (overlay is null) return baseF;
        return new TableStyleCondFmt(
            overlay.CellShading   ?? baseF.CellShading,
            overlay.CellBorders   ?? baseF.CellBorders,
            overlay.RunBold       ?? baseF.RunBold,
            overlay.RunItalic     ?? baseF.RunItalic,
            overlay.RunColor      ?? baseF.RunColor,
            overlay.RunFontSize   ?? baseF.RunFontSize,
            overlay.RunFontFamily ?? baseF.RunFontFamily
        );
    }

    private static bool HasRunProps(TableStyleCondFmt fmt) =>
        fmt.RunBold is not null || fmt.RunItalic is not null ||
        fmt.RunColor is not null || fmt.RunFontSize is not null ||
        fmt.RunFontFamily is not null;

    private static void ApplyRunCondFmt(Model.TableCell cell, TableStyleCondFmt fmt)
    {
        foreach (var block in cell.Children)
        {
            if (block is not ModelParagraph para) continue;
            foreach (var inline in para.Children)
            {
                if (inline is not ModelRun run) continue;
                var p = run.Properties;
                if (fmt.RunBold is not null     && !p.Bold)           p.Bold       = fmt.RunBold.Value;
                if (fmt.RunItalic is not null   && !p.Italic)         p.Italic     = fmt.RunItalic.Value;
                if (fmt.RunColor is not null    && p.Color is null)   p.Color      = fmt.RunColor;
                if (fmt.RunFontSize is not null && p.FontSize is null) p.FontSize  = fmt.RunFontSize;
                if (fmt.RunFontFamily is not null && p.FontFamily is null) p.FontFamily = fmt.RunFontFamily;
            }
        }
    }

    // ── Theme color resolution ────────────────────────────────────────────────

    // Standard Office 2016 theme defaults — used when ThemePart is absent or unparseable.
    // dk1/lt1 are almost universally black/white; the others match the default Office theme.
    private static readonly Dictionary<string, string> _defaultThemeColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dk1"]      = "000000",  // Dark 1  — window text (black)
        ["lt1"]      = "FFFFFF",  // Light 1 — window background (white)
        ["dk2"]      = "44546A",  // Dark 2
        ["lt2"]      = "E7E6E6",  // Light 2
        ["accent1"]  = "4472C4",
        ["accent2"]  = "ED7D31",
        ["accent3"]  = "A9D18E",
        ["accent4"]  = "FFC000",
        ["accent5"]  = "5B9BD5",
        ["accent6"]  = "70AD47",
        ["hlink"]    = "0563C1",
        ["folHlink"] = "954F72",
    };

    private static Dictionary<string, string> ReadThemeColors(MainDocumentPart mainPart)
    {
        // Pre-populate with Office defaults so theme refs always resolve even without ThemePart
        var colors = new Dictionary<string, string>(_defaultThemeColors, StringComparer.OrdinalIgnoreCase);

        var scheme = mainPart.ThemePart?.Theme?.ThemeElements?.ColorScheme;
        if (scheme is null) return colors;  // return defaults if no theme part

        foreach (var child in scheme.ChildElements)
        {
            var key = child.LocalName; // "dk1", "lt1", "accent1", etc.
            var srgb = child.GetFirstChild<A.RgbColorModelHex>();
            if (srgb?.Val?.Value is not null) { colors[key] = srgb.Val.Value; continue; }
            var sys = child.GetFirstChild<A.SystemColor>();
            if (sys?.LastColor?.Value is not null) colors[key] = sys.LastColor.Value;
        }
        return colors;
    }

    private static string? ThemeColorToKey(ThemeColorValues v)
    {
        if (v == ThemeColorValues.Dark1)             return "dk1";
        if (v == ThemeColorValues.Light1)            return "lt1";
        if (v == ThemeColorValues.Dark2)             return "dk2";
        if (v == ThemeColorValues.Light2)            return "lt2";
        if (v == ThemeColorValues.Accent1)           return "accent1";
        if (v == ThemeColorValues.Accent2)           return "accent2";
        if (v == ThemeColorValues.Accent3)           return "accent3";
        if (v == ThemeColorValues.Accent4)           return "accent4";
        if (v == ThemeColorValues.Accent5)           return "accent5";
        if (v == ThemeColorValues.Accent6)           return "accent6";
        if (v == ThemeColorValues.Hyperlink)         return "hlink";
        if (v == ThemeColorValues.FollowedHyperlink) return "folHlink";
        return null;
    }

    private static string? ResolveShading(
        Shading? shd,
        IReadOnlyDictionary<string, string>? themeColors)
    {
        if (shd is null) return null;

        bool isSolid = shd.Val?.Value == ShadingPatternValues.Solid;

        if (isSolid)
        {
            // w:val="solid": foreground pattern covers cell entirely → w:color is the visible color
            if (shd.Color?.Value is not null && shd.Color.Value != "auto")
                return shd.Color.Value;
            if (shd.ThemeColor is not null && themeColors is not null)
            {
                var key = ThemeColorToKey(shd.ThemeColor.Value);
                if (key is not null && themeColors.TryGetValue(key, out var hex)) return hex;
            }
            return null;
        }

        // w:val="clear" (or absent/other): background fill is the visible color
        // Priority 1: direct hex fill
        if (shd.Fill?.Value is not null && shd.Fill.Value != "auto")
            return shd.Fill.Value;

        // Priority 2: theme-based fill (w:themeFill)
        if (shd.ThemeFill is not null && themeColors is not null)
        {
            var key = ThemeColorToKey(shd.ThemeFill.Value);
            if (key is not null && themeColors.TryGetValue(key, out var hex))
            {
                // Apply shade (darkening): result = base * shade/255
                if (shd.ThemeFillShade?.Value is not null &&
                    int.TryParse(shd.ThemeFillShade.Value,
                        System.Globalization.NumberStyles.HexNumber, null, out var shade))
                    hex = ApplyShadeFactor(hex, shade);

                // Apply tint (lightening): result = base + (255-base) * (1 - tint/255)
                if (shd.ThemeFillTint?.Value is not null &&
                    int.TryParse(shd.ThemeFillTint.Value,
                        System.Globalization.NumberStyles.HexNumber, null, out var tint))
                    hex = ApplyTintFactor(hex, tint);

                return hex;
            }
        }

        // Priority 3 (Suspect B fallback): some generators write w:themeColor on w:val="clear"
        // when they mean "fill theme color" — w:fill="auto" with no w:themeFill but w:themeColor set
        if (shd.ThemeFill is null && shd.ThemeColor is not null && themeColors is not null)
        {
            var key = ThemeColorToKey(shd.ThemeColor.Value);
            if (key is not null && themeColors.TryGetValue(key, out var hex)) return hex;
        }

        return null;
    }

    private static string ApplyShadeFactor(string hex, int shade)
    {
        if (!TryParseHex6(hex, out int r, out int g, out int b)) return hex;
        double f = shade / 255.0;
        return $"{(int)Math.Round(r*f):X2}{(int)Math.Round(g*f):X2}{(int)Math.Round(b*f):X2}";
    }

    private static string ApplyTintFactor(string hex, int tint)
    {
        if (!TryParseHex6(hex, out int r, out int g, out int b)) return hex;
        double f = 1.0 - tint / 255.0;
        return $"{(int)Math.Round(r+(255-r)*f):X2}{(int)Math.Round(g+(255-g)*f):X2}{(int)Math.Round(b+(255-b)*f):X2}";
    }

    private static bool TryParseHex6(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (hex.Length != 6) return false;
        return int.TryParse(hex[..2], System.Globalization.NumberStyles.HexNumber, null, out r)
            && int.TryParse(hex[2..4], System.Globalization.NumberStyles.HexNumber, null, out g)
            && int.TryParse(hex[4..6], System.Globalization.NumberStyles.HexNumber, null, out b);
    }
}
