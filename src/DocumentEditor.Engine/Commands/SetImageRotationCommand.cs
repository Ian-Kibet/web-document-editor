using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;

namespace DocumentEditor.Engine.Commands;

/// <summary>Finds an image run by its node ID and updates its rotation.</summary>
public class SetImageRotationCommand : ICommand
{
    private readonly string _runId;
    private readonly double _degrees;

    public SetImageRotationCommand(string runId, double degrees)
    {
        _runId   = runId;
        // Clamp to [-360, 360]
        _degrees = Math.Max(-360, Math.Min(360, degrees));
    }

    public EditorState Execute(EditorState state)
    {
        var img = FindImageByRunId(state.Document, _runId);
        if (img is null) return state;
        img.RotationDegrees = _degrees;
        return state;
    }

    private static ImageContent? FindImageByRunId(DocxDocument doc, string runId)
    {
        foreach (var block in doc.Children)
        {
            var img = FindInBlock(block, runId);
            if (img is not null) return img;
        }
        return null;
    }

    private static ImageContent? FindInBlock(IBlockNode block, string runId)
    {
        if (block is Paragraph para)
        {
            foreach (var inline in para.Children)
                if (inline is Run run && run.Id == runId)
                    return run.Content.OfType<ImageContent>().FirstOrDefault();
        }
        else if (block is Table table)
        {
            foreach (var row in table.Rows)
                foreach (var cell in row.Cells)
                    foreach (var cellBlock in cell.Children)
                    {
                        var img = FindInBlock(cellBlock, runId);
                        if (img is not null) return img;
                    }
        }
        return null;
    }
}
