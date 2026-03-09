import { EngineBridge } from './bridge/engine-bridge';
import type { EngineResponse, RenderNode } from './bridge/types';
import { renderTree } from './renderer/dom-renderer';
import { restoreCursor, domToModelSelection } from './renderer/cursor-manager';
import { PageLayout } from './renderer/page-layout';
import { DebugMarginPanel } from './ui/debug-margin-panel';
import { attachImageHandles, hideImageHandles } from './renderer/image-handles';
import { ContextMenu } from './ui/context-menu';
import { InputHandler } from './input/input-handler';
import { KeyboardShortcuts } from './input/keyboard-shortcuts';
import { PasteHandler } from './input/paste-handler';
import { Toolbar } from './ui/toolbar';
import { Sidebar } from './ui/sidebar';
import { StatusBar } from './ui/status-bar';
import { HorizontalRuler } from './ui/ruler-h';
import { VerticalRuler } from './ui/ruler-v';
import { ZoomController } from './ui/zoom';
import { WORD_PRESET, GDOCS_PRESET, COMPACT_PRESET } from './ui/toolbar-presets';
import type { ToolbarPreset } from './ui/toolbar-config';
import { Ruler } from 'lucide';

import './styles/main.css';
import './styles/editor.css';
import './styles/pages.css';

export type { EngineBridge } from './bridge/engine-bridge';
export type * from './bridge/types';

/**
 * Options for mounting the Document Editor into a container element.
 */
export interface EditorMountOptions {
  /** The DOM element the editor will be rendered into. */
  container: HTMLElement;
  /** Optional serialized document JSON to load on startup. */
  initialDocJson?: string;
  /** localStorage key prefix. Defaults to 'documentEditor'. */
  storagePrefix?: string;
  /** Toolbar layout preset. Defaults to 'word'. */
  toolbarPreset?: 'word' | 'gdocs' | 'compact';
  /** Called when the editor is fully initialized and ready. */
  onReady?: (instance: EditorInstance) => void;
  /** Called if initialization fails. */
  onError?: (err: Error) => void;
}

/**
 * Handle returned by mountEditor(). Use destroy() for cleanup.
 */
export interface EditorInstance {
  /** The engine bridge for programmatic document operations. */
  engine: EngineBridge;
  /** Tears down the editor: removes listeners, clears DOM, cleans up globals. */
  destroy(): void;
}

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

function resolvePreset(name?: 'word' | 'gdocs' | 'compact'): ToolbarPreset {
  switch (name) {
    case 'gdocs': return GDOCS_PRESET;
    case 'compact': return COMPACT_PRESET;
    default: return WORD_PRESET;
  }
}

/**
 * Mount a Document Editor instance into the given container element.
 *
 * @param options - Mount configuration including container, preset, and callbacks.
 * @returns A promise that resolves to an EditorInstance once initialization is complete.
 *
 * @example
 * ```ts
 * import { mountEditor } from 'document-editor-vanilla';
 *
 * const instance = await mountEditor({
 *   container: document.getElementById('editor-root')!,
 *   toolbarPreset: 'word',
 * });
 *
 * // Export as .docx:
 * const bytes = await instance.engine.exportDocx();
 * ```
 */
