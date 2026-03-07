using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;

namespace DocumentEditor.Engine.Commands;

public static class ParagraphNormalizer
{
    public static void Normalize(Paragraph para)
    {
        // Step 1: Remove empty Runs (unless it's the only child)
        // A run is empty only if ALL its content items are empty text pieces (preserve image/tab/break runs)
        if (para.Children.Count > 1)
        {
            para.Children.RemoveAll(node => node is Run run && IsRunEmpty(run));
        }

        // Step 2: Merge adjacent Runs with identical properties
        // Only merge if BOTH runs contain only text content (no images/tabs/breaks)
        for (var i = 0; i < para.Children.Count - 1; i++)
        {
            if (para.Children[i] is not Run current) continue;
            if (para.Children[i + 1] is not Run next) continue;

            if (IsTextOnly(current) && IsTextOnly(next) && current.Properties.ValueEquals(next.Properties))
            {
                current.Text += next.Text;
                para.Children.RemoveAt(i + 1);
                i--; // Re-check current against new next
            }
        }

        // Step 3: Ensure at least one Run exists
        if (para.Children.Count == 0)
        {
            para.Children.Add(DocFactory.CreateRun(""));
        }
    }

    /// <summary>A run is empty only if all its content items are empty text pieces.</summary>
    private static bool IsRunEmpty(Run run)
    {
        return run.Content.All(c => c is TextPiece tp && tp.Text.Length == 0);
    }

    /// <summary>A run is text-only if it contains only TextPiece content (no images, tabs, or breaks).</summary>
    private static bool IsTextOnly(Run run)
    {
        return run.Content.All(c => c is TextPiece);
    }
}
