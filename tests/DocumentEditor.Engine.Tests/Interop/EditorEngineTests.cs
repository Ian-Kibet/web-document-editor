using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentEditor.Engine.Interop;
using DocumentEditor.Engine.RenderTree;

namespace DocumentEditor.Engine.Tests.Interop;

public class EditorEngineTests
{
    private readonly EditorEngine _engine = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static string MakeSel(int blockIndex = 0, int inlineIndex = 0, int offset = 0)
    {
        return JsonSerializer.Serialize(new
        {
            anchor = new { blockIndex, inlineIndex, offset },
            focus = new { blockIndex, inlineIndex, offset }
        }, JsonOptions);
    }

    private static string MakeRangeSel(
        int aBlock, int aInline, int aOffset,
        int fBlock, int fInline, int fOffset)
    {
        return JsonSerializer.Serialize(new
        {
            anchor = new { blockIndex = aBlock, inlineIndex = aInline, offset = aOffset },
            focus = new { blockIndex = fBlock, inlineIndex = fInline, offset = fOffset }
        }, JsonOptions);
    }

    private static EngineResponse ParseResponse(string json)
    {
        return JsonSerializer.Deserialize<EngineResponse>(json, JsonOptions)!;
    }

    /// <summary>
    /// Get a block-level RenderNode at the given index from the render tree.
    /// The render tree wraps all blocks in section nodes, so this navigates
    /// through the first section to reach the block.
    /// </summary>
    private static RenderNode GetBlock(EngineResponse response, int blockIndex = 0)
    {
        // All blocks are in the first section for single-section test documents
        return response.RenderTree[0].Children![blockIndex];
    }

    // ─── Initialize ───────────────────────────────────────────────

    [Fact]
    public void Initialize_ReturnsValidResponse()
    {
        var responseJson = _engine.Initialize();
        var response = ParseResponse(responseJson);

        Assert.NotNull(response);
        Assert.NotEmpty(response.RenderTree);
        Assert.NotNull(response.Selection);
        Assert.True(response.Selection.IsCollapsed);
        Assert.NotNull(response.FormatState);
        Assert.False(response.CanUndo);
        Assert.False(response.CanRedo);
    }

    [Fact]
    public void Initialize_CreatesOneParagraph()
    {
        var response = ParseResponse(_engine.Initialize());

        Assert.Single(response.RenderTree); // One section
        Assert.Equal("section", response.RenderTree[0].Tag);
        var block = GetBlock(response);
        Assert.Equal("p", block.Tag);
    }

    // ─── InsertText ───────────────────────────────────────────────

    [Fact]
    public void InsertText_InsertsCharacter()
    {
        _engine.Initialize();
        var response = ParseResponse(_engine.InsertText("A", MakeSel(0, 0, 0)));

        var span = GetBlock(response).Children![0];
        Assert.Equal("A", span.Text);
    }

    [Fact]
    public void InsertText_UpdatesSelection()
    {
        _engine.Initialize();
        var response = ParseResponse(_engine.InsertText("Hi", MakeSel(0, 0, 0)));

        Assert.True(response.Selection.IsCollapsed);
        Assert.Equal(2, response.Selection.Anchor.Offset);
    }

    [Fact]
    public void InsertText_EnablesUndo()
    {
        _engine.Initialize();
        var response = ParseResponse(_engine.InsertText("A", MakeSel(0, 0, 0)));

        Assert.True(response.CanUndo);
    }

    // ─── DeleteBackward ───────────────────────────────────────────

    [Fact]
    public void DeleteBackward_RemovesCharacter()
    {
        _engine.Initialize();
        _engine.InsertText("AB", MakeSel(0, 0, 0));
        var response = ParseResponse(_engine.DeleteBackward(MakeSel(0, 0, 2)));

        var span = GetBlock(response).Children![0];
        Assert.Equal("A", span.Text);
    }

    [Fact]
    public void DeleteBackward_AtStartOfDoc_IsNoOp()
    {
        _engine.Initialize();
        _engine.InsertText("A", MakeSel(0, 0, 0));
        var before = ParseResponse(_engine.InsertText("", MakeSel(0, 0, 0)));
        var response = ParseResponse(_engine.DeleteBackward(MakeSel(0, 0, 0)));

        // Should still have the same content
        Assert.NotEmpty(response.RenderTree);
    }

