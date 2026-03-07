/**
 * image-handles.ts — Selection overlay for inline images.
 *
 * Shows resize (8 cardinal handles) and rotate handles when an image is
 * clicked. Resize/rotate commits to the C# engine on mouseup.
 *
 * Usage: call attachImageHandles(canvas, scrollContainer, engine, onResponse)
 * once during editor init.
 */

import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';

const EMU_PER_PX = 9525;

type HandlePos = 'nw' | 'n' | 'ne' | 'e' | 'se' | 's' | 'sw' | 'w' | 'rotate';

interface ActiveState {
  imgEl: HTMLElement;
  nodeId: string;
  overlayEl: HTMLDivElement;
  handles: Map<HandlePos, HTMLDivElement>;
}

let _engine: EngineBridge;
let _onResponse: (r: EngineResponse) => void;
let _scrollContainer: HTMLElement;
let active: ActiveState | null = null;

// ─── Public API ────────────────────────────────────────────────────────────

export function attachImageHandles(
  canvas: HTMLElement,
  scrollContainer: HTMLElement,
  engine: EngineBridge,
  onResponse: (r: EngineResponse) => void,
): void {
  _engine = engine;
  _onResponse = onResponse;
  _scrollContainer = scrollContainer;

  // Ensure scroll container is a positioning context for the overlay
  if (getComputedStyle(scrollContainer).position === 'static') {
    scrollContainer.style.position = 'relative';
  }

  canvas.addEventListener('click', onCanvasClick);
  canvas.addEventListener('mousedown', (e: MouseEvent) => {
    const imgEl = (e.target as HTMLElement).closest<HTMLElement>('[data-type="image"]');
    if (imgEl) {
      e.preventDefault(); // stop browser placing text cursor / starting native drag
    }
  });
  document.addEventListener('click', onDocumentClick, true); // capture
}

/** Programmatically hide handles (call after engine response re-renders) */
export function hideImageHandles(): void {
  hideHandles();
}

// ─── Event handlers ────────────────────────────────────────────────────────

function onCanvasClick(e: MouseEvent): void {
  const imgWrapper = (e.target as HTMLElement).closest<HTMLElement>('[data-type="image"]');
  if (imgWrapper) {
    e.stopPropagation();
    showHandles(imgWrapper);
  }
}

function onDocumentClick(e: MouseEvent): void {
  if (!active) return;
  const target = e.target as HTMLElement;
  // Keep handles if clicking on the overlay itself or the image
  if (target === active.imgEl || active.overlayEl.contains(target)) return;
  hideHandles();
}

// ─── Show / hide ───────────────────────────────────────────────────────────

function showHandles(imgEl: HTMLElement): void {
  hideHandles();

  const nodeId = imgEl.dataset.nodeId ?? '';
  if (!nodeId) return;

  const overlay = document.createElement('div');
  overlay.className = 'wave-img-handles';
  // Absolute within scrollContainer
  overlay.style.cssText = 'position:absolute;pointer-events:none;z-index:100;';

  // Border ring
  const border = document.createElement('div');
  border.className = 'wave-img-selected-border';
  overlay.appendChild(border);

  // Resize handles
  const RESIZE_POSITIONS: HandlePos[] = ['nw', 'n', 'ne', 'e', 'se', 's', 'sw', 'w'];
  const handles = new Map<HandlePos, HTMLDivElement>();

  for (const pos of RESIZE_POSITIONS) {
    const h = document.createElement('div');
    h.className = 'wave-img-handle';
    h.dataset.pos = pos;
    h.style.cursor = getCursor(pos);
    overlay.appendChild(h);
    handles.set(pos, h);
    attachResizeDrag(h, pos, imgEl, nodeId);
  }

  // Move drag zone — fills overlay interior, sits below handles in DOM order
  const moveZone = document.createElement('div');
  moveZone.className = 'wave-img-move-zone';
  overlay.appendChild(moveZone);
  attachMoveDrag(moveZone, imgEl, nodeId);

  // Rotate line
  const rotateLine = document.createElement('div');
  rotateLine.className = 'wave-img-rotate-line';
  overlay.appendChild(rotateLine);

  // Rotate handle
  const rotateHandle = document.createElement('div');
  rotateHandle.className = 'wave-img-handle';
  rotateHandle.dataset.pos = 'rotate';
  rotateHandle.style.cursor = 'grab';
  overlay.appendChild(rotateHandle);
  handles.set('rotate', rotateHandle);
  attachRotateDrag(rotateHandle, imgEl, nodeId, overlay);

  _scrollContainer.appendChild(overlay);

  active = { imgEl, nodeId, overlayEl: overlay, handles };
  positionOverlay();
}

