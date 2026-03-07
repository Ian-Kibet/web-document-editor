using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class SplitParagraphCommandTests
{
    [Fact]
    public void SplitParagraph_Middle()
    {
        var state = TestHelpers.CreateState("Hello", offset: 2);
        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("He", TestHelpers.GetParaText(state, 0));
        Assert.Equal("llo", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void SplitParagraph_AtStart()
    {
        var state = TestHelpers.CreateState("Hello", offset: 0);
        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("", TestHelpers.GetParaText(state, 0));
        Assert.Equal("Hello", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void SplitParagraph_AtEnd()
    {
        var state = TestHelpers.CreateState("Hello", offset: 5);
        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));
        Assert.Equal("", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void SplitParagraph_HeadingResetsToNormal()
    {
        var state = TestHelpers.CreateState("Title Text", offset: 5);
        TestHelpers.GetPara(state, 0).Properties.Style = "Heading1";

        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal("Heading1", TestHelpers.GetPara(state, 0).Properties.Style);
        Assert.Null(TestHelpers.GetPara(state, 1).Properties.Style);
    }

    [Fact]
    public void SplitParagraph_NonHeadingInheritsStyle()
    {
        var state = TestHelpers.CreateState("Body text", offset: 4);
        TestHelpers.GetPara(state, 0).Properties.Style = "Quote";
        TestHelpers.GetPara(state, 0).Properties.Alignment = Alignment.Center;

        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal("Quote", TestHelpers.GetPara(state, 1).Properties.Style);
        Assert.Equal(Alignment.Center, TestHelpers.GetPara(state, 1).Properties.Alignment);
    }

    [Fact]
    public void SplitParagraph_WithRangeSelection_DeletesFirst()
    {
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 5),
            Focus = new ModelPosition(0, 0, 6)
        };

        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));
        Assert.Equal("World", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void SplitParagraph_CursorPosition()
    {
        var state = TestHelpers.CreateState("Hello", offset: 3);
        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(1, state.Selection.Anchor.BlockIndex);
        Assert.Equal(0, state.Selection.Anchor.InlineIndex);
        Assert.Equal(0, state.Selection.Anchor.Offset);
        Assert.True(state.Selection.IsCollapsed);
    }

    [Fact]
    public void SplitParagraph_InheritsNumbering()
    {
        var state = TestHelpers.CreateState("Item one", offset: 4);
        TestHelpers.GetPara(state, 0).Properties.NumberingId = 1;
        TestHelpers.GetPara(state, 0).Properties.NumberingLevel = 0;

        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(1, TestHelpers.GetPara(state, 1).Properties.NumberingId);
        Assert.Equal(0, TestHelpers.GetPara(state, 1).Properties.NumberingLevel);
    }
}
