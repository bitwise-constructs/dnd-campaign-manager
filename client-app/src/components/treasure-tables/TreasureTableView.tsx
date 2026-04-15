import { useState } from "react";
import { MagicItemDto, TreasureTableDto, TreasureTableEntryDto, RarityLabel, CategoryLabel } from "../../types";
import { RarityBadge } from "../common/RarityBadge";
import { MagicItemsTable } from "../magic-items/MagicItemsTable";
import "./TreasureTables.css";

interface GeneratorProps {
  items: MagicItemDto[];
  onGenerate: (name: string, description: string, selectedItems: { magicItemId: string; weight: number }[]) => void;
}

export function TreasureTableGenerator({ items, onGenerate }: GeneratorProps) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [weights, setWeights] = useState<Record<string, number>>({});
  const [tableName, setTableName] = useState("");
  const [tableDesc, setTableDesc] = useState("");

  const toggleSelect = (id: string) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
        if (!weights[id]) setWeights((w) => ({ ...w, [id]: 1 }));
      }
      return next;
    });
  };

  const selectAll = () => {
    const allIds = new Set(items.map((i) => i.id));
    setSelectedIds(allIds);
    const newWeights: Record<string, number> = {};
    items.forEach((i) => (newWeights[i.id] = weights[i.id] || 1));
    setWeights(newWeights);
  };

  const deselectAll = () => setSelectedIds(new Set());

  const handleGenerate = () => {
    if (!tableName.trim() || selectedIds.size === 0) return;
    const selected = Array.from(selectedIds).map((id) => ({
      magicItemId: id,
      weight: weights[id] || 1,
    }));
    onGenerate(tableName, tableDesc, selected);
    setTableName("");
    setTableDesc("");
    setSelectedIds(new Set());
    setWeights({});
  };

  return (
    <div className="generator-container">
      <div className="generator-config card">
        <h3>Generate Treasure Table</h3>
        <p className="generator-hint">
          Select items from the list below, assign weights, and generate a rollable treasure table.
        </p>

        <div className="generator-form">
          <div className="form-group">
            <label>Table Name</label>
            <input
              value={tableName}
              onChange={(e) => setTableName(e.target.value)}
              placeholder="e.g. Dragon's Hoard — Session 14"
            />
          </div>
          <div className="form-group">
            <label>Description</label>
            <input
              value={tableDesc}
              onChange={(e) => setTableDesc(e.target.value)}
              placeholder="Optional notes..."
            />
          </div>
        </div>

        {selectedIds.size > 0 && (
          <div className="weight-editor">
            <h4>Weights</h4>
            <p className="generator-hint">
              Higher weight = more likely to be rolled. Items with weight 2 are twice as likely as weight 1.
            </p>
            <div className="weight-list">
              {items
                .filter((i) => selectedIds.has(i.id))
                .map((item) => (
                  <div key={item.id} className="weight-row">
                    <span className="weight-item-name">{item.name}</span>
                    <RarityBadge rarity={item.rarity} />
                    <input
                      type="number"
                      min={1}
                      max={20}
                      value={weights[item.id] || 1}
                      onChange={(e) =>
                        setWeights((w) => ({
                          ...w,
                          [item.id]: Math.max(1, parseInt(e.target.value) || 1),
                        }))
                      }
                      className="weight-input"
                    />
                  </div>
                ))}
            </div>
          </div>
        )}

        <button
          className="btn btn-primary"
          disabled={!tableName.trim() || selectedIds.size === 0}
          onClick={handleGenerate}
        >
          Generate Table ({selectedIds.size} items)
        </button>
      </div>

      <div className="generator-items">
        <MagicItemsTable
          items={items}
          isDm={false}
          selectable
          selectedIds={selectedIds}
          onToggleSelect={toggleSelect}
          onSelectAll={selectAll}
          onDeselectAll={deselectAll}
        />
      </div>
    </div>
  );
}

// --- Display a generated treasure table ---

interface TableViewProps {
  table: TreasureTableDto;
  onRoll: (tableId: string) => void;
  onDelete?: (tableId: string) => void;
  rolledEntry?: TreasureTableEntryDto | null;
  rolledValue?: number | null;
}

export function TreasureTableView({ table, onRoll, onDelete, rolledEntry, rolledValue }: TableViewProps) {
  return (
    <div className="card treasure-table-card">
      <div className="tt-header">
        <div>
          <h3 className="tt-name">{table.name}</h3>
          {table.description && <p className="tt-desc">{table.description}</p>}
        </div>
        <div className="tt-actions">
          <button className="btn btn-primary btn-sm" onClick={() => onRoll(table.id)}>
            🎲 Roll
          </button>
          {onDelete && (
            <button className="btn btn-sm btn-danger" onClick={() => onDelete(table.id)}>
              Delete
            </button>
          )}
        </div>
      </div>

      {rolledEntry && (
        <div className="roll-result">
          <div className="roll-dice">
            <span className="roll-value">{rolledValue ?? "?"}</span>
          </div>
          <div className="roll-item">
            <span className="roll-item-name">{rolledEntry.magicItemName}</span>
            <RarityBadge rarity={rolledEntry.magicItemRarity} />
            <span className="roll-item-cat">{CategoryLabel[rolledEntry.magicItemCategory]}</span>
          </div>
        </div>
      )}

      <table className="data-table tt-entries">
        <thead>
          <tr>
            <th>Roll</th>
            <th>Item</th>
            <th>Rarity</th>
            <th>Weight</th>
          </tr>
        </thead>
        <tbody>
          {table.entries.map((entry) => (
            <tr
              key={entry.id}
              className={rolledEntry?.id === entry.id ? "row-rolled" : ""}
            >
              <td className="cell-roll">
                {entry.minRoll}–{entry.maxRoll}
              </td>
              <td className="cell-item-name">{entry.magicItemName}</td>
              <td>
                <RarityBadge rarity={entry.magicItemRarity} />
              </td>
              <td className="cell-weight">{entry.weight}×</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
