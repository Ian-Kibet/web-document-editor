namespace DocumentEditor.Engine.Selection;

public class SelectionModel
{
    public ModelPosition Anchor { get; set; } = new();
    public ModelPosition Focus { get; set; } = new();

    public bool IsCollapsed => Anchor.Equals(Focus);

    public static SelectionModel Collapsed(int blockIndex, int inlineIndex, int offset)
        => new()
        {
            Anchor = new ModelPosition(blockIndex, inlineIndex, offset),
            Focus = new ModelPosition(blockIndex, inlineIndex, offset)
        };

    public static SelectionModel Collapsed(ModelPosition pos)
        => new()
        {
            Anchor = pos.Clone(),
            Focus = pos.Clone()
        };

    public SelectionModel Clone() => new()
    {
        Anchor = Anchor.Clone(),
        Focus = Focus.Clone()
    };
}
