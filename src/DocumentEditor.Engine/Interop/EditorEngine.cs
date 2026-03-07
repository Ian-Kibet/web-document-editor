using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentEditor.Engine.Commands;
using DocumentEditor.Engine.Model;
using DocumentEditor.Engine.Model.Enums;
using DocumentEditor.Engine.Model.Interfaces;
using DocumentEditor.Engine.Model.Properties;
using DocumentEditor.Engine.RenderTree;
using DocumentEditor.Engine.Selection;
using DocumentEditor.Engine.Serialization;
using CellBorder = DocumentEditor.Engine.Model.Properties.CellBorder;
using CellBorders = DocumentEditor.Engine.Model.Properties.CellBorders;

namespace DocumentEditor.Engine.Interop;

/// <summary>
/// Public API surface exposed to JavaScript via Blazor WASM JS interop.
/// All public methods are [JSInvokable] and return JSON strings.
/// </summary>
public class EditorEngine
{
    private EditorState _state = new();
    private readonly CommandExecutor _executor = new();
    private readonly DocxExporter _exporter = new();
    private readonly DocxImporter _importer = new();
    private readonly RenderTreeBuilder _renderTreeBuilder = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ─── Lifecycle ───────────────────────────────────────────────

    /// <summary>Create a new empty document or load from JSON</summary>
    public string Initialize(string? initialDocJson = null)
    {
        _state = new EditorState
        {
            Document = initialDocJson is not null
                ? JsonSerializer.Deserialize<DocxDocument>(initialDocJson, JsonOptions)
                  ?? DocFactory.CreateDocument(DocFactory.CreateParagraph())
                : DocFactory.CreateDocument(DocFactory.CreateParagraph()),
            Selection = SelectionModel.Collapsed(0, 0, 0)
        };
        return GetResponse();
    }

    // ─── Text Editing ────────────────────────────────────────────

