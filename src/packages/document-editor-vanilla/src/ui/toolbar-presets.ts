import {
  Undo2, Redo2,
  Bold, Italic, Underline, Strikethrough,
  AlignLeft, AlignCenter, AlignRight, AlignJustify,
  List, ListOrdered,
  Table, Link, Image,
  Grid2x2, Grid3x3, PaintBucket, Eraser,
  Scissors, RectangleVertical, RectangleHorizontal, Columns2,
  Download, Upload, Pilcrow,
} from 'lucide';
import type { LucideIconDef, ToolbarPreset } from './toolbar-config';

// Cast helper: lucide icons are readonly tuples; LucideIconDef is the mutable equivalent
const ic = (icon: unknown): LucideIconDef => icon as LucideIconDef;

export const WORD_PRESET: ToolbarPreset = {
  id: 'word',
  name: 'Word',
  description: 'Microsoft Word-style toolbar with two rows',
  theme: 'word',
  rows: [
    // Row 1: main editing tools
    [
      {
        id: 'history',
        items: [
          {
            id: 'undo', type: 'button', icon: ic(Undo2),
            tooltip: 'Undo', shortcut: 'Ctrl+Z', action: 'undo',
          },
          {
            id: 'redo', type: 'button', icon: ic(Redo2),
            tooltip: 'Redo', shortcut: 'Ctrl+Y', action: 'redo',
          },
        ],
      },
      {
        id: 'fonts',
        items: [
          {
            id: 'fontFamily', type: 'combobox',
            tooltip: 'Font Family',
            options: [
              'Calibri', 'Arial', 'Arimo', 'Times New Roman', 'Tinos',
              'Georgia', 'Verdana', 'Trebuchet MS', 'Courier New', 'Cousine',
              'Comic Sans MS', 'Impact', 'Palatino Linotype', 'Tahoma',
              'Century Gothic', 'Roboto', 'Open Sans', 'Lato',
            ].map(f => ({ value: f, label: f })),
            getValue: (fs) => fs.fontFamily ?? '',
            action: 'fontFamily',
            width: '150px',
            placeholder: 'Font',
          },
          {
            id: 'fontSize', type: 'combobox',
            tooltip: 'Font Size',
            options: [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 32, 36, 40, 44, 48, 54, 60, 72]
              .map(n => ({ value: String(n), label: String(n) })),
            getValue: (fs) => fs.fontSize != null ? String(Math.round(fs.fontSize)) : '',
            action: 'fontSize',
            width: '55px',
            placeholder: 'Size',
          },
        ],
      },
      {
        id: 'style',
        items: [
          {
            id: 'paragraphStyle', type: 'select',
            tooltip: 'Paragraph style',
            options: [
              { value: 'Normal', label: 'Normal' },
              { value: 'Heading1', label: 'Heading 1' },
              { value: 'Heading2', label: 'Heading 2' },
              { value: 'Heading3', label: 'Heading 3' },
              { value: 'Heading4', label: 'Heading 4' },
            ],
            getValue: (fs) => fs.paragraphStyle ?? 'Normal',
            action: 'paragraphStyle',
            width: '120px',
          },
        ],
      },
      {
        id: 'format',
        items: [
          {
            id: 'bold', type: 'toggle', icon: ic(Bold),
            tooltip: 'Bold', shortcut: 'Ctrl+B', action: 'bold',
            isActive: (fs) => fs.bold,
          },
          {
            id: 'italic', type: 'toggle', icon: ic(Italic),
            tooltip: 'Italic', shortcut: 'Ctrl+I', action: 'italic',
            isActive: (fs) => fs.italic,
          },
          {
            id: 'underline', type: 'toggle', icon: ic(Underline),
            tooltip: 'Underline', shortcut: 'Ctrl+U', action: 'underline',
            isActive: (fs) => fs.underline,
          },
          {
            id: 'strikethrough', type: 'toggle', icon: ic(Strikethrough),
            tooltip: 'Strikethrough', action: 'strikethrough',
            isActive: (fs) => fs.strikethrough,
          },
        ],
      },
      {
        id: 'align',
        items: [
          {
            id: 'alignLeft', type: 'toggle', icon: ic(AlignLeft),
            tooltip: 'Align Left', action: 'alignLeft',
            isActive: (fs) => fs.alignment === 'left' || !fs.alignment,
          },
          {
            id: 'alignCenter', type: 'toggle', icon: ic(AlignCenter),
            tooltip: 'Center', action: 'alignCenter',
            isActive: (fs) => fs.alignment === 'center',
          },
          {
            id: 'alignRight', type: 'toggle', icon: ic(AlignRight),
            tooltip: 'Align Right', action: 'alignRight',
            isActive: (fs) => fs.alignment === 'right',
          },
          {
            id: 'alignJustify', type: 'toggle', icon: ic(AlignJustify),
            tooltip: 'Justify', action: 'alignJustify',
            isActive: (fs) => fs.alignment === 'both',
          },
        ],
      },
      {
        id: 'lists',
        items: [
          {
            id: 'bullet', type: 'toggle', icon: ic(List),
            tooltip: 'Bullet List', action: 'bullet',
            isActive: (fs) => fs.listType === 'bullet',
          },
          {
            id: 'numbered', type: 'toggle', icon: ic(ListOrdered),
            tooltip: 'Numbered List', action: 'numbered',
            isActive: (fs) => fs.listType === 'numbered',
          },
        ],
      },
      {
        id: 'insert',
        items: [
          {
            id: 'insertTable', type: 'button', icon: ic(Table),
            tooltip: 'Insert Table', action: 'insertTable',
          },
          {
            id: 'insertLink', type: 'button', icon: ic(Link),
            tooltip: 'Insert Hyperlink', action: 'insertLink',
          },
          {
            id: 'insertImage', type: 'button', icon: ic(Image),
            tooltip: 'Insert Image', action: 'insertImage',
          },
        ],
      },
      {
        id: 'file',
        items: [
          {
            id: 'exportDocx', type: 'button', icon: ic(Download),
            tooltip: 'Export .docx', action: 'exportDocx',
          },
          {
            id: 'importDocx', type: 'button', icon: ic(Upload),
            tooltip: 'Import .docx', action: 'importDocx',
          },
        ],
      },
    ],
    // Row 2: cell tools + page layout
    [
      {
        id: 'cellTools',
        items: [
          {
            id: 'cellBorders', type: 'dropdown', icon: ic(Grid2x2),
            tooltip: 'Cell Borders',
            options: [
              { label: 'No Borders',  value: 'none'   },
              { label: 'All Thin',    value: 'thin'   },
              { label: 'All Medium',  value: 'medium' },
              { label: 'All Thick',   value: 'thick'  },
            ],
            action: 'cellBorders',
          },
          {
            id: 'cellBackground', type: 'button', icon: ic(PaintBucket),
            tooltip: 'Cell Background Color', action: 'cellBackground',
          },
          {
            id: 'removeCellBackground', type: 'button', icon: ic(Eraser),
            tooltip: 'Remove Cell Background', action: 'removeCellBackground',
          },
        ],
      },
      {
        id: 'pageLayout',
        items: [
          {
            id: 'sectionBreak', type: 'dropdown', icon: ic(Scissors),
            tooltip: 'Insert Section Break',
            options: [
              { label: 'Next Page',  value: 'nextPage'   },
              { label: 'Continuous', value: 'continuous' },
              { label: 'Even Page',  value: 'evenPage'   },
              { label: 'Odd Page',   value: 'oddPage'    },
            ],
            action: 'sectionBreak',
          },
          {
            id: 'portrait', type: 'button', icon: ic(RectangleVertical),
            tooltip: 'Portrait Orientation', action: 'portrait',
          },
          {
            id: 'landscape', type: 'button', icon: ic(RectangleHorizontal),
            tooltip: 'Landscape Orientation', action: 'landscape',
          },
          {
            id: 'columns', type: 'dropdown', icon: ic(Columns2),
            tooltip: 'Columns',
            options: [
              { label: '1 Column',  value: '1' },
              { label: '2 Columns', value: '2' },
              { label: '3 Columns', value: '3' },
            ],
            action: 'columns',
          },
        ],
      },
      {
        id: 'view',
        items: [
          {
            id: 'toggleGridLines', type: 'toggle', icon: ic(Grid3x3),
            tooltip: 'Grid Lines', action: 'toggleGridLines',
            isActive: (_fs) => !!document.querySelector('.editor-canvas')?.classList.contains('show-grid'),
          },
          {
            id: 'togglePilcrow', type: 'toggle', icon: ic(Pilcrow),
            tooltip: 'Show/Hide Paragraph Marks', action: 'togglePilcrow',
            isActive: (_fs) => !!document.querySelector('.editor-canvas')?.classList.contains('show-pilcrow'),
          },
        ],
      },
    ],
  ],
};

