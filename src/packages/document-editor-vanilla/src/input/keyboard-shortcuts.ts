import type { EngineBridge } from '../bridge/engine-bridge';
import type { EngineResponse } from '../bridge/types';
import { domToModelSelection } from '../renderer/cursor-manager';

export type RenderCallback = (response: EngineResponse) => void;

interface ShortcutDef {
  key: string;
  ctrl?: boolean;
  shift?: boolean;
  handler: () => Promise<EngineResponse | null>;
}

/**
 * Keyboard shortcut handler.
 * Listens for keydown events and maps key combos to engine commands.
 */
export class KeyboardShortcuts {
  private engine: EngineBridge;
  private canvas: HTMLElement;
  private onResponse: RenderCallback;
  private shortcuts: ShortcutDef[];

  constructor(engine: EngineBridge, canvas: HTMLElement, onResponse: RenderCallback) {
    this.engine = engine;
    this.canvas = canvas;
    this.onResponse = onResponse;

    this.shortcuts = this.buildShortcuts();
    this.canvas.addEventListener('keydown', this.handleKeyDown);
  }

  destroy(): void {
    this.canvas.removeEventListener('keydown', this.handleKeyDown);
  }

  private buildShortcuts(): ShortcutDef[] {
    return [
      // Formatting
      { key: 'b', ctrl: true, handler: () => this.formatCmd('bold') },
      { key: 'i', ctrl: true, handler: () => this.formatCmd('italic') },
      { key: 'u', ctrl: true, handler: () => this.formatCmd('underline') },

      // History
      { key: 'z', ctrl: true, handler: () => this.engine.undo() },
      { key: 'z', ctrl: true, shift: true, handler: () => this.engine.redo() },
      { key: 'y', ctrl: true, handler: () => this.engine.redo() },

      // Alignment
      { key: 'l', ctrl: true, handler: () => this.alignCmd('left') },
      { key: 'e', ctrl: true, handler: () => this.alignCmd('center') },
      { key: 'r', ctrl: true, handler: () => this.alignCmd('right') },
      { key: 'j', ctrl: true, handler: () => this.alignCmd('both') },

      // Indent
      {
        key: 'Tab', handler: () => {
          const sel = domToModelSelection(this.canvas);
          if (!sel) return Promise.resolve(null);
          return this.engine.setIndent(720, 0, sel); // 0.5 inch = 720 twips
        },
      },
      {
        key: 'Tab', shift: true, handler: () => {
          const sel = domToModelSelection(this.canvas);
          if (!sel) return Promise.resolve(null);
          return this.engine.setIndent(-720, 0, sel);
        },
      },
    ];
  }

  private handleKeyDown = async (e: KeyboardEvent): Promise<void> => {
    // Use metaKey on Mac, ctrlKey on others
    const ctrlOrMeta = e.ctrlKey || e.metaKey;

    // Ctrl+Enter → insert inline page break
    if (ctrlOrMeta && !e.shiftKey && !e.altKey && e.key === 'Enter') {
      e.preventDefault();
      const sel = domToModelSelection(this.canvas);
      if (sel) {
        const response = await this.engine.insertBreak('page', sel);
        if (response) this.onResponse(response);
      }
      return;
    }

    for (const shortcut of this.shortcuts) {
      const ctrlMatch = shortcut.ctrl ? ctrlOrMeta : !ctrlOrMeta;
      const shiftMatch = shortcut.shift ? e.shiftKey : !e.shiftKey;
      const keyMatch = e.key.toLowerCase() === shortcut.key.toLowerCase();

      if (keyMatch && ctrlMatch && shiftMatch) {
        e.preventDefault();
        const response = await shortcut.handler();
        if (response) this.onResponse(response);
        return;
      }
    }
  };

  private async formatCmd(
    property: 'bold' | 'italic' | 'underline' | 'strikethrough',
  ): Promise<EngineResponse | null> {
    const sel = domToModelSelection(this.canvas);
    if (!sel) return null;
    return this.engine.toggleFormat(property, sel);
  }

  private async alignCmd(
    alignment: 'left' | 'center' | 'right' | 'both',
  ): Promise<EngineResponse | null> {
    const sel = domToModelSelection(this.canvas);
    if (!sel) return null;
    return this.engine.setAlignment(alignment, sel);
  }
}
