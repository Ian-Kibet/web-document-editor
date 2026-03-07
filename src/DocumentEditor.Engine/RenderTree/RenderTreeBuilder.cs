using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using CellBorderModel = DocumentEditor.Engine.Model.Properties.CellBorder;

namespace DocumentEditor.Engine.RenderTree;

/// <summary>
/// Converts the document model into a RenderTree for the TypeScript frontend.
/// Each model node becomes a RenderNode with the appropriate HTML tag, CSS styles, and attributes.
/// </summary>
public class RenderTreeBuilder
{
    public List<RenderNode> Build(DocxDocument document)
    {
        var sections = SectionResolver.GetSections(document);
        var nodes = new List<RenderNode>();

        for (var si = 0; si < sections.Count; si++)
        {
            var section = sections[si];
            var sp = section.Properties;

            // Full page geometry in px (96 dpi) — integer values to match JS Math.round()
            var pageWidthPx = TwipsToPxInt(sp.PageWidth);
            var marginTopPx = TwipsToPxInt(sp.MarginTop);
            var marginBottomPx = TwipsToPxInt(sp.MarginBottom);
            var marginLeftPx = TwipsToPxInt(sp.MarginLeft);
            var marginRightPx = TwipsToPxInt(sp.MarginRight);
            var pageHeightPx = TwipsToPxInt(sp.PageHeight);

            var sectionNode = new RenderNode
            {
                Id = $"section-{si}",
                Tag = "section",
                Attrs = new Dictionary<string, string>
                {
                    ["data-section-index"] = si.ToString()
                },
                Styles = new Dictionary<string, string>
                {
                    ["width"] = $"{pageWidthPx}px",
                    ["padding-top"] = $"{marginTopPx}px",
                    ["padding-bottom"] = $"{marginBottomPx}px",
                    ["padding-left"] = $"{marginLeftPx}px",
                    ["padding-right"] = $"{marginRightPx}px"
                },
                Children = []
            };

            if (sp.ColumnCount > 1)
            {
                sectionNode.Styles["column-count"] = sp.ColumnCount.ToString();
                sectionNode.Styles["column-gap"] = $"{TwipsToPxInt(sp.ColumnSpacing)}px";
                if (sp.ColumnSeparator)
                    sectionNode.Styles["column-rule"] = "1px solid #ccc";
            }
            else
            {
                sectionNode.Styles["min-height"] = $"{pageHeightPx}px";
            }

            var start = Math.Max(section.StartBlockIndex, 0);
            var end = Math.Min(section.EndBlockIndex, document.Children.Count - 1);
            for (var i = start; i <= end; i++)
            {
                var next = (i + 1 <= end) ? document.Children[i + 1] : null;
                sectionNode.Children.Add(BuildBlock(document.Children[i], next));
            }

            nodes.Add(sectionNode);
        }

        return nodes;
    }

    private static RenderNode BuildBlock(IBlockNode block, IBlockNode? nextBlock = null)
    {
        return block switch
        {
            Paragraph para => BuildParagraph(para, nextBlock as Paragraph),
            Table table => BuildTable(table),
            _ => throw new NotSupportedException($"Block type {block.GetType().Name} not supported")
        };
    }

    internal static RenderNode BuildParagraph(Paragraph para, Paragraph? nextPara = null)
    {
        var tag = ResolveParagraphTag(para.Properties);
        var attrs = BuildParagraphAttrs(para.Properties);

        // Paragraphs that anchor a floating image must not get the clearfix (p::after { clear:both })
        // because the clearfix would force the <p> to contain the float, preventing text in the
        // next paragraph from wrapping beside the image.
        var hasFloat = para.Children
            .OfType<Run>()
            .SelectMany(r => r.Content)
            .OfType<ImageContent>()
            .Any(img => img.WrapMode is ImageWrapMode.FloatLeft or ImageWrapMode.FloatRight);

        if (hasFloat)
        {
            attrs ??= new Dictionary<string, string>();
            attrs["data-has-float"] = "true";
        }

        var node = new RenderNode
        {
            Id = para.Id,
            Tag = tag,
            Styles = BuildParagraphStyles(para.Properties, nextPara?.Properties),
            Attrs = attrs,
            Children = []
        };

        foreach (var inline in para.Children)
        {
            switch (inline)
            {
                case Run run:
                    node.Children.AddRange(BuildRun(run));
                    break;
                case Hyperlink link:
                    node.Children.Add(BuildHyperlink(link));
                    break;
            }
        }

        return node;
    }

