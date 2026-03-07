using DocumentEditor.Engine.Model;

namespace DocumentEditor.Engine.Commands;

/// <summary>
/// Sets the column count and spacing for the section containing the cursor.
/// Modifies SectionBreak (mid-doc) or DocumentProperties (final section).
/// </summary>
public class SetColumnsCommand : ICommand
{
    private readonly int _columnCount;
    private readonly int _spacing;

    public SetColumnsCommand(int columnCount, int spacing = 720)
    {
        _columnCount = columnCount;
        _spacing = spacing;
    }

    public EditorState Execute(EditorState state)
    {
        var doc = state.Document;
        var pos = state.Selection.Anchor;

        var sections = SectionResolver.GetSections(doc);
        var sectionIdx = SectionResolver.GetSectionIndex(sections, pos.BlockIndex);
        var section = sections[sectionIdx];

        // No-op if values already match
        if (section.Properties.ColumnCount == _columnCount
            && section.Properties.ColumnSpacing == _spacing)
            return state;

        var isFinalSection = sectionIdx == sections.Count - 1;

        if (!isFinalSection)
        {
            // Mid-document section: modify the SectionBreak on the last paragraph
            var lastPara = (Paragraph)doc.Children[section.EndBlockIndex];
            var sp = lastPara.Properties.SectionBreak!;
            sp.ColumnCount = _columnCount;
            sp.ColumnSpacing = _spacing;
        }
        else
        {
            // Final section: modify DocumentProperties
            doc.Properties.ColumnCount = _columnCount;
            doc.Properties.ColumnSpacing = _spacing;
        }

        return state;
    }
}
