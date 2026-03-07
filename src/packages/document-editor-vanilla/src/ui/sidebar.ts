import type { RenderNode } from '../bridge/types';
import { BookOpen, BarChart2, Code2, ChevronLeft, ChevronRight } from 'lucide';

// ── Icon helper (shared with toolbar.ts) ────────────────────────────────────

function createIconSvg(
  iconDef: [string, Record<string, string>][],
  size = 14,
): SVGSVGElement {
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

// ── Tab definitions ──────────────────────────────────────────────────────────

const TABS = [
  { id: 'outline', label: 'Outline', icon: BookOpen as [string, Record<string, string>][] },
  { id: 'stats',   label: 'Stats',   icon: BarChart2  as [string, Record<string, string>][] },
  { id: 'xml',     label: 'XML',     icon: Code2      as [string, Record<string, string>][] },
] as const;

type TabId = typeof TABS[number]['id'];

/**
 * Collapsible sidebar with icon tabs: Outline, Statistics, XML Debug.
 * Styled with Tailwind CSS.
 */
export class Sidebar {
  private el: HTMLElement;
  private tabsRow: HTMLElement;
  private panelContainer: HTMLElement;
  private outlinePanel: HTMLElement;
  private statsPanel: HTMLElement;
  private xmlPanel: HTMLElement;
  private toggleBtn: HTMLButtonElement;
  private collapsed = false;
  private tabButtons: Map<TabId, HTMLButtonElement> = new Map();

  constructor(container: HTMLElement) {
    this.el = document.createElement('div');
    this.el.className =
      'flex flex-col border-l border-gray-200 bg-white flex-shrink-0 overflow-hidden transition-[width] duration-200';
    this.el.style.width = '240px';

    // Toggle button row
    const toggleRow = document.createElement('div');
    toggleRow.className = 'flex items-center justify-end px-1 py-1 border-b border-gray-100';

    this.toggleBtn = document.createElement('button');
    this.toggleBtn.type = 'button';
    this.toggleBtn.title = 'Toggle sidebar';
    this.toggleBtn.className =
      'flex items-center justify-center w-6 h-6 rounded text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors';
    this.toggleBtn.appendChild(createIconSvg(ChevronRight as [string, Record<string, string>][], 14));
    this.toggleBtn.addEventListener('click', () => this.toggle());
    toggleRow.appendChild(this.toggleBtn);
    this.el.appendChild(toggleRow);

    // Tab bar
    this.tabsRow = document.createElement('div');
    this.tabsRow.className = 'flex border-b border-gray-200 bg-gray-50';

    for (const tab of TABS) {
      const btn = this.createTabBtn(tab.id, tab.label, tab.icon);
      this.tabsRow.appendChild(btn);
      this.tabButtons.set(tab.id, btn);
    }
    this.el.appendChild(this.tabsRow);

    // Panel container
    this.panelContainer = document.createElement('div');
    this.panelContainer.className = 'flex-1 overflow-y-auto';

    this.outlinePanel = this.createPanel();
    this.statsPanel   = this.createPanel();
    this.xmlPanel     = this.createPanel();

    this.panelContainer.append(this.outlinePanel, this.statsPanel, this.xmlPanel);
    this.el.appendChild(this.panelContainer);

    container.appendChild(this.el);
    this.activateTab('outline');
  }

  getElement(): HTMLElement {
    return this.el;
  }

  toggle(): void {
    this.collapsed = !this.collapsed;
    this.el.style.width = this.collapsed ? '40px' : '240px';
    this.tabsRow.style.display = this.collapsed ? 'none' : '';
    this.panelContainer.style.display = this.collapsed ? 'none' : '';

    // Swap toggle icon direction
    this.toggleBtn.innerHTML = '';
    const iconDef = this.collapsed
      ? (ChevronLeft as [string, Record<string, string>][])
      : (ChevronRight as [string, Record<string, string>][]);
    this.toggleBtn.appendChild(createIconSvg(iconDef, 14));
  }

  updateOutline(renderTree: RenderNode[]): void {
    this.outlinePanel.innerHTML = '';

    const headings = renderTree.filter((n) =>
      ['h1', 'h2', 'h3', 'h4'].includes(n.tag),
    );

    if (headings.length === 0) {
      const empty = document.createElement('p');
      empty.className = 'text-xs text-gray-400 text-center py-4';
      empty.textContent = 'No headings found.';
      this.outlinePanel.appendChild(empty);
      return;
    }

    const list = document.createElement('ul');
    list.className = 'space-y-0.5';

    const indentMap: Record<string, string> = {
      h1: 'pl-2',
      h2: 'pl-5',
      h3: 'pl-8',
      h4: 'pl-11',
    };

    for (const h of headings) {
      const li = document.createElement('li');
      li.className =
        `flex items-center gap-1 py-0.5 text-xs text-gray-700 hover:text-blue-600 cursor-pointer rounded px-1 hover:bg-gray-50 ${indentMap[h.tag] ?? 'pl-2'}`;
      li.textContent = getTextContent(h);
      li.dataset.nodeId = h.id;
      if (h.tag === 'h1') li.classList.add('font-medium');
      if (h.tag === 'h3' || h.tag === 'h4') li.classList.add('text-gray-500');
      li.addEventListener('click', () => {
        const el = document.querySelector(`[data-node-id="${h.id}"]`);
        el?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      });
      list.appendChild(li);
    }

    this.outlinePanel.appendChild(list);
  }

  updateStats(renderTree: RenderNode[]): void {
    const text = renderTree.map(getTextContent).join(' ');
    const words = text.trim() ? text.trim().split(/\s+/).length : 0;
    const chars = text.length;
    const paragraphs = renderTree.filter((n) =>
      ['p', 'h1', 'h2', 'h3', 'h4'].includes(n.tag),
    ).length;
    const readingTime = Math.max(1, Math.ceil(words / 200));

    this.statsPanel.innerHTML = '';

    const grid = document.createElement('div');
    grid.className = 'grid grid-cols-2 gap-2';

    const stats = [
      { value: String(words), label: 'Words' },
      { value: String(chars), label: 'Characters' },
      { value: String(paragraphs), label: 'Paragraphs' },
      { value: `${readingTime} min`, label: 'Reading time' },
    ];

    for (const s of stats) {
      const cell = document.createElement('div');
      cell.className = 'text-center p-2 bg-gray-50 rounded';

      const val = document.createElement('span');
      val.className = 'block text-lg font-bold text-gray-800';
      val.textContent = s.value;

      const lbl = document.createElement('span');
      lbl.className = 'block text-xs text-gray-400 mt-0.5';
      lbl.textContent = s.label;

      cell.append(val, lbl);
      grid.appendChild(cell);
    }

    this.statsPanel.appendChild(grid);
  }

  updateXmlDebug(xml: string): void {
    this.xmlPanel.innerHTML = '';
    const pre = document.createElement('pre');
    pre.className =
      'text-xs leading-relaxed whitespace-pre-wrap break-all font-mono bg-gray-50 rounded p-2 max-h-full overflow-auto text-gray-600';
    pre.textContent = xml;
    this.xmlPanel.appendChild(pre);
  }

  // ── Private helpers ──────────────────────────────────────────────────────

  private createTabBtn(
    id: TabId,
    label: string,
    icon: [string, Record<string, string>][],
  ): HTMLButtonElement {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.title = label;
    btn.dataset.panel = id;
    btn.className = this.tabInactiveClass();
    btn.appendChild(createIconSvg(icon, 13));

    const labelEl = document.createElement('span');
    labelEl.textContent = label;
    btn.appendChild(labelEl);

    btn.addEventListener('click', () => this.activateTab(id));
    return btn;
  }

  private createPanel(): HTMLElement {
    const panel = document.createElement('div');
    panel.className = 'hidden p-3';
    return panel;
  }

  private activateTab(id: TabId): void {
    for (const [tabId, btn] of this.tabButtons) {
      btn.className = tabId === id ? this.tabActiveClass() : this.tabInactiveClass();
    }

    const panels = [this.outlinePanel, this.statsPanel, this.xmlPanel];
    const tabIds: TabId[] = ['outline', 'stats', 'xml'];
    for (let i = 0; i < panels.length; i++) {
      panels[i].className = tabIds[i] === id ? 'block p-3' : 'hidden p-3';
    }
  }

  private tabActiveClass(): string {
    return 'flex items-center justify-center gap-1 flex-1 px-2 py-2 text-xs font-medium text-blue-600 border-b-2 border-blue-500 bg-white transition-colors';
  }

  private tabInactiveClass(): string {
    return 'flex items-center justify-center gap-1 flex-1 px-2 py-2 text-xs font-medium text-gray-500 border-b-2 border-transparent hover:text-gray-700 hover:border-gray-300 bg-gray-50 transition-colors';
  }
}

function getTextContent(node: RenderNode): string {
  if (node.text !== undefined && node.text !== null) return node.text;
  return (node.children ?? []).map(getTextContent).join('');
}