    private static string ResolveParagraphTag(ParagraphProperties props)
    {
        return props.Style switch
        {
            "Heading1" => "h1",
            "Heading2" => "h2",
            "Heading3" => "h3",
            "Heading4" => "h4",
            _ => "p"
        };
    }

    private static Dictionary<string, string>? BuildParagraphStyles(ParagraphProperties props, ParagraphProperties? nextProps = null)
    {
        var styles = new Dictionary<string, string>();

        if (props.Alignment is not null)
        {
            styles["text-align"] = props.Alignment.Value switch
            {
                Alignment.Center => "center",
                Alignment.Right => "right",
                Alignment.Both => "justify",
                _ => "left"
            };
        }

        if (props.IndentLeft is not null)
            styles["margin-left"] = TwipsToPx(props.IndentLeft.Value);

        if (props.IndentFirstLine is not null)
            styles["text-indent"] = TwipsToPx(props.IndentFirstLine.Value);

        if (props.IndentHanging is not null)
            styles["text-indent"] = "-" + TwipsToPx(props.IndentHanging.Value);

        if (props.NumberingId is not null && props.IndentLeft is null && props.IndentHanging is null)
        {
            var level = props.NumberingLevel ?? 0;
            styles["margin-left"] = TwipsToPx(720 * (level + 1));
            styles["text-indent"] = "-" + TwipsToPx(360);
        }

        // Emit CSS custom property so ::before width matches the actual hanging indent
        if (props.NumberingId is not null)
        {
            var hangingPx = props.IndentHanging.HasValue
                ? TwipsToPxInt(props.IndentHanging.Value)
                : 24; // 360-twip default from fallback above
            styles["--list-hanging"] = $"{hangingPx}px";
        }

        if (props.SpaceBefore is not null)
            styles["margin-top"] = TwipsToPx(props.SpaceBefore.Value);

        if (props.SpaceAfter is not null)
        {
            var suppress = props.ContextualSpacing == true
                           && props.Style is not null
                           && nextProps?.Style == props.Style;
            if (!suppress)
                styles["margin-bottom"] = TwipsToPx(props.SpaceAfter.Value);
        }

        // List items: prevent doc-default spacing from inflating gaps between items
        if (props.NumberingId is not null)
        {
            if (!styles.ContainsKey("margin-top"))
                styles["margin-top"] = "0px";
            if (!styles.ContainsKey("margin-bottom"))
                styles["margin-bottom"] = "0px";
        }

        if (props.LineSpacing is not null)
        {
            if (props.LineSpacingRule is "exact" or "atleast")
                // Exact/AtLeast: w:line is in twips — emit as absolute px
                styles["line-height"] = TwipsToPx(props.LineSpacing.Value);
            else
            {
                // "auto" mode: two-regime mapping matching Word's line spacing semantics.
                // Single spacing (w:line=240) = font's natural line height = CSS `normal` ≈ 1.15.
                // Values at or below natural line height (N/240 ≤ 1.15) need the 1.15× correction.
                // Larger explicit spacings (1.5×, 2×) use the raw N/240 ratio — Word defines
                // them as em multiples, so no correction is needed.
                var ratio = props.LineSpacing.Value / 240.0;
                var lineHeight = ratio <= 1.15 ? ratio * 1.15 : ratio;
                styles["line-height"] = lineHeight.ToString("F2");
            }
        }

        // Paragraph-level font properties from the named style definition.
        // Emitting explicit values (including "normal") overrides the CSS class defaults in editor.css.
        if (props.StyleBold.HasValue)
            styles["font-weight"] = props.StyleBold.Value ? "bold" : "normal";
        if (props.StyleItalic.HasValue)
            styles["font-style"] = props.StyleItalic.Value ? "italic" : "normal";
        if (props.StyleFontSize.HasValue)
            styles["font-size"] = $"{props.StyleFontSize.Value / 2.0}pt";
        if (props.StyleFontFamily is not null)
            styles["font-family"] = $"'{props.StyleFontFamily}', sans-serif";
        if (props.StyleColor is not null)
            styles["color"] = $"#{props.StyleColor}";

        return styles.Count > 0 ? styles : null;
    }

