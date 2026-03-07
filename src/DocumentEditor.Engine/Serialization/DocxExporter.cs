using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using CellBorderStyle = DocumentEditor.Engine.Model.Enums.CellBorderStyle;
using ModelCellBorders = DocumentEditor.Engine.Model.Properties.CellBorders;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using ModelParagraph = DocumentEditor.Engine.Model.Paragraph;
using ModelRun = DocumentEditor.Engine.Model.Run;
using ModelTable = DocumentEditor.Engine.Model.Table;
using ModelHyperlink = DocumentEditor.Engine.Model.Hyperlink;
using OxParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OxRun = DocumentFormat.OpenXml.Wordprocessing.Run;
using OxTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using OxHyperlink = DocumentFormat.OpenXml.Wordprocessing.Hyperlink;
using OxRunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using OxParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using OxTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using OxTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using ModelRunProperties = DocumentEditor.Engine.Model.Properties.RunProperties;
using ModelParagraphProperties = DocumentEditor.Engine.Model.Properties.ParagraphProperties;
using ModelDocumentProperties = DocumentEditor.Engine.Model.Properties.DocumentProperties;
using ModelSectionProperties = DocumentEditor.Engine.Model.Properties.SectionProperties;
using OxSectionProperties = DocumentFormat.OpenXml.Wordprocessing.SectionProperties;
using OxSectionType = DocumentFormat.OpenXml.Wordprocessing.SectionType;

namespace DocumentEditor.Engine.Serialization;

public class DocxExporter
{
    private int _nextDocPrId;

    public byte[] Export(DocxDocument doc)
    {
        _nextDocPrId = 1;
        using var stream = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            StylesBuilder.AddStylesPart(mainPart);
            NumberingBuilder.AddNumberingPart(mainPart);
            AddSettingsPart(mainPart);

            foreach (var block in doc.Children)
            {
                mainPart.Document.Body!.Append(ConvertBlock(block, mainPart));
            }

            mainPart.Document.Body!.Append(CreateSectionProperties(doc.Properties, mainPart));
        }

