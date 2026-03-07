using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Serialization;

namespace DocumentEditor.Engine.Tests.Serialization;

public class OpenXmlValidationTests
{
    private readonly DocxExporter _exporter = new();

    /// <summary>
    /// Validate exported .docx against the OOXML schema.
    /// The StyleDefinitionsPart is excluded — a known SDK limitation that doesn't affect Word compatibility.
    /// </summary>
    private List<ValidationErrorInfo> ValidateExport(DocxDocument doc)
    {
        var bytes = _exporter.Export(doc);
        using var stream = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var validator = new OpenXmlValidator(FileFormatVersions.Office2019);
        return validator.Validate(wordDoc)
            .Where(e => e.Part is not StyleDefinitionsPart)
            .ToList();
    }

    [Fact]
    public void Validate_EmptyDocument()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_SimpleText()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Hello, World!"));
        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MultipleParagraphs()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("First"),
            DocFactory.CreateParagraph("Second"),
            DocFactory.CreateParagraph("Third")
        );
        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_FormattedText()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("bold", new RunProperties { Bold = true }));
        para.Children.Add(DocFactory.CreateRun("italic", new RunProperties { Italic = true }));
        para.Children.Add(DocFactory.CreateRun("underline", new RunProperties { Underline = UnderlineType.Single }));
        para.Children.Add(DocFactory.CreateRun("strike", new RunProperties { Strikethrough = true }));

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_HeadingStyles()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("H1", new ParagraphProperties { Style = "Heading1" }),
            DocFactory.CreateParagraph("H2", new ParagraphProperties { Style = "Heading2" }),
            DocFactory.CreateParagraph("H3", new ParagraphProperties { Style = "Heading3" }),
            DocFactory.CreateParagraph("Body text")
        );

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ParagraphAlignment()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("left", new ParagraphProperties { Alignment = Alignment.Left }),
            DocFactory.CreateParagraph("center", new ParagraphProperties { Alignment = Alignment.Center }),
            DocFactory.CreateParagraph("right", new ParagraphProperties { Alignment = Alignment.Right }),
            DocFactory.CreateParagraph("justify", new ParagraphProperties { Alignment = Alignment.Both })
        );

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ParagraphIndentation()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("indented", new ParagraphProperties
            {
                IndentLeft = 720,
                IndentFirstLine = 360
            })
        );

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ParagraphSpacing()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("spaced", new ParagraphProperties
            {
                SpaceBefore = 240,
                SpaceAfter = 120,
                LineSpacing = 360
            })
        );

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_NumberedList()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("item 1", new ParagraphProperties { NumberingId = 1, NumberingLevel = 0 }),
            DocFactory.CreateParagraph("item 2", new ParagraphProperties { NumberingId = 1, NumberingLevel = 0 }),
            DocFactory.CreateParagraph("nested", new ParagraphProperties { NumberingId = 1, NumberingLevel = 1 })
        );

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_FontProperties()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("styled", new RunProperties
        {
            FontFamily = "Times New Roman",
            FontSize = 28,
            Color = "FF0000"
        }));

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_HighlightColor()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("highlighted", new RunProperties { Highlight = HighlightColor.Yellow }));

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_VerticalAlign()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("super", new RunProperties { VerticalAlign = VerticalAlignType.Superscript }));
        para.Children.Add(DocFactory.CreateRun("sub", new RunProperties { VerticalAlign = VerticalAlignType.Subscript }));

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_SimpleTable()
    {
        var table = DocFactory.CreateTable(2, 3);
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Clear();
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Add(DocFactory.CreateRun("Cell text"));

        var doc = DocFactory.CreateDocument(table);
        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_TableWithBorders()
    {
        var table = DocFactory.CreateTable(2, 2);
        table.Properties.HasBorders = true;
        var doc = DocFactory.CreateDocument(table);

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_TableWithMergedCells()
    {
        var table = DocFactory.CreateTable(2, 2);
        table.Rows[0].Cells[0].Properties.GridSpan = 2;
        table.Rows[0].Cells[0].Properties.VerticalMerge = VerticalMergeType.Restart;
        table.Rows[1].Cells[0].Properties.VerticalMerge = VerticalMergeType.Continue;

        var doc = DocFactory.CreateDocument(table);
        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_TableCellProperties()
    {
        var table = DocFactory.CreateTable(1, 1);
        var cell = table.Rows[0].Cells[0];
        cell.Properties.Width = 4680;
        cell.Properties.VerticalAlignment = TableVerticalAlignment.Center;
        cell.Properties.Shading = "FFFF00";

        var doc = DocFactory.CreateDocument(table);
        var errors = ValidateExport(doc);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_Hyperlink()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        para.Children.Add(DocFactory.CreateRun("Click "));
        var link = DocFactory.CreateHyperlink("https://example.com", "here");
        link.Tooltip = "Example Site";
        para.Children.Add(link);
        para.Children.Add(DocFactory.CreateRun(" to visit"));

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_TabAndBreakContent()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var run = new Run();
        run.Content.Clear();
        run.Content.Add(new TextPiece { Text = "before" });
        run.Content.Add(new TabContent());
        run.Content.Add(new TextPiece { Text = "middle" });
        run.Content.Add(new BreakContent { BreakType = BreakType.TextWrapping });
        run.Content.Add(new TextPiece { Text = "after" });
        para.Children.Add(run);

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_PageBreak()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph());
        var para = (Paragraph)doc.Children[0];
        para.Children.Clear();
        var run = new Run();
        run.Content.Clear();
        run.Content.Add(new BreakContent { BreakType = BreakType.Page });
        para.Children.Add(run);

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DocumentProperties()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("test"));
        doc.Properties.PageWidth = 15840;
        doc.Properties.PageHeight = 12240;
        doc.Properties.MarginTop = 720;
        doc.Properties.MarginBottom = 720;
        doc.Properties.MarginLeft = 1080;
        doc.Properties.MarginRight = 1080;

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ComplexMixedDocument()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("Document Title", new ParagraphProperties { Style = "Heading1" }),
            DocFactory.CreateParagraph("This is body text with formatting.")
        );

        // Add formatted paragraph
        var mixedPara = DocFactory.CreateParagraph();
        var mp = (Paragraph)mixedPara;
        mp.Children.Clear();
        mp.Children.Add(DocFactory.CreateRun("Normal, "));
        mp.Children.Add(DocFactory.CreateRun("bold", new RunProperties { Bold = true }));
        mp.Children.Add(DocFactory.CreateRun(", "));
        mp.Children.Add(DocFactory.CreateRun("italic", new RunProperties { Italic = true }));
        doc.Children.Add(mp);

        // List items
        doc.Children.Add(DocFactory.CreateParagraph("Bullet 1", new ParagraphProperties { NumberingId = 1, NumberingLevel = 0 }));
        doc.Children.Add(DocFactory.CreateParagraph("Bullet 2", new ParagraphProperties { NumberingId = 1, NumberingLevel = 0 }));

        // Table
        var table = DocFactory.CreateTable(2, 2);
        table.Properties.HasBorders = true;
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Clear();
        ((Paragraph)table.Rows[0].Cells[0].Children[0]).Children.Add(DocFactory.CreateRun("A1"));
        ((Paragraph)table.Rows[0].Cells[1].Children[0]).Children.Clear();
        ((Paragraph)table.Rows[0].Cells[1].Children[0]).Children.Add(DocFactory.CreateRun("B1"));
        doc.Children.Add(table);

        // Hyperlink paragraph
        var linkPara = DocFactory.CreateParagraph();
        var lp = (Paragraph)linkPara;
        lp.Children.Clear();
        lp.Children.Add(DocFactory.CreateRun("Visit "));
        lp.Children.Add(DocFactory.CreateHyperlink("https://example.com", "Example"));
        doc.Children.Add(lp);

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_SpacePreservation()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph(" leading"),
            DocFactory.CreateParagraph("trailing "),
            DocFactory.CreateParagraph(" both ")
        );

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_KeepNextAndPageBreakBefore()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateParagraph("keep", new ParagraphProperties { KeepNext = true }),
            DocFactory.CreateParagraph("break", new ParagraphProperties { PageBreakBefore = true })
        );

        var errors = ValidateExport(doc);

        Assert.Empty(errors);
    }
}
