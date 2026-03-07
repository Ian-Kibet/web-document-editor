using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class DeleteForwardCommandTests
{
    [Fact]
    public void DeleteForward_MidRun()
    {
        var state = TestHelpers.CreateState("Hello", offset: 2);
        state = new DeleteForwardCommand().Execute(state);

        Assert.Equal("Helo", TestHelpers.GetParaText(state, 0));
        Assert.Equal(2, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void DeleteForward_EndOfRun_DeletesFromNextRun()
    {
        var doc = DocFactory.CreateDocument(new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("AB"),
                DocFactory.CreateRun("CD", new RunProperties { Bold = true })
            ]
        });
        var state = new EditorState
        {
            Document = doc,
            Selection = SelectionModel.Collapsed(0, 0, 2)
        };

        state = new DeleteForwardCommand().Execute(state);

        Assert.Equal("ABD", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteForward_EndOfParagraph_MergesWithNext()
    {
        var state = TestHelpers.CreateMultiParaState("Hello", "World");
        state.Selection = SelectionModel.Collapsed(0, 0, 5);

        state = new DeleteForwardCommand().Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("HelloWorld", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.BlockIndex);
    }

    [Fact]
    public void DeleteForward_EndOfDocument_NoOp()
    {
        var state = TestHelpers.CreateState("Hello", offset: 5);
        state = new DeleteForwardCommand().Execute(state);

        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));
        Assert.Equal(5, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void DeleteForward_WithRangeSelection_DeletesSelection()
    {
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 5),
            Focus = new ModelPosition(0, 0, 11)
        };

        state = new DeleteForwardCommand().Execute(state);

        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteForward_FirstChar()
    {
        var state = TestHelpers.CreateState("Hello", offset: 0);
        state = new DeleteForwardCommand().Execute(state);

        Assert.Equal("ello", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.Offset);
    }
}