        return stream.ToArray();
    }

    private static void AddSettingsPart(MainDocumentPart mainPart)
    {
        var settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
        settingsPart.Settings = new Settings(
            new Compatibility(
                new CompatibilitySetting
                {
                    Name = CompatSettingNameValues.CompatibilityMode,
                    Uri = "http://schemas.microsoft.com/office/word",
                    Val = "15"
                }
            )
        );
    }

    private OpenXmlElement ConvertBlock(IBlockNode node, MainDocumentPart mainPart)
    {
        return node switch
        {
            ModelParagraph para => ConvertParagraph(para, mainPart),
            ModelTable table => ConvertTable(table, mainPart),
            _ => throw new NotSupportedException($"Block type {node.GetType().Name} not supported")
        };
    }

    private OxParagraph ConvertParagraph(ModelParagraph para, MainDocumentPart mainPart)
    {
        var oxPara = new OxParagraph();

        var pPr = ConvertParagraphProperties(para.Properties, mainPart);
        if (pPr.HasChildren)
            oxPara.Append(pPr);

        foreach (var inline in para.Children)
        {
            switch (inline)
            {
                case ModelRun run:
                    oxPara.Append(ConvertRun(run, mainPart));
                    break;
                case ModelHyperlink link:
                    oxPara.Append(ConvertHyperlink(link, mainPart));
                    break;
            }
        }

        return oxPara;
    }

    private static OxParagraphProperties ConvertParagraphProperties(ModelParagraphProperties props, MainDocumentPart mainPart)
    {
        var pPr = new OxParagraphProperties();

        // CT_PPr sequence order must be respected:
        // pStyle → keepNext → keepLines → pageBreakBefore → ... → numPr → ... → spacing → ind → ... → jc → ... → sectPr

        if (props.Style is not null)
            pPr.Append(new ParagraphStyleId { Val = props.Style });

        if (props.KeepNext)
            pPr.Append(new KeepNext());

        if (props.PageBreakBefore)
            pPr.Append(new PageBreakBefore());

        if (props.NumberingId is not null)
        {
            var numPr = new NumberingProperties(
                new NumberingLevelReference { Val = props.NumberingLevel ?? 0 },
                new NumberingId { Val = props.NumberingId.Value }
            );
            pPr.Append(numPr);
        }

        if (props.SpaceBefore is not null || props.SpaceAfter is not null || props.LineSpacing is not null)
        {
            var spacing = new SpacingBetweenLines();
            if (props.SpaceBefore is not null) spacing.Before = props.SpaceBefore.Value.ToString();
            if (props.SpaceAfter is not null) spacing.After = props.SpaceAfter.Value.ToString();
            if (props.LineSpacing is not null)
            {
                spacing.Line = props.LineSpacing.Value.ToString();
                spacing.LineRule = LineSpacingRuleValues.Auto;
            }
            pPr.Append(spacing);
        }

        if (props.IndentLeft is not null || props.IndentFirstLine is not null || props.IndentHanging is not null)
        {
            var indent = new Indentation();
            if (props.IndentLeft is not null) indent.Left = props.IndentLeft.Value.ToString();
            if (props.IndentFirstLine is not null) indent.FirstLine = props.IndentFirstLine.Value.ToString();
            if (props.IndentHanging is not null) indent.Hanging = props.IndentHanging.Value.ToString();
            pPr.Append(indent);
        }

        if (props.Alignment is not null)
            pPr.Append(new Justification { Val = MapAlignment(props.Alignment.Value) });

        // Mid-document section break: w:sectPr inside w:pPr (must be last)
        if (props.SectionBreak is not null)
            pPr.Append(CreateSectionPropertiesFromSection(props.SectionBreak, mainPart));

        return pPr;
    }

    private OxRun ConvertRun(ModelRun run, MainDocumentPart mainPart)
    {
        var oxRun = new OxRun();

        var rPr = ConvertRunProperties(run.Properties);
        if (rPr.HasChildren)
            oxRun.Append(rPr);

        foreach (var content in run.Content)
        {
            switch (content)
            {
                case TextPiece text:
                    var t = new Text(text.Text);
                    if (text.Text.Length > 0 && (text.Text[0] == ' ' || text.Text[^1] == ' '))
                        t.Space = SpaceProcessingModeValues.Preserve;
                    oxRun.Append(t);
                    break;
                case TabContent:
                    oxRun.Append(new TabChar());
                    break;
                case BreakContent br:
                    var oxBr = new Break();
                    if (br.BreakType == BreakType.Page)
                        oxBr.Type = BreakValues.Page;
                    else if (br.BreakType == BreakType.Column)
                        oxBr.Type = BreakValues.Column;
                    oxRun.Append(oxBr);
                    break;
                case ImageContent img:
                    oxRun.Append(CreateDrawingElement(img, mainPart));
                    break;
            }
        }

        return oxRun;
    }

    private static OxRunProperties ConvertRunProperties(ModelRunProperties props)
    {
        var rPr = new OxRunProperties();

        // CT_RPr sequence order must be respected:
        // rFonts → b → i → ... → strike → ... → color → spacing → ... → sz → ... → highlight → u → ... → vertAlign

        if (props.FontFamily is not null)
            rPr.Append(new RunFonts { Ascii = props.FontFamily, HighAnsi = props.FontFamily });
        if (props.Bold)
            rPr.Append(new Bold());
        if (props.Italic)
            rPr.Append(new Italic());
        if (props.Strikethrough)
            rPr.Append(new Strike());
        if (props.Color is not null)
            rPr.Append(new Color { Val = props.Color });
        if (props.CharacterSpacing is not null)
            rPr.Append(new Spacing { Val = props.CharacterSpacing.Value });
        if (props.FontSize is not null)
            rPr.Append(new FontSize { Val = props.FontSize.Value.ToString() });
        if (props.Highlight is not null && props.Highlight != HighlightColor.None)
            rPr.Append(new Highlight { Val = MapHighlight(props.Highlight.Value) });
        if (props.Underline is not null && props.Underline != UnderlineType.None)
            rPr.Append(new Underline { Val = MapUnderline(props.Underline.Value) });
        if (props.VerticalAlign is not null && props.VerticalAlign != VerticalAlignType.Baseline)
            rPr.Append(new VerticalTextAlignment { Val = MapVerticalAlign(props.VerticalAlign.Value) });

        return rPr;
    }

    private OxHyperlink ConvertHyperlink(ModelHyperlink link, MainDocumentPart mainPart)
    {
        var relId = RelationshipsBuilder.AddHyperlinkRelationship(mainPart, link.Url);
        var oxLink = new OxHyperlink { Id = relId };

        if (link.Tooltip is not null)
            oxLink.Tooltip = link.Tooltip;

        foreach (var run in link.Children)
        {
            oxLink.Append(ConvertRun(run, mainPart));
        }

        return oxLink;
    }

    private Drawing CreateDrawingElement(ImageContent img, MainDocumentPart mainPart)
    {
        var imageBytes = Convert.FromBase64String(img.ImageData);
        var imagePart = mainPart.AddImagePart(img.ContentMimeType);

        using (var ms = new MemoryStream(imageBytes))
        {
            imagePart.FeedData(ms);
        }

        var relationshipId = mainPart.GetIdOfPart(imagePart);
        var docPrId = (uint)_nextDocPrId++;

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = img.WidthEmu, Cy = img.HeightEmu },
                new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                new DW.DocProperties
                {
                    Id = docPrId,
                    Name = img.Name ?? $"Picture {docPrId}",
                    Description = img.AltText ?? ""
                },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties
                                {
                                    Id = docPrId,
                                    Name = img.Name ?? $"Picture {docPrId}"
                                },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0, Y = 0 },
                                    new A.Extents { Cx = img.WidthEmu, Cy = img.HeightEmu })
                                {
                                    Rotation = img.RotationDegrees != 0
                                        ? (int)Math.Round(img.RotationDegrees * 60000)
                                        : (int?)null
                                },
                                new A.PresetGeometry(new A.AdjustValueList())
                                { Preset = A.ShapeTypeValues.Rectangle }))
                    )
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
            )
            {
                DistanceFromTop = 0,
                DistanceFromBottom = 0,
                DistanceFromLeft = 0,
                DistanceFromRight = 0
            });

        return drawing;
    }

    private OxTable ConvertTable(ModelTable table, MainDocumentPart mainPart)
    {
        var oxTable = new OxTable();

        var tblPr = new TableProperties();
        if (table.Properties.Style is not null)
            tblPr.Append(new TableStyle { Val = table.Properties.Style });
        else
            tblPr.Append(new TableStyle { Val = "TableGrid" });

        // CT_TblPr sequence: tblStyle → tblW → tblBorders → tblLook
        tblPr.Append(new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto });

        if (table.Properties.HasBorders)
        {
            // CT_TblBrd sequence: top → left → bottom → right → insideH → insideV
            tblPr.Append(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
                new RightBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "auto" }
            ));
        }

        tblPr.Append(new TableLook
        {
            Val = "04A0",
            FirstRow = true,
            LastRow = false,
            FirstColumn = true,
            LastColumn = false,
            NoHorizontalBand = false,
            NoVerticalBand = true
        });

        oxTable.Append(tblPr);

        var grid = new TableGrid();
        foreach (var colWidth in table.GridColumnWidths)
        {
            grid.Append(new GridColumn { Width = colWidth.ToString() });
        }
        oxTable.Append(grid);

        foreach (var row in table.Rows)
        {
            oxTable.Append(ConvertTableRow(row, mainPart));
        }

        return oxTable;
    }

    private OxTableRow ConvertTableRow(Model.TableRow row, MainDocumentPart mainPart)
    {
        var oxRow = new OxTableRow();

        if (row.Properties.Height is not null || row.Properties.IsHeader)
        {
            var trPr = new TableRowProperties();
            if (row.Properties.Height is not null)
                trPr.Append(new TableRowHeight { Val = (uint)row.Properties.Height.Value });
            if (row.Properties.IsHeader)
                trPr.Append(new TableHeader());
            oxRow.Append(trPr);
        }

        foreach (var cell in row.Cells)
        {
            oxRow.Append(ConvertTableCell(cell, mainPart));
        }

        return oxRow;
    }

    private OxTableCell ConvertTableCell(Model.TableCell cell, MainDocumentPart mainPart)
    {
        var oxCell = new OxTableCell();

        // CT_TcPr sequence: tcW → gridSpan → vMerge → tcBorders → shd → vAlign
        var tcPr = new TableCellProperties();
        if (cell.Properties.Width is not null)
            tcPr.Append(new TableCellWidth { Width = cell.Properties.Width.Value.ToString(), Type = TableWidthUnitValues.Dxa });
        if (cell.Properties.GridSpan is not null && cell.Properties.GridSpan > 1)
            tcPr.Append(new GridSpan { Val = cell.Properties.GridSpan.Value });
        if (cell.Properties.VerticalMerge is not null && cell.Properties.VerticalMerge != VerticalMergeType.None)
        {
            var vm = new VerticalMerge();
            if (cell.Properties.VerticalMerge == VerticalMergeType.Restart)
                vm.Val = MergedCellValues.Restart;
            tcPr.Append(vm);
        }
        if (cell.Properties.Borders is not null)
        {
            var tcBorders = new TableCellBorders();
            var b = cell.Properties.Borders;
            // CT_TcBorders sequence: top → left → bottom → right → insideH → insideV
            if (b.Top    != null) tcBorders.Append(new TopBorder    { Val = MapBorderStyle(b.Top.Style),    Size = (uint)b.Top.Size,    Space = 0, Color = b.Top.Color });
            if (b.Left   != null) tcBorders.Append(new LeftBorder   { Val = MapBorderStyle(b.Left.Style),   Size = (uint)b.Left.Size,   Space = 0, Color = b.Left.Color });
            if (b.Bottom != null) tcBorders.Append(new BottomBorder { Val = MapBorderStyle(b.Bottom.Style), Size = (uint)b.Bottom.Size, Space = 0, Color = b.Bottom.Color });
            if (b.Right  != null) tcBorders.Append(new RightBorder  { Val = MapBorderStyle(b.Right.Style),  Size = (uint)b.Right.Size,  Space = 0, Color = b.Right.Color });
            if (tcBorders.HasChildren) tcPr.Append(tcBorders);
        }
        if (cell.Properties.Shading is not null)
            tcPr.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = cell.Properties.Shading });
        if (cell.Properties.VerticalAlignment is not null)
            tcPr.Append(new TableCellVerticalAlignment { Val = MapTableVerticalAlignment(cell.Properties.VerticalAlignment.Value) });

        if (tcPr.HasChildren)
            oxCell.Append(tcPr);

        foreach (var block in cell.Children)
        {
            oxCell.Append(ConvertBlock(block, mainPart));
        }

        return oxCell;
    }

    private static OxSectionProperties CreateSectionProperties(ModelDocumentProperties props, MainDocumentPart mainPart)
    {
        // CT_SectPr sequence: (headerReference | footerReference)* → pgSz → pgMar → cols → titlePg
        var sectPr = new OxSectionProperties();

        // Header/footer references must come first
        AttachHeaderFooterParts(sectPr, mainPart, props.HeaderFooterParts);

        var pageSize = new PageSize
        {
            Width = (uint)props.PageWidth,
            Height = (uint)props.PageHeight
        };
        if (props.Orientation == Orientation.Landscape)
            pageSize.Orient = PageOrientationValues.Landscape;
        sectPr.Append(pageSize);

        sectPr.Append(new PageMargin
        {
            Top = props.MarginTop,
            Bottom = props.MarginBottom,
            Left = (uint)props.MarginLeft,
            Right = (uint)props.MarginRight,
            Header = (uint)props.HeaderDistance,
            Footer = (uint)props.FooterDistance
        });

        if (props.ColumnCount > 1)
        {
            var cols = new Columns { ColumnCount = (short)props.ColumnCount };
            if (props.ColumnSpacing != 720)
                cols.Space = props.ColumnSpacing.ToString();
            if (props.ColumnSeparator)
                cols.Separator = true;
            sectPr.Append(cols);
        }

        if (props.TitlePage)
            sectPr.Append(new TitlePage());

        return sectPr;
    }

    /// <summary>
    /// Creates a w:sectPr element from a model SectionProperties (for mid-document section breaks).
    /// </summary>
    private static OxSectionProperties CreateSectionPropertiesFromSection(ModelSectionProperties sp, MainDocumentPart mainPart)
    {
        // CT_SectPr sequence: (headerReference | footerReference)* → type? → pgSz → pgMar → cols → titlePg
        var sectPr = new OxSectionProperties();

        // Header/footer references must come first
        AttachHeaderFooterParts(sectPr, mainPart, sp.HeaderFooterParts);

        // w:type (section break type)
        if (sp.BreakType != SectionBreakType.NextPage)
        {
            SectionMarkValues val;
            if (sp.BreakType == SectionBreakType.Continuous) val = SectionMarkValues.Continuous;
            else if (sp.BreakType == SectionBreakType.EvenPage) val = SectionMarkValues.EvenPage;
            else if (sp.BreakType == SectionBreakType.OddPage) val = SectionMarkValues.OddPage;
            else val = SectionMarkValues.NextPage;
            sectPr.Append(new OxSectionType { Val = val });
        }

        // w:pgSz
        var pageSize = new PageSize
        {
            Width = (uint)sp.PageWidth,
            Height = (uint)sp.PageHeight
        };
        if (sp.Orientation == Orientation.Landscape)
            pageSize.Orient = PageOrientationValues.Landscape;
        sectPr.Append(pageSize);

        // w:pgMar
        sectPr.Append(new PageMargin
        {
            Top = sp.MarginTop,
            Bottom = sp.MarginBottom,
            Left = (uint)sp.MarginLeft,
            Right = (uint)sp.MarginRight,
            Header = (uint)sp.HeaderDistance,
            Footer = (uint)sp.FooterDistance
        });

        // w:cols
        if (sp.ColumnCount > 1)
        {
            var cols = new Columns { ColumnCount = (short)sp.ColumnCount };
            if (sp.ColumnSpacing != 720)
                cols.Space = sp.ColumnSpacing.ToString();
            if (sp.ColumnSeparator)
                cols.Separator = true;
            sectPr.Append(cols);
        }

        if (sp.TitlePage)
            sectPr.Append(new TitlePage());

        return sectPr;
    }

    /// <summary>
    /// Attaches raw header/footer XML parts back to the document for lossless round-trip export.
    /// Only appends HeaderReference/FooterReference elements — caller is responsible for
    /// appending TitlePage and other sectPr children in the correct schema order.
    /// </summary>
    private static void AttachHeaderFooterParts(
        OxSectionProperties sectPr,
        MainDocumentPart mainPart,
        Dictionary<string, byte[]>? headerFooterParts)
    {
        if (headerFooterParts is null || headerFooterParts.Count == 0) return;

        foreach (var (key, xmlBytes) in headerFooterParts)
        {
            // Skip hyperlink-rels entries — those are processed after the part is created
            if (key.EndsWith("-hyperlinks")) continue;

            var parts = key.Split('-', 2);
            if (parts.Length != 2) continue;

            var kind = parts[0];    // "header" or "footer"
            var typeKey = parts[1]; // "default", "first", "even"

            var hfType = typeKey switch
            {
                "first" => HeaderFooterValues.First,
                "even" => HeaderFooterValues.Even,
                _ => HeaderFooterValues.Default
            };

            if (kind == "header")
            {
                var headerPart = mainPart.AddNewPart<HeaderPart>();
                using (var ms = new MemoryStream(xmlBytes))
                    headerPart.FeedData(ms);

                // Restore hyperlink relationships so r:id references in the XML remain valid
                var hKey = $"header-{typeKey}-hyperlinks";
                if (headerFooterParts.TryGetValue(hKey, out var relsBytes))
                {
                    var rels = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, string>>(relsBytes)!;
                    foreach (var (hlRelId, hlUri) in rels)
                        headerPart.AddHyperlinkRelationship(new Uri(hlUri), true, hlRelId);
                }

                var partRelId = mainPart.GetIdOfPart(headerPart);
                sectPr.Append(new HeaderReference { Type = hfType, Id = partRelId });
            }
            else if (kind == "footer")
            {
                var footerPart = mainPart.AddNewPart<FooterPart>();
                using (var ms = new MemoryStream(xmlBytes))
                    footerPart.FeedData(ms);

                // Restore hyperlink relationships so r:id references in the XML remain valid
                var hKey = $"footer-{typeKey}-hyperlinks";
                if (headerFooterParts.TryGetValue(hKey, out var relsBytes))
                {
                    var rels = System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, string>>(relsBytes)!;
                    foreach (var (hlRelId, hlUri) in rels)
                        footerPart.AddHyperlinkRelationship(new Uri(hlUri), true, hlRelId);
                }

                var partRelId = mainPart.GetIdOfPart(footerPart);
                sectPr.Append(new FooterReference { Type = hfType, Id = partRelId });
            }
        }
    }

    private static JustificationValues MapAlignment(Alignment alignment) => alignment switch
    {
        Alignment.Left => JustificationValues.Left,
        Alignment.Center => JustificationValues.Center,
        Alignment.Right => JustificationValues.Right,
        Alignment.Both => JustificationValues.Both,
        _ => JustificationValues.Left
    };

    private static UnderlineValues MapUnderline(UnderlineType underline) => underline switch
    {
        UnderlineType.Single => UnderlineValues.Single,
        UnderlineType.Double => UnderlineValues.Double,
        UnderlineType.Dotted => UnderlineValues.Dotted,
        UnderlineType.Dash => UnderlineValues.Dash,
        UnderlineType.DotDash => UnderlineValues.DotDash,
        UnderlineType.DotDotDash => UnderlineValues.DotDotDash,
        UnderlineType.Wave => UnderlineValues.Wave,
        UnderlineType.Thick => UnderlineValues.Thick,
        UnderlineType.Words => UnderlineValues.Words,
        _ => UnderlineValues.None
    };

    private static HighlightColorValues MapHighlight(HighlightColor color) => color switch
    {
        HighlightColor.Black => HighlightColorValues.Black,
        HighlightColor.Blue => HighlightColorValues.Blue,
        HighlightColor.Cyan => HighlightColorValues.Cyan,
        HighlightColor.DarkBlue => HighlightColorValues.DarkBlue,
        HighlightColor.DarkCyan => HighlightColorValues.DarkCyan,
        HighlightColor.DarkGray => HighlightColorValues.DarkGray,
        HighlightColor.DarkGreen => HighlightColorValues.DarkGreen,
        HighlightColor.DarkMagenta => HighlightColorValues.DarkMagenta,
        HighlightColor.DarkRed => HighlightColorValues.DarkRed,
        HighlightColor.DarkYellow => HighlightColorValues.DarkYellow,
        HighlightColor.Green => HighlightColorValues.Green,
        HighlightColor.LightGray => HighlightColorValues.LightGray,
        HighlightColor.Magenta => HighlightColorValues.Magenta,
        HighlightColor.Red => HighlightColorValues.Red,
        HighlightColor.White => HighlightColorValues.White,
        HighlightColor.Yellow => HighlightColorValues.Yellow,
        _ => HighlightColorValues.None
    };

    private static VerticalPositionValues MapVerticalAlign(VerticalAlignType align) => align switch
    {
        VerticalAlignType.Superscript => VerticalPositionValues.Superscript,
        VerticalAlignType.Subscript => VerticalPositionValues.Subscript,
        _ => VerticalPositionValues.Baseline
    };

    private static TableVerticalAlignmentValues MapTableVerticalAlignment(TableVerticalAlignment align) => align switch
    {
        TableVerticalAlignment.Top => TableVerticalAlignmentValues.Top,
        TableVerticalAlignment.Center => TableVerticalAlignmentValues.Center,
        TableVerticalAlignment.Bottom => TableVerticalAlignmentValues.Bottom,
        _ => TableVerticalAlignmentValues.Top
    };

    private static BorderValues MapBorderStyle(CellBorderStyle style) => style switch
    {
        CellBorderStyle.None   => BorderValues.Nil,
        CellBorderStyle.Double => BorderValues.Double,
        CellBorderStyle.Dotted => BorderValues.Dotted,
        CellBorderStyle.Dashed => BorderValues.Dashed,
        CellBorderStyle.Thick  => BorderValues.Thick,
        _                      => BorderValues.Single
    };
}
