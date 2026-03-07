using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Selection;

public class SelectionHelperTests
{
    [Fact]
    public void Normalize_AnchorBeforeFocus_ReturnsSameOrder()
    {
        var sel = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 2),
            Focus = new ModelPosition(0, 0, 5)
        };
        var (start, end) = SelectionHelper.Normalize(sel);
        Assert.Equal(new ModelPosition(0, 0, 2), start);
        Assert.Equal(new ModelPosition(0, 0, 5), end);
    }

    [Fact]
    public void Normalize_AnchorAfterFocus_ReversesOrder()
    {
        var sel = new SelectionModel
        {
            Anchor = new ModelPosition(1, 0, 0),
            Focus = new ModelPosition(0, 0, 3)
        };
        var (start, end) = SelectionHelper.Normalize(sel);
        Assert.Equal(new ModelPosition(0, 0, 3), start);
        Assert.Equal(new ModelPosition(1, 0, 0), end);
    }

    [Fact]
    public void GetBlockRange_SameBlock()
    {
        var sel = new SelectionModel
        {
            Anchor = new ModelPosition(2, 0, 0),
            Focus = new ModelPosition(2, 1, 3)
        };
        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(sel);
        Assert.Equal(2, startBlock);
        Assert.Equal(2, endBlock);
    }

    [Fact]
    public void GetBlockRange_AcrossBlocks()
    {
        var sel = new SelectionModel
        {
            Anchor = new ModelPosition(3, 0, 0),
            Focus = new ModelPosition(0, 0, 0)
        };
        var (startBlock, endBlock) = SelectionHelper.GetBlockRange(sel);
        Assert.Equal(0, startBlock);
        Assert.Equal(3, endBlock);
    }

    [Fact]
    public void GetInlineTextLength_Run()
    {
        var run = DocFactory.CreateRun("Hello");
        Assert.Equal(5, SelectionHelper.GetInlineTextLength(run));
    }

    [Fact]
    public void GetInlineTextLength_EmptyRun()
    {
        var run = DocFactory.CreateRun("");
        Assert.Equal(0, SelectionHelper.GetInlineTextLength(run));
    }

    [Fact]
    public void GetInlineTextLength_Hyperlink()
    {
        var link = DocFactory.CreateHyperlink("http://example.com", "click here");
        Assert.Equal(10, SelectionHelper.GetInlineTextLength(link));
    }

    [Fact]
    public void GetParagraphTextLength_SingleRun()
    {
        var para = DocFactory.CreateParagraph("Hello world");
        Assert.Equal(11, SelectionHelper.GetParagraphTextLength(para));
    }

    [Fact]
    public void GetParagraphTextLength_MultipleRuns()
    {
        var para = new Paragraph
        {
            Children = [DocFactory.CreateRun("Hello"), DocFactory.CreateRun(" world")]
        };
        Assert.Equal(11, SelectionHelper.GetParagraphTextLength(para));
    }

    [Fact]
    public void ResolveToRun_ValidPosition()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Hello"));
        var result = SelectionHelper.ResolveToRun(doc, new ModelPosition(0, 0, 3));
        Assert.NotNull(result);
        Assert.Equal("Hello", result.Value.Run.Text);
        Assert.Equal(3, result.Value.CharOffset);
    }

    [Fact]
    public void ResolveToRun_InvalidBlock_ReturnsNull()
    {
        var doc = DocFactory.CreateDocument(DocFactory.CreateParagraph("Hi"));
        Assert.Null(SelectionHelper.ResolveToRun(doc, new ModelPosition(5, 0, 0)));
    }

    // FindWordInterior tests

    [Theory]
    [InlineData(1, 0, 5)]
    [InlineData(2, 0, 5)]
    [InlineData(3, 0, 5)]
    public void FindWordInterior_ReturnsWordBounds_WhenStrictlyInterior(int offset, int expectedStart, int expectedEnd)
    {
        var result = SelectionHelper.FindWordInterior("hello", offset);
        Assert.NotNull(result);
        Assert.Equal((expectedStart, expectedEnd), result.Value);
    }

    [Fact]
    public void FindWordInterior_ReturnsNull_OnFirstChar()
    {
        Assert.Null(SelectionHelper.FindWordInterior("hello", 0));
    }

    [Fact]
    public void FindWordInterior_ReturnsNull_OnLastChar()
    {
        Assert.Null(SelectionHelper.FindWordInterior("hello", 4));
    }

    [Fact]
    public void FindWordInterior_ReturnsNull_PastEnd()
    {
        Assert.Null(SelectionHelper.FindWordInterior("hello", 5));
    }

    [Fact]
    public void FindWordInterior_ReturnsNull_OnWhitespace()
    {
        Assert.Null(SelectionHelper.FindWordInterior("hello world", 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void FindWordInterior_ReturnsNull_SingleCharWord(int offset)
    {
        Assert.Null(SelectionHelper.FindWordInterior("a b c", offset));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FindWordInterior_ReturnsNull_TwoCharWord(int offset)
    {
        Assert.Null(SelectionHelper.FindWordInterior("ab cd", offset));
    }

    [Fact]
    public void FindWordInterior_ThreeCharWord_MiddleIsInterior()
    {
        var result = SelectionHelper.FindWordInterior("abc", 1);
        Assert.NotNull(result);
        Assert.Equal((0, 3), result.Value);
    }

    [Fact]
    public void FindWordInterior_ReturnsNull_EmptyString()
    {
        Assert.Null(SelectionHelper.FindWordInterior("", 0));
    }

    [Fact]
    public void FindWordInterior_MultiWord_Interior()
    {
        var result = SelectionHelper.FindWordInterior("Type hello world", 7);
        Assert.NotNull(result);
        Assert.Equal((5, 10), result.Value);
    }
}
