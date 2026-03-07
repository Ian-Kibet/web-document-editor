using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class DeleteSelectionCommandTests
{
    [Fact]
    public void DeleteSelection_Collapsed_NoOp()
    {
        var state = TestHelpers.CreateState("Hello", offset: 2);
        var original = TestHelpers.GetParaText(state, 0);

        state = new DeleteSelectionCommand().Execute(state);

        Assert.Equal(original, TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteSelection_SameRun()
    {
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 2),
            Focus = new ModelPosition(0, 0, 8)
        };

        state = new DeleteSelectionCommand().Execute(state);

        Assert.Equal("Herld", TestHelpers.GetParaText(state, 0));
        Assert.True(state.Selection.IsCollapsed);
    }

    [Fact]
    public void DeleteSelection_CrossRunSameParagraph()
    {
        var doc = DocFactory.CreateDocument(new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("Hello"),
                DocFactory.CreateRun(" World", new RunProperties { Bold = true })
            ]
        });
        var state = new EditorState
        {
            Document = doc,
            Selection = new SelectionModel
            {
                Anchor = new ModelPosition(0, 0, 3),
                Focus = new ModelPosition(0, 1, 3)
            }
        };

        state = new DeleteSelectionCommand().Execute(state);

        Assert.Contains("Hel", TestHelpers.GetParaText(state, 0));
        Assert.Contains("rld", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteSelection_CrossParagraph()
    {
        var state = TestHelpers.CreateMultiParaState("Hello", "World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 3),
            Focus = new ModelPosition(1, 0, 2)
        };

        state = new DeleteSelectionCommand().Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("Helrld", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteSelection_EntireParagraph()
    {
        // Selecting from end of "First" to start of "Last" merges them into one paragraph
        var state = TestHelpers.CreateMultiParaState("First", "Middle", "Last");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 5),
            Focus = new ModelPosition(2, 0, 0)
        };

        state = new DeleteSelectionCommand().Execute(state);

        // "First" and "Last" merge into one paragraph, "Middle" is removed entirely
        Assert.Single(state.Document.Children);
        Assert.Equal("FirstLast", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteSelection_CursorCollapsesToStart()
    {
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 8),
            Focus = new ModelPosition(0, 0, 2)
        };

        state = new DeleteSelectionCommand().Execute(state);

        Assert.True(state.Selection.IsCollapsed);
        Assert.Equal(2, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void DeleteSelection_BackwardSelection_SameResult()
    {
        // Backward selection (Focus before Anchor) should give same result
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 8),
            Focus = new ModelPosition(0, 0, 2)
        };

        state = new DeleteSelectionCommand().Execute(state);

        Assert.Equal("Herld", TestHelpers.GetParaText(state, 0));
    }
}
