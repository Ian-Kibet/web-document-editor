import { useRef, useState } from 'react';
import type { EngineBridge } from 'document-editor-vanilla';
import { ControlBar } from './ControlBar';
import { VanillaMode } from './VanillaMode';
import { ReactMode } from './ReactMode';

export type Mode = 'vanilla' | 'react';
export type Preset = 'word' | 'gdocs' | 'compact';

export function App() {
  const [editorKey, setEditorKey] = useState(0);
  const [mode, setMode] = useState<Mode>('vanilla');
  const [preset, setPreset] = useState<Preset>('word');
  const [storagePrefix, setStoragePrefix] = useState('documentEditor');

  const engineRef = useRef<EngineBridge | null>(null);

  function remount(updates: Partial<{ mode: Mode; preset: Preset; storagePrefix: string }>) {
    if (updates.mode !== undefined) setMode(updates.mode);
    if (updates.preset !== undefined) setPreset(updates.preset);
    if (updates.storagePrefix !== undefined) setStoragePrefix(updates.storagePrefix);
    setEditorKey(k => k + 1);
  }

  const handleEngine = (engine: EngineBridge | null) => {
    engineRef.current = engine;
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100vh', overflow: 'hidden' }}>
      <ControlBar
        mode={mode}
        preset={preset}
        storagePrefix={storagePrefix}
        engineRef={engineRef}
        onModeChange={m => remount({ mode: m })}
        onPresetChange={p => remount({ preset: p })}
        onStoragePrefixChange={s => remount({ storagePrefix: s })}
      />
      <div style={{ flex: 1, overflow: 'hidden' }}>
        {mode === 'vanilla' ? (
          <VanillaMode
            key={editorKey}
            preset={preset}
            storagePrefix={storagePrefix}
            onEngine={handleEngine}
          />
        ) : (
          <ReactMode
            key={editorKey}
            preset={preset}
            storagePrefix={storagePrefix}
            onEngine={handleEngine}
          />
        )}
      </div>
    </div>
  );
}
