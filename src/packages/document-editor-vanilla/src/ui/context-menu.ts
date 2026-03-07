import type { EngineResponse } from '../bridge/types';
import type { EngineBridge } from '../bridge/engine-bridge';
import { domToModelSelection } from '../renderer/cursor-manager';
import type { Selection } from '../bridge/types';

// Wrap mode entries — values match `data-wrap-mode` attribute (lowercased enum names)
const WRAP_MODES: { label: string; value: string }[] = [
  { label: 'Inline',           value: 'inline' },
  { label: 'Float Left',       value: 'floatleft' },
  { label: 'Float Right',      value: 'floatright' },
  { label: 'Break Text',       value: 'topandbottom' },
  { label: 'Behind Text',      value: 'behindtext' },
  { label: 'In Front of Text', value: 'infrontoftext' },
];

async function copyImageToClipboard(imgEl: HTMLImageElement): Promise<void> {
  const response = await fetch(imgEl.src); // data: URL — no network request
  const blob = await response.blob();
  await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
}

function getSelectedText(): string {
  return window.getSelection()?.toString() ?? '';
}

function isSelectionCollapsed(sel: Selection): boolean {
  const a = sel.anchor;
  const f = sel.focus;
  return (
    a.blockIndex === f.blockIndex &&
    a.inlineIndex === f.inlineIndex &&
    a.offset === f.offset
  );
}

export class ContextMenu {
  private readonly canvas: HTMLElement;
  private readonly engine: EngineBridge;
  private readonly onResponse: (r: EngineResponse) => void;
  private readonly menuEl: HTMLDivElement;

  // Bound handlers stored for cleanup
  private readonly _onContextMenu: (e: MouseEvent) => void;
  private readonly _onDocContextMenu: (e: MouseEvent) => void;
  private readonly _onClickOutside: (e: MouseEvent) => void;
  private readonly _onKeyDown: (e: KeyboardEvent) => void;

  constructor(
    canvas: HTMLElement,
    engine: EngineBridge,
    onResponse: (r: EngineResponse) => void,
  ) {
    this.canvas = canvas;
    this.engine = engine;
    this.onResponse = onResponse;

    this.menuEl = document.createElement('div');
    this.menuEl.id = 'wave-context-menu';
    this.menuEl.className =
      'fixed z-50 min-w-44 rounded-lg shadow-lg border border-gray-200 bg-white py-1 text-sm text-gray-700';
    this.menuEl.style.display = 'none';
    document.body.appendChild(this.menuEl);

    this._onContextMenu    = this.handleContextMenu.bind(this);
    this._onDocContextMenu = this.handleDocContextMenu.bind(this);
    this._onClickOutside   = this.handleClickOutside.bind(this);
    this._onKeyDown        = this.handleKeyDown.bind(this);

    canvas.addEventListener('contextmenu', this._onContextMenu);
    document.addEventListener('contextmenu', this._onDocContextMenu, true);
    document.addEventListener('click', this._onClickOutside, true);
    document.addEventListener('keydown', this._onKeyDown);
  }

  destroy(): void {
    this.canvas.removeEventListener('contextmenu', this._onContextMenu);
    document.removeEventListener('contextmenu', this._onDocContextMenu, true);
    document.removeEventListener('click', this._onClickOutside, true);
    document.removeEventListener('keydown', this._onKeyDown);
    this.menuEl.remove();
  }

  // ─── Event handlers ────────────────────────────────────────

  private handleContextMenu(e: MouseEvent): void {
    e.preventDefault();

    const target = e.target as HTMLElement;
    const imgEl =
      target.tagName === 'IMG' && target.dataset.type === 'image'
        ? (target as HTMLImageElement)
        : null;
    const runId          = imgEl?.dataset.nodeId ?? null;
    const currentWrapMode = imgEl?.dataset.wrapMode ?? null;

    // Capture the text selection at the moment of right-click
    const sel = domToModelSelection(this.canvas);

    this.buildMenu(imgEl, runId, currentWrapMode, sel, e.clientX, e.clientY);
  }

  private handleDocContextMenu(e: MouseEvent): void {
    // Close our menu when right-clicking outside the canvas
    if (!this.canvas.contains(e.target as Node)) {
      this.hide();
    }
  }

  private handleClickOutside(e: MouseEvent): void {
    if (!this.menuEl.contains(e.target as Node)) {
      this.hide();
    }
  }

