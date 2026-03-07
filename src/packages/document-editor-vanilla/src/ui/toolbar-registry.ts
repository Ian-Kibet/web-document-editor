import type { EngineBridge } from '../bridge/engine-bridge';
import type { CellBordersInput, EngineResponse } from '../bridge/types';
import { domToModelSelection } from '../renderer/cursor-manager';
import { ensureFontsLoaded } from '../renderer/font-loader';

export type ToolbarContext = {
  engine: EngineBridge;
  canvas: HTMLElement;
  onResponse: (r: EngineResponse) => void;
};

export type ToolbarAction = (ctx: ToolbarContext, value?: string) => Promise<void>;

function getCurrentCellId(): string | null {
  const sel = window.getSelection();
  if (!sel || sel.rangeCount === 0) return null;
  const node = sel.anchorNode;
  const el = node?.nodeType === Node.TEXT_NODE ? node.parentElement : (node as HTMLElement);
  return (el?.closest('td[data-node-id]') as HTMLElement | null)?.dataset.nodeId ?? null;
}

export const TOOLBAR_ACTIONS: Record<string, ToolbarAction> = {
  undo: async ({ engine, onResponse }) => {
    const response = await engine.undo();
    onResponse(response);
  },

  redo: async ({ engine, onResponse }) => {
    const response = await engine.redo();
    onResponse(response);
  },

  bold: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.toggleFormat('bold', sel);
    onResponse(response);
  },

  italic: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.toggleFormat('italic', sel);
    onResponse(response);
  },

  underline: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.toggleFormat('underline', sel);
    onResponse(response);
  },

  strikethrough: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.toggleFormat('strikethrough', sel);
    onResponse(response);
  },

  alignLeft: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setAlignment('left', sel);
    onResponse(response);
  },

  alignCenter: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setAlignment('center', sel);
    onResponse(response);
  },

  alignRight: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setAlignment('right', sel);
    onResponse(response);
  },

  alignJustify: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setAlignment('both', sel);
    onResponse(response);
  },

  bullet: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.toggleList('bullet', sel);
    onResponse(response);
  },

  numbered: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.toggleList('numbered', sel);
    onResponse(response);
  },

  insertTable: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.insertTable(3, 3, sel);
    onResponse(response);
  },

  insertLink: async ({ engine, canvas, onResponse }) => {
    const url = prompt('Enter URL:');
    if (!url) return;
    const text = prompt('Link text:', url) || url;
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.insertHyperlink(url, text, sel);
    onResponse(response);
  },

  cellBorders: async ({ engine, canvas, onResponse }, value) => {
    if (!value) return;
    const cellId = getCurrentCellId();
    if (!cellId) return;
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const sizeMap: Record<string, number> = { none: 4, thin: 4, medium: 8, thick: 12 };
    const size = sizeMap[value] ?? 4;
    const borders: CellBordersInput | null = value === 'none' ? null : {
      top:    { style: 'single', size, color: 'auto' },
      bottom: { style: 'single', size, color: 'auto' },
      left:   { style: 'single', size, color: 'auto' },
      right:  { style: 'single', size, color: 'auto' },
    };
    const response = await engine.setTableCellBorders(cellId, borders, sel);
    onResponse(response);
  },

  cellBackground: async ({ engine, canvas, onResponse }) => {
    let colorInput = document.getElementById('wave-cell-bg-input') as HTMLInputElement | null;
    if (!colorInput) {
      colorInput = document.createElement('input');
      colorInput.id = 'wave-cell-bg-input';
      colorInput.type = 'color';
      colorInput.value = '#ffffff';
      colorInput.style.cssText = 'position:fixed;width:0;height:0;opacity:0;pointer-events:none';
      document.body.appendChild(colorInput);
    }
    const captured = colorInput;
    const handleChange = async () => {
      captured.removeEventListener('change', handleChange);
      const cellId = getCurrentCellId();
      if (!cellId) return;
      const sel = domToModelSelection(canvas);
      if (!sel) return;
      const response = await engine.setTableCellShading(
        cellId,
        captured.value.replace('#', ''),
        sel,
      );
      onResponse(response);
      canvas.focus();
    };
    colorInput.addEventListener('change', handleChange);
    colorInput.click();
  },

  removeCellBackground: async ({ engine, canvas, onResponse }) => {
    const cellId = getCurrentCellId();
    if (!cellId) return;
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setTableCellShading(cellId, null, sel);
    onResponse(response);
  },

  sectionBreak: async ({ engine, canvas, onResponse }, value) => {
    if (!value) return;
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.insertSectionBreak(value as any, sel);
    onResponse(response);
  },

  portrait: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setPageOrientation('portrait', sel);
    onResponse(response);
  },

  landscape: async ({ engine, canvas, onResponse }) => {
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setPageOrientation('landscape', sel);
    onResponse(response);
  },

  columns: async ({ engine, canvas, onResponse }, value) => {
    if (!value) return;
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setColumns(parseInt(value), 720, sel);
    onResponse(response);
  },

  paragraphStyle: async ({ engine, canvas, onResponse }, value) => {
    if (!value) return;
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setParagraphStyle(value as any, sel);
    onResponse(response);
  },

  fontFamily: async ({ engine, canvas, onResponse }, value) => {
    if (!value?.trim()) return;
    ensureFontsLoaded([value.trim()]);
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setFontFamily(value.trim(), sel);
    onResponse(response);
  },

  fontSize: async ({ engine, canvas, onResponse }, value) => {
    const size = parseFloat(value ?? '');
    if (isNaN(size) || size <= 0) return;
    const sel = domToModelSelection(canvas);
    if (!sel) return;
    const response = await engine.setFontSize(size, sel);
    onResponse(response);
  },

  insertImage: async ({ engine, canvas, onResponse }) => {
    const input = document.createElement('input');
    input.type   = 'file';
    input.accept = 'image/png,image/jpeg,image/gif,image/webp';
    input.style.cssText = 'position:fixed;width:0;height:0;opacity:0;pointer-events:none';
    document.body.appendChild(input);
    input.addEventListener('change', async () => {
      const file = input.files?.[0];
      document.body.removeChild(input);
      if (!file) return;

      // Read as base64
      const base64 = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader();
        reader.onload  = () => resolve((reader.result as string).split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(file);
      });

      // Measure natural dimensions
      const dims = await new Promise<{ widthEmu: number; heightEmu: number }>((resolve) => {
        const img = new Image();
        img.onload = () => {
          // 96 DPI: 1px = 9525 EMU
          resolve({ widthEmu: img.naturalWidth * 9525, heightEmu: img.naturalHeight * 9525 });
          URL.revokeObjectURL(img.src);
        };
        img.src = URL.createObjectURL(file);
      });

      const sel = domToModelSelection(canvas);
      if (!sel) return;

      const response = await engine.insertImage(
        {
          imageData:       base64,
          contentMimeType: file.type,
          widthEmu:        dims.widthEmu,
          heightEmu:       dims.heightEmu,
          wrapMode:        'Inline',
        },
        sel,
      );
      onResponse(response);
      canvas.focus();
    });
    input.click();
  },

  exportDocx: async ({ engine }) => {
    const bytes = await engine.exportDocx();
    const blob = new Blob([bytes as BlobPart], {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'document.docx';
    a.click();
    URL.revokeObjectURL(url);
  },

  toggleGridLines: async ({ canvas }) => {
    const show = canvas.classList.toggle('show-grid');
    try { localStorage.setItem('documentEditor.gridLines', show ? '1' : '0'); } catch {}
  },

  togglePilcrow: async ({ canvas }) => {
    const show = canvas.classList.toggle('show-pilcrow');
    try { localStorage.setItem('documentEditor.pilcrow', show ? '1' : '0'); } catch {}
  },

  importDocx: async ({ engine, canvas, onResponse }) => {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.accept = '.docx';
    fileInput.style.cssText = 'position:fixed;width:0;height:0;opacity:0;pointer-events:none';
    document.body.appendChild(fileInput);
    fileInput.addEventListener('change', async () => {
      const file = fileInput.files?.[0];
      document.body.removeChild(fileInput);
      if (!file) return;
      const buffer = await file.arrayBuffer();
      const bytes = new Uint8Array(buffer);
      const response = await engine.importDocx(bytes);
      onResponse(response);
      canvas.focus();
    });
    fileInput.click();
  },
};
