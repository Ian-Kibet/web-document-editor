import type { Position, Selection, SelectionResponse } from '../bridge/types';

/**
 * Map a DOM selection point (node + offset) to a model Position.
 *
 * Strategy: walk up from the target node to find the nearest element
 * with data-node-id, then find which block and inline that belongs to
 * by walking the parent chain.
 */
export function domToModelPosition(
  node: Node,
  offset: number,
  canvas: HTMLElement,
): Position | null {
  // Get the element — for text nodes, use parent
  let el = node.nodeType === Node.TEXT_NODE ? node.parentElement : (node as HTMLElement);

  // Walk up to find the span (inline) with data-node-id
  while (el && el !== canvas && !el.dataset?.nodeId) {
    el = el.parentElement;
  }
  if (!el || el === canvas) return null;

  const inlineEl = el;

  // Find the block parent (p, h1-h4)
  let blockEl = inlineEl.parentElement;
  while (blockEl && blockEl !== canvas) {
    const tag = blockEl.tagName.toLowerCase();
    if (tag === 'p' || tag.match(/^h[1-6]$/)) break;
    // For hyperlinks (<a>), keep walking up
    blockEl = blockEl.parentElement;
  }
  if (!blockEl || blockEl === canvas) return null;

  // Adjust offset: account for zero-width spaces used in empty nodes
  let adjustedOffset = offset;
  if (node.nodeType === Node.TEXT_NODE && node.textContent === '\u200B') {
    adjustedOffset = 0;
  }

  // Get inline index: position among the block's inline children
  const inlineIndex = getInlineIndex(inlineEl, blockEl);

  // Is blockEl (the <p>) inside a <td>?
  const cellInfo = findTableCellAncestor(blockEl, canvas);
  if (cellInfo) {
    const { tableEl, rowIndex, cellIndex, cellBlockIndex } = cellInfo;
    const blockIndex = getBlockIndex(tableEl, canvas);
    if (blockIndex < 0) return null;
    return { blockIndex, inlineIndex, offset: adjustedOffset, cell: { rowIndex, cellIndex, cellBlockIndex } };
  }

  // Normal top-level path
  const blockIndex = getBlockIndex(blockEl, canvas);
  if (blockIndex < 0) return null;
  return { blockIndex, inlineIndex, offset: adjustedOffset };
}

/**
 * Map the full DOM selection to a model Selection (anchor + focus).
 */
export function domToModelSelection(canvas: HTMLElement): Selection | null {
  const sel = window.getSelection();
  if (!sel || sel.rangeCount === 0) return null;

  const anchor = sel.anchorNode
    ? domToModelPosition(sel.anchorNode, sel.anchorOffset, canvas)
    : null;
  const focus = sel.focusNode
    ? domToModelPosition(sel.focusNode, sel.focusOffset, canvas)
    : null;

  if (!anchor) return null;

  return {
    anchor,
    focus: focus ?? anchor,
  };
}

/**
 * After re-render, restore the browser cursor to the model position.
 *
 * Find the DOM text node that corresponds to the model's inline position,
 * then call Selection.setBaseAndExtent().
 */
export function restoreCursor(
  selection: SelectionResponse,
  canvas: HTMLElement,
): void {
  const anchorInfo = findTextNode(selection.anchor, canvas);
  if (!anchorInfo) return;

  const domSel = window.getSelection();
  if (!domSel) return;

  if (selection.isCollapsed) {
    domSel.setBaseAndExtent(
      anchorInfo.node,
      anchorInfo.offset,
      anchorInfo.node,
      anchorInfo.offset,
    );
  } else {
    const focusInfo = findTextNode(selection.focus, canvas);
    if (!focusInfo) return;
    domSel.setBaseAndExtent(
      anchorInfo.node,
      anchorInfo.offset,
      focusInfo.node,
      focusInfo.offset,
    );
  }
}

/**
 * Find the DOM text node for a model position.
 * Returns the text node and adjusted offset within it.
 */
function findTextNode(
  pos: Position,
  canvas: HTMLElement,
): { node: Node; offset: number } | null {
  // Get the block element at blockIndex
  const blocks = getBlockElements(canvas);
  if (pos.blockIndex >= blocks.length) return null;
  const blockEl = blocks[pos.blockIndex];

  // Navigate into table cell when cell path is present
  let targetParaEl: HTMLElement;
  if (pos.cell) {
    if (blockEl.tagName.toLowerCase() !== 'table') return null;
    const allRows = getAllTableRows(blockEl);
    if (pos.cell.rowIndex >= allRows.length) return null;
    const trEl = allRows[pos.cell.rowIndex];
    const cells = Array.from(trEl.children) as HTMLElement[];
    if (pos.cell.cellIndex >= cells.length) return null;
    const tdEl = cells[pos.cell.cellIndex];
    const cellBlocks = Array.from(tdEl.children) as HTMLElement[];
    if (pos.cell.cellBlockIndex >= cellBlocks.length) return null;
    targetParaEl = cellBlocks[pos.cell.cellBlockIndex];
  } else {
    targetParaEl = blockEl;
  }

  // Get span elements within the paragraph (direct children with data-node-id)
  const inlines = getInlineElements(targetParaEl);
  if (pos.inlineIndex >= inlines.length) return null;
  const inlineEl = inlines[pos.inlineIndex];

  // Find the text node within the inline element
  const textNode = findFirstTextNode(inlineEl);
  if (!textNode) {
    // No text node — place cursor at the element itself
    return { node: inlineEl, offset: 0 };
  }

  // Clamp offset to the text length (accounting for zero-width space)
  const textLen = textNode.textContent === '\u200B' ? 0 : (textNode.textContent?.length ?? 0);
  const offset = Math.min(pos.offset, textLen);

  // For zero-width space nodes, place cursor at offset 1 (after the ZWS)
  if (textNode.textContent === '\u200B') {
    return { node: textNode, offset: 1 };
  }

  return { node: textNode, offset };
}

