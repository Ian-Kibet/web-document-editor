namespace DocumentEditor.Engine.Commands;

public interface ICommand
{
    EditorState Execute(EditorState state);
}
