import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse, FormatState } from '../bridge/types';
import type {
  LucideIconDef,
  ToolbarButtonConfig,
  ToolbarComboConfig,
  ToolbarDropdownConfig,
  ToolbarGroupConfig,
  ToolbarItemConfig,
  ToolbarPreset,
  ToolbarSelectConfig,
  ToolbarTheme,
} from './toolbar-config';
import { TOOLBAR_ACTIONS, type ToolbarContext } from './toolbar-registry';
import { WORD_PRESET } from './toolbar-presets';
import { domToModelSelection } from '../renderer/cursor-manager';

export type RenderCallback = (response: EngineResponse) => void;

// ── Tailwind class constants (written as complete strings for Tailwind scanner) ──

const BTN_WORD =
  'flex items-center justify-center w-7 h-7 rounded text-gray-700 transition-colors hover:bg-gray-100 active:bg-gray-200 disabled:opacity-40 disabled:cursor-not-allowed';
const BTN_WORD_ACTIVE =
  'flex items-center justify-center w-7 h-7 rounded text-blue-700 bg-blue-100 transition-colors hover:bg-blue-200 disabled:opacity-40 disabled:cursor-not-allowed';
const BTN_GDOCS =
  'flex items-center justify-center w-7 h-7 rounded-sm text-gray-700 transition-colors hover:bg-gray-200 active:bg-gray-300 disabled:opacity-40 disabled:cursor-not-allowed';
const BTN_GDOCS_ACTIVE =
  'flex items-center justify-center w-7 h-7 rounded-sm text-blue-600 bg-blue-50 transition-colors hover:bg-blue-100 disabled:opacity-40 disabled:cursor-not-allowed';
const BTN_COMPACT =
  'flex items-center justify-center w-6 h-6 rounded text-gray-600 transition-colors hover:bg-gray-100 disabled:opacity-40 disabled:cursor-not-allowed';
const BTN_COMPACT_ACTIVE =
  'flex items-center justify-center w-6 h-6 rounded text-blue-700 bg-blue-100 transition-colors hover:bg-blue-200 disabled:opacity-40 disabled:cursor-not-allowed';

const DROP_WORD =
  'flex items-center gap-0.5 h-7 px-1.5 rounded text-gray-700 text-xs transition-colors hover:bg-gray-100 active:bg-gray-200 disabled:opacity-40';
const DROP_GDOCS =
  'flex items-center gap-0.5 h-7 px-1.5 rounded-sm text-gray-700 text-xs transition-colors hover:bg-gray-200 active:bg-gray-300 disabled:opacity-40';
const DROP_COMPACT =
  'flex items-center gap-0.5 h-6 px-1 rounded text-gray-600 text-xs transition-colors hover:bg-gray-100 disabled:opacity-40';

// ── Icon helper ──────────────────────────────────────────────────────────────

function createIconSvg(iconDef: LucideIconDef, size = 16): SVGSVGElement {
  const ns = 'http://www.w3.org/2000/svg';
  const svg = document.createElementNS(ns, 'svg') as SVGSVGElement;
  svg.setAttribute('width', String(size));
  svg.setAttribute('height', String(size));
  svg.setAttribute('viewBox', '0 0 24 24');
  svg.setAttribute('fill', 'none');
  svg.setAttribute('stroke', 'currentColor');
  svg.setAttribute('stroke-width', '1.75');
  svg.setAttribute('stroke-linecap', 'round');
  svg.setAttribute('stroke-linejoin', 'round');
  svg.style.pointerEvents = 'none';
  svg.style.flexShrink = '0';

  for (const [tag, attrs] of iconDef) {
    const el = document.createElementNS(ns, tag);
    for (const [key, val] of Object.entries(attrs)) {
      el.setAttribute(key, val);
    }
    svg.appendChild(el);
  }
  return svg;
}

