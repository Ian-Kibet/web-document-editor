using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.History;

public class HistoryManager
{
    private const int MaxEntries = 200;
    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Push(EditorState state)
    {
        _undoStack.Push(new HistoryEntry
        {
            Document = DocumentCloner.Clone(state.Document),
            Selection = state.Selection.Clone()
        });

        _redoStack.Clear();

        // Trim to max size
        if (_undoStack.Count > MaxEntries)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (var i = MaxEntries - 1; i >= 0; i--)
                _undoStack.Push(items[i]);
        }
    }

    public EditorState Undo(EditorState currentState)
    {
        if (!CanUndo) return currentState;

        _redoStack.Push(new HistoryEntry
        {
            Document = DocumentCloner.Clone(currentState.Document),
            Selection = currentState.Selection.Clone()
        });

        var entry = _undoStack.Pop();
        return new EditorState
        {
            Document = entry.Document,
            Selection = entry.Selection
        };
    }

    public EditorState Redo(EditorState currentState)
    {
        if (!CanRedo) return currentState;

        _undoStack.Push(new HistoryEntry
        {
            Document = DocumentCloner.Clone(currentState.Document),
            Selection = currentState.Selection.Clone()
        });

        var entry = _redoStack.Pop();
        return new EditorState
        {
            Document = entry.Document,
            Selection = entry.Selection
        };
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
