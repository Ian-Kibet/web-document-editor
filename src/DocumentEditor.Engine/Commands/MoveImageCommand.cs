using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;

namespace DocumentEditor.Engine.Commands;

/// <summary>
/// Moves an image to an absolute page position.
/// Flow images (Inline, FloatLeft, FloatRight, TopAndBottom) are auto-converted
/// to InFrontOfText, matching Word's drag behaviour.
/// </summary>
public class MoveImageCommand : ICommand
{
    private readonly string _runId;
    private readonly long _hEmu;
    private readonly long _vEmu;

    public MoveImageCommand(string runId, long hEmu, long vEmu)
    {
        _runId = runId;
        _hEmu  = Math.Max(0, hEmu);
        _vEmu  = Math.Max(0, vEmu);
    }

    public EditorState Execute(EditorState state)
    {
        var img = FindImageByRunId(state.Document, _runId);
        if (img is null) return state;

        if (img.WrapMode is not (ImageWrapMode.BehindText or ImageWrapMode.InFrontOfText))
            img.WrapMode = ImageWrapMode.InFrontOfText;

        img.HorizontalOffsetEmu = _hEmu;
        img.VerticalOffsetEmu   = _vEmu;
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
