using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.Selection;

namespace DocumentEditor.Engine.Tests.Commands;

public class FormattingCommandTests
{
    // ToggleFormatCommand
    [Fact]
    public void ToggleBold_Collapsed_TogglesCurrentRun()
    {
        var state = TestHelpers.CreateState("Hello", offset: 2);
        Assert.False(TestHelpers.GetRun(state, 0, 0).Properties.Bold);

        state = new ToggleFormatCommand("bold").Execute(state);
        Assert.True(TestHelpers.GetRun(state, 0, 0).Properties.Bold);

        state = new ToggleFormatCommand("bold").Execute(state);
        Assert.False(TestHelpers.GetRun(state, 0, 0).Properties.Bold);
    }

    [Fact]
    public void ToggleBold_Range_AppliesBoldToAllRuns()
    {
        var state = TestHelpers.CreateState("Hello World");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 0),
            Focus = new ModelPosition(0, 0, 11)
        };

        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        foreach (var child in para.Children)
        {
            if (child is Run run)
                Assert.True(run.Properties.Bold);
        }
    }

    [Fact]
    public void ToggleBold_Range_AllOn_TurnsOff()
    {
        var doc = DocFactory.CreateDocument(new Paragraph
        {
            Children =
            [
                DocFactory.CreateRun("Hello", new RunProperties { Bold = true }),
                DocFactory.CreateRun(" World", new RunProperties { Bold = true })
            ]
        });
        var state = new EditorState
        {
            Document = doc,
            Selection = new SelectionModel
            {
                Anchor = new ModelPosition(0, 0, 0),
                Focus = new ModelPosition(0, 1, 6)
            }
        };

        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        foreach (var child in para.Children)
        {
            if (child is Run run)
                Assert.False(run.Properties.Bold);
        }
    }

    [Fact]
    public void ToggleUnderline_CollapsedToggle()
    {
        var state = TestHelpers.CreateState("Text", offset: 0);
        state = new ToggleFormatCommand("underline").Execute(state);
        Assert.Equal(UnderlineType.Single, TestHelpers.GetRun(state, 0, 0).Properties.Underline);

        state = new ToggleFormatCommand("underline").Execute(state);
        Assert.Null(TestHelpers.GetRun(state, 0, 0).Properties.Underline);
    }

    [Fact]
    public void ToggleItalic_Collapsed()
    {
        var state = TestHelpers.CreateState("Text", offset: 0);
        state = new ToggleFormatCommand("italic").Execute(state);
        Assert.True(TestHelpers.GetRun(state, 0, 0).Properties.Italic);
    }

    [Fact]
    public void ToggleStrikethrough_Collapsed()
    {
        var state = TestHelpers.CreateState("Text", offset: 0);
        state = new ToggleFormatCommand("strikethrough").Execute(state);
        Assert.True(TestHelpers.GetRun(state, 0, 0).Properties.Strikethrough);
    }

    // SetParagraphStyleCommand
    [Fact]
    public void SetParagraphStyle_Heading1()
    {
        var state = TestHelpers.CreateState("Title");
        state = new SetParagraphStyleCommand("Heading1").Execute(state);
        Assert.Equal("Heading1", TestHelpers.GetPara(state, 0).Properties.Style);
    }

    [Fact]
    public void SetParagraphStyle_Normal_SetsNull()
    {
        var state = TestHelpers.CreateState("Title");
        TestHelpers.GetPara(state, 0).Properties.Style = "Heading1";

        state = new SetParagraphStyleCommand("Normal").Execute(state);
        Assert.Null(TestHelpers.GetPara(state, 0).Properties.Style);
    }

    [Fact]
    public void SetParagraphStyle_MultiParagraphRange()
    {
        var state = TestHelpers.CreateMultiParaState("First", "Second", "Third");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 0),
            Focus = new ModelPosition(2, 0, 0)
        };

        state = new SetParagraphStyleCommand("Heading2").Execute(state);

        Assert.Equal("Heading2", TestHelpers.GetPara(state, 0).Properties.Style);
        Assert.Equal("Heading2", TestHelpers.GetPara(state, 1).Properties.Style);
        Assert.Equal("Heading2", TestHelpers.GetPara(state, 2).Properties.Style);
    }

    // SetAlignmentCommand
    [Fact]
    public void SetAlignment_Center()
    {
        var state = TestHelpers.CreateState("Text");
        state = new SetAlignmentCommand(Alignment.Center).Execute(state);
        Assert.Equal(Alignment.Center, TestHelpers.GetPara(state, 0).Properties.Alignment);
    }

    // ToggleListCommand
    [Fact]
    public void ToggleList_Bullet_On()
    {
        var state = TestHelpers.CreateState("Item");
        state = new ToggleListCommand(ListType.Bullet).Execute(state);

        Assert.Equal(1, TestHelpers.GetPara(state, 0).Properties.NumberingId);
        Assert.Equal(0, TestHelpers.GetPara(state, 0).Properties.NumberingLevel);
    }

    [Fact]
    public void ToggleList_Bullet_Off()
    {
        var state = TestHelpers.CreateState("Item");
        TestHelpers.GetPara(state, 0).Properties.NumberingId = 1;
        TestHelpers.GetPara(state, 0).Properties.NumberingLevel = 0;

        state = new ToggleListCommand(ListType.Bullet).Execute(state);

        Assert.Null(TestHelpers.GetPara(state, 0).Properties.NumberingId);
        Assert.Null(TestHelpers.GetPara(state, 0).Properties.NumberingLevel);
    }

    [Fact]
    public void ToggleList_Numbered()
    {
        var state = TestHelpers.CreateState("Item");
        state = new ToggleListCommand(ListType.Numbered).Execute(state);
        Assert.Equal(2, TestHelpers.GetPara(state, 0).Properties.NumberingId);
    }

    // SetIndentCommand
    [Fact]
    public void SetIndent_Increase()
    {
        var state = TestHelpers.CreateState("Text");
        state = new SetIndentCommand(720).Execute(state);
        Assert.Equal(720, TestHelpers.GetPara(state, 0).Properties.IndentLeft);
    }

    [Fact]
    public void SetIndent_Decrease()
    {
        var state = TestHelpers.CreateState("Text");
        TestHelpers.GetPara(state, 0).Properties.IndentLeft = 720;

        state = new SetIndentCommand(-360).Execute(state);
        Assert.Equal(360, TestHelpers.GetPara(state, 0).Properties.IndentLeft);
    }

    [Fact]
    public void SetIndent_ClampToZero()
    {
        var state = TestHelpers.CreateState("Text");
        TestHelpers.GetPara(state, 0).Properties.IndentLeft = 200;

        state = new SetIndentCommand(-500).Execute(state);
        Assert.Null(TestHelpers.GetPara(state, 0).Properties.IndentLeft); // 0 maps to null
    }

    // Collapsed-cursor word-interior formatting tests

    [Fact]
    public void ToggleBold_Collapsed_InteriorOfWord_OnlyFormatsWord()
    {
        // "Type hello world", offset 7 (interior of "hello") → only "hello" bold
        var state = TestHelpers.CreateState("Type hello world", offset: 7);
        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Equal("Type hello world", TestHelpers.GetParaText(state, 0));

        var boldRuns = para.Children.OfType<Run>().Where(r => r.Properties.Bold).ToList();
        Assert.Single(boldRuns);
        Assert.Equal("hello", boldRuns[0].Text);

        var nonBoldRuns = para.Children.OfType<Run>().Where(r => !r.Properties.Bold).ToList();
        var nonBoldText = string.Join("", nonBoldRuns.Select(r => r.Text));
        Assert.Equal("Type  world", nonBoldText);
    }

    [Fact]
    public void ToggleBold_Collapsed_FirstCharOfWord_TogglesEntireRun()
    {
        // "hello world", offset 0 (first char) → entire run toggled
        var state = TestHelpers.CreateState("hello world", offset: 0);
        Assert.False(TestHelpers.GetRun(state, 0, 0).Properties.Bold);

        state = new ToggleFormatCommand("bold").Execute(state);

        // Should be a single run, all bold
        var para = TestHelpers.GetPara(state, 0);
        Assert.Single(para.Children);
        Assert.True(((Run)para.Children[0]).Properties.Bold);
    }

    [Fact]
    public void ToggleBold_Collapsed_LastCharOfWord_TogglesEntireRun()
    {
        // "hello world", offset 4 (last char of "hello") → entire run toggled
        var state = TestHelpers.CreateState("hello world", offset: 4);
        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Single(para.Children);
        Assert.True(((Run)para.Children[0]).Properties.Bold);
    }

    [Fact]
    public void ToggleBold_Collapsed_OnSpace_TogglesEntireRun()
    {
        // "hello world", offset 5 (space) → entire run toggled
        var state = TestHelpers.CreateState("hello world", offset: 5);
        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Single(para.Children);
        Assert.True(((Run)para.Children[0]).Properties.Bold);
    }

    [Fact]
    public void ToggleBold_Collapsed_SingleCharWord_TogglesEntireRun()
    {
        // "a b c", offset 2 (single char word "b") → entire run toggled
        var state = TestHelpers.CreateState("a b c", offset: 2);
        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Single(para.Children);
        Assert.True(((Run)para.Children[0]).Properties.Bold);
    }

    [Fact]
    public void ToggleBold_Collapsed_EmptyRun_TogglesEntireRun()
    {
        var state = TestHelpers.CreateState("", offset: 0);
        state = new ToggleFormatCommand("bold").Execute(state);

        Assert.True(TestHelpers.GetRun(state, 0, 0).Properties.Bold);
    }

    // Regression tests: ToggleFormatCommand mid-run selection
    [Fact]
    public void ToggleBold_MidRunSelection_OnlySelectedTextIsBold()
    {
        // "Type hello world" → select offset 5→10 → only "hello" should be bold
        var state = TestHelpers.CreateState("Type hello world");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 5),
            Focus = new ModelPosition(0, 0, 10)
        };

        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Equal("Type hello world", TestHelpers.GetParaText(state, 0));

        // Find the bold run — should be exactly "hello"
        var boldRuns = para.Children.OfType<Run>().Where(r => r.Properties.Bold).ToList();
        Assert.Single(boldRuns);
        Assert.Equal("hello", boldRuns[0].Text);

        // Non-bold runs should contain the rest
        var nonBoldRuns = para.Children.OfType<Run>().Where(r => !r.Properties.Bold).ToList();
        var nonBoldText = string.Join("", nonBoldRuns.Select(r => r.Text));
        Assert.Equal("Type  world", nonBoldText);
    }

    [Fact]
    public void ToggleBold_SelectionFromRunStart_OnlySelectedTextIsBold()
    {
        // "hello world" → select 0→5 → only "hello" should be bold
        var state = TestHelpers.CreateState("hello world");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 0),
            Focus = new ModelPosition(0, 0, 5)
        };

        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Equal("hello world", TestHelpers.GetParaText(state, 0));

        var boldRuns = para.Children.OfType<Run>().Where(r => r.Properties.Bold).ToList();
        Assert.Single(boldRuns);
        Assert.Equal("hello", boldRuns[0].Text);
    }

    [Fact]
    public void ToggleBold_SelectionToRunEnd_OnlySelectedTextIsBold()
    {
        // "hello world" → select 6→11 → only "world" should be bold
        var state = TestHelpers.CreateState("hello world");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 6),
            Focus = new ModelPosition(0, 0, 11)
        };

        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Equal("hello world", TestHelpers.GetParaText(state, 0));

        var boldRuns = para.Children.OfType<Run>().Where(r => r.Properties.Bold).ToList();
        Assert.Single(boldRuns);
        Assert.Equal("world", boldRuns[0].Text);
    }

    [Fact]
    public void ToggleBold_SingleCharMidRun_OnlySelectedCharIsBold()
    {
        // "abcde" → select 2→3 → only "c" should be bold
        var state = TestHelpers.CreateState("abcde");
        state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(0, 0, 2),
            Focus = new ModelPosition(0, 0, 3)
        };

        state = new ToggleFormatCommand("bold").Execute(state);

        var para = TestHelpers.GetPara(state, 0);
        Assert.Equal("abcde", TestHelpers.GetParaText(state, 0));

        var boldRuns = para.Children.OfType<Run>().Where(r => r.Properties.Bold).ToList();
        Assert.Single(boldRuns);
        Assert.Equal("c", boldRuns[0].Text);

        var nonBoldRuns = para.Children.OfType<Run>().Where(r => !r.Properties.Bold).ToList();
        var nonBoldText = string.Join("", nonBoldRuns.Select(r => r.Text));
        Assert.Equal("abde", nonBoldText);
    }
}
