/**
 * Zoom controls for the editor canvas.
 */
export class ZoomController {
  private level = 100;
  private target: HTMLElement;
  private onZoomChange?: (percent: number) => void;

  private static readonly MIN = 50;
  private static readonly MAX = 200;
  private static readonly STEP = 10;

  constructor(target: HTMLElement, onChange?: (percent: number) => void) {
    this.target = target;
    this.onZoomChange = onChange;
    this.apply();

    // Ctrl+scroll to zoom
    target.addEventListener('wheel', (e) => {
      if (e.ctrlKey || e.metaKey) {
        e.preventDefault();
        this.setLevel(this.level + (e.deltaY < 0 ? ZoomController.STEP : -ZoomController.STEP));
      }
    }, { passive: false });
  }

  getLevel(): number {
    return this.level;
  }

  setLevel(percent: number): void {
    this.level = Math.max(ZoomController.MIN, Math.min(ZoomController.MAX, percent));
    this.apply();
    this.onZoomChange?.(this.level);
  }

  zoomIn(): void {
    this.setLevel(this.level + ZoomController.STEP);
  }

  zoomOut(): void {
    this.setLevel(this.level - ZoomController.STEP);
  }

  resetZoom(): void {
    this.setLevel(100);
  }

  private apply(): void {
    this.target.style.transform = `scale(${this.level / 100})`;
    this.target.style.transformOrigin = 'top center';
  }
}
