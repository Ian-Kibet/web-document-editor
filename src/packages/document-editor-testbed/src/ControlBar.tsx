import { useRef, type MutableRefObject } from 'react';
import type { EngineBridge } from 'document-editor-vanilla';
import type { Mode, Preset } from './App';

interface ControlBarProps {
  mode: Mode;
  preset: Preset;
  storagePrefix: string;
  engineRef: MutableRefObject<EngineBridge | null>;
  onModeChange: (mode: Mode) => void;
  onPresetChange: (preset: Preset) => void;
  onStoragePrefixChange: (prefix: string) => void;
}

export function ControlBar({
  mode,
  preset,
  storagePrefix,
  engineRef,
  onModeChange,
  onPresetChange,
  onStoragePrefixChange,
}: ControlBarProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const prefixInputRef = useRef<HTMLInputElement>(null);

  async function handleExport() {
    const engine = engineRef.current;
    if (!engine) return;
    try {
      const bytes = await engine.exportDocx();
      const blob = new Blob([bytes.buffer as ArrayBuffer], { type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = 'document.docx';
      a.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Export failed:', err);
    }
  }

  function handleImportClick() {
    fileInputRef.current?.click();
  }

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file || !engineRef.current) return;
    try {
      const buffer = await file.arrayBuffer();
      await engineRef.current.importDocx(new Uint8Array(buffer));
    } catch (err) {
      console.error('Import failed:', err);
    }
    // Reset so the same file can be re-imported
    e.target.value = '';
  }

  function handlePrefixApply() {
    const val = prefixInputRef.current?.value.trim();
    if (val && val !== storagePrefix) {
      onStoragePrefixChange(val);
    }
  }

  function handlePrefixKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Enter') handlePrefixApply();
  }

  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      gap: '12px',
      padding: '6px 12px',
      background: '#1e1e2e',
      color: '#cdd6f4',
      fontSize: '13px',
      flexShrink: 0,
      flexWrap: 'wrap',
      borderBottom: '1px solid #313244',
    }}>
      {/* Mode toggle */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
        <span style={{ marginRight: '4px', color: '#a6adc8' }}>Mode:</span>
        {(['vanilla', 'react'] as Mode[]).map(m => (
          <button
            key={m}
            onClick={() => onModeChange(m)}
            style={{
              padding: '3px 10px',
              borderRadius: '4px',
              border: 'none',
              cursor: 'pointer',
              fontSize: '12px',
              fontWeight: mode === m ? 600 : 400,
              background: mode === m ? '#89b4fa' : '#313244',
              color: mode === m ? '#1e1e2e' : '#cdd6f4',
            }}
          >
            {m === 'vanilla' ? 'Vanilla' : 'React'}
          </button>
        ))}
      </div>

      <div style={{ width: '1px', height: '20px', background: '#45475a' }} />

      {/* Preset select */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
        <label style={{ color: '#a6adc8' }}>Preset:</label>
        <select
          value={preset}
          onChange={e => onPresetChange(e.target.value as Preset)}
          style={{
            background: '#313244',
            color: '#cdd6f4',
            border: '1px solid #45475a',
            borderRadius: '4px',
            padding: '2px 6px',
            fontSize: '12px',
          }}
        >
          <option value="word">word</option>
          <option value="gdocs">gdocs</option>
          <option value="compact">compact</option>
        </select>
      </div>

      <div style={{ width: '1px', height: '20px', background: '#45475a' }} />

      {/* Storage prefix */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
        <label style={{ color: '#a6adc8' }}>Prefix:</label>
        <input
          ref={prefixInputRef}
          defaultValue={storagePrefix}
          onKeyDown={handlePrefixKeyDown}
          placeholder="documentEditor"
          style={{
            background: '#313244',
            color: '#cdd6f4',
            border: '1px solid #45475a',
            borderRadius: '4px',
            padding: '2px 6px',
            fontSize: '12px',
            width: '130px',
          }}
        />
        <button
          onClick={handlePrefixApply}
          style={{
            padding: '2px 8px',
            borderRadius: '4px',
            border: 'none',
            cursor: 'pointer',
            fontSize: '12px',
            background: '#313244',
            color: '#cdd6f4',
          }}
        >
          Apply
        </button>
      </div>

      <div style={{ width: '1px', height: '20px', background: '#45475a' }} />

      {/* Export / Import */}
      <button
        onClick={handleExport}
        style={{
          padding: '3px 10px',
          borderRadius: '4px',
          border: 'none',
          cursor: 'pointer',
          fontSize: '12px',
          background: '#a6e3a1',
          color: '#1e1e2e',
          fontWeight: 500,
        }}
      >
        Export .docx
      </button>
      <button
        onClick={handleImportClick}
        style={{
          padding: '3px 10px',
          borderRadius: '4px',
          border: 'none',
          cursor: 'pointer',
          fontSize: '12px',
          background: '#89dceb',
          color: '#1e1e2e',
          fontWeight: 500,
        }}
      >
        Import .docx
      </button>
      <input
        ref={fileInputRef}
        type="file"
        accept=".docx"
        style={{ display: 'none' }}
        onChange={handleFileChange}
      />

      {/* Current state indicator */}
      <div style={{ marginLeft: 'auto', color: '#585b70', fontSize: '11px' }}>
        {mode} / {preset} / {storagePrefix}
      </div>
    </div>
  );
}