export const GDOCS_PRESET: ToolbarPreset = {
  id: 'gdocs',
  name: 'Google Docs',
  description: 'Google Docs-style single-row toolbar',
  theme: 'gdocs',
  rows: [
    [
      {
        id: 'file',
        items: [
          {
            id: 'exportDocx', type: 'button', icon: ic(Download),
            tooltip: 'Export .docx', action: 'exportDocx',
          },
          {
            id: 'importDocx', type: 'button', icon: ic(Upload),
            tooltip: 'Import .docx', action: 'importDocx',
          },
        ],
      },
      {
        id: 'history',
        items: [
          {
            id: 'undo', type: 'button', icon: ic(Undo2),
            tooltip: 'Undo', shortcut: 'Ctrl+Z', action: 'undo',
          },
          {
            id: 'redo', type: 'button', icon: ic(Redo2),
            tooltip: 'Redo', shortcut: 'Ctrl+Y', action: 'redo',
          },
        ],
      },
      {
        id: 'fonts',
        items: [
          {
            id: 'fontFamily', type: 'combobox',
            tooltip: 'Font Family',
            options: [
              'Calibri', 'Arial', 'Arimo', 'Times New Roman', 'Tinos',
              'Georgia', 'Verdana', 'Trebuchet MS', 'Courier New', 'Cousine',
              'Comic Sans MS', 'Impact', 'Palatino Linotype', 'Tahoma',
              'Century Gothic', 'Roboto', 'Open Sans', 'Lato',
            ].map(f => ({ value: f, label: f })),
            getValue: (fs) => fs.fontFamily ?? '',
            action: 'fontFamily',
            width: '150px',
            placeholder: 'Font',
          },
          {
            id: 'fontSize', type: 'combobox',
            tooltip: 'Font Size',
            options: [8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 32, 36, 40, 44, 48, 54, 60, 72]
              .map(n => ({ value: String(n), label: String(n) })),
            getValue: (fs) => fs.fontSize != null ? String(Math.round(fs.fontSize)) : '',
            action: 'fontSize',
            width: '55px',
            placeholder: 'Size',
          },
        ],
      },
      {
        id: 'style',
        items: [
          {
            id: 'paragraphStyle', type: 'select',
            tooltip: 'Paragraph style',
            options: [
              { value: 'Normal', label: 'Normal' },
              { value: 'Heading1', label: 'Heading 1' },
              { value: 'Heading2', label: 'Heading 2' },
              { value: 'Heading3', label: 'Heading 3' },
              { value: 'Heading4', label: 'Heading 4' },
            ],
            getValue: (fs) => fs.paragraphStyle ?? 'Normal',
            action: 'paragraphStyle',
            width: '110px',
          },
        ],
      },
      {
        id: 'format',
        items: [
          {
            id: 'bold', type: 'toggle', icon: ic(Bold),
            tooltip: 'Bold', shortcut: 'Ctrl+B', action: 'bold',
            isActive: (fs) => fs.bold,
          },
          {
            id: 'italic', type: 'toggle', icon: ic(Italic),
            tooltip: 'Italic', shortcut: 'Ctrl+I', action: 'italic',
            isActive: (fs) => fs.italic,
          },
          {
            id: 'underline', type: 'toggle', icon: ic(Underline),
            tooltip: 'Underline', shortcut: 'Ctrl+U', action: 'underline',
            isActive: (fs) => fs.underline,
          },
          {
            id: 'strikethrough', type: 'toggle', icon: ic(Strikethrough),
            tooltip: 'Strikethrough', action: 'strikethrough',
            isActive: (fs) => fs.strikethrough,
          },
        ],
      },
      {
        id: 'align',
        items: [
          {
            id: 'alignLeft', type: 'toggle', icon: ic(AlignLeft),
            tooltip: 'Align Left', action: 'alignLeft',
            isActive: (fs) => fs.alignment === 'left' || !fs.alignment,
          },
          {
            id: 'alignCenter', type: 'toggle', icon: ic(AlignCenter),
            tooltip: 'Center', action: 'alignCenter',
            isActive: (fs) => fs.alignment === 'center',
          },
          {
            id: 'alignRight', type: 'toggle', icon: ic(AlignRight),
            tooltip: 'Align Right', action: 'alignRight',
            isActive: (fs) => fs.alignment === 'right',
          },
          {
            id: 'alignJustify', type: 'toggle', icon: ic(AlignJustify),
            tooltip: 'Justify', action: 'alignJustify',
            isActive: (fs) => fs.alignment === 'both',
          },
        ],
      },
      {
        id: 'lists',
        items: [
          {
            id: 'bullet', type: 'toggle', icon: ic(List),
            tooltip: 'Bullet List', action: 'bullet',
            isActive: (fs) => fs.listType === 'bullet',
          },
          {
            id: 'numbered', type: 'toggle', icon: ic(ListOrdered),
            tooltip: 'Numbered List', action: 'numbered',
            isActive: (fs) => fs.listType === 'numbered',
          },
        ],
      },
      {
        id: 'insert',
        items: [
          {
            id: 'insertTable', type: 'button', icon: ic(Table),
            tooltip: 'Insert Table', action: 'insertTable',
          },
          {
            id: 'insertLink', type: 'button', icon: ic(Link),
            tooltip: 'Insert Hyperlink', action: 'insertLink',
          },
          {
            id: 'insertImage', type: 'button', icon: ic(Image),
            tooltip: 'Insert Image', action: 'insertImage',
          },
        ],
      },
      {
        id: 'cellTools',
        items: [
          {
            id: 'cellBorders', type: 'dropdown', icon: ic(Grid2x2),
            tooltip: 'Cell Borders',
            options: [
              { label: 'No Borders',  value: 'none'   },
              { label: 'All Thin',    value: 'thin'   },
              { label: 'All Medium',  value: 'medium' },
              { label: 'All Thick',   value: 'thick'  },
            ],
            action: 'cellBorders',
          },
          {
            id: 'cellBackground', type: 'button', icon: ic(PaintBucket),
            tooltip: 'Cell Background Color', action: 'cellBackground',
          },
        ],
      },
      {
        id: 'pageLayout',
        items: [
          {
            id: 'sectionBreak', type: 'dropdown', icon: ic(Scissors),
            tooltip: 'Insert Section Break',
            options: [
              { label: 'Next Page',  value: 'nextPage'   },
              { label: 'Continuous', value: 'continuous' },
              { label: 'Even Page',  value: 'evenPage'   },
              { label: 'Odd Page',   value: 'oddPage'    },
            ],
            action: 'sectionBreak',
          },
          {
            id: 'portrait', type: 'button', icon: ic(RectangleVertical),
            tooltip: 'Portrait Orientation', action: 'portrait',
          },
          {
            id: 'landscape', type: 'button', icon: ic(RectangleHorizontal),
            tooltip: 'Landscape Orientation', action: 'landscape',
          },
        ],
      },
      {
        id: 'view',
        items: [
          {
            id: 'toggleGridLines', type: 'toggle', icon: ic(Grid3x3),
            tooltip: 'Grid Lines', action: 'toggleGridLines',
            isActive: (_fs) => !!document.querySelector('.editor-canvas')?.classList.contains('show-grid'),
          },
          {
            id: 'togglePilcrow', type: 'toggle', icon: ic(Pilcrow),
            tooltip: 'Show/Hide Paragraph Marks', action: 'togglePilcrow',
            isActive: (_fs) => !!document.querySelector('.editor-canvas')?.classList.contains('show-pilcrow'),
          },
        ],
      },
    ],
  ],
};

