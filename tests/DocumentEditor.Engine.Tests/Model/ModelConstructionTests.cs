using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Tests.Model;

public class ModelConstructionTests
{
    [Fact]
    public void DocumentDefaults_UsLetterDimensions()
    {
        var doc = new DocxDocument();
        Assert.Equal(12240, doc.Properties.PageWidth);
        Assert.Equal(15840, doc.Properties.PageHeight);
        Assert.Equal(1440, doc.Properties.MarginTop);
        Assert.Equal(1440, doc.Properties.MarginBottom);
        Assert.Equal(1440, doc.Properties.MarginLeft);
        Assert.Equal(1440, doc.Properties.MarginRight);
    }

    [Fact]
    public void Document_NodeType_IsDocument()
    {
        var doc = new DocxDocument();
        Assert.Equal("document", doc.NodeType);
    }

    [Fact]
    public void Paragraph_AlwaysHasOneRun()
    {
        var para = new Paragraph();
        Assert.Single(para.Children);
        Assert.IsType<Run>(para.Children[0]);
    }

    [Fact]
    public void Paragraph_NodeType_IsParagraph()
    {
        var para = new Paragraph();
        Assert.Equal("paragraph", para.NodeType);
    }

    [Fact]
    public void Run_UniqueIds()
    {
        var run1 = new Run();
        var run2 = new Run();
        Assert.NotEqual(run1.Id, run2.Id);
    }

    [Fact]
    public void Run_TextProperty_GetsFirstTextPiece()
    {
        var run = DocFactory.CreateRun("hello");
        Assert.Equal("hello", run.Text);
    }

    [Fact]
    public void Run_TextProperty_SetsFirstTextPiece()
    {
        var run = DocFactory.CreateRun("hello");
        run.Text = "world";
        Assert.Equal("world", run.Text);
        Assert.Single(run.Content);
    }

    [Fact]
    public void Run_TextProperty_InsertsTextPieceIfNone()
    {
        var run = new Run { Content = [new TabContent()] };
        run.Text = "hello";
        Assert.Equal("hello", run.Text);
        Assert.Equal(2, run.Content.Count);
        Assert.IsType<TextPiece>(run.Content[0]);
    }

    [Fact]
    public void Table_StructureIsCorrect()
    {
        var table = DocFactory.CreateTable(2, 3);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(3, table.Rows[0].Cells.Count);
        Assert.Equal(3, table.GridColumnWidths.Count);
        Assert.Single(table.Rows[0].Cells[0].Children);
        Assert.IsType<Paragraph>(table.Rows[0].Cells[0].Children[0]);
    }

    [Fact]
    public void Table_EvenColumnWidths()
    {
        var table = DocFactory.CreateTable(1, 3);
        var expectedWidth = 9360 / 3;
        Assert.All(table.GridColumnWidths, w => Assert.Equal(expectedWidth, w));
        Assert.All(table.Rows[0].Cells, c => Assert.Equal(expectedWidth, c.Properties.Width));
    }

    [Fact]
    public void Table_NodeTypes()
    {
        var table = DocFactory.CreateTable(1, 1);
        Assert.Equal("table", table.NodeType);
        Assert.Equal("tableRow", table.Rows[0].NodeType);
        Assert.Equal("tableCell", table.Rows[0].Cells[0].NodeType);
    }

    [Fact]
    public void Hyperlink_DefaultsBlueUnderlined()
    {
        var link = DocFactory.CreateHyperlink("https://example.com", "Click");
        Assert.Equal("https://example.com", link.Url);
        Assert.Equal("hyperlink", link.NodeType);
        Assert.Single(link.Children);
        Assert.Equal("Click", link.Children[0].Text);
        Assert.Equal("0563C1", link.Children[0].Properties.Color);
        Assert.Equal(UnderlineType.Single, link.Children[0].Properties.Underline);
    }

    [Fact]
    public void DocFactory_CreateDocument_WithChildren()
    {
        var para = DocFactory.CreateParagraph("Hello");
        var table = DocFactory.CreateTable(1, 1);
        var doc = DocFactory.CreateDocument(para, table);
        Assert.Equal(2, doc.Children.Count);
        Assert.IsType<Paragraph>(doc.Children[0]);
        Assert.IsType<Table>(doc.Children[1]);
    }

