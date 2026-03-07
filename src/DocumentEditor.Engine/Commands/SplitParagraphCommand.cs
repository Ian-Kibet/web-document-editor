using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

public class SplitParagraphCommand : ICommand
{
    public EditorState Execute(EditorState state)
    {
        var sel = state.Selection;

        // Delete selection first if not collapsed
        if (!sel.IsCollapsed)
        {
            state = new DeleteSelectionCommand().Execute(state);
            sel = state.Selection;
        }

        var doc = state.Document;
        var pos = sel.Anchor;

        var para = CommandExecutor.ResolveParagraph(doc, pos);
        if (para is null) return state;
        if (para.Children[pos.InlineIndex] is not Run currentRun)
            return state;

        // Split current run text at offset
        var textBefore = currentRun.Text[..pos.Offset];
        var textAfter = currentRun.Text[pos.Offset..];

        // Build new paragraph with text after cursor + remaining inlines
        var newPara = new Paragraph();
        newPara.Children.Clear();

        // Create run with text after split point
        var newRun = DocFactory.CreateRun(textAfter, CloneRunProperties(currentRun.Properties));
        newPara.Children.Add(newRun);

        // Move remaining inlines from current para to new para
        for (var i = para.Children.Count - 1; i > pos.InlineIndex; i--)
        {
            newPara.Children.Insert(1, para.Children[i]);
            para.Children.RemoveAt(i);
        }

        // Trim current run
        currentRun.Text = textBefore;

        // Inherit paragraph properties
        var currentStyle = para.Properties.Style;
        if (currentStyle is not null && currentStyle.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
        {
            // Heading → new para gets Normal (null style)
            newPara.Properties = new ParagraphProperties
            {
                Alignment = para.Properties.Alignment,
                IndentLeft = para.Properties.IndentLeft,
                IndentFirstLine = para.Properties.IndentFirstLine,
                IndentHanging = para.Properties.IndentHanging,
                SpaceBefore = para.Properties.SpaceBefore,
                SpaceAfter = para.Properties.SpaceAfter,
                LineSpacing = para.Properties.LineSpacing,
                NumberingId = para.Properties.NumberingId,
                NumberingLevel = para.Properties.NumberingLevel,
                // Style is null (Normal)
            };
        }
        else
        {
            newPara.Properties = new ParagraphProperties
            {
                Style = para.Properties.Style,
                Alignment = para.Properties.Alignment,
                IndentLeft = para.Properties.IndentLeft,
                IndentFirstLine = para.Properties.IndentFirstLine,
                IndentHanging = para.Properties.IndentHanging,
                SpaceBefore = para.Properties.SpaceBefore,
                SpaceAfter = para.Properties.SpaceAfter,
                LineSpacing = para.Properties.LineSpacing,
                NumberingId = para.Properties.NumberingId,
                NumberingLevel = para.Properties.NumberingLevel,
            };
        }

        // Handle section break transfer:
        // SectionBreak stays on original paragraph (it's the last para of its section).
        // But if cursor was at end of paragraph, the new paragraph becomes the last
        // paragraph of that section, so move SectionBreak to it.
        if (para.Properties.SectionBreak is not null && textAfter.Length == 0
            && pos.InlineIndex == para.Children.Count - 1)
        {
            // Cursor at end: move section break to new paragraph
            newPara.Properties.SectionBreak = para.Properties.SectionBreak;
            para.Properties.SectionBreak = null;
        }

        // Normalize both paragraphs
        ParagraphNormalizer.Normalize(para);
        ParagraphNormalizer.Normalize(newPara);

        // Insert new paragraph after current (cell-aware)
        var childList = CommandExecutor.ResolveChildList(doc, pos);
        var insertIdx = pos.Cell is not null ? pos.Cell.CellBlockIndex + 1 : pos.BlockIndex + 1;
        childList.Insert(insertIdx, newPara);

        // Cursor at start of new paragraph
        state.Selection = pos.Cell is not null
            ? SelectionModel.Collapsed(new ModelPosition(pos.BlockIndex, 0, 0)
            {
                Cell = new CellPath { RowIndex = pos.Cell.RowIndex, CellIndex = pos.Cell.CellIndex, CellBlockIndex = pos.Cell.CellBlockIndex + 1 }
            })
            : SelectionModel.Collapsed(pos.BlockIndex + 1, 0, 0);
        return state;
    }

    private static RunProperties CloneRunProperties(RunProperties props)
    {
        return new RunProperties
        {
            Bold = props.Bold,
            Italic = props.Italic,
            Underline = props.Underline,
            Strikethrough = props.Strikethrough,
            FontFamily = props.FontFamily,
            FontSize = props.FontSize,
            Color = props.Color,
            Highlight = props.Highlight,
            VerticalAlign = props.VerticalAlign,
        };
    }
}
