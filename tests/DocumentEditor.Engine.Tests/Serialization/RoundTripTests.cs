using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Serialization;

namespace DocumentEditor.Engine.Tests.Serialization;

public class RoundTripTests
{
    private readonly DocxExporter _exporter = new();
    private readonly DocxImporter _importer = new();

    private DocxDocument RoundTrip(DocxDocument doc)
    {
        var bytes = _exporter.Export(doc);
        return _importer.Import(bytes);
    }

    [Fact]
    public void RoundTrip_SimpleText()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Hello, World!")
        );

        var result = RoundTrip(doc);

        Assert.Single(result.Children);
        var para = Assert.IsType<Paragraph>(result.Children[0]);
        var run = Assert.IsType<Run>(para.Children[0]);
        Assert.Equal("Hello, World!", run.Text);
    }

    [Fact]
    public void RoundTrip_MultipleParagraphs()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("First paragraph"),
            DocFactory.CreateParagraph("Second paragraph"),
            DocFactory.CreateParagraph("Third paragraph")
        );

        var result = RoundTrip(doc);

        Assert.Equal(3, result.Children.Count);
        var p1 = Assert.IsType<Paragraph>(result.Children[0]);
        var p2 = Assert.IsType<Paragraph>(result.Children[1]);
        var p3 = Assert.IsType<Paragraph>(result.Children[2]);
        Assert.Equal("First paragraph", ((Run)p1.Children[0]).Text);
        Assert.Equal("Second paragraph", ((Run)p2.Children[0]).Text);
        Assert.Equal("Third paragraph", ((Run)p3.Children[0]).Text);
    }

    [Fact]
    public void RoundTrip_BoldItalicStrikethrough()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph()
        );
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("bold", new RunProperties { Bold = true }));
        para.Children.Add(DocFactory.CreateRun("italic", new RunProperties { Italic = true }));
        para.Children.Add(DocFactory.CreateRun("strike", new RunProperties { Strikethrough = true }));

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal(3, rp.Children.Count);

        var r1 = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal("bold", r1.Text);
        Assert.True(r1.Properties.Bold);
        Assert.False(r1.Properties.Italic);

        var r2 = Assert.IsType<Run>(rp.Children[1]);
        Assert.Equal("italic", r2.Text);
        Assert.True(r2.Properties.Italic);
        Assert.False(r2.Properties.Bold);

        var r3 = Assert.IsType<Run>(rp.Children[2]);
        Assert.Equal("strike", r3.Text);
        Assert.True(r3.Properties.Strikethrough);
    }

    [Fact]
    public void RoundTrip_UnderlineTypes()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("single", new RunProperties { Underline = UnderlineType.Single }));
        para.Children.Add(DocFactory.CreateRun("double", new RunProperties { Underline = UnderlineType.Double }));
        para.Children.Add(DocFactory.CreateRun("wave", new RunProperties { Underline = UnderlineType.Wave }));

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        var r1 = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal(UnderlineType.Single, r1.Properties.Underline);
        var r2 = Assert.IsType<Run>(rp.Children[1]);
        Assert.Equal(UnderlineType.Double, r2.Properties.Underline);
        var r3 = Assert.IsType<Run>(rp.Children[2]);
        Assert.Equal(UnderlineType.Wave, r3.Properties.Underline);
    }

    [Fact]
    public void RoundTrip_FontProperties()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("styled", new RunProperties
        {
            FontFamily = "Times New Roman",
            FontSize = 28, // 14pt
            Color = "FF0000"
        }));

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        var run = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal("styled", run.Text);
        Assert.Equal("Times New Roman", run.Properties.FontFamily);
        Assert.Equal(28, run.Properties.FontSize);
        Assert.Equal("FF0000", run.Properties.Color);
    }

    [Fact]
    public void RoundTrip_HighlightColor()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("highlighted", new RunProperties { Highlight = HighlightColor.Yellow }));

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        var run = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal(HighlightColor.Yellow, run.Properties.Highlight);
    }

    [Fact]
    public void RoundTrip_VerticalAlign()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("super", new RunProperties { VerticalAlign = VerticalAlignType.Superscript }));
        para.Children.Add(DocFactory.CreateRun("sub", new RunProperties { VerticalAlign = VerticalAlignType.Subscript }));

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        var r1 = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal(VerticalAlignType.Superscript, r1.Properties.VerticalAlign);
        var r2 = Assert.IsType<Run>(rp.Children[1]);
        Assert.Equal(VerticalAlignType.Subscript, r2.Properties.VerticalAlign);
    }

    [Fact]
    public void RoundTrip_ParagraphStyle()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("heading", new ParagraphProperties { Style = "Heading1" }),
            DocFactory.CreateParagraph("normal")
        );

        var result = RoundTrip(doc);

        var p1 = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal("Heading1", p1.Properties.Style);
        var p2 = Assert.IsType<Paragraph>(result.Children[1]);
        Assert.Null(p2.Properties.Style);
    }

    [Fact]
    public void RoundTrip_ParagraphAlignment()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("left", new ParagraphProperties { Alignment = Alignment.Left }),
            DocFactory.CreateParagraph("center", new ParagraphProperties { Alignment = Alignment.Center }),
            DocFactory.CreateParagraph("right", new ParagraphProperties { Alignment = Alignment.Right }),
            DocFactory.CreateParagraph("justify", new ParagraphProperties { Alignment = Alignment.Both })
        );

        var result = RoundTrip(doc);

        Assert.Equal(Alignment.Left, ((Paragraph)result.Children[0]).Properties.Alignment);
        Assert.Equal(Alignment.Center, ((Paragraph)result.Children[1]).Properties.Alignment);
        Assert.Equal(Alignment.Right, ((Paragraph)result.Children[2]).Properties.Alignment);
        Assert.Equal(Alignment.Both, ((Paragraph)result.Children[3]).Properties.Alignment);
    }

    [Fact]
    public void RoundTrip_ParagraphIndentation()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("indented", new ParagraphProperties
            {
                IndentLeft = 720,
                IndentFirstLine = 360,
                IndentHanging = 180
            })
        );

        var result = RoundTrip(doc);

        var para = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal(720, para.Properties.IndentLeft);
        Assert.Equal(360, para.Properties.IndentFirstLine);
        Assert.Equal(180, para.Properties.IndentHanging);
    }

    [Fact]
    public void RoundTrip_ParagraphSpacing()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("spaced", new ParagraphProperties
            {
                SpaceBefore = 240,
                SpaceAfter = 120,
                LineSpacing = 360
            })
        );

        var result = RoundTrip(doc);

        var para = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal(240, para.Properties.SpaceBefore);
        Assert.Equal(120, para.Properties.SpaceAfter);
        Assert.Equal(360, para.Properties.LineSpacing);
    }

    [Fact]
    public void RoundTrip_KeepNextAndPageBreakBefore()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("keep", new ParagraphProperties { KeepNext = true }),
            DocFactory.CreateParagraph("break", new ParagraphProperties { PageBreakBefore = true })
        );

        var result = RoundTrip(doc);

        Assert.True(((Paragraph)result.Children[0]).Properties.KeepNext);
        Assert.True(((Paragraph)result.Children[1]).Properties.PageBreakBefore);
    }

    [Fact]
    public void RoundTrip_NumberingBulletList()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("item 1", new ParagraphProperties { NumberingId = 1, NumberingLevel = 0 }),
            DocFactory.CreateParagraph("item 2", new ParagraphProperties { NumberingId = 1, NumberingLevel = 0 }),
            DocFactory.CreateParagraph("nested", new ParagraphProperties { NumberingId = 1, NumberingLevel = 1 })
        );

        var result = RoundTrip(doc);

        var p1 = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal(1, p1.Properties.NumberingId);
        Assert.Equal(0, p1.Properties.NumberingLevel);
        var p2 = Assert.IsType<Paragraph>(result.Children[1]);
        Assert.Equal(1, p2.Properties.NumberingId);
        var p3 = Assert.IsType<Paragraph>(result.Children[2]);
        Assert.Equal(1, p3.Properties.NumberingId);
        Assert.Equal(1, p3.Properties.NumberingLevel);
    }

    [Fact]
    public void RoundTrip_NumberingNumberedList()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("step 1", new ParagraphProperties { NumberingId = 2, NumberingLevel = 0 }),
            DocFactory.CreateParagraph("step 2", new ParagraphProperties { NumberingId = 2, NumberingLevel = 0 })
        );

        var result = RoundTrip(doc);

        var p1 = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal(2, p1.Properties.NumberingId);
        Assert.Equal(0, p1.Properties.NumberingLevel);
    }

    [Fact]
    public void RoundTrip_Hyperlink()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("Click "));
        var link = DocFactory.CreateHyperlink("https://example.com", "here");
        link.Tooltip = "Example Site";
        para.Children.Add(link);
        para.Children.Add(DocFactory.CreateRun(" to visit"));

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal(3, rp.Children.Count);

        var r1 = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal("Click ", r1.Text);

        var hl = Assert.IsType<Hyperlink>(rp.Children[1]);
        Assert.Equal("https://example.com/", hl.Url);
        Assert.Equal("Example Site", hl.Tooltip);
        Assert.Single(hl.Children);
        Assert.Equal("here", hl.Children[0].Text);

        var r3 = Assert.IsType<Run>(rp.Children[2]);
        Assert.Equal(" to visit", r3.Text);
    }

    [Fact]
    public void RoundTrip_TabContent()
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

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        var rr = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal(3, rr.Content.Count);
        Assert.IsType<TextPiece>(rr.Content[0]);
        Assert.Equal("before", ((TextPiece)rr.Content[0]).Text);
        Assert.IsType<TabContent>(rr.Content[1]);
        Assert.IsType<TextPiece>(rr.Content[2]);
        Assert.Equal("after", ((TextPiece)rr.Content[2]).Text);
    }

    [Fact]
    public void RoundTrip_BreakContent()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var run = new Run();
        run.Content.Clear();
        run.Content.Add(new TextPiece { Text = "before" });
        run.Content.Add(new BreakContent { BreakType = BreakType.Page });
        run.Content.Add(new TextPiece { Text = "after" });
        para.Children.Add(run);

        var result = RoundTrip(doc);

        var rp = Assert.IsType<Paragraph>(result.Children[0]);
        var rr = Assert.IsType<Run>(rp.Children[0]);
        Assert.Equal(3, rr.Content.Count);
        Assert.IsType<TextPiece>(rr.Content[0]);
        var br = Assert.IsType<BreakContent>(rr.Content[1]);
        Assert.Equal(BreakType.Page, br.BreakType);
        Assert.IsType<TextPiece>(rr.Content[2]);
    }

    [Fact]
    public void RoundTrip_SimpleTable()
    {
        var table = DocFactory.CreateTable(2, 3);
        // Set text in first cell
        var cell00 = table.Rows[0].Cells[0];
        ((Paragraph)cell00.Children[0]).Children.Clear();
        ((Paragraph)cell00.Children[0]).Children.Add(DocFactory.CreateRun("Cell 0,0"));

        var doc = DocFactory.CreateDocument(table);

        var result = RoundTrip(doc);

        var rt = Assert.IsType<Table>(result.Children[0]);
        Assert.Equal(2, rt.Rows.Count);
        Assert.Equal(3, rt.Rows[0].Cells.Count);
        Assert.Equal(3, rt.Rows[1].Cells.Count);
        Assert.Equal(3, rt.GridColumnWidths.Count);

        var rc00 = rt.Rows[0].Cells[0];
        var rp = Assert.IsType<Paragraph>(rc00.Children[0]);
        Assert.Equal("Cell 0,0", ((Run)rp.Children[0]).Text);
    }

    [Fact]
    public void RoundTrip_TableWithMergedCells()
    {
        var table = DocFactory.CreateTable(2, 2);
        // Horizontal merge: first row spans 2 columns
        table.Rows[0].Cells[0].Properties.GridSpan = 2;

        // Vertical merge: first column spans 2 rows
        table.Rows[0].Cells[0].Properties.VerticalMerge = VerticalMergeType.Restart;
        table.Rows[1].Cells[0].Properties.VerticalMerge = VerticalMergeType.Continue;

        var doc = DocFactory.CreateDocument(table);

        var result = RoundTrip(doc);

        var rt = Assert.IsType<Table>(result.Children[0]);
        Assert.Equal(2, rt.Rows[0].Cells[0].Properties.GridSpan);
        Assert.Equal(VerticalMergeType.Restart, rt.Rows[0].Cells[0].Properties.VerticalMerge);
        Assert.Equal(VerticalMergeType.Continue, rt.Rows[1].Cells[0].Properties.VerticalMerge);
    }

    [Fact]
    public void RoundTrip_TableCellProperties()
    {
        var table = DocFactory.CreateTable(1, 1);
        var cell = table.Rows[0].Cells[0];
        cell.Properties.Width = 4680;
        cell.Properties.VerticalAlignment = TableVerticalAlignment.Center;
        cell.Properties.Shading = "FFFF00";

        var doc = DocFactory.CreateDocument(table);

        var result = RoundTrip(doc);

        var rt = Assert.IsType<Table>(result.Children[0]);
        var rc = rt.Rows[0].Cells[0];
        Assert.Equal(4680, rc.Properties.Width);
        Assert.Equal(TableVerticalAlignment.Center, rc.Properties.VerticalAlignment);
        Assert.Equal("FFFF00", rc.Properties.Shading);
    }

    [Fact]
    public void RoundTrip_TableRowProperties()
    {
        var table = DocFactory.CreateTable(2, 1);
        table.Rows[0].Properties.IsHeader = true;
        table.Rows[0].Properties.Height = 720;

        var doc = DocFactory.CreateDocument(table);

        var result = RoundTrip(doc);

        var rt = Assert.IsType<Table>(result.Children[0]);
        Assert.True(rt.Rows[0].Properties.IsHeader);
        Assert.Equal(720, rt.Rows[0].Properties.Height);
    }

    [Fact]
    public void RoundTrip_TableWithBorders()
    {
        var table = DocFactory.CreateTable(1, 1);
        table.Properties.HasBorders = true;
        table.Properties.Style = "TableGrid";

        var doc = DocFactory.CreateDocument(table);

        var result = RoundTrip(doc);

        var rt = Assert.IsType<Table>(result.Children[0]);
        Assert.True(rt.Properties.HasBorders);
        Assert.Equal("TableGrid", rt.Properties.Style);
    }

    [Fact]
    public void RoundTrip_DocumentProperties()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("test"));
        doc.Properties.PageWidth = 15840;   // 11" landscape
        doc.Properties.PageHeight = 12240;  // 8.5" landscape
        doc.Properties.MarginTop = 720;
        doc.Properties.MarginBottom = 720;
        doc.Properties.MarginLeft = 1080;
        doc.Properties.MarginRight = 1080;

        var result = RoundTrip(doc);

        Assert.Equal(15840, result.Properties.PageWidth);
        Assert.Equal(12240, result.Properties.PageHeight);
        Assert.Equal(720, result.Properties.MarginTop);
        Assert.Equal(720, result.Properties.MarginBottom);
        Assert.Equal(1080, result.Properties.MarginLeft);
        Assert.Equal(1080, result.Properties.MarginRight);
    }

    [Fact]
    public void RoundTrip_EmptyParagraph()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());

        var result = RoundTrip(doc);

        Assert.Single(result.Children);
        var para = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Single(para.Children);
        var run = Assert.IsType<Run>(para.Children[0]);
        Assert.Equal("", run.Text);
    }

    [Fact]
    public void RoundTrip_SpacePreservation()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph(" leading space"),
            DocFactory.CreateParagraph("trailing space "),
            DocFactory.CreateParagraph(" both sides ")
        );

        var result = RoundTrip(doc);

        Assert.Equal(" leading space", ((Run)((Paragraph)result.Children[0]).Children[0]).Text);
        Assert.Equal("trailing space ", ((Run)((Paragraph)result.Children[1]).Children[0]).Text);
        Assert.Equal(" both sides ", ((Run)((Paragraph)result.Children[2]).Children[0]).Text);
    }

    [Fact]
    public void RoundTrip_ComplexDocument()
    {
        // Build a document with mixed content: heading, body text, list, table, hyperlink
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Document Title", new ParagraphProperties { Style = "Heading1" }),
            DocFactory.CreateParagraph("This is body text with formatting.")
        );

        // Add a paragraph with bold and italic runs
        var mixedPara = DocFactory.CreateParagraph();
        var mp = (Paragraph)mixedPara;
        mp.Children.Clear();
        mp.Children.Add(DocFactory.CreateRun("Normal text, "));
        mp.Children.Add(DocFactory.CreateRun("bold text", new RunProperties { Bold = true }));
        mp.Children.Add(DocFactory.CreateRun(", and "));
        mp.Children.Add(DocFactory.CreateRun("italic text", new RunProperties { Italic = true }));
        mp.Children.Add(DocFactory.CreateRun("."));
        doc.Children.Add(mp);

        // Add a list item
        doc.Children.Add(DocFactory.CreateParagraph("List item one", new ParagraphProperties
        {
            NumberingId = 1,
            NumberingLevel = 0
        }));

        // Add a table
        var table = DocFactory.CreateTable(1, 2);
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Clear();
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Add(DocFactory.CreateRun("Left cell"));
        ((Paragraph)table.Rows[0].Cells[1].Children[0]).Children.Clear();
        ((Paragraph)table.Rows[0].Cells[1].Children[0]).Children.Add(DocFactory.CreateRun("Right cell"));
        doc.Children.Add(table);

        // Add a paragraph with hyperlink
        var linkPara = DocFactory.CreateParagraph();
        var lp = (Paragraph)linkPara;
        lp.Children.Clear();
        lp.Children.Add(DocFactory.CreateRun("Visit "));
        lp.Children.Add(DocFactory.CreateHyperlink("https://example.com", "Example"));
        doc.Children.Add(lp);

        var result = RoundTrip(doc);

        // Verify structure
        Assert.Equal(6, result.Children.Count);

        // Heading
        var h1 = Assert.IsType<Paragraph>(result.Children[0]);
        Assert.Equal("Heading1", h1.Properties.Style);
        Assert.Equal("Document Title", ((Run)h1.Children[0]).Text);

        // Body text
        var body = Assert.IsType<Paragraph>(result.Children[1]);
        Assert.Equal("This is body text with formatting.", ((Run)body.Children[0]).Text);

        // Mixed formatting paragraph
        var mixed = Assert.IsType<Paragraph>(result.Children[2]);
        Assert.True(mixed.Children.Count >= 3); // At least normal, bold, and italic runs

        // List item
        var listItem = Assert.IsType<Paragraph>(result.Children[3]);
        Assert.Equal(1, listItem.Properties.NumberingId);

        // Table
        var tbl = Assert.IsType<Table>(result.Children[4]);
        Assert.Single(tbl.Rows);
        Assert.Equal(2, tbl.Rows[0].Cells.Count);

        // Hyperlink paragraph
        var linkP = Assert.IsType<Paragraph>(result.Children[5]);
        Assert.Equal(2, linkP.Children.Count);
        var hl = Assert.IsType<Hyperlink>(linkP.Children[1]);
        Assert.Contains("example.com", hl.Url);
    }

    [Fact]
    public void Export_ProducesValidDocxBytes()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Test"));
        var bytes = _exporter.Export(doc);

        // .docx files are ZIP archives; first two bytes should be 'PK'
        Assert.True(bytes.Length > 4);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public void Import_ThrowsOnInvalidBytes()
    {
        var badBytes = new byte[] { 0, 1, 2, 3, 4 };
        Assert.ThrowsAny<Exception>(() => _importer.Import(badBytes));
    }
}