function createChevronSvg(): SVGSVGElement {
  const ns = 'http://www.w3.org/2000/svg';
  const svg = document.createElementNS(ns, 'svg') as SVGSVGElement;
  svg.setAttribute('width', '10');
  svg.setAttribute('height', '10');
  svg.setAttribute('viewBox', '0 0 24 24');
  svg.setAttribute('fill', 'none');
  svg.setAttribute('stroke', 'currentColor');
  svg.setAttribute('stroke-width', '2.5');
  svg.setAttribute('stroke-linecap', 'round');
  svg.setAttribute('stroke-linejoin', 'round');
  svg.style.pointerEvents = 'none';
  const path = document.createElementNS(ns, 'polyline');
  path.setAttribute('points', '6 9 12 15 18 9');
  svg.appendChild(path);
  return svg;
}

// ── Toolbar class ────────────────────────────────────────────────────────────

export class Toolbar {
  private el: HTMLElement;
  private ctx: ToolbarContext;
  private currentPreset: ToolbarPreset;
  // Item element references by item ID
  private itemElements: Map<string, HTMLElement> = new Map();
  // Updaters for undo/redo and isEnabled state — receive full EngineResponse
  private stateUpdaters: Array<(r: EngineResponse) => void> = [];
  // Updaters for format-sensitive controls (toggles, selects, combos) — receive FormatState only
  private formatStateUpdaters: Array<(fs: FormatState) => void> = [];

  constructor(
    container: HTMLElement,
    engine: EngineBridge,
    canvas: HTMLElement,
    onResponse: RenderCallback,
    preset: ToolbarPreset = WORD_PRESET,
  ) {
    this.ctx = { engine, canvas, onResponse };
    this.currentPreset = preset;

    this.el = document.createElement('div');
    this.el.className = 'bg-white border-b border-gray-200 flex-shrink-0 sticky top-0 z-50';
    container.appendChild(this.el);

    this.renderPreset(preset);
    this.loadCustomization();
  }

  getElement(): HTMLElement {
    return this.el;
  }

  updateState(response: EngineResponse): void {
    for (const update of this.stateUpdaters) {
      update(response);
    }
    for (const update of this.formatStateUpdaters) {
      update(response.formatState);
    }
  }

  updateFormatState(fs: FormatState): void {
    for (const update of this.formatStateUpdaters) {
      update(fs);
    }
  }

  switchPreset(preset: ToolbarPreset): void {
    this.currentPreset = preset;
    this.itemElements.clear();
    this.stateUpdaters = [];
    this.formatStateUpdaters = [];
    this.el.innerHTML = '';
    this.renderPreset(preset);
    this.loadCustomization();
  }

  setItemVisible(id: string, visible: boolean): void {
    const el = this.itemElements.get(id);
    if (el) el.style.display = visible ? '' : 'none';
  }

  getHiddenItems(): string[] {
    const hidden: string[] = [];
    for (const [id, el] of this.itemElements) {
      if (el.style.display === 'none') hidden.push(id);
    }
    return hidden;
  }

  saveCustomization(): void {
    const data = {
      hiddenItems: this.getHiddenItems(),
      activePresetId: this.currentPreset.id,
    };
    localStorage.setItem('documentEditor.toolbarConfig', JSON.stringify(data));
  }

  loadCustomization(): void {
    try {
      const raw = localStorage.getItem('documentEditor.toolbarConfig');
      if (!raw) return;
      const data = JSON.parse(raw) as { hiddenItems?: string[] };
      for (const id of data.hiddenItems ?? []) {
        this.setItemVisible(id, false);
      }
    } catch {
      // ignore malformed storage
    }
  }

  // ── Private: rendering ───────────────────────────────────────────────────

  private renderPreset(preset: ToolbarPreset): void {
    for (const row of preset.rows) {
      const rowEl = this.buildRow(row, preset.theme);
      this.el.appendChild(rowEl);
    }
  }

  private buildRow(groups: ToolbarGroupConfig[], theme: ToolbarTheme): HTMLElement {
    const row = document.createElement('div');
    row.className = 'flex items-center flex-wrap px-2 py-1 gap-0.5';

    for (let i = 0; i < groups.length; i++) {
      if (i > 0) {
        const sep = document.createElement('div');
        sep.className = 'w-px h-5 bg-gray-200 mx-1 flex-shrink-0';
        row.appendChild(sep);
      }
      row.appendChild(this.buildGroup(groups[i], theme));
    }
    return row;
  }

