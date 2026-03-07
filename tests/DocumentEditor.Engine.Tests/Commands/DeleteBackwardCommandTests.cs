using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class DeleteBackwardCommandTests
{
    [Fact]
    public void DeleteBackward_MidRun()
    {
        var state = TestHelpers.CreateState("Hello", offset: 3);
        state = new DeleteBackwardCommand().Execute(state);

        Assert.Equal("Helo", TestHelpers.GetParaText(state, 0));
        Assert.Equal(2, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void DeleteBackward_StartOfSecondRun()
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
            Selection = SelectionModel.Collapsed(0, 1, 0)
        };

        state = new DeleteBackwardCommand().Execute(state);

        // Should delete last char of previous run
        Assert.Equal("ACD", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteBackward_StartOfParagraph_MergesWithPrevious()
    {
        var state = TestHelpers.CreateMultiParaState("Hello", "World");
        state.Selection = SelectionModel.Collapsed(1, 0, 0);

        state = new DeleteBackwardCommand().Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("HelloWorld", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.BlockIndex);
        // Cursor should be at the join point (end of "Hello")
        Assert.Equal(5, GetAbsoluteOffset(state));
    }

    [Fact]
    public void DeleteBackward_StartOfDocument_NoOp()
    {
        var state = TestHelpers.CreateState("Hello", offset: 0);
        state = new DeleteBackwardCommand().Execute(state);

        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void DeleteBackward_WithRangeSelection_DeletesSelection()
    {
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 0),
            Focus = new ModelPosition(0, 0, 5)
        };

        state = new DeleteBackwardCommand().Execute(state);

        Assert.Equal(" World", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteBackward_AfterTable_NoOp()
    {
        var doc = DocFactory.CreateDocument(
            DocFactory.CreateTable(1, 1),
            DocFactory.CreateParagraph("Text")
        );
        var state = new EditorState
        {
            Document = doc,
            Selection = SelectionModel.Collapsed(1, 0, 0)
        };

        state = new DeleteBackwardCommand().Execute(state);

        // Previous block is table — should be no-op
        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("Text", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void DeleteBackward_LastCharInRun()
    {
        var state = TestHelpers.CreateState("A", offset: 1);
        state = new DeleteBackwardCommand().Execute(state);

        // Should leave empty paragraph with one empty run
        Assert.Equal("", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void DeleteBackward_CursorPositionCorrectAfterMerge()
    {
        var state = TestHelpers.CreateMultiParaState("ABC", "DEF");
        state.Selection = SelectionModel.Collapsed(1, 0, 0);

        state = new DeleteBackwardCommand().Execute(state);

        Assert.Equal("ABCDEF", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.BlockIndex);
    }

    private static int GetAbsoluteOffset(EditorState state)
    {
        var pos = state.Selection.Anchor;
        var para = (Paragraph)state.Document.Children[pos.BlockIndex];
        var abs = 0;
        for (var i = 0; i < pos.InlineIndex; i++)
        {
            if (para.Children[i] is Run r) abs += r.Text.Length;
        }
        return abs + pos.Offset;
    }
}
