using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Commands;

/// <summary>
/// Inserts a section break at the cursor position.
/// Splits the current paragraph (reusing SplitParagraph logic) and attaches
/// SectionProperties to the paragraph before the split point.
/// </summary>
public class InsertSectionBreakCommand : ICommand
{
    private readonly SectionBreakType _breakType;
    private readonly SectionProperties? _sectionProps;

    public InsertSectionBreakCommand(SectionBreakType breakType, SectionProperties? sectionProps = null)
    {
        _breakType = breakType;
        _sectionProps = sectionProps;
    }

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

        if (pos.BlockIndex < 0 || pos.BlockIndex >= doc.Children.Count)
            return state;
        if (doc.Children[pos.BlockIndex] is not Paragraph para)
            return state;

        // Determine section properties for the break.
        // Inherit from current section if not provided.
        var sections = SectionResolver.GetSections(doc);
        var currentSectionIdx = SectionResolver.GetSectionIndex(sections, pos.BlockIndex);
        var currentSection = sections[currentSectionIdx];

        var sp = _sectionProps != null
            ? new SectionProperties
            {
                BreakType = _breakType,
                Orientation = _sectionProps.Orientation,
                PageWidth = _sectionProps.PageWidth,
                PageHeight = _sectionProps.PageHeight,
                MarginTop = _sectionProps.MarginTop,
                MarginBottom = _sectionProps.MarginBottom,
                MarginLeft = _sectionProps.MarginLeft,
                MarginRight = _sectionProps.MarginRight
            }
            : new SectionProperties
            {
                BreakType = _breakType,
                Orientation = currentSection.Properties.Orientation,
                PageWidth = currentSection.Properties.PageWidth,
                PageHeight = currentSection.Properties.PageHeight,
                MarginTop = currentSection.Properties.MarginTop,
                MarginBottom = currentSection.Properties.MarginBottom,
                MarginLeft = currentSection.Properties.MarginLeft,
                MarginRight = currentSection.Properties.MarginRight
            };

        // Split the paragraph at the cursor position
        state = new SplitParagraphCommand().Execute(state);

        // After split, the original paragraph is at pos.BlockIndex,
        // the new paragraph is at pos.BlockIndex + 1.
        // Attach the section break to the original paragraph (before the split).
        var originalPara = (Paragraph)doc.Children[pos.BlockIndex];
        originalPara.Properties.SectionBreak = sp;

        // Cursor is already at the start of the new paragraph (set by SplitParagraph)
        return state;
    }
}
