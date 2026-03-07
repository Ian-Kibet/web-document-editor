using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;

namespace DocumentEditor.Engine.Commands;

/// <summary>
/// Changes the page orientation for the section containing the cursor.
/// Swaps PageWidth/PageHeight and toggles the Orientation enum.
/// </summary>
public class SetPageOrientationCommand : ICommand
{
    private readonly Orientation _orientation;

    public SetPageOrientationCommand(Orientation orientation)
    {
        _orientation = orientation;
    }

    public EditorState Execute(EditorState state)
    {
        var doc = state.Document;
        var pos = state.Selection.Anchor;

        var sections = SectionResolver.GetSections(doc);
        var sectionIdx = SectionResolver.GetSectionIndex(sections, pos.BlockIndex);
        var section = sections[sectionIdx];

        // Determine if we need to change anything
        if (section.Properties.Orientation == _orientation)
            return state;

        // Check if this is the final section (uses DocumentProperties)
        var isFinalSection = sectionIdx == sections.Count - 1;
        var sectionHasBreak = !isFinalSection;

        if (sectionHasBreak)
        {
            // Mid-document section: modify the SectionBreak on the last paragraph
            var lastPara = (Paragraph)doc.Children[section.EndBlockIndex];
            var sp = lastPara.Properties.SectionBreak!;
            SwapOrientation(sp);
        }
        else
        {
            // Final section: modify DocumentProperties
            var dp = doc.Properties;
            var newWidth = dp.PageHeight;
            var newHeight = dp.PageWidth;
            dp.PageWidth = newWidth;
            dp.PageHeight = newHeight;
            dp.Orientation = _orientation;
        }

        return state;
    }

    private void SwapOrientation(SectionProperties sp)
    {
        var newWidth = sp.PageHeight;
        var newHeight = sp.PageWidth;
        sp.PageWidth = newWidth;
        sp.PageHeight = newHeight;
        sp.Orientation = _orientation;
    }
}