    private static Dictionary<string, string>? BuildParagraphAttrs(ParagraphProperties props)
    {
        var attrs = new Dictionary<string, string>();

        if (props.NumberingId is not null)
        {
            var isBullet = string.Equals(props.NumberingFormat, "bullet",
                                         StringComparison.OrdinalIgnoreCase);
            attrs["data-list-type"] = props.NumberingFormat is null
                ? "unknown"
                : isBullet ? "bullet" : "numbered";
            attrs["data-list-level"] = (props.NumberingLevel ?? 0).ToString();
        }

        if (props.PageBreakBefore)
            attrs["data-page-break-before"] = "true";

        if (props.SectionBreak is not null)
            attrs["data-section-break"] = props.SectionBreak.BreakType.ToString().ToLowerInvariant();

        return attrs.Count > 0 ? attrs : null;
    }

    /// <summary>
    /// Builds render nodes for a Run. A run with only text content becomes a single span.
    /// Runs with mixed content (text + tabs + breaks) produce multiple sibling nodes.
    /// </summary>
    private static List<RenderNode> BuildRun(Run run)
    {
        var nodes = new List<RenderNode>();
        var styles = BuildRunStyles(run.Properties);

        foreach (var content in run.Content)
        {
            switch (content)
            {
                case TextPiece text:
                    nodes.Add(new RenderNode
                    {
                        Id = run.Id,
                        Tag = "span",
                        Styles = styles,
                        Attrs = run.FieldType != null
                            ? new Dictionary<string, string> { ["data-field"] = run.FieldType }
                            : null,
                        Text = text.Text
                    });
                    break;
                case TabContent:
                    nodes.Add(new RenderNode
                    {
                        Id = run.Id,
                        Tag = "span",
                        Styles = styles,
                        Attrs = new Dictionary<string, string> { ["data-type"] = "tab" },
                        Text = "\t"
                    });
                    break;
                case BreakContent br:
                    if (br.BreakType == BreakType.Column)
                    {
                        nodes.Add(new RenderNode
                        {
                            Id = run.Id,
                            Tag = "span",
                            Styles = new Dictionary<string, string> { ["break-before"] = "column" },
                            Attrs = new Dictionary<string, string> { ["data-break-type"] = "column" }
                        });
                    }
                    else
                    {
                        nodes.Add(new RenderNode
                        {
                            Id = run.Id,
                            Tag = "br",
                            Attrs = br.BreakType != BreakType.TextWrapping
                                ? new Dictionary<string, string> { ["data-break-type"] = br.BreakType.ToString().ToLowerInvariant() }
                                : null
                        });
                        if (br.BreakType == BreakType.Page)
                        {
                            nodes.Add(new RenderNode
                            {
                                Id = $"{run.Id}-lbl",
                                Tag = "span",
                                Attrs = new Dictionary<string, string>
                                {
                                    ["data-break-label"] = "page",
                                    ["contenteditable"] = "false"
                                }
                            });
                        }
                    }
                    break;
                case ImageContent img:
                    nodes.Add(BuildImageNode(run.Id, img));
                    break;
            }
        }

        // Empty run — still emit a span so the cursor can target it
        if (nodes.Count == 0)
        {
            nodes.Add(new RenderNode
            {
                Id = run.Id,
                Tag = "span",
                Styles = styles,
                Text = ""
            });
        }

        return nodes;
    }