    [Fact]
    public void DocFactory_CreateParagraph_EnforcesInvariant()
    {
        var para = DocFactory.CreateParagraph();
        Assert.Single(para.Children);
        Assert.IsType<Run>(para.Children[0]);
    }

    [Fact]
    public void DocFactory_CreateParagraph_WithText()
    {
        var para = DocFactory.CreateParagraph("Hello world");
        var run = Assert.IsType<Run>(para.Children[0]);
        Assert.Equal("Hello world", run.Text);
    }

    [Fact]
    public void DocFactory_CreateParagraph_WithProperties()
    {
        var props = new ParagraphProperties { Alignment = Alignment.Center };
        var para = DocFactory.CreateParagraph("text", props);
        Assert.Equal(Alignment.Center, para.Properties.Alignment);
    }

    [Fact]
    public void DocFactory_CreateRun_WithProperties()
    {
        var props = new RunProperties { Bold = true, FontSize = 48 };
        var run = DocFactory.CreateRun("bold text", props);
        Assert.Equal("bold text", run.Text);
        Assert.True(run.Properties.Bold);
        Assert.Equal(48, run.Properties.FontSize);
    }

    [Fact]
    public void IdGen_UniqueIds_10k()
    {
        var ids = new HashSet<string>();
        for (var i = 0; i < 10_000; i++)
            ids.Add(IdGen.Next());
        Assert.Equal(10_000, ids.Count);
    }

    [Fact]
    public void IdGen_Length_Is12()
    {
        var id = IdGen.Next();
        Assert.Equal(12, id.Length);
    }

    [Fact]
    public void IdGen_IsHexOnly()
    {
        var id = IdGen.Next();
        Assert.Matches("^[0-9a-f]{12}$", id);
    }

    [Fact]
    public void RunProperties_ValueEquals_SameValues()
    {
        var a = new RunProperties { Bold = true, Italic = true, FontSize = 24, Color = "FF0000" };
        var b = new RunProperties { Bold = true, Italic = true, FontSize = 24, Color = "FF0000" };
        Assert.True(a.ValueEquals(b));
    }

    [Fact]
    public void RunProperties_ValueEquals_DifferentValues()
    {
        var a = new RunProperties { Bold = true };
        var b = new RunProperties { Bold = false };
        Assert.False(a.ValueEquals(b));
    }

    [Fact]
    public void RunProperties_ValueEquals_NullIsFalse()
    {
        var a = new RunProperties();
        Assert.False(a.ValueEquals(null));
    }

    [Fact]
    public void RunProperties_ValueEquals_AllFields()
    {
        var a = new RunProperties
        {
            Bold = true,
            Italic = true,
            Underline = UnderlineType.Double,
            Strikethrough = true,
            FontFamily = "Arial",
            FontSize = 24,
            Color = "00FF00",
            Highlight = HighlightColor.Yellow,
            VerticalAlign = VerticalAlignType.Superscript
        };
        var b = new RunProperties
        {
            Bold = true,
            Italic = true,
            Underline = UnderlineType.Double,
            Strikethrough = true,
            FontFamily = "Arial",
            FontSize = 24,
            Color = "00FF00",
            Highlight = HighlightColor.Yellow,
            VerticalAlign = VerticalAlignType.Superscript
        };
        Assert.True(a.ValueEquals(b));

        b.Highlight = HighlightColor.Green;
        Assert.False(a.ValueEquals(b));
    }

    [Fact]
    public void TextContent_Types()
    {
        Assert.Equal("text", new TextPiece().ContentType);
        Assert.Equal("tab", new TabContent().ContentType);
        Assert.Equal("break", new BreakContent().ContentType);
    }

    [Fact]
    public void BreakContent_DefaultType()
    {
        var br = new BreakContent();
        Assert.Equal(BreakType.TextWrapping, br.BreakType);
    }

    [Fact]
    public void ParagraphProperties_NullableDefaults()
    {
        var props = new ParagraphProperties();
        Assert.Null(props.Style);
        Assert.Null(props.Alignment);
        Assert.Null(props.IndentLeft);
        Assert.Null(props.NumberingId);
        Assert.Null(props.SpaceBefore);
        Assert.Null(props.LineSpacing);
        Assert.False(props.KeepNext);
        Assert.False(props.PageBreakBefore);
    }
}
