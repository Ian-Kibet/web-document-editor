using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class InsertTextCommandTests
{
    [Fact]
    public void InsertText_AtStartOfRun()
    {
        var state = TestHelpers.CreateState("Hello", offset: 0);
        state = new InsertTextCommand("X").Execute(state);

        Assert.Equal("XHello", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.InlineIndex);
        Assert.Equal(1, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void InsertText_AtMiddleOfRun()
    {
        var state = TestHelpers.CreateState("Hello", offset: 2);
        state = new InsertTextCommand("XY").Execute(state);

        Assert.Equal("HeXYllo", TestHelpers.GetParaText(state, 0));
        Assert.Equal(4, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void InsertText_AtEndOfRun()
    {
        var state = TestHelpers.CreateState("Hello", offset: 5);
        state = new InsertTextCommand("!").Execute(state);

        Assert.Equal("Hello!", TestHelpers.GetParaText(state, 0));
        Assert.Equal(6, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void InsertText_WithRangeSelection_DeletesFirst()
    {
        var state = TestHelpers.CreateState("Hello");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 1),
            Focus = new ModelPosition(0, 0, 4)
        };

        state = new InsertTextCommand("i").Execute(state);

        Assert.Equal("Hio", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void InsertText_InEmptyParagraph()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new InsertTextCommand("A").Execute(state);

        Assert.Equal("A", TestHelpers.GetParaText(state, 0));
        Assert.Equal(1, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void InsertText_CursorAdvances()
    {
        var state = TestHelpers.CreateState("AB", offset: 1);
        state = new InsertTextCommand("XYZ").Execute(state);

        Assert.Equal("AXYZB", TestHelpers.GetParaText(state, 0));
        Assert.Equal(4, state.Selection.Anchor.Offset);
        Assert.True(state.Selection.IsCollapsed);
    }

    [Fact]
    public void InsertText_MultipleCharacters()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new InsertTextCommand("Hello World").Execute(state);

        Assert.Equal("Hello World", TestHelpers.GetParaText(state, 0));
        Assert.Equal(11, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void InsertText_PreservesOtherParagraphs()
    {
        var state = TestHelpers.CreateMultiParaState("First", "Second");
        state.Selection = SelectionModel.Collapsed(1, 0, 0);
        state = new InsertTextCommand("X").Execute(state);

        Assert.Equal("First", TestHelpers.GetParaText(state, 0));
        Assert.Equal("XSecond", TestHelpers.GetParaText(state, 1));
    }
}
