using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class EdgeCaseTests
{
    // ─── Delete at document boundaries ────────────────────────────

    [Fact]
    public void DeleteForward_AtEndOfDocument_IsNoOp()
    {
        var state = TestHelpers.CreateState("ABC", offset: 3);
        var original = TestHelpers.GetParaText(state, 0);

        state = new DeleteForwardCommand().Execute(state);

        Assert.Equal(original, TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteBackward_AtStartOfDocument_IsNoOp()
    {
        var state = TestHelpers.CreateState("ABC", offset: 0);
        state = new DeleteBackwardCommand().Execute(state);

        Assert.Equal("ABC", TestHelpers.GetParaText(state, 0));
        Assert.Equal(0, state.Selection.Anchor.Offset);
    }

    // ─── Delete with empty paragraphs ─────────────────────────────

    [Fact]
    public void DeleteBackward_EmptyParagraph_MergesWithPrevious()
    {
        var state = TestHelpers.CreateMultiParaState("First", "");
        state.Selection = SelectionModel.Collapsed(1, 0, 0);

        state = new DeleteBackwardCommand().Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("First", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteForward_IntoEmptyParagraph_MergesIt()
    {
        var state = TestHelpers.CreateMultiParaState("First", "");
        state.Selection = SelectionModel.Collapsed(0, 0, 5);

        state = new DeleteForwardCommand().Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("First", TestHelpers.GetParaText(state, 0));
    }

    // ─── SplitParagraph edge cases ────────────────────────────────

    [Fact]
    public void SplitParagraph_AtStartOfParagraph_CreatesEmptyBefore()
    {
        var state = TestHelpers.CreateState("Hello", offset: 0);
        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("", TestHelpers.GetParaText(state, 0));
        Assert.Equal("Hello", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void SplitParagraph_AtEndOfParagraph_CreatesEmptyAfter()
    {
        var state = TestHelpers.CreateState("Hello", offset: 5);
        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));
        Assert.Equal("", TestHelpers.GetParaText(state, 1));
    }

    [Fact]
    public void SplitParagraph_Heading_NewParagraphIsNormal()
    {
        var state = TestHelpers.CreateState("Title", offset: 5);
        TestHelpers.GetPara(state, 0).Properties.Style = "Heading1";

        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal("Heading1", TestHelpers.GetPara(state, 0).Properties.Style);
        Assert.Null(TestHelpers.GetPara(state, 1).Properties.Style);
    }

    [Fact]
    public void SplitParagraph_InEmptyParagraph_CreatesTwoEmpty()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new SplitParagraphCommand().Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("", TestHelpers.GetParaText(state, 0));
        Assert.Equal("", TestHelpers.GetParaText(state, 1));
    }

    // ─── DeleteSelection across paragraphs ────────────────────────

    [Fact]
    public void DeleteSelection_AcrossParagraphs_MergesRemaining()
    {
        var state = TestHelpers.CreateMultiParaState("Hello", "World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 3),
            Focus = new ModelPosition(1, 0, 2)
        };

        state = new DeleteSelectionCommand().Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("Helrld", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void DeleteSelection_EntireDocument_LeavesEmptyParagraph()
    {
        var state = TestHelpers.CreateMultiParaState("Hello", "World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 0),
            Focus = new ModelPosition(1, 0, 5)
        };

        state = new DeleteSelectionCommand().Execute(state);

        Assert.Single(state.Document.Children);
        Assert.Equal("", TestHelpers.GetParaText(state, 0));
    }

    // ─── Formatting on multi-paragraph range ──────────────────────

    [Fact]
    public void SetAlignment_MultiParagraph_AppliesToAll()
    {
        var state = TestHelpers.CreateMultiParaState("First", "Second", "Third");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 0),
            Focus = new ModelPosition(2, 0, 5)
        };

        state = new SetAlignmentCommand(Alignment.Center).Execute(state);

        Assert.Equal(Alignment.Center, TestHelpers.GetPara(state, 0).Properties.Alignment);
        Assert.Equal(Alignment.Center, TestHelpers.GetPara(state, 1).Properties.Alignment);
        Assert.Equal(Alignment.Center, TestHelpers.GetPara(state, 2).Properties.Alignment);
    }

    [Fact]
    public void ToggleList_MultiParagraph_AppliesToAll()
    {
        var state = TestHelpers.CreateMultiParaState("Item 1", "Item 2", "Item 3");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 0),
            Focus = new ModelPosition(2, 0, 6)
        };

        state = new ToggleListCommand(ListType.Bullet).Execute(state);

        Assert.Equal(1, TestHelpers.GetPara(state, 0).Properties.NumberingId);
        Assert.Equal(1, TestHelpers.GetPara(state, 1).Properties.NumberingId);
        Assert.Equal(1, TestHelpers.GetPara(state, 2).Properties.NumberingId);
    }

    // ─── ToggleList switch type ───────────────────────────────────

    [Fact]
    public void ToggleList_SwitchBulletToNumbered()
    {
        var state = TestHelpers.CreateState("Item");
        state = new ToggleListCommand(ListType.Bullet).Execute(state);
        Assert.Equal(1, TestHelpers.GetPara(state, 0).Properties.NumberingId);

        // Switch to numbered
        state = new ToggleListCommand(ListType.Numbered).Execute(state);
        Assert.Equal(2, TestHelpers.GetPara(state, 0).Properties.NumberingId);
    }

    // ─── SetIndent with first-line indent ─────────────────────────

    [Fact]
    public void SetIndent_FirstLineIndent()
    {
        var state = TestHelpers.CreateState("Text");
        state = new SetIndentCommand(0, 360).Execute(state);

        Assert.Equal(360, TestHelpers.GetPara(state, 0).Properties.IndentFirstLine);
    }

    [Fact]
    public void SetIndent_CumulativeIndent()
    {
        var state = TestHelpers.CreateState("Text");
        state = new SetIndentCommand(720).Execute(state);
        state = new SetIndentCommand(720).Execute(state);

        Assert.Equal(1440, TestHelpers.GetPara(state, 0).Properties.IndentLeft);
    }

    // ─── InsertText with special characters ───────────────────────

    [Fact]
    public void InsertText_Unicode()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new InsertTextCommand("日本語テスト").Execute(state);

        Assert.Equal("日本語テスト", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void InsertText_Emoji()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new InsertTextCommand("Hello 🌍").Execute(state);

        Assert.Equal("Hello 🌍", TestHelpers.GetParaText(state, 0));
    }

    // ─── PasteText edge cases ─────────────────────────────────────

    [Fact]
    public void PasteText_EmptyString_IsNoOp()
    {
        var state = TestHelpers.CreateState("Hello", offset: 5);
        state = new PasteTextCommand("").Execute(state);

        Assert.Equal("Hello", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void PasteText_SingleNewline_SplitsParagraph()
    {
        var state = TestHelpers.CreateState("AB", offset: 1);
        state = new PasteTextCommand("\n").Execute(state);

        Assert.Equal(2, state.Document.Children.Count);
        Assert.Equal("A", TestHelpers.GetParaText(state, 0));
        Assert.Equal("B", TestHelpers.GetParaText(state, 1));
    }

    // ─── CommandExecutor undo/redo integration ────────────────────

    [Fact]
    public void CommandExecutor_UndoRedo_MultipleCommands()
    {
        var executor = new CommandExecutor();
        var state = TestHelpers.CreateState("", offset: 0);

        state = executor.Execute(new InsertTextCommand("A"), state);
        state = executor.Execute(new InsertTextCommand("B"), state);
        state = executor.Execute(new InsertTextCommand("C"), state);
        Assert.Equal("ABC", TestHelpers.GetParaText(state, 0));

        state = executor.Undo(state);
        Assert.Equal("AB", TestHelpers.GetParaText(state, 0));

        state = executor.Undo(state);
        Assert.Equal("A", TestHelpers.GetParaText(state, 0));

        state = executor.Redo(state);
        Assert.Equal("AB", TestHelpers.GetParaText(state, 0));
    }

    [Fact]
    public void CommandExecutor_NewCommandClearsRedoStack()
    {
        var executor = new CommandExecutor();
        var state = TestHelpers.CreateState("", offset: 0);

        state = executor.Execute(new InsertTextCommand("A"), state);
        state = executor.Execute(new InsertTextCommand("B"), state);
        state = executor.Undo(state);
        Assert.True(executor.CanRedo);

        state = executor.Execute(new InsertTextCommand("C"), state);
        Assert.False(executor.CanRedo);
        Assert.Equal("AC", TestHelpers.GetParaText(state, 0));
    }

    // ─── InsertTable in multi-paragraph document ──────────────────

    [Fact]
    public void InsertTable_InMiddleOfDocument()
    {
        var state = TestHelpers.CreateMultiParaState("Before", "After");
        state.Selection = SelectionModel.Collapsed(0, 0, 6);

        state = new InsertTableCommand(2, 2).Execute(state);

        // Should have: Before, Table, empty para, After
        Assert.True(state.Document.Children.Count >= 3);
        Assert.IsType<Table>(state.Document.Children[1]);
    }

    // ─── Large document stability ─────────────────────────────────

    [Fact]
    public void InsertText_ManyParagraphs_Stable()
    {
        var texts = Enumerable.Range(0, 50).Select(i => $"Paragraph {i}").ToArray();
        var state = TestHelpers.CreateMultiParaState(texts);
        state.Selection = SelectionModel.Collapsed(25, 0, 0);

        state = new InsertTextCommand("Inserted").Execute(state);

        Assert.Equal(50, state.Document.Children.Count);
        Assert.StartsWith("Inserted", TestHelpers.GetParaText(state, 25));
    }
}