    // ─── DeleteForward ────────────────────────────────────────────

    [Fact]
    public void DeleteForward_RemovesCharacter()
    {
        _engine.Initialize();
        _engine.InsertText("AB", MakeSel(0, 0, 0));
        var response = ParseResponse(_engine.DeleteForward(MakeSel(0, 0, 0)));

        var span = GetBlock(response).Children![0];
        Assert.Equal("B", span.Text);
    }

    // ─── SplitParagraph ───────────────────────────────────────────

    [Fact]
    public void SplitParagraph_CreatesNewParagraph()
    {
        _engine.Initialize();
        _engine.InsertText("AB", MakeSel(0, 0, 0));
        var response = ParseResponse(_engine.SplitParagraph(MakeSel(0, 0, 1)));

        // Both paragraphs are children of the single section
        Assert.Single(response.RenderTree);
        Assert.Equal(2, response.RenderTree[0].Children!.Count);
    }

    // ─── DeleteSelection ──────────────────────────────────────────

    [Fact]
    public void DeleteSelection_RemovesSelectedRange()
    {
        _engine.Initialize();
        _engine.InsertText("ABCDE", MakeSel(0, 0, 0));
        // Select positions B through D (offset 1 to 4)
        var response = ParseResponse(
            _engine.DeleteSelection(MakeRangeSel(0, 0, 1, 0, 0, 4))
        );

        var span = GetBlock(response).Children![0];
        Assert.Equal("AE", span.Text);
    }

    // ─── PasteText ────────────────────────────────────────────────

    [Fact]
    public void PasteText_InsertsText()
    {
        _engine.Initialize();
        var response = ParseResponse(_engine.PasteText("Pasted", MakeSel(0, 0, 0)));

        var span = GetBlock(response).Children![0];
        Assert.Equal("Pasted", span.Text);
    }

    // ─── ToggleFormat ─────────────────────────────────────────────

    [Fact]
    public void ToggleFormat_Bold_UpdatesFormatState()
    {
        _engine.Initialize();
        _engine.InsertText("Hello", MakeSel(0, 0, 0));
        // Select all text and toggle bold
        var response = ParseResponse(
            _engine.ToggleFormat("bold", MakeRangeSel(0, 0, 0, 0, 0, 5))
        );

        Assert.True(response.FormatState.Bold);
    }

    [Fact]
    public void ToggleFormat_Italic_UpdatesFormatState()
    {
        _engine.Initialize();
        _engine.InsertText("Hello", MakeSel(0, 0, 0));
        var response = ParseResponse(
            _engine.ToggleFormat("italic", MakeRangeSel(0, 0, 0, 0, 0, 5))
        );

        Assert.True(response.FormatState.Italic);
    }

    // ─── SetParagraphStyle ────────────────────────────────────────

    [Fact]
    public void SetParagraphStyle_ChangesToHeading()
    {
        _engine.Initialize();
        _engine.InsertText("Title", MakeSel(0, 0, 0));
        var response = ParseResponse(
            _engine.SetParagraphStyle("Heading1", MakeSel(0, 0, 0))
        );

        Assert.Equal("h1", GetBlock(response).Tag);
        Assert.Equal("Heading1", response.FormatState.ParagraphStyle);
    }

    // ─── SetAlignment ─────────────────────────────────────────────

    [Fact]
    public void SetAlignment_Center_UpdatesFormatState()
    {
        _engine.Initialize();
        var response = ParseResponse(
            _engine.SetAlignment("center", MakeSel(0, 0, 0))
        );

        Assert.Equal("center", response.FormatState.Alignment);
    }

    [Fact]
    public void SetAlignment_Center_UpdatesRenderTree()
    {
        _engine.Initialize();
        var response = ParseResponse(
            _engine.SetAlignment("center", MakeSel(0, 0, 0))
        );

        var block = GetBlock(response);
        Assert.NotNull(block.Styles);
        Assert.Equal("center", block.Styles!["text-align"]);
    }

