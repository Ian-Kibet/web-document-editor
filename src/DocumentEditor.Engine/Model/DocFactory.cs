using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Model;

public static class DocFactory
{
    public static DocxDocument CreateDocument(params IBlockNode[] children)
    {
        var doc = new DocxDocument();
        doc.Children.AddRange(children);
        return doc;
    }

    public static Paragraph CreateParagraph(string text = "", ParagraphProperties? props = null)
    {
        var para = new Paragraph
        {
            Properties = props ?? new ParagraphProperties()
        };
        // Enforce invariant: always at least one Run
        para.Children = [CreateRun(text)];
        return para;
    }

    public static Run CreateRun(string text = "", RunProperties? props = null)
    {
        var run = new Run
        {
            Properties = props ?? new RunProperties(),
            Content = [new TextPiece { Text = text }]
        };
        return run;
    }

    public static Table CreateTable(int rows, int cols)
    {
        var totalWidth = 9360; // US Letter content width in twips (12240 - 1440*2)
        var colWidth = totalWidth / cols;

        var table = new Table
        {
            GridColumnWidths = Enumerable.Range(0, cols).Select(_ => colWidth).ToList()
        };

        for (var r = 0; r < rows; r++)
        {
            var row = new TableRow();
            for (var c = 0; c < cols; c++)
            {
                var cell = new TableCell
                {
                    Properties = new TableCellProperties { Width = colWidth },
                    Children = [CreateParagraph()]
                };
                row.Cells.Add(cell);
            }
            table.Rows.Add(row);
        }

        return table;
    }

    public static Hyperlink CreateHyperlink(string url, string text)
    {
        var run = CreateRun(text, new RunProperties
        {
            Color = "0563C1",
            Underline = UnderlineType.Single
        });

        return new Hyperlink
        {
            Url = url,
            Children = [run]
        };
    }
}