    private static RenderNode BuildImageNode(string runId, ImageContent img)
    {
        var W    = EmuToPx(img.WidthEmu);
        var H    = EmuToPx(img.HeightEmu);
        var mode = img.WrapMode;

        // BehindText/InFrontOfText are absolutely positioned — rotation is purely
        // visual and doesn't affect text flow regardless, so keep the simple single-img approach.
        bool isAbsoluteLayer = mode is ImageWrapMode.BehindText or ImageWrapMode.InFrontOfText;

        if (isAbsoluteLayer)
        {
            var styles = new Dictionary<string, string>
            {
                ["width"]  = $"{W}px",
                ["height"] = $"{H}px"
            };
            ApplyWrapModeStyles(img, styles);
            if (img.RotationDegrees != 0)
                styles["transform"] = $"rotate({img.RotationDegrees}deg)";

            return new RenderNode
            {
                Id    = runId,
                Tag   = "img",
                Attrs = new Dictionary<string, string>
                {
                    ["src"]              = $"data:{img.ContentMimeType};base64,{img.ImageData}",
                    ["alt"]              = img.AltText ?? img.Name ?? "Image",
                    ["data-type"]        = "image",
                    ["contenteditable"]  = "false",
                    ["data-wrap-mode"]   = mode.ToString().ToLowerInvariant(),
                    ["data-rotation"]    = img.RotationDegrees.ToString("F4"),
                    ["data-orig-width"]  = $"{W}",
                    ["data-orig-height"] = $"{H}"
                },
                Styles = styles
            };
        }

        // For flow-affecting wrap modes (Inline, FloatLeft, FloatRight, TopAndBottom):
        // text wraps around the axis-aligned bounding box of the rotated image.
        // Use a wrapper <span> sized to the rotated bbox as the layout placeholder,
        // with an absolutely-centered inner <img> that has the visual rotation applied.
        var rad  = img.RotationDegrees * Math.PI / 180.0;
        var cosA = Math.Abs(Math.Cos(rad));
        var sinA = Math.Abs(Math.Sin(rad));
        var rotW = W * cosA + H * sinA;
        var rotH = W * sinA + H * cosA;

        var innerImg = new RenderNode
        {
            Id  = runId + ":img",
            Tag = "img",
            Attrs = new Dictionary<string, string>
            {
                ["src"]             = $"data:{img.ContentMimeType};base64,{img.ImageData}",
                ["alt"]             = img.AltText ?? img.Name ?? "Image",
                ["contenteditable"] = "false"
            },
            Styles = new Dictionary<string, string>
            {
                ["position"]       = "absolute",
                ["top"]            = "50%",
                ["left"]           = "50%",
                ["width"]          = $"{W}px",
                ["height"]         = $"{H}px",
                ["transform"]      = $"translate(-50%,-50%) rotate({img.RotationDegrees}deg)",
                ["pointer-events"] = "none"
            }
        };

        var wrapperStyles = new Dictionary<string, string>
        {
            ["width"]    = $"{rotW:F2}px",
            ["height"]   = $"{rotH:F2}px",
            ["position"] = "relative"
        };
        ApplyWrapModeStyles(img, wrapperStyles);

        // Images may spill into margins — remove the content-area constraint
        wrapperStyles.Remove("max-width");

        // Shift the wrapper toward the anchoring margin so the image center stays
        // at the same document position it occupied before rotation.
        var offsetPx = (W - rotW) / 2.0; // 0 when unrotated; negative when rotW > W
        switch (mode)
        {
            case ImageWrapMode.FloatLeft:
                wrapperStyles["margin-left"] = $"{offsetPx:F2}px";
                break;
            case ImageWrapMode.FloatRight:
                wrapperStyles["margin-right"] = $"{offsetPx:F2}px";
                break;
            case ImageWrapMode.Inline:
            case ImageWrapMode.TopAndBottom:
                wrapperStyles["margin-left"] = $"{offsetPx:F2}px";
                break;
        }

        return new RenderNode
        {
            Id  = runId,
            Tag = "span",
            Attrs = new Dictionary<string, string>
            {
                ["data-type"]        = "image",
                ["contenteditable"]  = "false",
                ["data-wrap-mode"]   = mode.ToString().ToLowerInvariant(),
                ["data-rotation"]    = img.RotationDegrees.ToString("F4"),
                ["data-orig-width"]  = $"{W}",
                ["data-orig-height"] = $"{H}"
            },
            Styles   = wrapperStyles,
            Children = [innerImg]
        };
    }

