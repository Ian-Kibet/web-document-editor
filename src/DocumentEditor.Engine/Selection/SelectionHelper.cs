using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;

namespace DocumentEditor.Engine.Selection;

public static class SelectionHelper
{
    public static (ModelPosition Start, ModelPosition End) Normalize(SelectionModel selection)
    {
        var anchor = selection.Anchor;
        var focus = selection.Focus;
        return anchor <= focus ? (anchor, focus) : (focus, anchor);
    }

    public static (int StartBlock, int EndBlock) GetBlockRange(SelectionModel selection)
    {
        var (start, end) = Normalize(selection);
        return (start.BlockIndex, end.BlockIndex);
    }

    public static int GetInlineTextLength(IInlineNode node)
    {
        return node switch
        {
            Run run => run.Text.Length,
            Hyperlink link => link.Children.Sum(r => r.Text.Length),
            _ => 0
        };
    }

    public static int GetParagraphTextLength(Paragraph para)
    {
        return para.Children.Sum(GetInlineTextLength);
    }

    public static (Run Run, int CharOffset)? ResolveToRun(DocxDocument doc, ModelPosition pos)
    {
        if (pos.BlockIndex < 0 || pos.BlockIndex >= doc.Children.Count)
            return null;

        if (doc.Children[pos.BlockIndex] is not Paragraph para)
            return null;

        if (pos.InlineIndex < 0 || pos.InlineIndex >= para.Children.Count)
            return null;

        if (para.Children[pos.InlineIndex] is not Run run)
            return null;

        return (run, pos.Offset);
    }

    /// <summary>
    /// Returns word bounds only when the offset is strictly between the first and last
    /// character of a contiguous non-whitespace word. Returns null otherwise.
    /// </summary>
    public static (int Start, int End)? FindWordInterior(string text, int offset)
    {
        if (offset < 0 || offset >= text.Length) return null;
        if (char.IsWhiteSpace(text[offset])) return null;

        var wordStart = offset;
        while (wordStart > 0 && !char.IsWhiteSpace(text[wordStart - 1]))
            wordStart--;

        var wordEnd = offset + 1;
        while (wordEnd < text.Length && !char.IsWhiteSpace(text[wordEnd]))
            wordEnd++;

        // Must be strictly interior: not on first char, not on last char
        if (offset == wordStart) return null;
        if (offset == wordEnd - 1) return null;

        return (wordStart, wordEnd);
    }
}