/**
 * Get all block-level elements from the canvas.
 * When sections are present, blocks are nested inside <section> elements.
 * Otherwise, blocks are direct children of canvas (fallback).
 */
function getBlockElements(canvas: HTMLElement): HTMLElement[] {
  const blocks: HTMLElement[] = [];
  for (const child of canvas.children) {
    const el = child as HTMLElement;
    const tag = el.tagName?.toLowerCase();
    if (tag === 'section') {
      // Collect block children from within the section
      for (const sectionChild of el.children) {
        const sTag = (sectionChild as HTMLElement).tagName?.toLowerCase();
        if (sTag === 'p' || sTag?.match(/^h[1-6]$/) || sTag === 'table') {
          blocks.push(sectionChild as HTMLElement);
        }
      }
    } else if (tag === 'p' || tag?.match(/^h[1-6]$/) || tag === 'table') {
      blocks.push(el);
    }
  }
  return blocks;
}

/**
 * Get inline elements within a block element.
 * These are the span/a elements with data-node-id that map to Run/Hyperlink.
 */
function getInlineElements(blockEl: HTMLElement): HTMLElement[] {
  const inlines: HTMLElement[] = [];
  for (const child of blockEl.children) {
    const el = child as HTMLElement;
    if (el.dataset?.nodeId) {
      inlines.push(el);
    }
  }
  return inlines;
}

function findFirstTextNode(el: HTMLElement): Text | null {
  for (const child of el.childNodes) {
    if (child.nodeType === Node.TEXT_NODE) return child as Text;
    if (child.nodeType === Node.ELEMENT_NODE) {
      const found = findFirstTextNode(child as HTMLElement);
      if (found) return found;
    }
  }
  return null;
}

function getBlockIndex(blockEl: HTMLElement, canvas: HTMLElement): number {
  // Walk up to find the block element that lives inside a section or directly in canvas
  let target = blockEl;
  while (target.parentElement && target.parentElement !== canvas) {
    // Stop if parent is a <section> that is a direct child of canvas
    if (
      target.parentElement.tagName?.toLowerCase() === 'section' &&
      target.parentElement.parentElement === canvas
    ) {
      break;
    }
    target = target.parentElement;
  }

  const blocks = getBlockElements(canvas);
  return blocks.indexOf(target);
}

function getInlineIndex(inlineEl: HTMLElement, blockEl: HTMLElement): number {
  const inlines = getInlineElements(blockEl);
  // Find the inline element or its parent <a> in the list
  for (let i = 0; i < inlines.length; i++) {
    if (inlines[i] === inlineEl || inlines[i].contains(inlineEl)) {
      return i;
    }
  }
  return 0;
}

/** Walk up from el to find the nearest <table> ancestor within canvas. */
function findTableAncestor(el: HTMLElement, canvas: HTMLElement): HTMLElement | null {
  let node: HTMLElement | null = el;
  while (node && node !== canvas) {
    if (node.tagName.toLowerCase() === 'table') return node;
    node = node.parentElement;
  }
  return null;
}

/** Collect all <tr> elements from a table, handling thead/tbody/tfoot wrappers. */
function getAllTableRows(tableEl: HTMLElement): HTMLElement[] {
  const rows: HTMLElement[] = [];
  for (const child of tableEl.children) {
    const tag = child.tagName.toLowerCase();
    if (tag === 'tr') {
      rows.push(child as HTMLElement);
    } else if (tag === 'thead' || tag === 'tbody' || tag === 'tfoot') {
      for (const row of child.children) {
        if (row.tagName.toLowerCase() === 'tr') rows.push(row as HTMLElement);
      }
    }
  }
  return rows;
}

/**
 * If el (a <p> or heading element) is inside a <td> within canvas, return the table cell info.
 * Returns null if el is not inside a table cell.
 */
function findTableCellAncestor(
  el: HTMLElement,
  canvas: HTMLElement,
): { tableEl: HTMLElement; rowIndex: number; cellIndex: number; cellBlockIndex: number } | null {
  let node: HTMLElement | null = el.parentElement;
  while (node && node !== canvas) {
    if (node.tagName.toLowerCase() === 'td' || node.tagName.toLowerCase() === 'th') {
      const tdEl = node;
      const trEl = tdEl.parentElement as HTMLElement;
      const tableEl = findTableAncestor(trEl, canvas);
      if (!tableEl) return null;

      const allRows = getAllTableRows(tableEl);
      const rowIndex = allRows.indexOf(trEl);
      if (rowIndex < 0) return null;

      const cellIndex = Array.from(trEl.children).indexOf(tdEl);
      if (cellIndex < 0) return null;

      // Find which direct child of td is el (or an ancestor of el)
      let blockInCell: HTMLElement = el;
      while (blockInCell.parentElement !== tdEl) {
        if (!blockInCell.parentElement || blockInCell.parentElement === canvas) return null;
        blockInCell = blockInCell.parentElement;
      }
      const cellBlockIndex = Array.from(tdEl.children).indexOf(blockInCell);
      if (cellBlockIndex < 0) return null;

      return { tableEl, rowIndex, cellIndex, cellBlockIndex };
    }
    node = node.parentElement;
  }
  return null;
}