function hideHandles(): void {
  if (!active) return;
  active.overlayEl.remove();
  active = null;
}

// ─── Overlay positioning ───────────────────────────────────────────────────

function positionOverlay(liveW?: number, liveH?: number): void {
  if (!active) return;
  const { imgEl, overlayEl, handles } = active;

  const deg = parseFloat(imgEl.dataset.rotation ?? '0');

  const scRect  = _scrollContainer.getBoundingClientRect();
  const imgRect = imgEl.getBoundingClientRect();  // axis-aligned box (post-rotation)

  // Original (unrotated) image dimensions from data attributes
  const origW  = parseFloat(imgEl.dataset.origWidth  ?? '0') || imgEl.offsetWidth;
  const origH  = parseFloat(imgEl.dataset.origHeight ?? '0') || imgEl.offsetHeight;
  const width  = liveW  ?? origW;
  const height = liveH ?? origH;

  // Visual center of the (possibly rotated) image in scroll-container coords
  const cx = imgRect.left + imgRect.width  / 2 - scRect.left + _scrollContainer.scrollLeft;
  const cy = imgRect.top  + imgRect.height / 2 - scRect.top  + _scrollContainer.scrollTop;

  overlayEl.style.left            = `${cx - width  / 2}px`;
  overlayEl.style.top             = `${cy - height / 2}px`;
  overlayEl.style.width           = `${width}px`;
  overlayEl.style.height          = `${height}px`;
  overlayEl.style.transform       = deg !== 0 ? `rotate(${deg}deg)` : '';
  overlayEl.style.transformOrigin = '50% 50%';

  // Resize handle positions (% of overlay box)
  const POS: Record<string, [string, string]> = {
    nw: ['0%',   '0%'  ],
    n:  ['50%',  '0%'  ],
    ne: ['100%', '0%'  ],
    e:  ['100%', '50%' ],
    se: ['100%', '100%'],
    s:  ['50%',  '100%'],
    sw: ['0%',   '100%'],
    w:  ['0%',   '50%' ],
  };

  for (const [pos, [x, y]] of Object.entries(POS)) {
    const h = handles.get(pos as HandlePos);
    if (h) { h.style.left = x; h.style.top = y; }
  }

  // Rotate handle + line: 28px above top-centre
  const ROTATE_OFFSET = 28;
  const rotateHandle = handles.get('rotate');
  const rotateLine   = overlayEl.querySelector<HTMLElement>('.wave-img-rotate-line');
  if (rotateHandle) {
    rotateHandle.style.left = '50%';
    rotateHandle.style.top  = `${-ROTATE_OFFSET}px`;
  }
  if (rotateLine) {
    rotateLine.style.left   = `${width / 2}px`;
    rotateLine.style.top    = `${-ROTATE_OFFSET}px`;
    rotateLine.style.height = `${ROTATE_OFFSET}px`;
  }
}

// ─── Resize drag ───────────────────────────────────────────────────────────

function attachResizeDrag(
  handleEl: HTMLDivElement,
  pos: HandlePos,
  imgEl: HTMLElement,
  nodeId: string,
): void {
  handleEl.addEventListener('mousedown', (startEv) => {
    startEv.preventDefault();
    startEv.stopPropagation();

    const startX      = startEv.clientX;
    const startY      = startEv.clientY;
    const startW      = parseFloat(imgEl.dataset.origWidth  ?? '0') || imgEl.offsetWidth;
    const startH      = parseFloat(imgEl.dataset.origHeight ?? '0') || imgEl.offsetHeight;
    const aspectRatio = startW / startH;

    // Live overlay rect (mirrors what the final image size will be)
    let liveW = startW;
    let liveH = startH;

    function onMove(mv: MouseEvent): void {
      const dx = mv.clientX - startX;
      const dy = mv.clientY - startY;
      const lockAspect = mv.shiftKey;

      let newW = startW;
      let newH = startH;

      // Derive new dimensions from handle position
      if (pos.includes('e')) newW = Math.max(16, startW + dx);
      if (pos.includes('w')) newW = Math.max(16, startW - dx);
      if (pos.includes('s')) newH = Math.max(16, startH + dy);
      if (pos.includes('n')) newH = Math.max(16, startH - dy);

      if (lockAspect) {
        // Drive from the larger delta
        if (Math.abs(dx) >= Math.abs(dy)) {
          newH = newW / aspectRatio;
        } else {
          newW = newH * aspectRatio;
        }
      }

      liveW = newW;
      liveH = newH;

      // Update overlay size visually (no engine call yet)
      positionOverlay(liveW, liveH);
    }

    function onUp(): void {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);

      const widthEmu  = Math.round(liveW  * EMU_PER_PX);
      const heightEmu = Math.round(liveH * EMU_PER_PX);

      _engine.setImageSize(nodeId, widthEmu, heightEmu).then((resp) => {
        _onResponse(resp);
        // Handles will be rebuilt after re-render; hide current overlay
        hideHandles();
      });
    }

    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  });
}

