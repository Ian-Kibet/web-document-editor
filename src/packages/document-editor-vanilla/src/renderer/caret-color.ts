/**
 * Dynamically adapts the caret color based on the background color
 * under the cursor position. Call once to wire up the listener.
 */
export function setupCaretColorSync(canvas: HTMLElement): void {
  document.addEventListener('selectionchange', () => updateCaretColor(canvas));
}

function updateCaretColor(canvas: HTMLElement): void {
  const sel = window.getSelection();
  if (!sel || sel.rangeCount === 0) return;

  const node = sel.anchorNode;
  if (!node || !canvas.contains(node)) return;

  const el = node.nodeType === Node.TEXT_NODE ? node.parentElement : (node as HTMLElement);
  const bg = getEffectiveBackground(el, canvas);

  canvas.style.caretColor = bg && isColorDark(bg) ? 'white' : '';
}

/** Walk up the DOM to find the nearest non-transparent background. */
function getEffectiveBackground(
  el: Element | null,
  canvas: HTMLElement,
): string | null {
  let node: Element | null = el;
  while (node && node !== canvas.parentElement) {
    const bg = getComputedStyle(node).backgroundColor;
    if (bg && bg !== 'transparent' && bg !== 'rgba(0, 0, 0, 0)') {
      return bg;
    }
    node = node.parentElement;
  }
  return null;
}

/** Returns true if an rgb/rgba color string has relative luminance < 0.4. */
function isColorDark(color: string): boolean {
  const m = color.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
  if (!m) return false;
  const [r, g, b] = [+m[1] / 255, +m[2] / 255, +m[3] / 255];
  // Relative luminance (WCAG formula)
  const lum = 0.2126 * linearize(r) + 0.7152 * linearize(g) + 0.0722 * linearize(b);
  return lum < 0.4;
}

function linearize(c: number): number {
  return c <= 0.04045 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
}