export async function mountEditor(options: EditorMountOptions): Promise<EditorInstance> {
  const {
    container,
    storagePrefix = 'documentEditor',
    toolbarPreset,
    onReady,
    onError,
  } = options;

  try {
    // Show loading state
    container.innerHTML = '<div class="editor-loading">Loading editor engine...</div>';

    // Set up the globals that C# calls into (idempotent)
    if (!(window as any).setDotNetReference) {
      let _ref: unknown = null;
      (window as any).setDotNetReference = (ref: unknown) => {
        _ref = ref;
        window.dispatchEvent(new CustomEvent('engine-ready', { detail: ref }));
      };
      (window as any).getDotNetReference = () => _ref;
    }

    // Dynamically load Blazor WASM from _framework/ sibling to this module (idempotent)
    if (!(window as any).__blazorScriptInjected) {
      (window as any).__blazorScriptInjected = true;
      const frameworkBase = '/_framework/';
      await new Promise<void>((resolve, reject) => {
        const s = document.createElement('script');
        s.src = `${frameworkBase}blazor.webassembly.js`;
        s.setAttribute('autostart', 'true');
        s.addEventListener('load', () => resolve());
        s.addEventListener('error', () => reject(new Error(`Failed to load Blazor from ${s.src}`)));
        document.head.appendChild(s);
      });
    }

    // Initialize engine bridge
    const engine = new EngineBridge();
    await engine.waitForReady();

    // Clear loading state
    container.innerHTML = '';
    container.className = 'flex flex-col h-screen overflow-hidden bg-white';

    // Toolbar
    const toolbarContainer = document.createElement('div');
    container.appendChild(toolbarContainer);

    // Content area (below toolbar, above status bar)
    const contentArea = document.createElement('div');
    contentArea.className = 'flex flex-1 overflow-hidden';
    container.appendChild(contentArea);

    // Main column (ruler + scroll)
    const mainArea = document.createElement('div');
    mainArea.className = 'flex flex-col flex-1 overflow-hidden';
    contentArea.appendChild(mainArea);

    // Ruler row: [corner | H-ruler]
    const rulerRow = document.createElement('div');
    rulerRow.className = 'flex bg-gray-50 flex-shrink-0';
    mainArea.appendChild(rulerRow);

    // Corner piece (where the two rulers meet, top-left) — also hosts debug toggle
    const rulerCorner = document.createElement('div');
    rulerCorner.className = 'w-6 border-b border-r border-gray-200 flex-shrink-0';
    rulerRow.appendChild(rulerCorner);

    // Debug toggle button inside the corner
    const debugToggle = document.createElement('button');
    debugToggle.type = 'button';
    debugToggle.title = 'Toggle margin debug panel (doc vs applied)';
    debugToggle.className = 'w-full h-full flex items-center justify-center text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors';
    debugToggle.appendChild(createIconSvg(Ruler as [string, Record<string, string>][], 12));
    rulerCorner.appendChild(debugToggle);

    // Horizontal ruler container (fills ruler row width)
    const rulerHContainer = document.createElement('div');
    rulerHContainer.className = 'flex-1 overflow-hidden';
    rulerRow.appendChild(rulerHContainer);

    // Editor row: [V-ruler | scroll area]
    const editorRow = document.createElement('div');
    editorRow.className = 'flex flex-1 overflow-hidden';
    mainArea.appendChild(editorRow);

    // Vertical ruler container (left of scroll area, constrained by editorRow height)
    const rulerVContainer = document.createElement('div');
    editorRow.appendChild(rulerVContainer);

    // Debug margin panel (between V-ruler and scroll area)
    const debugPanelMount = document.createElement('div');
    editorRow.appendChild(debugPanelMount);

    // Scroll area
    const scrollArea = document.createElement('div');
    scrollArea.className = 'flex flex-1 overflow-auto bg-[#e8eaed]';
    editorRow.appendChild(scrollArea);

    // Sidebar
    const sidebarContainer = document.createElement('div');
    contentArea.appendChild(sidebarContainer);

    // Status bar
    const statusBarContainer = document.createElement('div');
    container.appendChild(statusBarContainer);

    // Initialize page layout
    const pageLayout = new PageLayout(scrollArea);
    const canvas = pageLayout.getCanvas();

    // Debug margin panel
    const debugPanel = new DebugMarginPanel(debugPanelMount, canvas);
    debugToggle.addEventListener('click', () => debugPanel.toggle());

    // Restore grid lines preference
    if (localStorage.getItem(`${storagePrefix}.gridLines`) === '1') {
      canvas.classList.add('show-grid');
    }

    // Restore paragraph marks preference
    if (localStorage.getItem(`${storagePrefix}.pilcrow`) === '1') {
      canvas.classList.add('show-pilcrow');
    }

    // Guard: suppress selectionchange adjustment during programmatic cursor restore
    let suppressSelectionAdjust = false;

    // Ruler tracking: update only when the cursor moves to a different page/section
    let cursorPage = 1;
    const updateRulersForCursorPage = (page: number, force = false): void => {
      if (page === cursorPage && !force) return;
      cursorPage = page;
      const dims = pageLayout.getPageRulerDimensions(page);
      if (!dims) return;
      const pageTopScrollY = pageLayout.getPageTopScrollY(page);
      rulerH.updateDimensions(dims.pageWidth, dims.marginLeft, dims.marginRight, dims.sectionIndex);
      rulerV.updateDimensions(dims.pageHeight, dims.marginTop, dims.marginBottom, page, pageTopScrollY);
    };

    // Debounce utility for non-critical per-keystroke work
    function debounce<T extends unknown[]>(fn: (...args: T) => void, ms: number) {
      let timer: ReturnType<typeof setTimeout> | undefined;
      return (...args: T) => { clearTimeout(timer); timer = setTimeout(() => fn(...args), ms); };
    }

    // Debounced update functions — assigned real implementations after component init
    let debouncedUpdateOutline = (_tree: RenderNode[]) => {};
    let debouncedUpdateStats   = (_tree: RenderNode[]) => {};
    let debouncedWordCount     = (_tree: RenderNode[]) => {};
    let debouncedPagination    = () => {};

    // Shared response handler
    const handleResponse = (response: EngineResponse): void => {
      suppressSelectionAdjust = true;
      hideImageHandles(); // dismiss handles before re-render
      if (response.sections?.length > 0) {
        pageLayout.updateFromSections(response.sections);
        debugPanel.update(pageLayout.getDebugSectionData());
      }
      renderTree(response.renderTree, canvas);
      restoreCursor(response.selection, canvas);
      toolbar.updateState(response);
      debouncedWordCount(response.renderTree);
      debouncedUpdateOutline(response.renderTree);
      debouncedUpdateStats(response.renderTree);
      debouncedPagination();
      const currentPage = pageLayout.getCurrentPage(scrollArea);
      const cp = pageLayout.getPageForCursor();
      updateRulersForCursorPage(cp, true); // force: dimensions may have changed
      statusBar.updatePageInfo(currentPage, pageLayout.pageCount);
      requestAnimationFrame(() => {
        suppressSelectionAdjust = false;
      });
    };

    // Initialize UI components
    const preset = resolvePreset(toolbarPreset);
    const toolbar = new Toolbar(toolbarContainer, engine, canvas, handleResponse, preset);
    const sidebar = new Sidebar(sidebarContainer, `${storagePrefix}.sidebarCollapsed`);
    const statusBar = new StatusBar(statusBarContainer);
    const rulerH = new HorizontalRuler(rulerHContainer, engine, canvas, handleResponse);
    const rulerV = new VerticalRuler(rulerVContainer);

    // Assign debounced implementations now that all components exist
    debouncedUpdateOutline = debounce((tree) => sidebar.updateOutline(tree), 300);
    debouncedUpdateStats   = debounce((tree) => sidebar.updateStats(tree), 300);
    debouncedWordCount     = debounce((tree) => statusBar.updateWordCount(tree), 300);
    debouncedPagination    = debounce(() => {
      pageLayout.updatePagination();
      rulerV.setPageCount(pageLayout.pageCount);
      rulerV.setTotalScrollHeight(pageLayout.getTotalScrollHeight());
      updateRulersForCursorPage(cursorPage, true); // re-sync after pagination settles
    }, 150);

    // Zoom
    const pagesWrapper = scrollArea.querySelector('.pages-wrapper') as HTMLElement | null;
    const zoom = new ZoomController(pagesWrapper!, (percent) => {
      statusBar.updateZoom(percent);
      rulerV.setZoom(percent / 100);
      rulerH.setZoom(percent / 100);
    });

    // Status bar zoom buttons dispatch custom events handled here
    const onZoomOut = () => zoom.zoomOut();
    const onZoomIn  = () => zoom.zoomIn();
    window.addEventListener('doc:zoom-out', onZoomOut);
    window.addEventListener('doc:zoom-in', onZoomIn);

    // Sync vertical ruler scroll + update current page/ruler on scroll
    scrollArea.addEventListener('scroll', () => {
      rulerV.syncScroll(scrollArea.scrollTop);
      rulerH.syncScrollLeft(scrollArea.scrollLeft, scrollArea.clientWidth);
      const page = pageLayout.getCurrentPage(scrollArea);
      statusBar.updatePageInfo(page, pageLayout.pageCount);
    });

    // Image handles overlay
    attachImageHandles(canvas, scrollArea, engine, handleResponse);

    // Context menu (right-click)
    const contextMenu = new ContextMenu(canvas, engine, handleResponse);

    // Initialize input handling
    const inputHandler = new InputHandler(engine, canvas, handleResponse);
    const shortcuts = new KeyboardShortcuts(engine, canvas, handleResponse);
    const pasteHandler = new PasteHandler(engine, canvas, handleResponse);

    // Snap caret out of page-break gap zones on any selection change
    const onSelectionChange = () => {
      if (suppressSelectionAdjust) return;
      const sel = window.getSelection();
      if (!sel || !sel.isCollapsed || sel.rangeCount === 0) return;
      if (!canvas.contains(sel.getRangeAt(0).startContainer)) return;
      pageLayout.adjustCursorForPageBreaks();
      updateRulersForCursorPage(pageLayout.getPageForCursor());
      debugPanel.setCursorSection(pageLayout.getSectionForCursor());
    };
    document.addEventListener('selectionchange', onSelectionChange);

    // Update format-state-reactive toolbar controls on cursor movement
    const debouncedFormatQuery = debounce(async () => {
      if (suppressSelectionAdjust || !engine.isReady) return;
      const sel = window.getSelection();
      if (!sel || sel.rangeCount === 0 || !canvas.contains(sel.anchorNode)) return;
      const modelSel = domToModelSelection(canvas);
      if (!modelSel) return;
      try {
        const fs = await engine.getFormatState(modelSel);
        toolbar.updateFormatState(fs);
      } catch { /* engine might not be ready */ }
    }, 80);

    document.addEventListener('selectionchange', debouncedFormatQuery);

    // Initialize the document
    const response = await engine.initialize();
    handleResponse(response);

    // Focus the canvas
    canvas.focus();

    // Build instance object
    const instance: EditorInstance = {
      engine,
      destroy() {
        document.removeEventListener('selectionchange', onSelectionChange);
        document.removeEventListener('selectionchange', debouncedFormatQuery);
        window.removeEventListener('doc:zoom-out', onZoomOut);
        window.removeEventListener('doc:zoom-in', onZoomIn);
        inputHandler.destroy();
        shortcuts.destroy();
        pasteHandler.destroy();
        contextMenu.destroy();
        container.innerHTML = '';
        delete (window as any).__documentEditor;
      },
    };

    // Expose for debugging
    (window as any).__documentEditor = {
      engine,
      canvas,
      toolbar,
      sidebar,
      statusBar,
      rulerH,
      rulerV,
      zoom,
      inputHandler,
      shortcuts,
      pasteHandler,
      pageLayout,
      contextMenu,
      debugPanel,
      instance,
    };

    onReady?.(instance);
    return instance;
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    onError?.(error);
    throw error;
  }
}
