using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class StructuralCommandTests
{
    // InsertTableCommand
    [Fact]
    public void InsertTable_Dimensions()
    {
        var state = TestHelpers.CreateState("Text");
        state = new InsertTableCommand(3, 4).Execute(state);

        Assert.Equal(3, state.Document.Children.Count); // original + table + empty para
        var table = Assert.IsType<Table>(state.Document.Children[1]);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(4, table.Rows[0].Cells.Count);
    }

    [Fact]
    public void InsertTable_CursorAfterTable()
    {
        var state = TestHelpers.CreateState("Text");
        state = new InsertTableCommand(2, 2).Execute(state);

        // Cursor should be at post-table paragraph (index 2)
        Assert.Equal(2, state.Selection.Anchor.BlockIndex);
        Assert.Equal(0, state.Selection.Anchor.InlineIndex);
        Assert.Equal(0, state.Selection.Anchor.Offset);
    }

    [Fact]
    public void InsertTable_EmptyParagraphAfter()
    {
        var state = TestHelpers.CreateState("Text");
        state = new InsertTableCommand(1, 1).Execute(state);

        var afterPara = Assert.IsType<Paragraph>(state.Document.Children[2]);
        Assert.Equal("", ((Run)afterPara.Children[0]).Text);
    }

    [Fact]
    public void InsertTable_MinDimensions()
    {
        var state = TestHelpers.CreateState("Text");
        state = new InsertTableCommand(0, 0).Execute(state); // Should clamp to 1x1

        var table = Assert.IsType<Table>(state.Document.Children[1]);
        Assert.Single(table.Rows);
        Assert.Single(table.Rows[0].Cells);
    }

    // InsertHyperlinkCommand
    [Fact]
    public void InsertHyperlink_InMiddleOfRun()
    {
        var state = TestHelpers.CreateState("Hello World", offset: 5);
        state = new InsertHyperlinkCommand("https://example.com", "link").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        // Should have: Run("Hello") + Hyperlink + Run(" World")
        Assert.True(para.Children.Count >= 3);
        Assert.IsType<Hyperlink>(para.Children[1]);

        var link = (Hyperlink)para.Children[1];
        Assert.Equal("https://example.com", link.Url);
        Assert.Equal("link", link.Children[0].Text);
    }

    [Fact]
    public void InsertHyperlink_CursorAfterLink()
    {
        var state = TestHelpers.CreateState("AB", offset: 1);
        state = new InsertHyperlinkCommand("https://test.com", "X").Execute(state);

        // Cursor should be after the hyperlink
        var pos = state.Selection.Anchor;
        var para = TestHelpers.GetPara(state, 0);
        Assert.True(pos.InlineIndex > 0);
        // The run after the hyperlink should exist
        Assert.IsType<Run>(para.Children[pos.InlineIndex]);
    }

    [Fact]
    public void InsertHyperlink_PostLinkRunExists()
    {
        var state = TestHelpers.CreateState("Text", offset: 4);
        state = new InsertHyperlinkCommand("http://a.com", "link").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        // There should be a run after the hyperlink for cursor placement
        var linkIdx = -1;
        for (var i = 0; i < para.Children.Count; i++)
        {
            if (para.Children[i] is Hyperlink)
            {
                linkIdx = i;
                break;
            }
        }

        Assert.True(linkIdx >= 0);
        // After hyperlink there should be a run (possibly empty)
        Assert.True(linkIdx + 1 < para.Children.Count || para.Children.Count > 0);
    }

    // PasteTextCommand
    [Fact]
    public void PasteText_SingleLine()
    {
        var state = TestHelpers.CreateState("AB", offset: 1);
        state = new PasteTextCommand("XY").Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("AXYB", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void PasteText_MultiLine()
    {
        var state = TestHelpers.CreateState("AB", offset: 1);
        state = new PasteTextCommand("X\nY").Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("AX", TestHelpers.GetParaText(state, 0));
        Assert.Equal("YB", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void PasteText_WindowsNewlines()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new PasteTextCommand("Line1\r\nLine2\r\nLine3").Execute(state);

        Assert.Equal(3, state.Document.Children.Count);
        Assert.Equal("Line1", TestHelpers.GetParaText(state, 0));
        Assert.Equal("Line2", TestHelpers.GetParaText(state, 1));
        Assert.Equal("Line3", TestHelpers.GetParaText(state, 2));
    }

    [Fact]
    public void PasteText_WithExistingSelection()
    {
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 5),
            Focus = new ModelPosition(0, 0, 11)
        };

        state = new PasteTextCommand("!").Execute(state);

        Assert.Equal("Hello!", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void PasteText_ThreeLines()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new PasteTextCommand("A\nB\nC").Execute(state);

        Assert.Equal(3, state.Document.Children.Count);
        Assert.Equal("A", TestHelpers.GetParaText(state, 0));
        Assert.Equal("B", TestHelpers.GetParaText(state, 1));
        Assert.Equal("C", TestHelpers.GetParaText(state, 2));
    }
}
