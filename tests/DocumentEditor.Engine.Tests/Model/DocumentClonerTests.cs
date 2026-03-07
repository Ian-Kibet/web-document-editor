using DocumentEditor.Engine.History;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Tests.Model;

public class DocumentClonerTests
{
    [Fact]
    public void Clone_EmptyDocument()
    {
        var doc = new DocxDocument();
        var clone = DocumentCloner.Clone(doc);

        Assert.NotNull(clone);
        Assert.Empty(clone.Children);
        Assert.Equal(doc.Properties.PageWidth, clone.Properties.PageWidth);
    }

    [Fact]
    public void Clone_DocumentWithParagraphs()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("First"),
            DocFactory.CreateParagraph("Second")
        );

        var clone = DocumentCloner.Clone(doc);

        Assert.Equal(2, clone.Children.Count);
        var p1 = Assert.IsType<Paragraph>(clone.Children[0]);
        var p2 = Assert.IsType<Paragraph>(clone.Children[1]);
        var r1 = Assert.IsType<Run>(p1.Children[0]);
        var r2 = Assert.IsType<Run>(p2.Children[0]);
        Assert.Equal("First", r1.Text);
        Assert.Equal("Second", r2.Text);
    }

    [Fact]
    public void Clone_DocumentWithFormatting()
    {
        var para = DocFactory.CreateParagraph("Bold text", new ParagraphProperties
        {
            Alignment = Alignment.Center,
            SpaceBefore = 240
        });
        var run = (Run)para.Children[0];
        run.Properties = new RunProperties
        {
            Bold = true,
            FontSize = 48,
            Color = "FF0000",
            Underline = UnderlineType.Double
        };

        var doc = DocFactory.CreateDocument(para);
        var clone = DocumentCloner.Clone(doc);

        var clonedPara = Assert.IsType<Paragraph>(clone.Children[0]);
        Assert.Equal(Alignment.Center, clonedPara.Properties.Alignment);
        Assert.Equal(240, clonedPara.Properties.SpaceBefore);

        var clonedRun = Assert.IsType<Run>(clonedPara.Children[0]);
        Assert.True(clonedRun.Properties.Bold);
        Assert.Equal(48, clonedRun.Properties.FontSize);
        Assert.Equal("FF0000", clonedRun.Properties.Color);
        Assert.Equal(UnderlineType.Double, clonedRun.Properties.Underline);
    }

    [Fact]
    public void Clone_DocumentWithTable()
    {
        var table = DocFactory.CreateTable(2, 3);
        var doc = DocFactory.CreateDocument(table);

        var clone = DocumentCloner.Clone(doc);

        var clonedTable = Assert.IsType<Table>(clone.Children[0]);
        Assert.Equal(2, clonedTable.Rows.Count);
        Assert.Equal(3, clonedTable.Rows[0].Cells.Count);
        Assert.Equal(3, clonedTable.GridColumnWidths.Count);
        Assert.IsType<Paragraph>(clonedTable.Rows[0].Cells[0].Children[0]);
    }

    [Fact]
    public void Clone_DocumentWithHyperlink()
    {
        var para = new Paragraph
        {
            Children = [DocFactory.CreateHyperlink("https://example.com", "Link")]
        };
        var doc = DocFactory.CreateDocument(para);

        var clone = DocumentCloner.Clone(doc);

        var clonedPara = Assert.IsType<Paragraph>(clone.Children[0]);
        var clonedLink = Assert.IsType<Hyperlink>(clonedPara.Children[0]);
        Assert.Equal("https://example.com", clonedLink.Url);
        Assert.Equal("Link", clonedLink.Children[0].Text);
        Assert.Equal("0563C1", clonedLink.Children[0].Properties.Color);
    }

    [Fact]
    public void Clone_PreservesIds()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Test"));
        var clone = DocumentCloner.Clone(doc);

        Assert.Equal(doc.Id, clone.Id);
        var origPara = (Paragraph)doc.Children[0];
        var clonedPara = (Paragraph)clone.Children[0];
        Assert.Equal(origPara.Id, clonedPara.Id);
    }

    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Original"));
        var clone = DocumentCloner.Clone(doc);

        // Mutate clone
        var clonedRun = (Run)((Paragraph)clone.Children[0]).Children[0];
        clonedRun.Text = "Modified";

        // Original is unaffected
        var origRun = (Run)((Paragraph)doc.Children[0]).Children[0];
        Assert.Equal("Original", origRun.Text);
        Assert.Equal("Modified", clonedRun.Text);
    }

    [Fact]
    public void Clone_PolymorphicBlockNodes()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Text"),
            DocFactory.CreateTable(1, 2),
            DocFactory.CreateParagraph("More text")
        );

        var clone = DocumentCloner.Clone(doc);

        Assert.Equal(3, clone.Children.Count);
        Assert.IsType<Paragraph>(clone.Children[0]);
        Assert.IsType<Table>(clone.Children[1]);
        Assert.IsType<Paragraph>(clone.Children[2]);

        var table = (Table)clone.Children[1];
        Assert.Single(table.Rows);
        Assert.Equal(2, table.Rows[0].Cells.Count);
    }

    [Fact]
    public void Clone_PolymorphicInlineNodes()
    {
        var para = new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("Before "),
                DocFactory.CreateHyperlink("https://test.com", "link"),
                DocFactory.CreateRun(" after")
            ]
        };
        var doc = DocFactory.CreateDocument(para);

        var clone = DocumentCloner.Clone(doc);

        var clonedPara = Assert.IsType<Paragraph>(clone.Children[0]);
        Assert.Equal(3, clonedPara.Children.Count);
        var run1 = Assert.IsType<Run>(clonedPara.Children[0]);
        var link = Assert.IsType<Hyperlink>(clonedPara.Children[1]);
        var run2 = Assert.IsType<Run>(clonedPara.Children[2]);
        Assert.Equal("Before ", run1.Text);
        Assert.Equal("https://test.com", link.Url);
        Assert.Equal(" after", run2.Text);
    }

    [Fact]
    public void Clone_TextContentPolymorphism()
    {
        var run = new Run
        {
            Content =
            [
                new TextPiece { Text = "Hello" },
                new TabContent(),
                new BreakContent { BreakType = BreakType.Page }
            ]
        };
        var para = new Paragraph { Children = [run] };
        var doc = DocFactory.CreateDocument(para);

        var clone = DocumentCloner.Clone(doc);

        var clonedRun = (Run)((Paragraph)clone.Children[0]).Children[0];
        Assert.Equal(3, clonedRun.Content.Count);
        Assert.IsType<TextPiece>(clonedRun.Content[0]);
        Assert.IsType<TabContent>(clonedRun.Content[1]);
        var br = Assert.IsType<BreakContent>(clonedRun.Content[2]);
        Assert.Equal(BreakType.Page, br.BreakType);
    }
}
