using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Selection;

public class ModelPositionTests
{
    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        var a = new ModelPosition(1, 2, 3);
        var b = new ModelPosition(1, 2, 3);
        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var a = new ModelPosition(1, 2, 3);
        var b = new ModelPosition(1, 2, 4);
        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new ModelPosition(0, 0, 0);
        Assert.False(a.Equals(null));
    }

    [Fact]
    public void CompareTo_BlockOrder()
    {
        var a = new ModelPosition(0, 5, 5);
        var b = new ModelPosition(1, 0, 0);
        Assert.True(a < b);
        Assert.True(b > a);
    }

    [Fact]
    public void CompareTo_InlineOrder()
    {
        var a = new ModelPosition(1, 0, 5);
        var b = new ModelPosition(1, 1, 0);
        Assert.True(a < b);
    }

    [Fact]
    public void CompareTo_OffsetOrder()
    {
        var a = new ModelPosition(1, 1, 2);
        var b = new ModelPosition(1, 1, 5);
        Assert.True(a < b);
        Assert.True(a <= b);
    }

    [Fact]
    public void CompareTo_Equal()
    {
        var a = new ModelPosition(1, 2, 3);
        var b = new ModelPosition(1, 2, 3);
        Assert.True(a <= b);
        Assert.True(a >= b);
        Assert.Equal(0, a.CompareTo(b));
    }

    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var a = new ModelPosition(1, 2, 3);
        var b = a.Clone();
        Assert.Equal(a, b);
        b.Offset = 99;
        Assert.NotEqual(a.Offset, b.Offset);
    }

    [Fact]
    public void GetHashCode_SameForEqualPositions()
    {
        var a = new ModelPosition(1, 2, 3);
        var b = new ModelPosition(1, 2, 3);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