  private buildGroup(group: ToolbarGroupConfig, theme: ToolbarTheme): HTMLElement {
    const groupEl = document.createElement('div');
    groupEl.className = 'flex items-center gap-0.5';

    for (const item of group.items) {
      const itemEl = this.buildItem(item, theme);
      if (itemEl) {
        groupEl.appendChild(itemEl);
        if (item.type !== 'separator') {
          this.itemElements.set(item.id, itemEl);
        }
      }
    }
    return groupEl;
  }

  private buildItem(item: ToolbarItemConfig, theme: ToolbarTheme): HTMLElement | null {
    switch (item.type) {
      case 'button':
      case 'toggle':
        return this.buildButton(item, theme);
      case 'select':
        return this.buildSelect(item, theme);
      case 'dropdown':
        return this.buildDropdown(item, theme);
      case 'combobox':
        return this.buildCombo(item, theme);
      case 'separator':
        return this.buildInlineSeparator();
      default:
        return null;
    }
  }

  private buildButton(config: ToolbarButtonConfig, theme: ToolbarTheme): HTMLButtonElement {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.title = config.shortcut
      ? `${config.tooltip} (${config.shortcut})`
      : config.tooltip;

    const inactiveClass = this.btnBase(theme);
    const activeClass = this.btnActive(theme);
    btn.className = inactiveClass;

    btn.appendChild(createIconSvg(config.icon, theme === 'compact' ? 14 : 16));

    btn.addEventListener('mousedown', (e) => e.preventDefault());
    btn.addEventListener('click', async () => {
      await TOOLBAR_ACTIONS[config.action]?.(this.ctx);
      this.ctx.canvas.focus();
    });

    // Register format state updater for toggles
    if (config.type === 'toggle' && config.isActive) {
      const isActiveFn = config.isActive;
      this.formatStateUpdaters.push((fs) => {
        btn.className = isActiveFn(fs) ? activeClass : inactiveClass;
      });
    }

    // Undo/redo disabled state
    if (config.id === 'undo') {
      this.stateUpdaters.push((r) => { btn.disabled = !r.canUndo; });
    }
    if (config.id === 'redo') {
      this.stateUpdaters.push((r) => { btn.disabled = !r.canRedo; });
    }

    // isEnabled (custom disable logic)
    if (config.isEnabled) {
      const isEnabledFn = config.isEnabled;
      this.stateUpdaters.push((r) => {
        btn.disabled = !isEnabledFn(r.formatState);
      });
    }

    return btn;
  }

  private buildSelect(config: ToolbarSelectConfig, _theme: ToolbarTheme): HTMLSelectElement {
    const select = document.createElement('select');
    select.title = config.tooltip;
    select.className =
      'h-7 px-2 text-xs border border-gray-300 rounded bg-white text-gray-700 cursor-pointer focus:outline-none focus:ring-1 focus:ring-blue-400 hover:border-gray-400';
    if (config.width) select.style.width = config.width;

    for (const opt of config.options) {
      const option = document.createElement('option');
      option.value = opt.value;
      option.textContent = opt.label;
      select.appendChild(option);
    }

    select.addEventListener('change', async () => {
      await TOOLBAR_ACTIONS[config.action]?.(this.ctx, select.value);
      this.ctx.canvas.focus();
    });

    const getValueFn = config.getValue;
    this.formatStateUpdaters.push((fs) => {
      select.value = getValueFn(fs);
    });

    return select;
  }

