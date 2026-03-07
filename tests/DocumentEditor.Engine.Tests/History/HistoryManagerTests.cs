using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.History;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.History;

public class HistoryManagerTests
{
    [Fact]
    public void Push_Undo_RestoresPreviousState()
    {
        var manager = new HistoryManager();
        var state1 = TestHelpers.CreateState("Hello");

        manager.Push(state1);

        // Modify document
        var state2 = TestHelpers.CreateState("World");
        var restored = manager.Undo(state2);

        Assert.Equal("Hello", TestHelpers.GetParaText(restored, 0));
    }

    [Fact]
    public void Undo_EmptyStack_ReturnsCurrent()
    {
        var manager = new HistoryManager();
        var state = TestHelpers.CreateState("Hello");

        var result = manager.Undo(state);
        Assert.Equal("Hello", TestHelpers.GetParaText(result, 0));
    }

    [Fact]
    public void Redo_EmptyStack_ReturnsCurrent()
    {
        var manager = new HistoryManager();
        var state = TestHelpers.CreateState("Hello");

        var result = manager.Redo(state);
        Assert.Equal("Hello", TestHelpers.GetParaText(result, 0));
    }

    [Fact]
    public void Undo_Redo_Cycle()
    {
        var manager = new HistoryManager();
        var state1 = TestHelpers.CreateState("Before");
        manager.Push(state1);

        var state2 = TestHelpers.CreateState("After");

        // Undo → get "Before"
        var undone = manager.Undo(state2);
        Assert.Equal("Before", TestHelpers.GetParaText(undone, 0));

        // Redo → get "After"
        var redone = manager.Redo(undone);
        Assert.Equal("After", TestHelpers.GetParaText(redone, 0));
    }

    [Fact]
    public void Push_ClearsRedoStack()
    {
        var manager = new HistoryManager();
        var state1 = TestHelpers.CreateState("First");
        manager.Push(state1);

        var state2 = TestHelpers.CreateState("Second");
        manager.Undo(state2); // Now redo has "Second"

        Assert.True(manager.CanRedo);

        // New push should clear redo
        manager.Push(TestHelpers.CreateState("Third"));
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void Undo_ProducesIndependentState()
    {
        var manager = new HistoryManager();
        var state1 = TestHelpers.CreateState("Original");
        manager.Push(state1);

        var state2 = TestHelpers.CreateState("Modified");
        var restored = manager.Undo(state2);

        // Modifying restored should not affect original
        ((Paragraph)restored.Document.Children[0]).Children.Clear();
        Assert.Equal("Original", TestHelpers.GetParaText(state1, 0));
    }

    [Fact]
    public void CanUndo_CanRedo_CorrectStates()
    {
        var manager = new HistoryManager();
        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);

        manager.Push(TestHelpers.CreateState("A"));
        Assert.True(manager.CanUndo);
        Assert.False(manager.CanRedo);

        manager.Undo(TestHelpers.CreateState("B"));
        Assert.False(manager.CanUndo);
        Assert.True(manager.CanRedo);
    }

    [Fact]
    public void Clear_EmptiesBothStacks()
    {
        var manager = new HistoryManager();
        manager.Push(TestHelpers.CreateState("A"));
        manager.Push(TestHelpers.CreateState("B"));
        manager.Undo(TestHelpers.CreateState("C"));

        Assert.True(manager.CanUndo);
        Assert.True(manager.CanRedo);

        manager.Clear();

        Assert.False(manager.CanUndo);
        Assert.False(manager.CanRedo);
    }

    [Fact]
    public void MultipleUndo_MultipleLevels()
    {
        var manager = new HistoryManager();
        manager.Push(TestHelpers.CreateState("First"));
        manager.Push(TestHelpers.CreateState("Second"));
        manager.Push(TestHelpers.CreateState("Third"));

        var state = TestHelpers.CreateState("Current");

        // Undo three times
        state = manager.Undo(state);
        Assert.Equal("Third", TestHelpers.GetParaText(state, 0));

        state = manager.Undo(state);
        Assert.Equal("Second", TestHelpers.GetParaText(state, 0));

        state = manager.Undo(state);
        Assert.Equal("First", TestHelpers.GetParaText(state, 0));

        Assert.False(manager.CanUndo);
    }

    [Fact]
    public void CommandExecutor_UndoRedo_Integration()
    {
        var executor = new CommandExecutor();
        var state = TestHelpers.CreateState("Hello", offset: 5);

        // Type " World"
        state = executor.Execute(new InsertTextCommand(" World"), state);
        Assert.Equal("Hello World", TestHelpers.GetParaText(state, 0));

        // Undo
        Assert.True(executor.CanUndo);
        state = executor.Undo(state);
        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));

        // Redo
        Assert.True(executor.CanRedo);
        state = executor.Redo(state);
        Assert.Equal("Hello World", TestHelpers.GetParaText(state, 0));
    }
}