    // ─── ToggleList ───────────────────────────────────────────────

    [Fact]
    public void ToggleList_Bullet_UpdatesFormatState()
    {
        _engine.Initialize();
        var response = ParseResponse(
            _engine.ToggleList("bullet", MakeSel(0, 0, 0))
        );

        Assert.Equal("bullet", response.FormatState.ListType);
    }

    [Fact]
    public void ToggleList_Numbered_UpdatesFormatState()
    {
        _engine.Initialize();
        var response = ParseResponse(
            _engine.ToggleList("numbered", MakeSel(0, 0, 0))
        );

        Assert.Equal("numbered", response.FormatState.ListType);
    }

    // ─── SetIndent ────────────────────────────────────────────────

    [Fact]
    public void SetIndent_AppliesLeftIndent()
    {
        _engine.Initialize();
        _engine.InsertText("Text", MakeSel(0, 0, 0));
        var response = ParseResponse(
            _engine.SetIndent(720, 0, MakeSel(0, 0, 0))
        );

        var block = GetBlock(response);
        Assert.NotNull(block.Styles);
        Assert.Contains("margin-left", block.Styles!.Keys);
    }

    // ─── InsertTable ──────────────────────────────────────────────

    [Fact]
    public void InsertTable_CreatesTableInRenderTree()
    {
        _engine.Initialize();
        var response = ParseResponse(
            _engine.InsertTable(2, 3, MakeSel(0, 0, 0))
        );

        var sectionChildren = response.RenderTree[0].Children!;
        var tableNode = sectionChildren.FirstOrDefault(n => n.Tag == "table");
        Assert.NotNull(tableNode);
        var rows = tableNode.Children!.Where(c => c.Tag == "tr").ToList();
        Assert.Equal(2, rows.Count); // 2 rows
        Assert.Equal(3, rows[0].Children!.Count); // 3 cols
    }

    // ─── InsertHyperlink ──────────────────────────────────────────

    [Fact]
    public void InsertHyperlink_CreatesLinkInRenderTree()
    {
        _engine.Initialize();
        var response = ParseResponse(
            _engine.InsertHyperlink("https://example.com", "Example", MakeSel(0, 0, 0))
        );

        // Find the <a> node in the first paragraph
        var paraChildren = GetBlock(response).Children;
        Assert.NotNull(paraChildren);
        var aNode = paraChildren!.FirstOrDefault(c => c.Tag == "a");
        Assert.NotNull(aNode);
        Assert.Equal("https://example.com", aNode.Attrs!["href"]);
    }

    // ─── Undo / Redo ──────────────────────────────────────────────

    [Fact]
    public void Undo_RevertsLastCommand()
    {
        _engine.Initialize();
        _engine.InsertText("Hello", MakeSel(0, 0, 0));
        var response = ParseResponse(_engine.Undo());

        var span = GetBlock(response).Children![0];
        Assert.Equal("", span.Text);
        Assert.True(response.CanRedo);
        Assert.False(response.CanUndo);
    }

    [Fact]
    public void Redo_ReappliesUndo()
    {
        _engine.Initialize();
        _engine.InsertText("Hello", MakeSel(0, 0, 0));
        _engine.Undo();
        var response = ParseResponse(_engine.Redo());

        var span = GetBlock(response).Children![0];
        Assert.Equal("Hello", span.Text);
        Assert.True(response.CanUndo);
        Assert.False(response.CanRedo);
    }

    [Fact]
    public void Undo_OnEmptyHistory_DoesNotCrash()
    {
        _engine.Initialize();
        var response = ParseResponse(_engine.Undo());

        Assert.NotNull(response);
        Assert.NotEmpty(response.RenderTree);
    }

    // ─── ExportDocx / ImportDocx ──────────────────────────────────

