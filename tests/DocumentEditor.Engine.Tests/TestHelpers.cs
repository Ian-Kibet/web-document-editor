using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests;

public static class TestHelpers
{
    public static EditorState CreateState(string text, int blockIdx = 0, int inlineIdx = 0, int offset = 0)
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph(text));
        return new EditorState
        {
            Document = doc,
            Selection = SelectionModel.Collapsed(blockIdx, inlineIdx, offset)
        };
    }

    public static EditorState CreateMultiParaState(params string[] texts)
    {
        var paras = texts.Select(t => DocFactory.CreateParagraph(t)).ToArray();
        var doc = DocFactory.CreateDocument(paras);
        return new EditorState
        {
            Document = doc,
            Selection = SelectionModel.Collapsed(0, 0, 0)
        };
    }

    public static string GetParaText(EditorState state, int blockIndex)
    {
        var para = (Paragraph)state.Document.Children[blockIndex];
        return string.Concat(para.Children.OfType<Run>().Select(r => r.Text));
    }

    public static Run GetRun(EditorState state, int blockIndex, int inlineIndex)
    {
        var para = (Paragraph)state.Document.Children[blockIndex];
        return (Run)para.Children[inlineIndex];
    }

    public static Paragraph GetPara(EditorState state, int blockIndex)
    {
        return (Paragraph)state.Document.Children[blockIndex];
    }
}
