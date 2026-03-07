import type {
  CellBordersInput,
  EngineResponse,
  FormatProperty,
  FormatState,
  ImageInsertParams,
  ListType,
  ParagraphStyle,
  Selection,
  TextAlignment,
} from './types';

/**
 * TypeScript wrapper over the C# WASM EditorEngine.
 * All calls go through Blazor JS interop → JsBridge → EditorEngine.
 *
 * Usage:
 *   const bridge = new EngineBridge();
 *   await bridge.waitForReady();
 *   const response = await bridge.initialize();
 */
export class EngineBridge {
  private dotNetRef: DotNetObjectReference | null = null;
  private readyPromise: Promise<void>;
  private resolveReady!: () => void;

  constructor() {
    this.readyPromise = new Promise((resolve) => {
      this.resolveReady = resolve;
    });

    // Check if already available
    const existing = (window as any).getDotNetReference?.();
    if (existing) {
      this.dotNetRef = existing;
      this.resolveReady();
    }

    // Listen for the engine-ready event dispatched from index.html
    window.addEventListener('engine-ready', ((e: CustomEvent) => {
      this.dotNetRef = e.detail;
      this.resolveReady();
    }) as EventListener);
  }

  /** Wait for the WASM runtime to load and the .NET reference to be available */
  async waitForReady(): Promise<void> {
    return this.readyPromise;
  }

  get isReady(): boolean {
    return this.dotNetRef !== null;
  }

  // ─── Lifecycle ─────────────────────────────────────────────