  private handleKeyDown(e: KeyboardEvent): void {
    if (e.key === 'Escape' && this.menuEl.style.display !== 'none') {
      this.hide();
    }
  }

  // ─── Menu construction ─────────────────────────────────────

  private buildMenu(
    imgEl: HTMLImageElement | null,
    runId: string | null,
    currentWrapMode: string | null,
    sel: Selection | null,
    clientX: number,
    clientY: number,
  ): void {
    this.menuEl.innerHTML = '';

    const hasTextSel = !!(sel && !isSelectionCollapsed(sel));
    const hasImage   = !!(imgEl && runId);

    // Cut
    this.addItem('Cut', !(hasImage || hasTextSel), async () => {
      if (hasImage && imgEl && runId) {
        await copyImageToClipboard(imgEl).catch(() => {});
        const r = await this.engine.deleteImageRun(runId);
        this.onResponse(r);
      } else if (hasTextSel && sel) {
        await navigator.clipboard.writeText(getSelectedText()).catch(() => {});
        const r = await this.engine.deleteSelection(sel);
        this.onResponse(r);
      }
      this.hide();
    });

    // Copy
    this.addItem('Copy', !(hasImage || hasTextSel), async () => {
      if (hasImage && imgEl) {
        await copyImageToClipboard(imgEl).catch(() => {});
      } else if (hasTextSel) {
        await navigator.clipboard.writeText(getSelectedText()).catch(() => {});
      }
      this.hide();
    });

    // Paste
    this.addItem('Paste', false, async () => {
      try {
        const text = await navigator.clipboard.readText();
        if (text && sel) {
          const r = await this.engine.pasteText(text, sel);
          this.onResponse(r);
        }
      } catch { /* clipboard access denied */ }
      this.hide();
    });

    // Paste without formatting
    this.addItem('Paste without formatting', false, async () => {
      try {
        const text = await navigator.clipboard.readText();
        if (text && sel) {
          const r = await this.engine.pasteText(text, sel);
          this.onResponse(r);
        }
      } catch { /* clipboard access denied */ }
      this.hide();
    });

    // Delete
    this.addItem('Delete', !(hasImage || hasTextSel), async () => {
      if (hasImage && runId) {
        const r = await this.engine.deleteImageRun(runId);
        this.onResponse(r);
      } else if (hasTextSel && sel) {
        const r = await this.engine.deleteSelection(sel);
        this.onResponse(r);
      }
      this.hide();
    });

    // Wrap mode section (image only)
    if (hasImage && runId) {
      this.addSeparator();
      for (const { label, value } of WRAP_MODES) {
        const isActive = currentWrapMode === value;
        this.addItem(
          (isActive ? '✓ ' : '\u00a0\u00a0 ') + label,
          false,
          async () => {
            const r = await this.engine.setImageWrapMode(runId, value);
            this.onResponse(r);
            this.hide();
          },
        );
      }
    }

    // Position after browser has laid out the menu
    this.menuEl.style.display = 'block';
    this.menuEl.style.left = '-9999px';
    this.menuEl.style.top = '-9999px';

    requestAnimationFrame(() => {
      this.position(clientX, clientY);
    });
  }

  // ─── Helpers ───────────────────────────────────────────────

  private addItem(label: string, disabled: boolean, action: () => void): void {
    const item = document.createElement('div');
    item.textContent = label;
    item.className = [
      'flex items-center gap-2 px-3 py-1.5 select-none rounded-sm mx-1',
      disabled
        ? 'opacity-40 cursor-default pointer-events-none'
        : 'hover:bg-gray-100 cursor-pointer',
    ].join(' ');
    if (!disabled) {
      // Prevent focus/selection loss when the menu item is pressed
      item.addEventListener('mousedown', (e) => e.preventDefault());
      item.addEventListener('click', () => action());
    }
    this.menuEl.appendChild(item);
  }

  private addSeparator(): void {
    const sep = document.createElement('div');
    sep.className = 'border-t border-gray-200 my-1';
    this.menuEl.appendChild(sep);
  }

  private position(x: number, y: number): void {
    const rect = this.menuEl.getBoundingClientRect();
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    const left = Math.min(x, vw - rect.width - 4);
    const top  = Math.min(y, vh - rect.height - 4);
    this.menuEl.style.left = `${Math.max(0, left)}px`;
    this.menuEl.style.top  = `${Math.max(0, top)}px`;
  }

  private hide(): void {
    this.menuEl.style.display = 'none';
  }
}
