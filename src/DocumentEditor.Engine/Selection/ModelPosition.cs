namespace DocumentEditor.Engine.Selection;

public class CellPath
{
    public int RowIndex { get; set; }
    public int CellIndex { get; set; }
    public int CellBlockIndex { get; set; }
}

public class ModelPosition : IEquatable<ModelPosition>, IComparable<ModelPosition>
{
    public int BlockIndex { get; set; }
    public int InlineIndex { get; set; }
    public int Offset { get; set; }

    /// <summary>Non-null when the cursor is inside a table cell paragraph.</summary>
    public CellPath? Cell { get; set; }

    public ModelPosition() { }

    public ModelPosition(int blockIndex, int inlineIndex, int offset)
    {
        BlockIndex = blockIndex;
        InlineIndex = inlineIndex;
        Offset = offset;
    }

    public ModelPosition Clone() => new(BlockIndex, InlineIndex, Offset)
    {
        Cell = Cell is not null
            ? new CellPath { RowIndex = Cell.RowIndex, CellIndex = Cell.CellIndex, CellBlockIndex = Cell.CellBlockIndex }
            : null
    };

    public int CompareTo(ModelPosition? other)
    {
        if (other is null) return 1;
        var block = BlockIndex.CompareTo(other.BlockIndex);
        if (block != 0) return block;
        if (Cell is not null || other.Cell is not null)
        {
            if (Cell is null) return -1;
            if (other.Cell is null) return 1;
            var rowCmp = Cell.RowIndex.CompareTo(other.Cell.RowIndex);
            if (rowCmp != 0) return rowCmp;
            var colCmp = Cell.CellIndex.CompareTo(other.Cell.CellIndex);
            if (colCmp != 0) return colCmp;
            var cbCmp = Cell.CellBlockIndex.CompareTo(other.Cell.CellBlockIndex);
            if (cbCmp != 0) return cbCmp;
        }
        var inline = InlineIndex.CompareTo(other.InlineIndex);
        if (inline != 0) return inline;
        return Offset.CompareTo(other.Offset);
    }

    public bool Equals(ModelPosition? other)
    {
        if (other is null) return false;
        if (BlockIndex != other.BlockIndex || InlineIndex != other.InlineIndex || Offset != other.Offset)
            return false;
        if (Cell is null && other.Cell is null) return true;
        if (Cell is null || other.Cell is null) return false;
        return Cell.RowIndex == other.Cell.RowIndex
            && Cell.CellIndex == other.Cell.CellIndex
            && Cell.CellBlockIndex == other.Cell.CellBlockIndex;
    }

    public override bool Equals(object? obj) => Equals(obj as ModelPosition);

    public override int GetHashCode() => HashCode.Combine(BlockIndex, InlineIndex, Offset,
        Cell?.RowIndex ?? -1, Cell?.CellIndex ?? -1, Cell?.CellBlockIndex ?? -1);

    public static bool operator ==(ModelPosition? left, ModelPosition? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ModelPosition? left, ModelPosition? right)
        => !(left == right);

    public static bool operator <(ModelPosition left, ModelPosition right)
        => left.CompareTo(right) < 0;

    public static bool operator >(ModelPosition left, ModelPosition right)
        => left.CompareTo(right) > 0;

    public static bool operator <=(ModelPosition left, ModelPosition right)
        => left.CompareTo(right) <= 0;

    public static bool operator >=(ModelPosition left, ModelPosition right)
        => left.CompareTo(right) >= 0;

    public override string ToString() => $"({BlockIndex},{InlineIndex},{Offset})";
}