  private buildDropdown(config: ToolbarDropdownConfig, theme: ToolbarTheme): HTMLElement {
    const wrapper = document.createElement('div');
    wrapper.className = 'relative';

    const btn = document.createElement('button');
    btn.type = 'button';
    btn.title = config.tooltip;
    btn.className = this.dropBase(theme);

    btn.appendChild(createIconSvg(config.icon, theme === 'compact' ? 13 : 15));
    btn.appendChild(createChevronSvg());

    const menu = document.createElement('div');
    menu.className =
      'absolute top-full left-0 z-50 min-w-36 bg-white border border-gray-200 rounded shadow-lg py-1 hidden';

    for (const opt of config.options) {
      const item = document.createElement('button');
      item.type = 'button';
      item.className =
        'block w-full text-left px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-100 whitespace-nowrap';
      item.textContent = opt.label;
      item.addEventListener('mousedown', (e) => e.preventDefault());
      item.addEventListener('click', async () => {
        menu.classList.add('hidden');
        await TOOLBAR_ACTIONS[config.action]?.(this.ctx, opt.value);
        this.ctx.canvas.focus();
      });
      menu.appendChild(item);
    }

    btn.addEventListener('mousedown', (e) => e.preventDefault());
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      menu.classList.toggle('hidden');
    });

    document.addEventListener('click', () => menu.classList.add('hidden'));

    wrapper.appendChild(btn);
    wrapper.appendChild(menu);
    return wrapper;
  }

  private buildCombo(config: ToolbarComboConfig, _theme: ToolbarTheme): HTMLElement {
    const wrap = document.createElement('span');
    wrap.className = 'relative inline-flex items-center';

    const listId = `wave-combo-${config.id}`;
    const input = document.createElement('input');
    const datalist = document.createElement('datalist');
    datalist.id = listId;
    input.setAttribute('list', listId);
    input.type = 'text';
    input.title = config.tooltip;
    if (config.placeholder) input.placeholder = config.placeholder;
    if (config.width) input.style.width = config.width;
    input.className =
      'h-7 px-2 text-xs border border-gray-300 rounded bg-white text-gray-700 focus:outline-none focus:ring-1 focus:ring-blue-400 hover:border-gray-400';

    for (const opt of config.options) {
      const option = document.createElement('option');
      option.value = opt.value;
      datalist.appendChild(option);
    }

    let capturedSel: ReturnType<typeof domToModelSelection> = null;
    input.addEventListener('mousedown', () => {
      capturedSel = domToModelSelection(this.ctx.canvas);
    });

    const apply = async () => {
      // Use captured selection (taken before focus moved to input) or live canvas selection
      const sel = capturedSel ?? domToModelSelection(this.ctx.canvas);
      if (sel && input.value.trim()) {
        // Call engine directly to inject the captured selection
        const action = config.action;
        let response: EngineResponse | undefined;
        if (action === 'fontFamily') {
          response = await this.ctx.engine.setFontFamily(input.value.trim(), sel);
        } else if (action === 'fontSize') {
          const size = parseFloat(input.value);
          if (!isNaN(size) && size > 0) {
            response = await this.ctx.engine.setFontSize(size, sel);
          }
        } else {
          await TOOLBAR_ACTIONS[config.action]?.(this.ctx, input.value);
        }
        if (response) this.ctx.onResponse(response);
      }
      this.ctx.canvas.focus();
    };
    input.addEventListener('change', apply);
    input.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') { e.preventDefault(); apply(); }
      if (e.key === 'Escape') { this.ctx.canvas.focus(); }
    });

    this.formatStateUpdaters.push((fs) => {
      const val = config.getValue(fs);
      if (document.activeElement !== input) input.value = val;
    });

    wrap.appendChild(input);
    wrap.appendChild(datalist);
    return wrap;
  }

  private buildInlineSeparator(): HTMLElement {
    const sep = document.createElement('div');
    sep.className = 'w-px h-4 bg-gray-200 mx-0.5 flex-shrink-0';
    return sep;
  }

  // ── Theme helpers ────────────────────────────────────────────────────────

  private btnBase(theme: ToolbarTheme): string {
    if (theme === 'gdocs') return BTN_GDOCS;
    if (theme === 'compact') return BTN_COMPACT;
    return BTN_WORD;
  }

  private btnActive(theme: ToolbarTheme): string {
    if (theme === 'gdocs') return BTN_GDOCS_ACTIVE;
    if (theme === 'compact') return BTN_COMPACT_ACTIVE;
    return BTN_WORD_ACTIVE;
  }

  private dropBase(theme: ToolbarTheme): string {
    if (theme === 'gdocs') return DROP_GDOCS;
    if (theme === 'compact') return DROP_COMPACT;
    return DROP_WORD;
  }
}