export const COMPACT_PRESET: ToolbarPreset = {
  id: 'compact',
  name: 'Compact',
  description: 'Minimal single-row toolbar',
  theme: 'compact',
  rows: [
    [
      {
        id: 'history',
        items: [
          {
            id: 'undo', type: 'button', icon: ic(Undo2),
            tooltip: 'Undo', shortcut: 'Ctrl+Z', action: 'undo',
          },
          {
            id: 'redo', type: 'button', icon: ic(Redo2),
            tooltip: 'Redo', shortcut: 'Ctrl+Y', action: 'redo',
          },
        ],
      },
      {
        id: 'format',
        items: [
          {
            id: 'bold', type: 'toggle', icon: ic(Bold),
            tooltip: 'Bold', shortcut: 'Ctrl+B', action: 'bold',
            isActive: (fs) => fs.bold,
          },
          {
            id: 'italic', type: 'toggle', icon: ic(Italic),
            tooltip: 'Italic', shortcut: 'Ctrl+I', action: 'italic',
            isActive: (fs) => fs.italic,
          },
          {
            id: 'underline', type: 'toggle', icon: ic(Underline),
            tooltip: 'Underline', shortcut: 'Ctrl+U', action: 'underline',
            isActive: (fs) => fs.underline,
          },
        ],
      },
      {
        id: 'align',
        items: [
          {
            id: 'alignLeft', type: 'toggle', icon: ic(AlignLeft),
            tooltip: 'Align Left', action: 'alignLeft',
            isActive: (fs) => fs.alignment === 'left' || !fs.alignment,
          },
          {
            id: 'alignCenter', type: 'toggle', icon: ic(AlignCenter),
            tooltip: 'Center', action: 'alignCenter',
            isActive: (fs) => fs.alignment === 'center',
          },
          {
            id: 'alignRight', type: 'toggle', icon: ic(AlignRight),
            tooltip: 'Align Right', action: 'alignRight',
            isActive: (fs) => fs.alignment === 'right',
          },
        ],
      },
      {
        id: 'lists',
        items: [
          {
            id: 'bullet', type: 'toggle', icon: ic(List),
            tooltip: 'Bullet List', action: 'bullet',
            isActive: (fs) => fs.listType === 'bullet',
          },
          {
            id: 'numbered', type: 'toggle', icon: ic(ListOrdered),
            tooltip: 'Numbered List', action: 'numbered',
            isActive: (fs) => fs.listType === 'numbered',
          },
        ],
      },
      {
        id: 'insert',
        items: [
          {
            id: 'insertImage', type: 'button', icon: ic(Image),
            tooltip: 'Insert Image', action: 'insertImage',
          },
        ],
      },
    ],
  ],
};