    public string InsertText(string text, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new InsertTextCommand(text), _state);
        return GetResponse();
    }

    public string DeleteBackward(string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new DeleteBackwardCommand(), _state);
        return GetResponse();
    }

    public string DeleteForward(string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new DeleteForwardCommand(), _state);
        return GetResponse();
    }

    public string SplitParagraph(string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new SplitParagraphCommand(), _state);
        return GetResponse();
    }

    public string InsertBreak(string breakType, string selectionJson)
    {
        ApplySelection(selectionJson);
        var bt = breakType.ToLowerInvariant() switch
        {
            "page"   => BreakType.Page,
            "column" => BreakType.Column,
            _        => BreakType.TextWrapping
        };
        _state = _executor.Execute(new InsertBreakCommand(bt), _state);
        return GetResponse();
    }

    public string DeleteSelection(string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new DeleteSelectionCommand(), _state);
        return GetResponse();
    }

    public string PasteText(string text, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new PasteTextCommand(text), _state);
        return GetResponse();
    }

    // ─── Formatting ──────────────────────────────────────────────

    public string ToggleFormat(string property, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new ToggleFormatCommand(property), _state);
        return GetResponse();
    }

    public string SetParagraphStyle(string style, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new SetParagraphStyleCommand(style), _state);
        return GetResponse();
    }

    public string SetAlignment(string alignment, string selectionJson)
    {
        ApplySelection(selectionJson);
        var parsed = Enum.Parse<Alignment>(alignment, ignoreCase: true);
        _state = _executor.Execute(new SetAlignmentCommand(parsed), _state);
        return GetResponse();
    }

    public string ToggleList(string listType, string selectionJson)
    {
        ApplySelection(selectionJson);
        var parsed = Enum.Parse<ListType>(listType, ignoreCase: true);
        _state = _executor.Execute(new ToggleListCommand(parsed), _state);
        return GetResponse();
    }

    public string SetIndent(int leftDelta, int firstLineDelta, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new SetIndentCommand(leftDelta, firstLineDelta), _state);
        return GetResponse();
    }

    public string SetFontFamily(string? fontFamily, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(
            new SetFontFamilyCommand(string.IsNullOrWhiteSpace(fontFamily) ? null : fontFamily),
            _state);
        return GetResponse();
    }

    public string SetFontSize(double fontSizePt, string selectionJson)
    {
        ApplySelection(selectionJson);
        var halfPoints = fontSizePt > 0 ? (int)Math.Round(fontSizePt * 2) : (int?)null;
        _state = _executor.Execute(new SetFontSizeCommand(halfPoints), _state);
        return GetResponse();
    }

    // ─── Insertions ──────────────────────────────────────────────

    public string InsertTable(int rows, int cols, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new InsertTableCommand(rows, cols), _state);
        return GetResponse();
    }

    public string SetTableCellBorders(string cellId, string? bordersJson, string selectionJson)
    {
        ApplySelection(selectionJson);
        CellBorders? borders = null;
        if (bordersJson is not null)
        {
            var input = JsonSerializer.Deserialize<CellBordersInput>(bordersJson, JsonOptions);
            borders = input is null ? null : new CellBorders
            {
                Top    = input.Top    is null ? null : new CellBorder { Style = Enum.Parse<CellBorderStyle>(input.Top.Style,    true), Size = input.Top.Size,    Color = input.Top.Color    ?? "auto" },
                Bottom = input.Bottom is null ? null : new CellBorder { Style = Enum.Parse<CellBorderStyle>(input.Bottom.Style, true), Size = input.Bottom.Size, Color = input.Bottom.Color ?? "auto" },
                Left   = input.Left   is null ? null : new CellBorder { Style = Enum.Parse<CellBorderStyle>(input.Left.Style,   true), Size = input.Left.Size,   Color = input.Left.Color   ?? "auto" },
                Right  = input.Right  is null ? null : new CellBorder { Style = Enum.Parse<CellBorderStyle>(input.Right.Style,  true), Size = input.Right.Size,  Color = input.Right.Color  ?? "auto" },
            };
        }
        _state = _executor.Execute(new SetTableCellBordersCommand(cellId, borders), _state);
        return GetResponse();
    }

    public string SetTableCellShading(string cellId, string? hexColor, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new SetTableCellShadingCommand(cellId, hexColor), _state);
        return GetResponse();
    }

    public string InsertHyperlink(string url, string text, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new InsertHyperlinkCommand(url, text), _state);
        return GetResponse();
    }

    public string InsertImage(string imageInfoJson, string selectionJson)
    {
        ApplySelection(selectionJson);
        var input = JsonSerializer.Deserialize<ImageInsertInput>(imageInfoJson, JsonOptions);
        if (input is null) return GetResponse();
        var wrapMode = Enum.TryParse<ImageWrapMode>(input.WrapMode, ignoreCase: true, out var wm)
            ? wm : ImageWrapMode.Inline;
        var imageContent = new ImageContent
        {
            ImageData       = input.ImageData,
            ContentMimeType = input.ContentMimeType,
            WidthEmu        = input.WidthEmu,
            HeightEmu       = input.HeightEmu,
            AltText         = input.AltText,
            WrapMode        = wrapMode
        };
        _state = _executor.Execute(new InsertImageCommand(imageContent), _state);
        return GetResponse();
    }

    public string SetImageSize(string imageNodeId, long widthEmu, long heightEmu)
    {
        _state = _executor.Execute(new SetImageSizeCommand(imageNodeId, widthEmu, heightEmu), _state);
        return GetResponse();
    }

    public string SetImageRotation(string imageNodeId, double degrees)
    {
        _state = _executor.Execute(new SetImageRotationCommand(imageNodeId, degrees), _state);
        return GetResponse();
    }

    public string SetImageWrapMode(string imageNodeId, string wrapMode)
    {
        var mode = Enum.TryParse<ImageWrapMode>(wrapMode, ignoreCase: true, out var m)
            ? m : ImageWrapMode.Inline;
        _state = _executor.Execute(new SetImageWrapModeCommand(imageNodeId, mode), _state);
        return GetResponse();
    }

    public string SetImagePosition(string imageNodeId, long horizontalOffsetEmu, long verticalOffsetEmu)
    {
        _state = _executor.Execute(
            new MoveImageCommand(imageNodeId, horizontalOffsetEmu, verticalOffsetEmu),
            _state);
        return GetResponse();
    }

    public string DeleteImageRun(string imageNodeId)
    {
        _state = _executor.Execute(new DeleteImageRunCommand(imageNodeId), _state);
        return GetResponse();
    }

    // ─── Sections ──────────────────────────────────────────────

    public string InsertSectionBreak(string breakType, string selectionJson)
    {
        ApplySelection(selectionJson);
        var parsed = Enum.Parse<SectionBreakType>(breakType, ignoreCase: true);
        _state = _executor.Execute(new InsertSectionBreakCommand(parsed), _state);
        return GetResponse();
    }

    public string RemoveSectionBreak(string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new RemoveSectionBreakCommand(), _state);
        return GetResponse();
    }

    public string SetPageOrientation(string orientation, string selectionJson)
    {
        ApplySelection(selectionJson);
        var parsed = Enum.Parse<Orientation>(orientation, ignoreCase: true);
        _state = _executor.Execute(new SetPageOrientationCommand(parsed), _state);
        return GetResponse();
    }

    public string SetColumns(int columnCount, int spacing, string selectionJson)
    {
        ApplySelection(selectionJson);
        _state = _executor.Execute(new SetColumnsCommand(columnCount, spacing), _state);
        return GetResponse();
    }

    // ─── History ─────────────────────────────────────────────────

    public string Undo()
    {
        _state = _executor.Undo(_state);
        return GetResponse();
    }

    public string Redo()
    {
        _state = _executor.Redo(_state);
        return GetResponse();
    }

    // ─── File I/O ────────────────────────────────────────────────

    public byte[] ExportDocx()
    {
        return _exporter.Export(_state.Document);
    }

    public string ImportDocx(byte[] docxBytes)
    {
        _state = new EditorState
        {
            Document = _importer.Import(docxBytes),
            Selection = SelectionModel.Collapsed(0, 0, 0)
        };
        return GetResponse();
    }

    // ─── Query (no mutation) ─────────────────────────────────────

    /// <summary>Get current format state at cursor without mutating</summary>
    public string GetFormatState(string selectionJson)
    {
        ApplySelection(selectionJson);
        var formatState = BuildFormatState();
        return JsonSerializer.Serialize(formatState, JsonOptions);
    }

    // ─── Internal helpers ────────────────────────────────────────

    private void ApplySelection(string selectionJson)
    {
        var sel = JsonSerializer.Deserialize<SelectionInput>(selectionJson, JsonOptions);
        if (sel is null) return;

        _state.Selection = new SelectionModel
        {
            Anchor = new ModelPosition(sel.Anchor.BlockIndex, sel.Anchor.InlineIndex, sel.Anchor.Offset)
            {
                Cell = sel.Anchor.Cell is not null
                    ? new CellPath { RowIndex = sel.Anchor.Cell.RowIndex, CellIndex = sel.Anchor.Cell.CellIndex, CellBlockIndex = sel.Anchor.Cell.CellBlockIndex }
                    : null
            },
            Focus = new ModelPosition(sel.Focus.BlockIndex, sel.Focus.InlineIndex, sel.Focus.Offset)
            {
                Cell = sel.Focus.Cell is not null
                    ? new CellPath { RowIndex = sel.Focus.Cell.RowIndex, CellIndex = sel.Focus.Cell.CellIndex, CellBlockIndex = sel.Focus.Cell.CellBlockIndex }
                    : null
            }
        };
    }

    private string GetResponse()
    {
        var sections = SectionResolver.GetSections(_state.Document);
        var response = new EngineResponse
        {
            RenderTree = _renderTreeBuilder.Build(_state.Document),
            Selection = BuildSelectionResponse(),
            FormatState = BuildFormatState(),
            Sections = BuildSectionsResponse(sections),
            CanUndo = _executor.CanUndo,
            CanRedo = _executor.CanRedo
        };
        return JsonSerializer.Serialize(response, JsonOptions);
    }

    private static List<SectionInfoResponse> BuildSectionsResponse(List<SectionInfo> sections)
    {
        var result = new List<SectionInfoResponse>();
        for (var i = 0; i < sections.Count; i++)
        {
            var s = sections[i];
            result.Add(new SectionInfoResponse
            {
                Index = i,
                StartBlockIndex = s.StartBlockIndex,
                EndBlockIndex = s.EndBlockIndex,
                PageWidth = s.Properties.PageWidth,
                PageHeight = s.Properties.PageHeight,
                Orientation = s.Properties.Orientation.ToString().ToLowerInvariant(),
                MarginTop = s.Properties.MarginTop,
                MarginBottom = s.Properties.MarginBottom,
                MarginLeft = s.Properties.MarginLeft,
                MarginRight = s.Properties.MarginRight,
                BreakType = s.Properties.BreakType switch
                {
                    SectionBreakType.Continuous => "continuous",
                    SectionBreakType.EvenPage => "evenPage",
                    SectionBreakType.OddPage => "oddPage",
                    _ => "nextPage"
                },
                Headers = s.Properties.Headers,
                Footers = s.Properties.Footers,
                TitlePage = s.Properties.TitlePage,
                HeaderDistance = s.Properties.HeaderDistance,
                FooterDistance = s.Properties.FooterDistance,
                ColumnCount = s.Properties.ColumnCount,
                ColumnSpacing = s.Properties.ColumnSpacing,
                ColumnSeparator = s.Properties.ColumnSeparator
            });
        }
        return result;
    }

    private SelectionResponse BuildSelectionResponse()
    {
        return new SelectionResponse
        {
            Anchor = BuildPositionResponse(_state.Selection.Anchor),
            Focus = BuildPositionResponse(_state.Selection.Focus),
            IsCollapsed = _state.Selection.IsCollapsed
        };
    }

    private static PositionResponse BuildPositionResponse(ModelPosition pos) => new()
    {
        BlockIndex = pos.BlockIndex,
        InlineIndex = pos.InlineIndex,
        Offset = pos.Offset,
        Cell = pos.Cell is not null
            ? new CellPathResponse { RowIndex = pos.Cell.RowIndex, CellIndex = pos.Cell.CellIndex, CellBlockIndex = pos.Cell.CellBlockIndex }
            : null
    };

    private FormatState BuildFormatState()
    {
        var pos = _state.Selection.Anchor;
        var doc = _state.Document;

        var formatState = new FormatState();

        var para = CommandExecutor.ResolveParagraph(doc, pos);
        if (para is not null)
        {
            formatState.ParagraphStyle = para.Properties.Style ?? "Normal";
            formatState.Alignment = (para.Properties.Alignment ?? Alignment.Left).ToString().ToLowerInvariant();

            if (para.Properties.NumberingId is not null)
            {
                formatState.ListType = para.Properties.NumberingId.Value switch
                {
                    1 => "bullet",
                    2 => "numbered",
                    _ => null
                };
            }

            if (pos.InlineIndex >= 0 && pos.InlineIndex < para.Children.Count
                && para.Children[pos.InlineIndex] is Run run)
            {
                formatState.Bold = run.Properties.Bold;
                formatState.Italic = run.Properties.Italic;
                formatState.Underline = run.Properties.Underline is not null
                    && run.Properties.Underline != UnderlineType.None;
                formatState.Strikethrough = run.Properties.Strikethrough;
                formatState.FontFamily = run.Properties.FontFamily;
                formatState.FontSize = run.Properties.FontSize is not null
                    ? run.Properties.FontSize.Value / 2.0
                    : null;
                formatState.Color = run.Properties.Color;
            }
        }

        return formatState;
    }

    /// <summary>Input DTO for selection coming from TypeScript</summary>
    private class SelectionInput
    {
        public PositionInput Anchor { get; set; } = new();
        public PositionInput Focus { get; set; } = new();
    }

    private class PositionInput
    {
        public int BlockIndex { get; set; }
        public int InlineIndex { get; set; }
        public int Offset { get; set; }
        public CellPathInput? Cell { get; set; }
    }

    private class CellPathInput
    {
        public int RowIndex { get; set; }
        public int CellIndex { get; set; }
        public int CellBlockIndex { get; set; }
    }
}
