import type { RenderNode } from '../bridge/types';

/**
 * Status bar at the bottom of the editor showing page info, word count, and zoom.
 * Styled with Tailwind CSS.
 */
export class StatusBar {
  private el: HTMLElement;
  private pageInfo: HTMLElement;
  private wordCount: HTMLElement;
  private zoomLabel: HTMLElement;
  private currentPage = 1;
  private totalPages = 1;
  private words = 0;

  constructor(container: HTMLElement) {
    this.el = document.createElement('div');
    this.el.className =
      'flex items-center justify-between px-4 py-1.5 text-xs text-gray-500 bg-gray-50 border-t border-gray-200 select-none flex-shrink-0';

    // Left section: editing mode + page info + word count
    const leftSection = document.createElement('div');
    leftSection.className = 'flex items-center gap-3';

    const editMode = document.createElement('span');
    editMode.className = 'text-blue-600 font-medium';
    editMode.textContent = 'Editing';

    this.pageInfo = document.createElement('span');
    this.pageInfo.className = 'text-gray-500';

    this.wordCount = document.createElement('span');
    this.wordCount.className = 'text-gray-500';

    leftSection.append(editMode, this.pageInfo, this.wordCount);

    // Right section: zoom controls
    const rightSection = document.createElement('div');
    rightSection.className = 'flex items-center gap-1';

    const zoomOut = document.createElement('button');
    zoomOut.type = 'button';
    zoomOut.textContent = '−';
    zoomOut.className =
      'w-5 h-5 flex items-center justify-center rounded hover:bg-gray-200 text-gray-600 text-sm leading-none transition-colors';
    zoomOut.title = 'Zoom out';

    this.zoomLabel = document.createElement('span');
    this.zoomLabel.className = 'w-10 text-center text-gray-600 tabular-nums';
    this.zoomLabel.textContent = '100%';

    const zoomIn = document.createElement('button');
    zoomIn.type = 'button';
    zoomIn.textContent = '+';
    zoomIn.className =
      'w-5 h-5 flex items-center justify-center rounded hover:bg-gray-200 text-gray-600 text-sm leading-none transition-colors';
    zoomIn.title = 'Zoom in';

    // Zoom button handlers — dispatch synthetic events for ZoomController
    zoomOut.addEventListener('click', () => {
      window.dispatchEvent(new CustomEvent('doc:zoom-out'));
    });
    zoomIn.addEventListener('click', () => {
      window.dispatchEvent(new CustomEvent('doc:zoom-in'));
    });

    rightSection.append(zoomOut, this.zoomLabel, zoomIn);

    this.el.append(leftSection, rightSection);
    container.appendChild(this.el);

    this.refreshLeft();
  }

  getElement(): HTMLElement {
    return this.el;
  }

  updatePageInfo(currentPage: number, totalPages: number): void {
    this.currentPage = currentPage;
    this.totalPages = totalPages;
    this.refreshLeft();
  }

  updateWordCount(renderTree: RenderNode[]): void {
    const text = renderTree.map(getTextContent).join(' ');
    this.words = text.trim() ? text.trim().split(/\s+/).length : 0;
    this.refreshLeft();
  }

  updateZoom(percent: number): void {
    this.zoomLabel.textContent = `${Math.round(percent)}%`;
  }

  private refreshLeft(): void {
    this.pageInfo.textContent = `Page ${this.currentPage} of ${this.totalPages}`;
    const wordLabel = `${this.words} word${this.words !== 1 ? 's' : ''}`;
    this.wordCount.textContent = wordLabel;
  }
}

function getTextContent(node: RenderNode): string {
  if (node.text !== undefined && node.text !== null) return node.text;
  return (node.children ?? []).map(getTextContent).join('');
}
