import { useState, useCallback, useRef, useEffect } from 'react';
import type {
  WorkflowDefinitionModel,
  WorkflowStepDefinition,
  WorkflowTransition,
  NodePosition,
} from '@/api/types/workflow';
import {
  useWorkflowDefinitions,
  useWorkflowDefinition,
  useCreateWorkflowDefinition,
  useUpdateWorkflowDefinition,
} from '@/api/hooks/useWorkflow';

const NODE_WIDTH = 180;
const NODE_HEIGHT = 70;

const STEP_COLORS: Record<string, string> = {
  action: '#4a90d9',
  approval: '#e6a817',
  end: '#d94a4a',
  parallel_gateway: '#7b4ad9',
  condition_gateway: '#4ad98a',
};

function emptyDefinition(): WorkflowDefinitionModel {
  return {
    name: 'Nowy workflow',
    version: 1,
    entityType: '',
    initialStep: 'start',
    steps: [
      { name: 'start', type: 'action', transitions: [] },
      { name: 'end', type: 'end', transitions: [] },
    ],
  };
}

function defaultPositions(steps: WorkflowStepDefinition[]): Record<string, NodePosition> {
  const pos: Record<string, NodePosition> = {};
  steps.forEach((s, i) => {
    pos[s.name] = { x: 100 + (i % 4) * 220, y: 100 + Math.floor(i / 4) * 120 };
  });
  return pos;
}

