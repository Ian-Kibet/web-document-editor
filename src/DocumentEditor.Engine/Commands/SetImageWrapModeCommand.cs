using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;

namespace DocumentEditor.Engine.Commands;

/// <summary>Finds an image run by its node ID and updates its wrap mode.</summary>
public class SetImageWrapModeCommand : ICommand
{
    private readonly string _runId;
    private readonly ImageWrapMode _wrapMode;

    public SetImageWrapModeCommand(string runId, ImageWrapMode wrapMode)
    {
        _runId    = runId;
        _wrapMode = wrapMode;
    }

    public EditorState Execute(EditorState state)
    {
        var img = FindImageByRunId(state.Document, _runId);
        if (img is null) return state;
        img.WrapMode = _wrapMode;
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
