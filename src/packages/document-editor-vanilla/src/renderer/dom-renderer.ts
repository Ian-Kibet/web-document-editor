import type { RenderNode } from '../bridge/types';
import { ensureFontsLoaded } from './font-loader';

// Module-level map for incremental reconciliation: nodeId → live HTMLElement
const _nodeMap = new Map<string, HTMLElement>();

/**
 * Render a RenderNode tree to DOM elements inside a container.
 * Uses keyed reconciliation (data-node-id) to avoid full DOM rebuilds on every keystroke.
 */
export function renderTree(nodes: RenderNode[], container: HTMLElement): void {
  reconcileChildren(nodes, container);
  ensureFontsLoaded(collectFonts(nodes));
}

function collectFonts(nodes: RenderNode[]): Set<string> {
  const fonts = new Set<string>();
  function walk(ns: RenderNode[]) {
    for (const n of ns) {
      const ff = n.styles?.['font-family'];
      if (ff) {
        // strip quotes: "'Arimo', sans-serif" → "Arimo"
        const m = ff.match(/['"]?([^'",]+)['"]?/);
        if (m) fonts.add(m[1].trim());
      }
      if (n.children) walk(n.children);
    }
  }
  walk(nodes);
  return fonts;
}

function reconcileChildren(newNodes: RenderNode[], container: HTMLElement): void {
  const newIds = new Set(newNodes.map(n => n.id));

  // Remove stale direct children (and all their descendants from the map)
  for (const child of Array.from(container.children)) {
    const el = child as HTMLElement;
    const id = el.dataset.nodeId;
    if (id && !newIds.has(id)) {
      removeFromMap(el);
      container.removeChild(el);
    }
  }

  // Reconcile children in the desired order
  let refNode: ChildNode | null = container.firstChild;
  for (const newNode of newNodes) {
    const existing = _nodeMap.get(newNode.id);
    if (existing && existing.tagName.toLowerCase() === newNode.tag) {
      updateDomNode(existing, newNode);
      if (existing !== refNode) {
        container.insertBefore(existing, refNode);
      } else {
        refNode = existing.nextSibling;
      }
    } else {
      // New node or tag changed: remove old element from DOM and map if it exists
      if (existing) {
        if (existing === refNode) refNode = existing.nextSibling;
        removeFromMap(existing);
        existing.parentNode?.removeChild(existing);
      }
      const el = createDomNode(newNode);
      registerInMap(el);
      container.insertBefore(el, refNode);
    }
  }
}

function updateDomNode(el: HTMLElement, node: RenderNode): void {
  // Update inline style
  const newStyle = node.styles
    ? Object.entries(node.styles).map(([k, v]) => `${k}:${v}`).join(';')
    : '';
  if (el.style.cssText !== newStyle) el.style.cssText = newStyle;

  // Update attributes
  reconcileAttrs(el, node.attrs ?? {});

  // Update text content (leaf nodes — mutually exclusive with children)
  if (node.text !== undefined) {
    const expected = node.text || '\u200B';
    if (el.textContent !== expected) el.textContent = expected;
  }

  // Recurse into children
  if (node.children) {
    reconcileChildren(node.children, el);
  }
}

function reconcileAttrs(el: HTMLElement, newAttrs: Record<string, string>): void {
  // Set / update new attrs
  for (const [k, v] of Object.entries(newAttrs)) {
    if (k.startsWith('data-')) {
      const key = camelCase(k.slice(5));
      if (el.dataset[key] !== v) el.dataset[key] = v;
    } else {
      if (el.getAttribute(k) !== v) el.setAttribute(k, v);
    }
  }
  // Remove attrs no longer present (skip data-node-id and style, which are managed separately)
  for (const attr of Array.from(el.attributes)) {
    const name = attr.name;
    if (name === 'data-node-id' || name === 'style') continue;
    if (name.startsWith('data-')) {
      if (!(name in newAttrs)) delete el.dataset[camelCase(name.slice(5))];
    } else {
      if (!(name in newAttrs)) el.removeAttribute(name);
    }
  }
}

function registerInMap(el: HTMLElement): void {
  if (el.dataset.nodeId) _nodeMap.set(el.dataset.nodeId, el);
  for (const child of Array.from(el.children)) {
    registerInMap(child as HTMLElement);
  }
}

function removeFromMap(el: HTMLElement): void {
  if (el.dataset.nodeId) _nodeMap.delete(el.dataset.nodeId);
  for (const child of Array.from(el.children)) {
    removeFromMap(child as HTMLElement);
  }
}

/**
 * Recursively create a DOM element from a RenderNode.
 * Each element gets data-node-id for cursor mapping.
 */
export function createDomNode(node: RenderNode): HTMLElement {
  const el = document.createElement(node.tag);
  el.dataset.nodeId = node.id;

  if (node.styles) {
    el.style.cssText = Object.entries(node.styles)
      .map(([k, v]) => `${k}:${v}`)
      .join(';');
  }

  if (node.attrs) {
    for (const [k, v] of Object.entries(node.attrs)) {
      if (k.startsWith('data-')) {
        el.dataset[camelCase(k.slice(5))] = v;
      } else {
        el.setAttribute(k, v);
      }
    }
  }

  if (node.text !== undefined && node.text !== null) {
    // Use zero-width space for empty nodes so cursor can land there
    el.textContent = node.text || '\u200B';
  }

  if (node.children) {
    for (const child of node.children) {
      el.appendChild(createDomNode(child));
    }
  }

  return el;
}

/**
 * Create a read-only DOM element from a RenderNode.
 * The root is non-editable and non-selectable (for header/footer display).
 */
export function createReadOnlyDomNode(node: RenderNode): HTMLElement {
  const el = createDomNode(node);
  el.contentEditable = 'false';
  el.style.userSelect = 'none';
  return el;
}

function camelCase(str: string): string {
  return str.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
}