export function WorkflowBuilderPage() {
  const { data: definitions } = useWorkflowDefinitions();
  const [selectedDefId, setSelectedDefId] = useState<string | null>(null);
  const { data: selectedDef } = useWorkflowDefinition(selectedDefId);
  const createMut = useCreateWorkflowDefinition();
  const updateMut = useUpdateWorkflowDefinition();

  const [definition, setDefinition] = useState<WorkflowDefinitionModel>(emptyDefinition());
  const [positions, setPositions] = useState<Record<string, NodePosition>>({});
  const [dragging, setDragging] = useState<string | null>(null);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  const [selectedNode, setSelectedNode] = useState<string | null>(null);
  const [connecting, setConnecting] = useState<string | null>(null);
  const [defName, setDefName] = useState('');
  const [defDescription, setDefDescription] = useState('');
  const canvasRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (selectedDef) {
      try {
        const model = JSON.parse(selectedDef.definitionJson) as WorkflowDefinitionModel;
        setDefinition(model);
        setPositions(defaultPositions(model.steps));
        setDefName(selectedDef.name);
        setDefDescription(selectedDef.description ?? '');
      } catch {
        /* invalid JSON */
      }
    }
  }, [selectedDef]);

  const handleMouseDown = useCallback(
    (stepName: string, e: React.MouseEvent) => {
      if (connecting) {
        // Complete connection
        if (connecting !== stepName) {
          setDefinition((prev) => {
            const steps = prev.steps.map((s) =>
              s.name === connecting
                ? { ...s, transitions: [...s.transitions, { outcome: 'next', targetStep: stepName }] }
                : s,
            );
            return { ...prev, steps };
          });
        }
        setConnecting(null);
        return;
      }
      e.stopPropagation();
      const pos = positions[stepName] ?? { x: 0, y: 0 };
      setDragging(stepName);
      setDragOffset({ x: e.clientX - pos.x, y: e.clientY - pos.y });
      setSelectedNode(stepName);
    },
    [connecting, positions],
  );

  const handleMouseMove = useCallback(
    (e: React.MouseEvent) => {
      if (!dragging) return;
      setPositions((prev) => ({
        ...prev,
        [dragging]: { x: e.clientX - dragOffset.x, y: e.clientY - dragOffset.y },
      }));
    },
    [dragging, dragOffset],
  );

  const handleMouseUp = useCallback(() => setDragging(null), []);

  const addStep = useCallback(
    (type: WorkflowStepDefinition['type']) => {
      const name = `${type}_${Date.now()}`;
      setDefinition((prev) => ({
        ...prev,
        steps: [...prev.steps, { name, type, transitions: [] }],
      }));
      setPositions((prev) => ({
        ...prev,
        [name]: { x: 200 + Math.random() * 300, y: 200 + Math.random() * 200 },
      }));
      setSelectedNode(name);
    },
    [],
  );

  const removeStep = useCallback(
    (stepName: string) => {
      setDefinition((prev) => ({
        ...prev,
        steps: prev.steps
          .filter((s) => s.name !== stepName)
          .map((s) => ({
            ...s,
            transitions: s.transitions.filter((t) => t.targetStep !== stepName),
          })),
        initialStep: prev.initialStep === stepName ? prev.steps[0]?.name ?? '' : prev.initialStep,
      }));
      setPositions((prev) => {
        const copy = { ...prev };
        delete copy[stepName];
        return copy;
      });
      if (selectedNode === stepName) setSelectedNode(null);
    },
    [selectedNode],
  );

  const updateStepName = useCallback(
    (oldName: string, newName: string) => {
      if (!newName || oldName === newName) return;
      setDefinition((prev) => ({
        ...prev,
        initialStep: prev.initialStep === oldName ? newName : prev.initialStep,
        steps: prev.steps.map((s) => ({
          ...(s.name === oldName ? { ...s, name: newName } : s),
          name: s.name === oldName ? newName : s.name,
          transitions: s.transitions.map((t) => ({
            ...t,
            targetStep: t.targetStep === oldName ? newName : t.targetStep,
          })),
        })),
      }));
      setPositions((prev) => {
        const copy = { ...prev };
        copy[newName] = copy[oldName] ?? { x: 0, y: 0 };
        delete copy[oldName];
        return copy;
      });
      if (selectedNode === oldName) setSelectedNode(newName);
    },
    [selectedNode],
  );

  const removeTransition = useCallback((fromStep: string, idx: number) => {
    setDefinition((prev) => ({
      ...prev,
      steps: prev.steps.map((s) =>
        s.name === fromStep
          ? { ...s, transitions: s.transitions.filter((_, i) => i !== idx) }
          : s,
      ),
    }));
  }, []);

  const updateTransition = useCallback(
    (fromStep: string, idx: number, field: keyof WorkflowTransition, value: string) => {
      setDefinition((prev) => ({
        ...prev,
        steps: prev.steps.map((s) =>
          s.name === fromStep
            ? {
                ...s,
                transitions: s.transitions.map((t, i) => (i === idx ? { ...t, [field]: value } : t)),
              }
            : s,
        ),
      }));
    },
    [],
  );

  const handleSave = async () => {
    const json = JSON.stringify(definition, null, 2);
    if (selectedDefId) {
      await updateMut.mutateAsync({ id: selectedDefId, name: defName, definitionJson: json, description: defDescription || undefined });
    } else {
      await createMut.mutateAsync({ name: defName, definitionJson: json, description: defDescription || undefined });
    }
  };

  const handleNew = () => {
    setSelectedDefId(null);
    const def = emptyDefinition();
    setDefinition(def);
    setPositions(defaultPositions(def.steps));
    setDefName('');
    setDefDescription('');
    setSelectedNode(null);
  };

  const selectedStep = definition.steps.find((s) => s.name === selectedNode);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* Toolbar */}
      <div
        style={{
          padding: 12,
          borderBottom: '1px solid #ddd',
          display: 'flex',
          gap: 8,
          alignItems: 'center',
          flexWrap: 'wrap',
        }}
      >
        <select
          value={selectedDefId ?? ''}
          onChange={(e) => setSelectedDefId(e.target.value || null)}
          style={{ padding: '6px 10px', borderRadius: 4, border: '1px solid #ccc' }}
        >
          <option value="">-- Wybierz definicję --</option>
          {definitions?.map((d) => (
            <option key={d.id} value={d.id}>
              {d.name} (v{d.version})
            </option>
          ))}
        </select>
        <button onClick={handleNew} style={btnStyle}>
          Nowy
        </button>
        <span style={{ borderLeft: '1px solid #ccc', height: 24 }} />
        <span style={{ fontWeight: 600, fontSize: 13 }}>Dodaj krok:</span>
        {(['action', 'approval', 'end', 'parallel_gateway', 'condition_gateway'] as const).map((t) => (
          <button
            key={t}
            onClick={() => addStep(t)}
            style={{ ...btnStyle, background: STEP_COLORS[t], color: '#fff' }}
          >
            {t.replace('_', ' ')}
          </button>
        ))}
        <span style={{ borderLeft: '1px solid #ccc', height: 24 }} />
        <input
          placeholder="Nazwa"
          value={defName}
          onChange={(e) => setDefName(e.target.value)}
          style={{ padding: '6px 10px', borderRadius: 4, border: '1px solid #ccc', width: 160 }}
        />
        <button onClick={handleSave} style={{ ...btnStyle, background: '#28a745', color: '#fff' }}>
          Zapisz
        </button>
      </div>

      <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
        {/* Canvas */}
        <div
          ref={canvasRef}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          style={{
            flex: 1,
            position: 'relative',
            background: '#f8f9fa',
            overflow: 'auto',
            cursor: connecting ? 'crosshair' : dragging ? 'grabbing' : 'default',
          }}
        >
          {/* SVG connections */}
          <svg
            style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', pointerEvents: 'none' }}
          >
            {definition.steps.flatMap((step) =>
              step.transitions.map((t, idx) => {
                const from = positions[step.name];
                const to = positions[t.targetStep];
                if (!from || !to) return null;
                const x1 = from.x + NODE_WIDTH / 2;
                const y1 = from.y + NODE_HEIGHT;
                const x2 = to.x + NODE_WIDTH / 2;
                const y2 = to.y;
                return (
                  <g key={`${step.name}-${idx}`}>
                    <line
                      x1={x1} y1={y1} x2={x2} y2={y2}
                      stroke="#666" strokeWidth={2}
                      markerEnd="url(#arrowhead)"
                    />
                    {t.condition && (
                      <text
                        x={(x1 + x2) / 2}
                        y={(y1 + y2) / 2 - 6}
                        fontSize={10}
                        fill="#888"
                        textAnchor="middle"
                      >
                        {t.condition}
                      </text>
                    )}
                    <text
                      x={(x1 + x2) / 2}
                      y={(y1 + y2) / 2 + 8}
                      fontSize={11}
                      fill="#333"
                      textAnchor="middle"
                    >
                      {t.outcome}
                    </text>
                  </g>
                );
              }),
            )}
            <defs>
              <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto">
                <polygon points="0 0, 10 3.5, 0 7" fill="#666" />
              </marker>
            </defs>
          </svg>

          {/* Nodes */}
          {definition.steps.map((step) => {
            const pos = positions[step.name] ?? { x: 0, y: 0 };
            const isSelected = selectedNode === step.name;
            const isInitial = definition.initialStep === step.name;
            return (
              <div
                key={step.name}
                onMouseDown={(e) => handleMouseDown(step.name, e)}
                style={{
                  position: 'absolute',
                  left: pos.x,
                  top: pos.y,
                  width: NODE_WIDTH,
                  minHeight: NODE_HEIGHT,
                  background: '#fff',
                  border: `2px solid ${isSelected ? '#007bff' : STEP_COLORS[step.type] ?? '#999'}`,
                  borderRadius: 8,
                  cursor: connecting ? 'crosshair' : 'grab',
                  boxShadow: isSelected ? '0 0 0 3px rgba(0,123,255,0.3)' : '0 1px 4px rgba(0,0,0,0.12)',
                  userSelect: 'none',
                  zIndex: isSelected ? 10 : 1,
                }}
              >
                <div
                  style={{
                    background: STEP_COLORS[step.type] ?? '#999',
                    color: '#fff',
                    padding: '4px 8px',
                    borderRadius: '6px 6px 0 0',
                    fontSize: 11,
                    fontWeight: 600,
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                  }}
                >
                  <span>{step.type.replace('_', ' ')}</span>
                  {isInitial && <span title="Krok początkowy">⭐</span>}
                </div>
                <div style={{ padding: '6px 8px', fontSize: 13, fontWeight: 500 }}>{step.name}</div>
                <div style={{ display: 'flex', gap: 2, padding: '0 8px 4px' }}>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      setConnecting(step.name);
                    }}
                    style={{ ...tinyBtnStyle, background: '#17a2b8', color: '#fff' }}
                    title="Połącz z innym krokiem"
                  >
                    →
                  </button>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      removeStep(step.name);
                    }}
                    style={{ ...tinyBtnStyle, background: '#dc3545', color: '#fff' }}
                    title="Usuń krok"
                  >
                    ✕
                  </button>
                </div>
              </div>
            );
          })}
        </div>

        {/* Properties panel */}
        {selectedStep && (
          <div
            style={{
              width: 300,
              borderLeft: '1px solid #ddd',
              padding: 16,
              overflowY: 'auto',
              background: '#fff',
              fontSize: 13,
            }}
          >
            <h3 style={{ margin: '0 0 12px' }}>Właściwości kroku</h3>
            <label style={labelStyle}>Nazwa</label>
            <input
              value={selectedStep.name}
              onChange={(e) => updateStepName(selectedStep.name, e.target.value)}
              style={inputStyle}
            />
            <label style={labelStyle}>Typ</label>
            <select
              value={selectedStep.type}
              onChange={(e) =>
                setDefinition((prev) => ({
                  ...prev,
                  steps: prev.steps.map((s) =>
                    s.name === selectedStep.name
                      ? { ...s, type: e.target.value as WorkflowStepDefinition['type'] }
                      : s,
                  ),
                }))
              }
              style={inputStyle}
            >
              <option value="action">action</option>
              <option value="approval">approval</option>
              <option value="end">end</option>
              <option value="parallel_gateway">parallel_gateway</option>
              <option value="condition_gateway">condition_gateway</option>
            </select>

            {selectedStep.type === 'approval' && (
              <>
                <label style={labelStyle}>Strategia akceptanta</label>
                <select
                  value={selectedStep.approverStrategy ?? ''}
                  onChange={(e) =>
                    setDefinition((prev) => ({
                      ...prev,
                      steps: prev.steps.map((s) =>
                        s.name === selectedStep.name
                          ? { ...s, approverStrategy: e.target.value || undefined }
                          : s,
                      ),
                    }))
                  }
                  style={inputStyle}
                >
                  <option value="">-- brak --</option>
                  <option value="supervisor">Przełożony</option>
                  <option value="role">Rola</option>
                  <option value="specific">Konkretna osoba</option>
                </select>
                <label style={labelStyle}>Liczba poziomów akceptacji</label>
                <input
                  type="number"
                  min={1}
                  max={10}
                  value={selectedStep.approverLevels ?? 1}
                  onChange={(e) =>
                    setDefinition((prev) => ({
                      ...prev,
                      steps: prev.steps.map((s) =>
                        s.name === selectedStep.name
                          ? { ...s, approverLevels: parseInt(e.target.value) || 1 }
                          : s,
                      ),
                    }))
                  }
                  style={inputStyle}
                />
              </>
            )}

            {selectedStep.type === 'parallel_gateway' && (
              <>
                <label style={labelStyle}>Typ złączenia</label>
                <select
                  value={selectedStep.joinType ?? 'all'}
                  onChange={(e) =>
                    setDefinition((prev) => ({
                      ...prev,
                      steps: prev.steps.map((s) =>
                        s.name === selectedStep.name
                          ? { ...s, joinType: e.target.value as 'all' | 'any' }
                          : s,
                      ),
                    }))
                  }
                  style={inputStyle}
                >
                  <option value="all">Wszystkie gałęzie (AND)</option>
                  <option value="any">Pierwsza gałąź (OR)</option>
                </select>
                <label style={labelStyle}>Krok zbieżności</label>
                <select
                  value={selectedStep.convergenceStep ?? ''}
                  onChange={(e) =>
                    setDefinition((prev) => ({
                      ...prev,
                      steps: prev.steps.map((s) =>
                        s.name === selectedStep.name
                          ? { ...s, convergenceStep: e.target.value || undefined }
                          : s,
                      ),
                    }))
                  }
                  style={inputStyle}
                >
                  <option value="">-- brak --</option>
                  {definition.steps
                    .filter((s) => s.name !== selectedStep.name)
                    .map((s) => (
                      <option key={s.name} value={s.name}>
                        {s.name}
                      </option>
                    ))}
                </select>
              </>
            )}

            <label style={labelStyle}>Krok początkowy?</label>
            <button
              onClick={() => setDefinition((prev) => ({ ...prev, initialStep: selectedStep.name }))}
              disabled={definition.initialStep === selectedStep.name}
              style={{ ...btnStyle, marginBottom: 12 }}
            >
              {definition.initialStep === selectedStep.name ? '⭐ Tak' : 'Ustaw jako początkowy'}
            </button>

            <h4 style={{ margin: '12px 0 8px' }}>Przejścia</h4>
            {selectedStep.transitions.map((t, idx) => (
              <div key={idx} style={{ marginBottom: 8, padding: 8, background: '#f4f4f4', borderRadius: 4 }}>
                <div style={{ display: 'flex', gap: 4, marginBottom: 4 }}>
                  <input
                    value={t.outcome}
                    onChange={(e) => updateTransition(selectedStep.name, idx, 'outcome', e.target.value)}
                    placeholder="Outcome"
                    style={{ ...inputStyle, flex: 1, marginBottom: 0 }}
                  />
                  <button onClick={() => removeTransition(selectedStep.name, idx)} style={tinyBtnStyle}>
                    ✕
                  </button>
                </div>
                <select
                  value={t.targetStep}
                  onChange={(e) => updateTransition(selectedStep.name, idx, 'targetStep', e.target.value)}
                  style={{ ...inputStyle, marginBottom: 4 }}
                >
                  {definition.steps.map((s) => (
                    <option key={s.name} value={s.name}>
                      {s.name}
                    </option>
                  ))}
                </select>
                <input
                  value={t.condition ?? ''}
                  onChange={(e) => updateTransition(selectedStep.name, idx, 'condition', e.target.value)}
                  placeholder="Warunek (opcjonalnie)"
                  style={inputStyle}
                />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

const btnStyle: React.CSSProperties = {
  padding: '6px 12px',
  borderRadius: 4,
  border: '1px solid #ccc',
  background: '#fff',
  cursor: 'pointer',
  fontSize: 12,
  fontWeight: 600,
};

const tinyBtnStyle: React.CSSProperties = {
  padding: '2px 6px',
  borderRadius: 3,
  border: 'none',
  cursor: 'pointer',
  fontSize: 11,
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  marginBottom: 4,
  marginTop: 8,
  fontSize: 12,
};

const inputStyle: React.CSSProperties = {
  display: 'block',
  width: '100%',
  padding: '6px 8px',
  borderRadius: 4,
  border: '1px solid #ccc',
  marginBottom: 8,
  fontSize: 13,
  boxSizing: 'border-box',
};
