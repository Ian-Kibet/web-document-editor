using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

/// <summary>
/// Removes the section break at or before the cursor position.
/// Clears SectionBreak from the paragraph, merging the two sections.
/// </summary>
public class RemoveSectionBreakCommand : ICommand
{
    public EditorState Execute(EditorState state)
    {
        var doc = state.Document;
        var pos = state.Selection.Anchor;

        if (pos.BlockIndex < 0 || pos.BlockIndex >= doc.Children.Count)
            return state;

        // Find the nearest paragraph with a section break at or before cursor
        for (var i = pos.BlockIndex; i >= 0; i--)
        {
            if (doc.Children[i] is Paragraph para && para.Properties.SectionBreak is not null)
            {
                para.Properties.SectionBreak = null;
                return state;
            }
        }

        return state;
    }
}
