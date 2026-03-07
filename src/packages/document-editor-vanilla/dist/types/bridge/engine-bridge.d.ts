import type { CellBordersInput, EngineResponse, FormatProperty, FormatState, ImageInsertParams, ListType, ParagraphStyle, Selection, TextAlignment } from './types';
/**
 * TypeScript wrapper over the C# WASM EditorEngine.
 * All calls go through Blazor JS interop → JsBridge → EditorEngine.
 *
 * Usage:
 *   const bridge = new EngineBridge();
 *   await bridge.waitForReady();
 *   const response = await bridge.initialize();
 */
export declare class EngineBridge {
    private dotNetRef;
    private readyPromise;
    private resolveReady;
    constructor();
    /** Wait for the WASM runtime to load and the .NET reference to be available */
    waitForReady(): Promise<void>;
    get isReady(): boolean;
    initialize(initialDocJson?: string): Promise<EngineResponse>;
    insertText(text: string, selection: Selection): Promise<EngineResponse>;
    deleteBackward(selection: Selection): Promise<EngineResponse>;
    deleteForward(selection: Selection): Promise<EngineResponse>;
    splitParagraph(selection: Selection): Promise<EngineResponse>;
    insertBreak(breakType: 'page' | 'textwrapping' | 'column', selection: Selection): Promise<EngineResponse>;
    deleteSelection(selection: Selection): Promise<EngineResponse>;
    pasteText(text: string, selection: Selection): Promise<EngineResponse>;
    toggleFormat(property: FormatProperty, selection: Selection): Promise<EngineResponse>;
    setParagraphStyle(style: ParagraphStyle, selection: Selection): Promise<EngineResponse>;
    setAlignment(alignment: TextAlignment, selection: Selection): Promise<EngineResponse>;
    toggleList(listType: ListType, selection: Selection): Promise<EngineResponse>;
    setIndent(leftDelta: number, firstLineDelta: number, selection: Selection): Promise<EngineResponse>;
    insertTable(rows: number, cols: number, selection: Selection): Promise<EngineResponse>;
    setTableCellBorders(cellId: string, borders: CellBordersInput | null, sel: Selection): Promise<EngineResponse>;
    setTableCellShading(cellId: string, hexColor: string | null, sel: Selection): Promise<EngineResponse>;
    insertHyperlink(url: string, text: string, selection: Selection): Promise<EngineResponse>;
    insertImage(params: ImageInsertParams, selection: Selection): Promise<EngineResponse>;
    setImageSize(imageNodeId: string, widthEmu: number, heightEmu: number): Promise<EngineResponse>;
    setImageRotation(imageNodeId: string, degrees: number): Promise<EngineResponse>;
    setImageWrapMode(imageNodeId: string, wrapMode: string): Promise<EngineResponse>;
    setImagePosition(imageNodeId: string, horizontalOffsetEmu: number, verticalOffsetEmu: number): Promise<EngineResponse>;
    deleteImageRun(imageNodeId: string): Promise<EngineResponse>;
    insertSectionBreak(breakType: 'nextPage' | 'continuous' | 'evenPage' | 'oddPage', selection: Selection): Promise<EngineResponse>;
    removeSectionBreak(selection: Selection): Promise<EngineResponse>;
    setPageOrientation(orientation: 'portrait' | 'landscape', selection: Selection): Promise<EngineResponse>;
    setColumns(columnCount: number, spacing: number, selection: Selection): Promise<EngineResponse>;
    undo(): Promise<EngineResponse>;
    redo(): Promise<EngineResponse>;
    exportDocx(): Promise<Uint8Array>;
    importDocx(bytes: Uint8Array): Promise<EngineResponse>;
    setFontFamily(fontFamily: string | null, selection: Selection): Promise<EngineResponse>;
    setFontSize(fontSizePt: number, selection: Selection): Promise<EngineResponse>;
    getFormatState(selection: Selection): Promise<FormatState>;
    private invoke;
    private requireRef;
}
//# sourceMappingURL=engine-bridge.d.ts.map