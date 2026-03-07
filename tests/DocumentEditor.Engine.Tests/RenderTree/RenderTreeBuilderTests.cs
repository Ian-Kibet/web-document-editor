using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.RenderTree;

namespace DocumentEditor.Engine.Tests.RenderTree;

public class RenderTreeBuilderTests
{
    private readonly RenderTreeBuilder _builder = new();

    /// <summary>Get the first block from the render tree (navigating through section wrapper)</summary>
    private static RenderNode FirstBlock(List<RenderNode> nodes) => nodes[0].Children![0];

    /// <summary>Get the Nth block from the first section</summary>
    private static RenderNode BlockAt(List<RenderNode> nodes, int index) => nodes[0].Children![index];

    /// <summary>Get the first tr from a table node, skipping any colgroup</summary>
    private static RenderNode FirstTableRow(RenderNode tableNode) =>
        tableNode.Children!.First(c => c.Tag == "tr");

    // ─── Section wrapper ────────────────────────────────────────

    [Fact]
    public void Build_SingleParagraph_WrappedInSection()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Hello"));
        var nodes = _builder.Build(doc);

        Assert.Single(nodes);
        Assert.Equal("section", nodes[0].Tag);
        Assert.NotNull(nodes[0].Children);
        Assert.Single(nodes[0].Children!);
    }

    // ─── Paragraph tag resolution ─────────────────────────────────

    [Fact]
    public void Build_NormalParagraph_ProducesPTag()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Hello"));
        var nodes = _builder.Build(doc);

        Assert.Single(nodes);
        var block = FirstBlock(nodes);
        Assert.Equal("p", block.Tag);
    }

    [Theory]
    [InlineData("Heading1", "h1")]
    [InlineData("Heading2", "h2")]
    [InlineData("Heading3", "h3")]
    [InlineData("Heading4", "h4")]
    public void Build_HeadingStyles_ResolveToCorrectTags(string style, string expectedTag)
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Heading", new ParagraphProperties { Style = style })
        );
        var nodes = _builder.Build(doc);

        Assert.Single(nodes);
        var block = FirstBlock(nodes);
        Assert.Equal(expectedTag, block.Tag);
    }

    [Fact]
    public void Build_UnknownStyle_FallsToPTag()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { Style = "CustomStyle" })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.Equal("p", block.Tag);
    }

    // ─── Paragraph node ID ────────────────────────────────────────

    [Fact]
    public void Build_ParagraphNode_HasCorrectId()
    {
        var para = DocFactory.CreateParagraph("Test");
        var doc = DocFactory.CreateDocument(para);
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.Equal(((Paragraph)doc.Children[0]).Id, block.Id);
    }

    // ─── Paragraph styles ─────────────────────────────────────────

    [Theory]
    [InlineData(Alignment.Left, "left")]
    [InlineData(Alignment.Center, "center")]
    [InlineData(Alignment.Right, "right")]
    [InlineData(Alignment.Both, "justify")]
    public void Build_ParagraphAlignment_MapsToTextAlign(Alignment alignment, string expected)
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { Alignment = alignment })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal(expected, block.Styles!["text-align"]);
    }

    [Fact]
    public void Build_ParagraphIndentLeft_MapsToMarginLeft()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { IndentLeft = 720 })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal("48px", block.Styles!["margin-left"]); // 720 * 96 / 1440 = 48
    }

    [Fact]
    public void Build_ParagraphFirstLineIndent_MapsToTextIndent()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { IndentFirstLine = 360 })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal("24px", block.Styles!["text-indent"]); // 360 * 96 / 1440 = 24
    }

    [Fact]
    public void Build_ParagraphHangingIndent_MapsToNegativeTextIndent()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { IndentHanging = 360 })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal("-24px", block.Styles!["text-indent"]);
    }

    [Fact]
    public void Build_ParagraphSpacing_MapsToMargins()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties
            {
                SpaceBefore = 240,
                SpaceAfter = 120
            })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal("16px", block.Styles!["margin-top"]);  // 240 * 96 / 1440 = 16
        Assert.Equal("8px", block.Styles!["margin-bottom"]); // 120 * 96 / 1440 = 8
    }

    [Fact]
    public void Build_ContextualSpacing_SuppressesMarginBottomBetweenSameStyleParagraphs()
    {
        var props = new ParagraphProperties
        {
            Style = "Normal",
            SpaceAfter = 160,
            ContextualSpacing = true
        };
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("First",  props),
            DocFactory.CreateParagraph("Second", props)
        );
        var nodes = _builder.Build(doc);

        var first  = BlockAt(nodes, 0);
        var second = BlockAt(nodes, 1);

        // First paragraph: same style follows → margin-bottom suppressed
        Assert.Null(first.Styles?.GetValueOrDefault("margin-bottom"));
        // Second paragraph: nothing follows → margin-bottom emitted
        Assert.Equal("11px", second.Styles!["margin-bottom"]); // 160 * 96 / 1440 ≈ 11
    }

    [Fact]
    public void Build_ContextualSpacing_NotSuppressedWhenNextStyleDiffers()
    {
        var normalProps = new ParagraphProperties
        {
            Style = "Normal",
            SpaceAfter = 160,
            ContextualSpacing = true
        };
        var headingProps = new ParagraphProperties
        {
            Style = "Heading1",
            SpaceAfter = 160,
            ContextualSpacing = true
        };
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Body",    normalProps),
            DocFactory.CreateParagraph("Heading", headingProps)
        );
        var nodes = _builder.Build(doc);

        var first = BlockAt(nodes, 0);

        // Different style follows → margin-bottom must NOT be suppressed
        Assert.Equal("11px", first.Styles!["margin-bottom"]);
    }

    [Fact]
    public void Build_ParagraphLineSpacing_MapsToLineHeight()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { LineSpacing = 360 })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal("1.50", block.Styles!["line-height"]); // 360 / 240.0 = 1.5 → "1.50" (raw ratio, N > 276)
    }

    [Fact]
    public void Build_ParagraphLineSpacing_NearSingle_AppliesCorrection()
    {
        // w:line=259 (Word default body spacing): N/240 = 1.079 ≤ 1.15 → ×1.15 → "1.24"
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { LineSpacing = 259 })
        );
        var nodes = _builder.Build(doc);
        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal("1.24", block.Styles!["line-height"]);
    }

    [Fact]
    public void Build_ParagraphLineSpacing_Double_UsesRawRatio()
    {
        // w:line=480 = 2× spacing: N/240 = 2.0 > 1.15 → raw ratio → "2.00"
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { LineSpacing = 480 })
        );
        var nodes = _builder.Build(doc);
        var block = FirstBlock(nodes);
        Assert.NotNull(block.Styles);
        Assert.Equal("2.00", block.Styles!["line-height"]);
    }

    [Fact]
    public void Build_ParagraphNoStyles_ReturnsNullStyles()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Plain text"));
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.Null(block.Styles);
    }

    // ─── Paragraph attributes (lists, page breaks) ────────────────

    [Fact]
    public void Build_BulletList_SetsDataListTypeAttr()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Item", new ParagraphProperties { NumberingId = 1, NumberingLevel = 0, NumberingFormat = "bullet" })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Attrs);
        Assert.Equal("bullet", block.Attrs!["data-list-type"]);
        Assert.Equal("0", block.Attrs!["data-list-level"]);
    }

    [Fact]
    public void Build_NumberedList_SetsDataListTypeAttr()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Step", new ParagraphProperties { NumberingId = 2, NumberingLevel = 1, NumberingFormat = "decimal" })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Attrs);
        Assert.Equal("numbered", block.Attrs!["data-list-type"]);
        Assert.Equal("1", block.Attrs!["data-list-level"]);
    }

    [Fact]
    public void Build_PageBreakBefore_SetsDataAttr()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text", new ParagraphProperties { PageBreakBefore = true })
        );
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.NotNull(block.Attrs);
        Assert.Equal("true", block.Attrs!["data-page-break-before"]);
    }

    [Fact]
    public void Build_NoParagraphAttrs_ReturnsNullAttrs()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Text"));
        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.Null(block.Attrs);
    }

    // ─── Run styles ───────────────────────────────────────────────

    [Fact]
    public void Build_BoldRun_HasFontWeightBold()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("bold", new RunProperties { Bold = true }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("span", span.Tag);
        Assert.Equal("bold", span.Styles!["font-weight"]);
    }

    [Fact]
    public void Build_ItalicRun_HasFontStyleItalic()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("italic", new RunProperties { Italic = true }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("italic", span.Styles!["font-style"]);
    }

    [Theory]
    [InlineData(UnderlineType.Single, "underline")]
    [InlineData(UnderlineType.Double, "underline double")]
    [InlineData(UnderlineType.Dotted, "underline dotted")]
    [InlineData(UnderlineType.Dash, "underline dashed")]
    [InlineData(UnderlineType.Wave, "underline wavy")]
    public void Build_UnderlineRun_HasCorrectTextDecoration(UnderlineType type, string expected)
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("text", new RunProperties { Underline = type }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal(expected, span.Styles!["text-decoration"]);
    }

    [Fact]
    public void Build_StrikethroughRun_HasLineThrough()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("strike", new RunProperties { Strikethrough = true }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("line-through", span.Styles!["text-decoration"]);
    }

    [Fact]
    public void Build_UnderlineAndStrikethrough_CombinesTextDecoration()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("both", new RunProperties
        {
            Underline = UnderlineType.Single,
            Strikethrough = true
        }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("underline line-through", span.Styles!["text-decoration"]);
    }

    [Fact]
    public void Build_FontFamily_WrapsInQuotes()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("text", new RunProperties { FontFamily = "Times New Roman" }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("'Times New Roman', sans-serif", span.Styles!["font-family"]);
    }

    [Fact]
    public void Build_FontSize_ConvertHalfPointsToPt()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("text", new RunProperties { FontSize = 28 })); // 14pt

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("14pt", span.Styles!["font-size"]);
    }

    [Fact]
    public void Build_Color_HasHashPrefix()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("text", new RunProperties { Color = "FF0000" }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("#FF0000", span.Styles!["color"]);
    }

    [Fact]
    public void Build_HighlightYellow_MapsToBackgroundColor()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("text", new RunProperties { Highlight = HighlightColor.Yellow }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("#FFFF00", span.Styles!["background-color"]);
    }

    [Theory]
    [InlineData(VerticalAlignType.Superscript, "super")]
    [InlineData(VerticalAlignType.Subscript, "sub")]
    public void Build_VerticalAlign_MapsCorrectly(VerticalAlignType align, string expected)
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("text", new RunProperties { VerticalAlign = align }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal(expected, span.Styles!["vertical-align"]);
        Assert.Equal("smaller", span.Styles!["font-size"]);
    }

    [Fact]
    public void Build_PlainRun_HasNullStyles()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("plain"));
        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Null(span.Styles);
    }

    // ─── Run text and IDs ─────────────────────────────────────────

    [Fact]
    public void Build_RunTextContent_PreservedExactly()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Hello World"));
        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("Hello World", span.Text);
    }

    [Fact]
    public void Build_RunNode_HasCorrectId()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Test"));
        var run = (Run)((Paragraph)doc.Children[0]).Children[0];
        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal(run.Id, span.Id);
    }

    // ─── Empty runs ───────────────────────────────────────────────

    [Fact]
    public void Build_EmptyRun_ProducesSpanWithEmptyText()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var emptyRun = new Run();
        emptyRun.Content.Clear();
        para.Children.Add(emptyRun);

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("span", span.Tag);
        Assert.Equal("", span.Text);
    }

    // ─── Tab content ──────────────────────────────────────────────

    [Fact]
    public void Build_TabContent_ProducesSpanWithTabChar()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var run = new Run();
        run.Content.Clear();
        run.Content.Add(new TabContent());
        para.Children.Add(run);

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Equal("span", span.Tag);
        Assert.Equal("\t", span.Text);
        Assert.NotNull(span.Attrs);
        Assert.Equal("tab", span.Attrs!["data-type"]);
    }

    // ─── Break content ────────────────────────────────────────────

    [Fact]
    public void Build_LineBreak_ProducesBrTag()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var run = new Run();
        run.Content.Clear();
        run.Content.Add(new BreakContent { BreakType = BreakType.TextWrapping });
        para.Children.Add(run);

        var nodes = _builder.Build(doc);
        var br = FirstBlock(nodes).Children![0];

        Assert.Equal("br", br.Tag);
        Assert.Null(br.Attrs); // TextWrapping breaks don't need data-break-type
    }

    [Fact]
    public void Build_PageBreak_ProducesBrTagWithDataAttr()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var run = new Run();
        run.Content.Clear();
        run.Content.Add(new BreakContent { BreakType = BreakType.Page });
        para.Children.Add(run);

        var nodes = _builder.Build(doc);
        var br = FirstBlock(nodes).Children![0];

        Assert.Equal("br", br.Tag);
        Assert.NotNull(br.Attrs);
        Assert.Equal("page", br.Attrs!["data-break-type"]);
    }

    // ─── Mixed content run ────────────────────────────────────────

    [Fact]
    public void Build_MixedContentRun_ProducesMultipleSiblingNodes()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var run = new Run();
        run.Content.Clear();
        run.Content.Add(new TextPiece { Text = "before" });
        run.Content.Add(new TabContent());
        run.Content.Add(new TextPiece { Text = "after" });
        para.Children.Add(run);

        var nodes = _builder.Build(doc);
        var children = FirstBlock(nodes).Children!;

        Assert.Equal(3, children.Count);
        Assert.Equal("before", children[0].Text);
        Assert.Equal("\t", children[1].Text);
        Assert.Equal("after", children[2].Text);
        // All share the same run ID
        Assert.Equal(run.Id, children[0].Id);
        Assert.Equal(run.Id, children[1].Id);
        Assert.Equal(run.Id, children[2].Id);
    }

    // ─── Hyperlinks ───────────────────────────────────────────────

    [Fact]
    public void Build_Hyperlink_ProducesATag()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var link = DocFactory.CreateHyperlink("https://example.com", "Click here");
        para.Children.Add(link);

        var nodes = _builder.Build(doc);
        var aNode = FirstBlock(nodes).Children![0];

        Assert.Equal("a", aNode.Tag);
        Assert.Equal(link.Id, aNode.Id);
        Assert.NotNull(aNode.Attrs);
        Assert.Equal("https://example.com", aNode.Attrs!["href"]);
    }

    [Fact]
    public void Build_HyperlinkWithTooltip_SetsTitle()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var link = DocFactory.CreateHyperlink("https://example.com", "Click");
        link.Tooltip = "Example Site";
        para.Children.Add(link);

        var nodes = _builder.Build(doc);
        var aNode = FirstBlock(nodes).Children![0];

        Assert.Equal("Example Site", aNode.Attrs!["title"]);
    }

    [Fact]
    public void Build_HyperlinkWithRuns_HasSpanChildren()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var link = DocFactory.CreateHyperlink("https://example.com", "Click here");
        para.Children.Add(link);

        var nodes = _builder.Build(doc);
        var aNode = FirstBlock(nodes).Children![0];

        Assert.NotNull(aNode.Children);
        Assert.Single(aNode.Children);
        Assert.Equal("span", aNode.Children[0].Tag);
        Assert.Equal("Click here", aNode.Children[0].Text);
    }

    // ─── Tables ───────────────────────────────────────────────────

    [Fact]
    public void Build_SimpleTable_ProducesCorrectStructure()
    {
        var table = DocFactory.CreateTable(2, 3);
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);

        Assert.Single(nodes); // 1 section
        var tableNode = FirstBlock(nodes);
        Assert.Equal("table", tableNode.Tag);
        var rows = tableNode.Children!.Where(c => c.Tag == "tr").ToList();
        Assert.Equal(2, rows.Count); // 2 rows
        Assert.All(rows, row =>
        {
            Assert.Equal("tr", row.Tag);
            Assert.Equal(3, row.Children!.Count); // 3 cells
            Assert.All(row.Children!, cell => Assert.Equal("td", cell.Tag));
        });
    }

    [Fact]
    public void Build_TableWithBorders_HasBorderCollapse()
    {
        var table = DocFactory.CreateTable(1, 1);
        table.Properties.HasBorders = true;
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);

        var tableNode = FirstBlock(nodes);
        Assert.NotNull(tableNode.Styles);
        Assert.Equal("collapse", tableNode.Styles!["border-collapse"]);
    }

    [Fact]
    public void Build_TableCellWithWidth_HasWidthStyle()
    {
        var table = DocFactory.CreateTable(1, 1);
        table.Rows[0].Cells[0].Properties.Width = 4680;
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);
        var cell = FirstTableRow(FirstBlock(nodes)).Children![0]; // table > tr > td

        Assert.NotNull(cell.Styles);
        Assert.Contains("width", cell.Styles!.Keys);
    }

    [Fact]
    public void Build_TableCellWithGridSpan_HasColspanAttr()
    {
        var table = DocFactory.CreateTable(1, 2);
        table.Rows[0].Cells[0].Properties.GridSpan = 2;
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);
        var cell = FirstTableRow(FirstBlock(nodes)).Children![0];

        Assert.NotNull(cell.Attrs);
        Assert.Equal("2", cell.Attrs!["colspan"]);
    }

    [Fact]
    public void Build_TableCellWithShading_HasBackgroundColor()
    {
        var table = DocFactory.CreateTable(1, 1);
        table.Rows[0].Cells[0].Properties.Shading = "FFFF00";
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);
        var cell = FirstTableRow(FirstBlock(nodes)).Children![0];

        Assert.NotNull(cell.Styles);
        Assert.Equal("#FFFF00", cell.Styles!["background-color"]);
    }

    [Theory]
    [InlineData(TableVerticalAlignment.Center, "middle")]
    [InlineData(TableVerticalAlignment.Bottom, "bottom")]
    [InlineData(TableVerticalAlignment.Top, "top")]
    public void Build_TableCellVerticalAlignment_MapsCorrectly(
        TableVerticalAlignment align, string expected)
    {
        var table = DocFactory.CreateTable(1, 1);
        table.Rows[0].Cells[0].Properties.VerticalAlignment = align;
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);
        var cell = FirstTableRow(FirstBlock(nodes)).Children![0];

        Assert.NotNull(cell.Styles);
        Assert.Equal(expected, cell.Styles!["vertical-align"]);
    }

    [Fact]
    public void Build_TableRowWithHeight_HasHeightStyle()
    {
        var table = DocFactory.CreateTable(1, 1);
        table.Rows[0].Properties.Height = 720;
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);
        var row = FirstTableRow(FirstBlock(nodes));

        Assert.NotNull(row.Styles);
        Assert.Contains("height", row.Styles!.Keys);
    }

    [Fact]
    public void Build_TableCellContainsParagraphs()
    {
        var table = DocFactory.CreateTable(1, 1);
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Clear();
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Add(DocFactory.CreateRun("Cell text"));
        var doc = DocFactory.CreateDocument(table);

        var nodes = _builder.Build(doc);
        var cell = FirstTableRow(FirstBlock(nodes)).Children![0]; // table > tr > td

        Assert.NotNull(cell.Children);
        Assert.Single(cell.Children);
        Assert.Equal("p", cell.Children[0].Tag); // Cell contains a paragraph
    }

    // ─── Multiple blocks ──────────────────────────────────────────

    [Fact]
    public void Build_MultipleParagraphs_ProducesMultipleNodes()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("First"),
            DocFactory.CreateParagraph("Second"),
            DocFactory.CreateParagraph("Third")
        );

        var nodes = _builder.Build(doc);

        Assert.Single(nodes); // 1 section
        var sectionChildren = nodes[0].Children!;
        Assert.Equal(3, sectionChildren.Count);
        Assert.All(sectionChildren, n => Assert.Equal("p", n.Tag));
    }

    [Fact]
    public void Build_MixedBlocks_ParagraphsAndTable()
    {
        var table = DocFactory.CreateTable(1, 1);
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Before table"),
            table,
            DocFactory.CreateParagraph("After table")
        );

        var nodes = _builder.Build(doc);

        Assert.Single(nodes); // 1 section
        var sectionChildren = nodes[0].Children!;
        Assert.Equal(3, sectionChildren.Count);
        Assert.Equal("p", sectionChildren[0].Tag);
        Assert.Equal("table", sectionChildren[1].Tag);
        Assert.Equal("p", sectionChildren[2].Tag);
    }

    // ─── Multiple runs in a paragraph ─────────────────────────────

    [Fact]
    public void Build_MultipleRuns_AllRenderedAsSpans()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("plain "));
        para.Children.Add(DocFactory.CreateRun("bold", new RunProperties { Bold = true }));
        para.Children.Add(DocFactory.CreateRun(" and "));
        para.Children.Add(DocFactory.CreateRun("italic", new RunProperties { Italic = true }));

        var nodes = _builder.Build(doc);

        var block = FirstBlock(nodes);
        Assert.Equal(4, block.Children!.Count);
        Assert.All(block.Children!, c => Assert.Equal("span", c.Tag));
        Assert.Equal("plain ", block.Children![0].Text);
        Assert.Equal("bold", block.Children![1].Text);
        Assert.Equal(" and ", block.Children![2].Text);
        Assert.Equal("italic", block.Children![3].Text);
    }

    // ─── Empty document ───────────────────────────────────────────

    [Fact]
    public void Build_EmptyDocument_ReturnsSingleEmptySection()
    {
        var doc = new DocxDocument();
        doc.Children.Clear();

        var nodes = _builder.Build(doc);

        // Even an empty document produces a section wrapper (the final/default section)
        Assert.Single(nodes);
        Assert.Equal("section", nodes[0].Tag);
        Assert.NotNull(nodes[0].Children);
        Assert.Empty(nodes[0].Children!);
    }

    // ─── UnderlineType.None is treated as no underline ────────────

    [Fact]
    public void Build_UnderlineNone_DoesNotSetTextDecoration()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("text", new RunProperties { Underline = UnderlineType.None }));

        var nodes = _builder.Build(doc);
        var span = FirstBlock(nodes).Children![0];

        Assert.Null(span.Styles);
    }
}
