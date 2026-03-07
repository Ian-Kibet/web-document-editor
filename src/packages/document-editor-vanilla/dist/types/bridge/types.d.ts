/** Lightweight node produced by RenderTreeBuilder in C# */
export interface RenderNode {
    id: string;
    tag: string;
    styles?: Record<string, string>;
    attrs?: Record<string, string>;
    text?: string;
    children?: RenderNode[];
}
export interface CellPosition {
    rowIndex: number;
    cellIndex: number;
    cellBlockIndex: number;
}
export interface Position {
    blockIndex: number;
    inlineIndex: number;
    offset: number;
    cell?: CellPosition;
}
export interface Selection {
    anchor: Position;
    focus: Position;
}
export interface SelectionResponse {
    anchor: Position;
    focus: Position;
    isCollapsed: boolean;
}
export interface FormatState {
    bold: boolean;
    italic: boolean;
    underline: boolean;
    strikethrough: boolean;
    fontFamily?: string;
    fontSize?: number;
    color?: string;
    paragraphStyle?: string;
    alignment?: string;
    listType?: string;
}
export interface SectionInfo {
    index: number;
    startBlockIndex: number;
    endBlockIndex: number;
    pageWidth: number;
    pageHeight: number;
    orientation: 'portrait' | 'landscape';
    marginTop: number;
    marginBottom: number;
    marginLeft: number;
    marginRight: number;
    breakType: 'nextPage' | 'continuous' | 'evenPage' | 'oddPage';
    headers?: Record<string, RenderNode[]>;
    footers?: Record<string, RenderNode[]>;
    titlePage: boolean;
    headerDistance: number;
    footerDistance: number;
    columnCount: number;
    columnSpacing: number;
    columnSeparator: boolean;
}
export interface EngineResponse {
    renderTree: RenderNode[];
    selection: SelectionResponse;
    formatState: FormatState;
    sections: SectionInfo[];
    canUndo: boolean;
    canRedo: boolean;
}
export type FormatProperty = 'bold' | 'italic' | 'underline' | 'strikethrough';
export type ParagraphStyle = 'Normal' | 'Heading1' | 'Heading2' | 'Heading3' | 'Heading4';
export type TextAlignment = 'left' | 'center' | 'right' | 'both';
export type ListType = 'bullet' | 'numbered';
export interface ImageInsertParams {
    imageData: string;
    contentMimeType: string;
    widthEmu: number;
    heightEmu: number;
    altText?: string;
    wrapMode?: string;
}
export type CellBorderStyle = 'none' | 'single' | 'double' | 'dotted' | 'dashed' | 'thick';
export interface CellBorderInput {
    style: CellBorderStyle;
    size: number;
    color: string;
}
export interface CellBordersInput {
    top?: CellBorderInput;
    bottom?: CellBorderInput;
    left?: CellBorderInput;
    right?: CellBorderInput;
}
//# sourceMappingURL=types.d.ts.map