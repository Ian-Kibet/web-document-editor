import { useEffect, useRef } from 'react';
import type { EditorInstance, EngineBridge } from 'document-editor-vanilla';
import { mountEditor } from 'document-editor-vanilla';
import type { Preset } from './App';

interface VanillaModeProps {
  preset: Preset;
  storagePrefix: string;
  onEngine: (engine: EngineBridge | null) => void;
}

export function VanillaMode({ preset, storagePrefix, onEngine }: VanillaModeProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current) return;
    let instance: EditorInstance | null = null;

    mountEditor({
      container: containerRef.current,
      toolbarPreset: preset,
      storagePrefix,
    })
      .then(inst => {
        instance = inst;
        onEngine(inst.engine);
      })
      .catch(console.error);

    return () => {
      instance?.destroy();
      onEngine(null);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // mount once — key prop handles remounting on option changes

  return (
    <div
      ref={containerRef}
      style={{ width: '100%', height: '100%' }}
    />
  );
}