    private static void ApplyWrapModeStyles(ImageContent img, Dictionary<string, string> styles)
    {
        static string Dpx(long? emu) => emu is > 0 ? $"{EmuToPx(emu.Value)}px" : "0px";

        switch (img.WrapMode)
        {
            case ImageWrapMode.Inline:
                styles["display"]        = "inline-block";
                styles["vertical-align"] = "bottom";
                styles["max-width"]      = "100%";
                break;

            case ImageWrapMode.FloatLeft:
                styles["float"]        = "left";
                styles["margin-right"] = Dpx(img.DistRightEmu);
                styles["margin-top"]   = Dpx(img.DistTopEmu);
                break;

            case ImageWrapMode.FloatRight:
                styles["float"]       = "right";
                styles["margin-left"] = Dpx(img.DistLeftEmu);
                styles["margin-top"]  = Dpx(img.DistTopEmu);
                break;

            case ImageWrapMode.TopAndBottom:
                styles["display"]       = "block";
                styles["clear"]         = "both";
                styles["margin-top"]    = Dpx(img.DistTopEmu);
                styles["margin-bottom"] = Dpx(img.DistBottomEmu);
                break;

            case ImageWrapMode.BehindText:
                styles["position"] = "absolute";
                styles["z-index"]  = "-1";
                if (img.HorizontalOffsetEmu is not null) styles["left"] = $"{EmuToPx(img.HorizontalOffsetEmu.Value)}px";
                if (img.VerticalOffsetEmu is not null)   styles["top"]  = $"{EmuToPx(img.VerticalOffsetEmu.Value)}px";
                break;

            case ImageWrapMode.InFrontOfText:
                styles["position"] = "absolute";
                styles["z-index"]  = "10";
                if (img.HorizontalOffsetEmu is not null) styles["left"] = $"{EmuToPx(img.HorizontalOffsetEmu.Value)}px";
                if (img.VerticalOffsetEmu is not null)   styles["top"]  = $"{EmuToPx(img.VerticalOffsetEmu.Value)}px";
                break;
        }
    }

    private static Dictionary<string, string>? BuildRunStyles(RunProperties props)
    {
        var styles = new Dictionary<string, string>();

        if (props.Bold)
            styles["font-weight"] = "bold";

        if (props.Italic)
            styles["font-style"] = "italic";

        if (props.Underline is not null && props.Underline != UnderlineType.None)
        {
            styles["text-decoration"] = props.Underline.Value switch
            {
                UnderlineType.Double => "underline double",
                UnderlineType.Dotted => "underline dotted",
                UnderlineType.Dash => "underline dashed",
                UnderlineType.Wave => "underline wavy",
                _ => "underline"
            };
        }

        if (props.Strikethrough)
        {
            // Combine with existing text-decoration if present
            if (styles.ContainsKey("text-decoration"))
                styles["text-decoration"] += " line-through";
            else
                styles["text-decoration"] = "line-through";
        }

        if (props.FontFamily is not null)
            styles["font-family"] = $"'{props.FontFamily}', sans-serif";

        if (props.FontSize is not null)
        {
            // FontSize is in half-points: 24 = 12pt
            var pt = props.FontSize.Value / 2.0;
            styles["font-size"] = $"{pt}pt";
        }

        if (props.Color is not null)
            styles["color"] = $"#{props.Color}";

        if (props.Highlight is not null && props.Highlight != HighlightColor.None)
            styles["background-color"] = MapHighlightToColor(props.Highlight.Value);

        if (props.VerticalAlign is not null && props.VerticalAlign != VerticalAlignType.Baseline)
        {
            styles["vertical-align"] = props.VerticalAlign.Value switch
            {
                VerticalAlignType.Superscript => "super",
                VerticalAlignType.Subscript => "sub",
                _ => "baseline"
            };
            styles["font-size"] = "smaller";
        }

        if (props.CharacterSpacing is not null && props.CharacterSpacing != 0)
        {
            // CharacterSpacing is in twentieths of a point; CSS letter-spacing uses pt
            var pt = props.CharacterSpacing.Value / 20.0;
            styles["letter-spacing"] = $"{pt:F2}pt";
        }

        return styles.Count > 0 ? styles : null;
    }