// ─── Rotate drag ───────────────────────────────────────────────────────────

function attachRotateDrag(
  handleEl: HTMLDivElement,
  imgEl: HTMLElement,
  nodeId: string,
  overlay: HTMLDivElement,
): void {
  handleEl.addEventListener('mousedown', (startEv) => {
    startEv.preventDefault();
    startEv.stopPropagation();

    const imgRect   = imgEl.getBoundingClientRect();
    const centerX   = imgRect.left + imgRect.width  / 2;
    const centerY   = imgRect.top  + imgRect.height / 2;
    const startRad  = Math.atan2(startEv.clientY - centerY, startEv.clientX - centerX);
    const initDeg   = parseFloat(imgEl.dataset.rotation ?? '0');

    let currentDeg = initDeg;
    handleEl.style.cursor = 'grabbing';

    function onMove(mv: MouseEvent): void {
      const angle  = Math.atan2(mv.clientY - centerY, mv.clientX - centerX);
      const deltaDeg = (angle - startRad) * (180 / Math.PI);
      let deg = initDeg + deltaDeg;

      if (mv.shiftKey) {
        // Snap to 15° increments
        deg = Math.round(deg / 15) * 15;
      }

      currentDeg = deg;
      // Live visual feedback on overlay
      overlay.style.transform = `rotate(${deg}deg)`;
      overlay.style.transformOrigin = '50% 50%';
    }

    function onUp(): void {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
      handleEl.style.cursor = 'grab';
      overlay.style.transform = '';
      overlay.style.transformOrigin = '';

      _engine.setImageRotation(nodeId, currentDeg).then((resp) => {
        _onResponse(resp);
        hideHandles();
      });
    }

    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  });
}

// ─── Move drag ──────────────────────────────────────────────────────────────

function attachMoveDrag(
  moveZoneEl: HTMLDivElement,
  imgEl: HTMLElement,
  nodeId: string,
): void {
  moveZoneEl.addEventListener('mousedown', (startEv) => {
    if (startEv.button !== 0) return;
    startEv.preventDefault();
    startEv.stopPropagation();

    if (!active) return;
    const { overlayEl } = active;

    const startLeft  = parseFloat(overlayEl.style.left) || 0;
    const startTop   = parseFloat(overlayEl.style.top)  || 0;
    const startX     = startEv.clientX;
    const startY     = startEv.clientY;

    let liveLeft = startLeft;
    let liveTop  = startTop;

    overlayEl.style.cursor = 'grabbing';

    function onMove(mv: MouseEvent): void {
      liveLeft = startLeft + (mv.clientX - startX);
      liveTop  = startTop  + (mv.clientY - startY);
      overlayEl.style.left = `${liveLeft}px`;
      overlayEl.style.top  = `${liveTop}px`;
    }

    function onUp(): void {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup',   onUp);
      overlayEl.style.cursor = '';

      // Compute image top-left relative to .editor-canvas (the CSS positioning context
      // for position:absolute images — see RenderTreeBuilder.ApplyWrapModeStyles).
      const canvasEl  = imgEl.closest<HTMLElement>('.editor-canvas')
                     ?? document.querySelector<HTMLElement>('.editor-canvas')!;
      const scRect     = _scrollContainer.getBoundingClientRect();
      const canvasRect = canvasEl.getBoundingClientRect();

      // Canvas origin in scroll-container–scroll-adjusted coords:
      const canvasLeftInSC = canvasRect.left - scRect.left + _scrollContainer.scrollLeft;
      const canvasTopInSC  = canvasRect.top  - scRect.top  + _scrollContainer.scrollTop;

      const newLeftPx = liveLeft - canvasLeftInSC;
      const newTopPx  = liveTop  - canvasTopInSC;

      const hEmu = Math.round(Math.max(0, newLeftPx) * EMU_PER_PX);
      const vEmu = Math.round(Math.max(0, newTopPx)  * EMU_PER_PX);

      _engine.setImagePosition(nodeId, hEmu, vEmu).then((resp) => {
        _onResponse(resp);
        hideHandles();
      });
    }

    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup',   onUp);
  });
}

// ─── Helpers ───────────────────────────────────────────────────────────────

function getCursor(pos: HandlePos): string {
  switch (pos) {
    case 'nw': case 'se': return 'nwse-resize';
    case 'ne': case 'sw': return 'nesw-resize';
    case 'n':  case 's':  return 'ns-resize';
    case 'e':  case 'w':  return 'ew-resize';
    default: return 'grab';
  }
}
