import { MagicItemDto, CategoryLabel } from "../../types";
import { RarityBadge } from "../common/RarityBadge";
import "./MagicItems.css";

interface Props {
  items: MagicItemDto[];
  isDm: boolean;
  selectable?: boolean;
  selectedIds?: Set<string>;
  onToggleSelect?: (id: string) => void;
  onSelectAll?: () => void;
  onDeselectAll?: () => void;
  onEdit?: (item: MagicItemDto) => void;
  onDelete?: (id: string) => void;
}

export function MagicItemsTable({
  items,
  isDm,
  selectable = false,
  selectedIds = new Set(),
  onToggleSelect,
  onSelectAll,
  onDeselectAll,
  onEdit,
  onDelete,
}: Props) {
  if (items.length === 0) {
    return <div className="empty-state">No magic items have been added to this campaign.</div>;
  }

  const allSelected = items.length > 0 && items.every((i) => selectedIds.has(i.id));

  return (
    <div className="items-table-wrapper">
      {selectable && (
        <div className="selection-bar">
          <span className="selection-count">
            {selectedIds.size} of {items.length} selected
          </span>
          <button className="btn btn-sm" onClick={allSelected ? onDeselectAll : onSelectAll}>
            {allSelected ? "Deselect All" : "Select All"}
          </button>
        </div>
      )}

      <table className="data-table items-table">
        <thead>
          <tr>
            {selectable && <th style={{ width: 40 }}></th>}
            <th>Item</th>
            <th>Rarity</th>
            <th>Category</th>
            <th>Attune</th>
            <th>Source</th>
            {isDm && <th style={{ width: 120 }}></th>}
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr
              key={item.id}
              className={selectedIds.has(item.id) ? "row-selected" : ""}
            >
              {selectable && (
                <td>
                  <input
                    type="checkbox"
                    checked={selectedIds.has(item.id)}
                    onChange={() => onToggleSelect?.(item.id)}
                  />
                </td>
              )}
              <td>
                <div className="item-name">{item.name}</div>
                {item.description && (
                  <div className="item-desc">{item.description}</div>
                )}
              </td>
              <td>
                <RarityBadge rarity={item.rarity} />
              </td>
              <td className="cell-category">{CategoryLabel[item.category]}</td>
              <td className="cell-attune">
                {item.requiresAttunement ? (
                  <span className="attune-yes" title={item.attunementRequirement || "Yes"}>
                    ◆
                  </span>
                ) : (
                  <span className="attune-no">—</span>
                )}
              </td>
              <td className="cell-source">{item.source || "—"}</td>
              {isDm && (
                <td>
                  <div className="cell-actions">
                    {onEdit && (
                      <button className="btn btn-sm" onClick={() => onEdit(item)}>
                        Edit
                      </button>
                    )}
                    {onDelete && (
                      <button
                        className="btn btn-sm btn-danger"
                        onClick={() => onDelete(item.id)}
                      >
                        ✕
                      </button>
                    )}
                  </div>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