    private static RenderNode BuildHyperlink(Hyperlink link)
    {
        var node = new RenderNode
        {
            Id = link.Id,
            Tag = "a",
            Attrs = new Dictionary<string, string> { ["href"] = link.Url },
            Children = []
        };

        if (link.Tooltip is not null)
            node.Attrs["title"] = link.Tooltip;

        foreach (var run in link.Children)
        {
            node.Children.AddRange(BuildRun(run));
        }

        return node;
    }

    internal static RenderNode BuildTable(Table table)
    {
        var node = new RenderNode
        {
            Id = table.Id,
            Tag = "table",
            Styles = BuildTableStyles(table.Properties),
            Children = []
        };

        if (table.GridColumnWidths.Count > 0)
        {
            var colgroup = new RenderNode
            {
                Id = $"{table.Id}-colgroup",
                Tag = "colgroup",
                Children = table.GridColumnWidths
                    .Select((w, i) => new RenderNode
                    {
                        Id = $"{table.Id}-col-{i}",
                        Tag = "col",
                        Styles = new Dictionary<string, string> { ["width"] = TwipsToPx(w) }
                    })
                    .ToList()
            };
            node.Children.Add(colgroup);
        }

        foreach (var row in table.Rows)
        {
            node.Children.Add(BuildTableRow(row));
        }

        return node;
    }

    private static Dictionary<string, string>? BuildTableStyles(TableProperties props)
    {
        var styles = new Dictionary<string, string>();

        if (props.CellSpacing is > 0)
        {
            styles["border-collapse"] = "separate";
            styles["border-spacing"] = TwipsToPx(props.CellSpacing.Value);
        }
        else if (props.HasBorders)
        {
            styles["border-collapse"] = "collapse";
        }

        if (props.Width is not null)
        {
            styles["width"] = TwipsToPx(props.Width.Value);
            styles["table-layout"] = "fixed";
        }

        return styles.Count > 0 ? styles : null;
    }

    private static RenderNode BuildTableRow(TableRow row)
    {
        var node = new RenderNode
        {
            Id = row.Id,
            Tag = "tr",
            Children = []
        };

        if (row.Properties.Height is not null)
        {
            node.Styles = new Dictionary<string, string>
            {
                ["height"] = TwipsToPx(row.Properties.Height.Value)
            };
        }

        foreach (var cell in row.Cells)
        {
            node.Children.Add(BuildTableCell(cell));
        }

        return node;
    }

