using DocumentEditor.Engine.Model.Enums;

namespace DocumentEditor.Engine.Model.Properties;

public class CellBorder
{
    public CellBorderStyle Style { get; set; } = CellBorderStyle.Single;
    public int Size { get; set; } = 4;          // 1/8th point units: 4=thin, 8=medium, 12=thick
    public string Color { get; set; } = "auto"; // hex without '#', or "auto"

    public CellBorder DeepClone() => new()
    {
        Style = Style,
        Size = Size,
        Color = Color,
    };
}

public class CellBorders
{
    public CellBorder? Top { get; set; }
    public CellBorder? Bottom { get; set; }
    public CellBorder? Left { get; set; }
    public CellBorder? Right { get; set; }

    public CellBorders DeepClone() => new()
    {
        Top = Top?.DeepClone(),
        Bottom = Bottom?.DeepClone(),
        Left = Left?.DeepClone(),
        Right = Right?.DeepClone(),
    };
}
