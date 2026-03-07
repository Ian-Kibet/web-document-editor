import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
import { domToModelSelection } from '../renderer/cursor-manager';

export type RenderCallback = (response: EngineResponse) => void;

/**
 * Handles paste events with explicit clipboard access.
 * The beforeinput handler covers most paste cases via insertFromPaste,
 * but this is a fallback for browsers that fire paste before beforeinput.
 */
export class PasteHandler {
  private engine: EngineBridge;
  private canvas: HTMLElement;
  private onResponse: RenderCallback;

  constructor(engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback) {
    this.engine = engine;
    this.canvas = canvas;
    this.onResponse = onResponse;

    this.canvas.addEventListener('paste', this.handlePaste);
  }

  destroy(): void {
    this.canvas.removeEventListener('paste', this.handlePaste);
  }

  private handlePaste = async (e: ClipboardEvent): Promise<void> => {
    e.preventDefault();

    const text = e.clipboardData?.getData('text/plain');
    if (!text) return;

    const sel = domToModelSelection(this.canvas);
    if (!sel) return;

    const response = await this.engine.pasteText(text, sel);
    this.onResponse(response);
  };
}
