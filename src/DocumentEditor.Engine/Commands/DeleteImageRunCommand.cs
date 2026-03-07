using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;

namespace DocumentEditor.Engine.Commands;

/// <summary>Finds a run by its node ID and removes it from its parent paragraph.</summary>
public class DeleteImageRunCommand : ICommand
{
    private readonly string _runId;

    public DeleteImageRunCommand(string runId)
    {
        _runId = runId;
    }

    public EditorState Execute(EditorState state)
    {
        var found = FindRunParent(state.Document, _runId);
        if (found is { } f)
        {
            f.para.Children.RemoveAt(f.idx);
            ParagraphNormalizer.Normalize(f.para);
        }
        return state;
    }

    private static (Paragraph para, int idx)? FindRunParent(DocxDocument doc, string runId)
    {
        foreach (var block in doc.Children)
        {
            var result = FindInBlock(block, runId);
            if (result is not null) return result;
        }
        return null;
    }

    private static (Paragraph para, int idx)? FindInBlock(IBlockNode block, string runId)
    {
        if (block is Paragraph para)
        {
            for (var i = 0; i < para.Children.Count; i++)
                if (para.Children[i] is Run run && run.Id == runId)
                    return (para, i);
        }
        else if (block is Table table)
        {
            foreach (var row in table.Rows)
                foreach (var cell in row.Cells)
                    foreach (var cellBlock in cell.Children)
                    {
                        var result = FindInBlock(cellBlock, runId);
                        if (result is not null) return result;
                    }
        }
        return null;
    }
}