  async initialize(initialDocJson?: string): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('Initialize', initialDocJson ?? null);
  }

  // ─── Text Editing ──────────────────────────────────────────

  async insertText(text: string, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('InsertText', text, toJson(selection));
  }

  async deleteBackward(selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('DeleteBackward', toJson(selection));
  }

  async deleteForward(selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('DeleteForward', toJson(selection));
  }

  async splitParagraph(selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SplitParagraph', toJson(selection));
  }

  async insertBreak(breakType: 'page' | 'textwrapping' | 'column', selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('InsertBreak', breakType, toJson(selection));
  }

  async deleteSelection(selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('DeleteSelection', toJson(selection));
  }

  async pasteText(text: string, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('PasteText', text, toJson(selection));
  }

  // ─── Formatting ────────────────────────────────────────────

  async toggleFormat(property: FormatProperty, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('ToggleFormat', property, toJson(selection));
  }

  async setParagraphStyle(style: ParagraphStyle, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetParagraphStyle', style, toJson(selection));
  }

  async setAlignment(alignment: TextAlignment, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetAlignment', alignment, toJson(selection));
  }

  async toggleList(listType: ListType, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('ToggleList', listType, toJson(selection));
  }

  async setIndent(
    leftDelta: number,
    firstLineDelta: number,
    selection: Selection,
  ): Promise<EngineResponse> {
    return this.invoke<EngineResponse>(
      'SetIndent',
      leftDelta,
      firstLineDelta,
      toJson(selection),
    );
  }

  // ─── Insertions ────────────────────────────────────────────

  async insertTable(rows: number, cols: number, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('InsertTable', rows, cols, toJson(selection));
  }

  async setTableCellBorders(
    cellId: string,
    borders: CellBordersInput | null,
    sel: Selection,
  ): Promise<EngineResponse> {
    return this.invoke<EngineResponse>(
      'SetTableCellBorders',
      cellId,
      borders ? JSON.stringify(borders) : null,
      toJson(sel),
    );
  }

  async setTableCellShading(
    cellId: string,
    hexColor: string | null,
    sel: Selection,
  ): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetTableCellShading', cellId, hexColor, toJson(sel));
  }

  async insertHyperlink(
    url: string,
    text: string,
    selection: Selection,
  ): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('InsertHyperlink', url, text, toJson(selection));
  }

  async insertImage(params: ImageInsertParams, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('InsertImage', JSON.stringify(params), toJson(selection));
  }

  async setImageSize(
    imageNodeId: string,
    widthEmu: number,
    heightEmu: number,
  ): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetImageSize', imageNodeId, widthEmu, heightEmu);
  }

  async setImageRotation(imageNodeId: string, degrees: number): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetImageRotation', imageNodeId, degrees);
  }

  async setImageWrapMode(imageNodeId: string, wrapMode: string): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetImageWrapMode', imageNodeId, wrapMode);
  }

  async setImagePosition(
    imageNodeId: string,
    horizontalOffsetEmu: number,
    verticalOffsetEmu: number,
  ): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetImagePosition', imageNodeId, horizontalOffsetEmu, verticalOffsetEmu);
  }

  async deleteImageRun(imageNodeId: string): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('DeleteImageRun', imageNodeId);
  }

  // ─── Sections ─────────────────────────────────────────

  async insertSectionBreak(
    breakType: 'nextPage' | 'continuous' | 'evenPage' | 'oddPage',
    selection: Selection,
  ): Promise<EngineResponse> {
    // Map camelCase to PascalCase for C# enum
    const typeMap: Record<string, string> = {
      nextPage: 'NextPage',
      continuous: 'Continuous',
      evenPage: 'EvenPage',
      oddPage: 'OddPage',
    };
    return this.invoke<EngineResponse>(
      'InsertSectionBreak',
      typeMap[breakType] ?? 'NextPage',
      toJson(selection),
    );
  }

  async removeSectionBreak(selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('RemoveSectionBreak', toJson(selection));
  }

  async setPageOrientation(
    orientation: 'portrait' | 'landscape',
    selection: Selection,
  ): Promise<EngineResponse> {
    const orientMap: Record<string, string> = {
      portrait: 'Portrait',
      landscape: 'Landscape',
    };
    return this.invoke<EngineResponse>(
      'SetPageOrientation',
      orientMap[orientation] ?? 'Portrait',
      toJson(selection),
    );
  }

  async setColumns(
    columnCount: number,
    spacing: number,
    selection: Selection,
  ): Promise<EngineResponse> {
    return this.invoke<EngineResponse>(
      'SetColumns',
      columnCount,
      spacing,
      toJson(selection),
    );
  }

  // ─── History ───────────────────────────────────────────────

  async undo(): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('Undo');
  }

  async redo(): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('Redo');
  }

  // ─── File I/O ──────────────────────────────────────────────

  async exportDocx(): Promise<Uint8Array> {
    const ref = this.requireRef();
    return await ref.invokeMethodAsync<Uint8Array>('ExportDocx');
  }

  async importDocx(bytes: Uint8Array): Promise<EngineResponse> {
    const ref = this.requireRef();
    const json = await ref.invokeMethodAsync<string>('ImportDocx', bytes);
    return JSON.parse(json);
  }

  async setFontFamily(fontFamily: string | null, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetFontFamily', fontFamily, toJson(selection));
  }

  async setFontSize(fontSizePt: number, selection: Selection): Promise<EngineResponse> {
    return this.invoke<EngineResponse>('SetFontSize', fontSizePt, toJson(selection));
  }

  // ─── Query ─────────────────────────────────────────────────

  async getFormatState(selection: Selection): Promise<FormatState> {
    const ref = this.requireRef();
    const json = await ref.invokeMethodAsync<string>('GetFormatState', toJson(selection));
    return JSON.parse(json);
  }

  // ─── Internal ──────────────────────────────────────────────

  private async invoke<T>(method: string, ...args: any[]): Promise<T> {
    const ref = this.requireRef();
    const json = await ref.invokeMethodAsync<string>(method, ...args);
    return JSON.parse(json);
  }

  private requireRef(): DotNetObjectReference {
    if (!this.dotNetRef) {
      throw new Error(
        'Engine not ready. Call waitForReady() before invoking methods.',
      );
    }
    return this.dotNetRef;
  }
}

/** Serialize a selection to JSON string for the C# side */
function toJson(selection: Selection): string {
  return JSON.stringify(selection);
}

/** Blazor JS interop DotNetObjectReference type */
interface DotNetObjectReference {
  invokeMethodAsync<T>(methodName: string, ...args: any[]): Promise<T>;
}