    [Fact]
    public void ExportDocx_ReturnsValidZipBytes()
    {
        _engine.Initialize();
        _engine.InsertText("Export test", MakeSel(0, 0, 0));

        var bytes = _engine.ExportDocx();

        Assert.True(bytes.Length > 4);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public void ImportDocx_LoadsDocument()
    {
        _engine.Initialize();
        _engine.InsertText("Round trip", MakeSel(0, 0, 0));
        var bytes = _engine.ExportDocx();

        var response = ParseResponse(_engine.ImportDocx(bytes));

        Assert.NotEmpty(response.RenderTree);
        Assert.True(response.Selection.IsCollapsed);
        Assert.Equal(0, response.Selection.Anchor.BlockIndex);
    }

    [Fact]
    public void ExportImportRoundTrip_PreservesText()
    {
        _engine.Initialize();
        _engine.InsertText("Hello World", MakeSel(0, 0, 0));
        var bytes = _engine.ExportDocx();
        var response = ParseResponse(_engine.ImportDocx(bytes));

        // Find the text in the render tree (sections → blocks → inlines)
        var texts = response.RenderTree
            .SelectMany(section => section.Children ?? [])   // blocks
            .SelectMany(block => block.Children ?? [])       // inlines
            .Where(c => c.Text is not null)
            .Select(c => c.Text)
            .ToList();
        Assert.Contains("Hello World", texts);
    }

    // ─── GetFormatState ───────────────────────────────────────────

    [Fact]
    public void GetFormatState_ReturnsCurrentFormatting()
    {
        _engine.Initialize();
        _engine.InsertText("Hello", MakeSel(0, 0, 0));
        _engine.ToggleFormat("bold", MakeRangeSel(0, 0, 0, 0, 0, 5));

        var formatJson = _engine.GetFormatState(MakeSel(0, 0, 0));
        var format = JsonSerializer.Deserialize<FormatState>(formatJson, JsonOptions)!;

        Assert.True(format.Bold);
    }

    [Fact]
    public void GetFormatState_ReturnsDefaultStyle()
    {
        _engine.Initialize();

        var formatJson = _engine.GetFormatState(MakeSel(0, 0, 0));
        var format = JsonSerializer.Deserialize<FormatState>(formatJson, JsonOptions)!;

        Assert.Equal("Normal", format.ParagraphStyle);
        Assert.Equal("left", format.Alignment);
    }

    // ─── JSON serialization consistency ───────────────────────────

    [Fact]
    public void Response_UseCamelCase()
    {
        var responseJson = _engine.Initialize();

        // Verify camelCase keys
        Assert.Contains("\"renderTree\"", responseJson);
        Assert.Contains("\"selection\"", responseJson);
        Assert.Contains("\"formatState\"", responseJson);
        Assert.Contains("\"canUndo\"", responseJson);
        Assert.Contains("\"canRedo\"", responseJson);
    }

    [Fact]
    public void Response_OmitsNullValues()
    {
        var responseJson = _engine.Initialize();

        // A minimal response should not include null fields
        // RenderNode with no styles should omit the styles key
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var sectionNode = root.GetProperty("renderTree")[0];
        var firstBlock = sectionNode.GetProperty("children")[0];

        // A normal paragraph without styles shouldn't have the styles key
        if (firstBlock.TryGetProperty("styles", out _))
        {
            // If styles is present, it should not be null
            Assert.NotEqual(JsonValueKind.Null, firstBlock.GetProperty("styles").ValueKind);
        }
    }

    // ─── Multiple operations sequence ─────────────────────────────

    [Fact]
    public void MultipleOperations_MaintainConsistentState()
    {
        _engine.Initialize();

        // Insert text
        _engine.InsertText("Hello ", MakeSel(0, 0, 0));
        _engine.InsertText("World", MakeSel(0, 0, 6));

        // Split paragraph
        _engine.SplitParagraph(MakeSel(0, 0, 6));

        // Apply formatting
        _engine.SetParagraphStyle("Heading1", MakeSel(0, 0, 0));
        var response = ParseResponse(
            _engine.SetAlignment("center", MakeSel(0, 0, 0))
        );

        // First para should be h1 with center alignment
        var firstBlock = GetBlock(response, 0);
        Assert.Equal("h1", firstBlock.Tag);
        Assert.Equal("center", firstBlock.Styles!["text-align"]);

        // Second para should still be normal
        var secondBlock = GetBlock(response, 1);
        Assert.Equal("p", secondBlock.Tag);
    }
}