    private static RenderNode BuildTableCell(TableCell cell)
    {
        var styles = new Dictionary<string, string>();

        if (cell.Properties.Width is not null)
            styles["width"] = TwipsToPx(cell.Properties.Width.Value);

        if (cell.Properties.VerticalAlignment is not null)
        {
            styles["vertical-align"] = cell.Properties.VerticalAlignment.Value switch
            {
                TableVerticalAlignment.Center => "middle",
                TableVerticalAlignment.Bottom => "bottom",
                _ => "top"
            };
        }

        if (cell.Properties.Shading is not null && cell.Properties.Shading != "auto")
            styles["background-color"] = $"#{cell.Properties.Shading}";

        if (cell.Properties.Borders is not null)
        {
            var b = cell.Properties.Borders;
            if (b.Top    != null) styles["border-top"]    = MapBorderToCss(b.Top);
            if (b.Bottom != null) styles["border-bottom"] = MapBorderToCss(b.Bottom);
            if (b.Left   != null) styles["border-left"]   = MapBorderToCss(b.Left);
            if (b.Right  != null) styles["border-right"]  = MapBorderToCss(b.Right);
        }

        // Use explicit padding or Word's built-in default: 108 twips left/right, 72 twips top/bottom
        // (108 twips = 7.2px, 72 twips = 4.8px at 96 DPI)
        var pad = cell.Properties.Padding ?? new CellPadding { Top = 72, Bottom = 72, Left = 108, Right = 108 };
        {
            static string Px(int twips) => $"{twips / 15.0:F1}px";
            styles["padding"] = $"{Px(pad.Top)} {Px(pad.Right)} {Px(pad.Bottom)} {Px(pad.Left)}";
        }

        var node = new RenderNode
        {
            Id = cell.Id,
            Tag = "td",
            Styles = styles.Count > 0 ? styles : null,
            Attrs = BuildTableCellAttrs(cell.Properties),
            Children = []
        };

        for (int i = 0; i < cell.Children.Count; i++)
        {
            var blockNode = BuildBlock(cell.Children[i]);
            if (i == 0)
                (blockNode.Styles ??= new())["margin-top"] = "0";
            node.Children.Add(blockNode);
        }

        return node;
    }

    private static Dictionary<string, string>? BuildTableCellAttrs(TableCellProperties props)
    {
        var attrs = new Dictionary<string, string>();

        if (props.GridSpan is not null && props.GridSpan > 1)
            attrs["colspan"] = props.GridSpan.Value.ToString();

        return attrs.Count > 0 ? attrs : null;
    }

    /// <summary>Convert twips to integer px (1 twip = 1/1440 inch, at 96dpi: 1 twip ≈ 0.0667px)</summary>
    private static int TwipsToPxInt(int twips)
    {
        return (int)Math.Round(twips * 96.0 / 1440.0, MidpointRounding.AwayFromZero);
    }

    /// <summary>Convert twips to px CSS string</summary>
    private static string TwipsToPx(int twips)
    {
        return $"{TwipsToPxInt(twips)}px";
    }

    /// <summary>Convert EMU to integer px (914400 EMU = 1 inch, at 96dpi)</summary>
    private static int EmuToPx(long emu)
    {
        return (int)Math.Round(emu * 96.0 / 914400.0);
    }

    private static string MapBorderToCss(CellBorderModel border)
    {
        if (border.Style == CellBorderStyle.None) return "none";
        var widthPx = Math.Max(1.0, border.Size / 8.0 * 1.333);
        var styleStr = border.Style switch
        {
            CellBorderStyle.Double => "double",
            CellBorderStyle.Dotted => "dotted",
            CellBorderStyle.Dashed => "dashed",
            _ => "solid"
        };
        var colorStr = border.Color is "auto" or null or "" ? "#000000" : $"#{border.Color}";
        return $"{widthPx:F1}px {styleStr} {colorStr}";
    }

    private static string MapHighlightToColor(HighlightColor color) => color switch
    {
        HighlightColor.Yellow => "#FFFF00",
        HighlightColor.Green => "#00FF00",
        HighlightColor.Cyan => "#00FFFF",
        HighlightColor.Magenta => "#FF00FF",
        HighlightColor.Blue => "#0000FF",
        HighlightColor.Red => "#FF0000",
        HighlightColor.DarkBlue => "#00008B",
        HighlightColor.DarkCyan => "#008B8B",
        HighlightColor.DarkGreen => "#006400",
        HighlightColor.DarkMagenta => "#8B008B",
        HighlightColor.DarkRed => "#8B0000",
        HighlightColor.DarkYellow => "#808000",
        HighlightColor.DarkGray => "#A9A9A9",
        HighlightColor.LightGray => "#D3D3D3",
        HighlightColor.Black => "#000000",
        HighlightColor.White => "#FFFFFF",
        _ => "transparent"
    };
}