import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
import { domToModelSelection } from '../renderer/cursor-manager';
import { setupCaretColorSync } from '../renderer/caret-color';

export type RenderCallback = (response: EngineResponse) => void;

/**
 * Intercepts all user input via the beforeinput event.
 * ALWAYS prevents default — the C# engine is the single source of truth.
 */
export class InputHandler {
  private engine: EngineBridge;
  private canvas: HTMLElement;
  private onResponse: RenderCallback;
  private processing = false;

  constructor(engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback) {
    this.engine = engine;
    this.canvas = canvas;
    this.onResponse = onResponse;

    this.canvas.addEventListener('beforeinput', this.handleBeforeInput);
    this.canvas.addEventListener('compositionend', this.handleCompositionEnd);
    setupCaretColorSync(canvas);
  }

  destroy(): void {
    this.canvas.removeEventListener('beforeinput', this.handleBeforeInput);
    this.canvas.removeEventListener('compositionend', this.handleCompositionEnd);
  }

  private handleBeforeInput = async (e: InputEvent): Promise<void> => {
    // During IME composition, allow the browser to handle it
    if (e.isComposing) return;

    e.preventDefault();
    if (this.processing) return;

    const sel = domToModelSelection(this.canvas);
    if (!sel) return;

    this.processing = true;
    let response: EngineResponse | null = null;

    try {
      switch (e.inputType) {
        case 'insertText':
          if (e.data) {
            response = await this.engine.insertText(e.data, sel);
          }
          break;

        case 'insertParagraph':
          response = await this.engine.splitParagraph(sel);
          break;

        case 'insertLineBreak':
          response = await this.engine.insertBreak('textwrapping', sel);
          break;

        case 'deleteContentBackward':
        case 'deleteSoftLineBackward':
        case 'deleteWordBackward':
          if (sel.anchor.blockIndex === sel.focus.blockIndex
            && sel.anchor.inlineIndex === sel.focus.inlineIndex
            && sel.anchor.offset === sel.focus.offset) {
            response = await this.engine.deleteBackward(sel);
          } else {
            response = await this.engine.deleteSelection(sel);
          }
          break;

        case 'deleteContentForward':
        case 'deleteSoftLineForward':
        case 'deleteWordForward':
          if (sel.anchor.blockIndex === sel.focus.blockIndex
            && sel.anchor.inlineIndex === sel.focus.inlineIndex
            && sel.anchor.offset === sel.focus.offset) {
            response = await this.engine.deleteForward(sel);
          } else {
            response = await this.engine.deleteSelection(sel);
          }
          break;

        case 'insertFromPaste': {
          const text = e.dataTransfer?.getData('text/plain');
          if (text) {
            response = await this.engine.pasteText(text, sel);
          }
          break;
        }

        case 'insertFromDrop': {
          const dropText = e.dataTransfer?.getData('text/plain');
          if (dropText) {
            response = await this.engine.pasteText(dropText, sel);
          }
          break;
        }

        case 'formatBold':
          response = await this.engine.toggleFormat('bold', sel);
          break;

        case 'formatItalic':
          response = await this.engine.toggleFormat('italic', sel);
          break;

        case 'formatUnderline':
          response = await this.engine.toggleFormat('underline', sel);
          break;

        case 'formatStrikeThrough':
          response = await this.engine.toggleFormat('strikethrough', sel);
          break;

        case 'historyUndo':
          response = await this.engine.undo();
          break;

        case 'historyRedo':
          response = await this.engine.redo();
          break;
      }
    } finally {
      this.processing = false;
    }

    if (response) {
      this.onResponse(response);
    }
  };

  private handleCompositionEnd = async (e: CompositionEvent): Promise<void> => {
    // After IME composition ends, insert the composed text
    const text = e.data;
    if (!text) return;

    const sel = domToModelSelection(this.canvas);
    if (!sel) return;

    const response = await this.engine.insertText(text, sel);
    this.onResponse(response);
  };
}
