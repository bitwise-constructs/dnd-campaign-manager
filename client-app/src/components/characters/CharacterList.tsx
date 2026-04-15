import { useState } from "react";
import { CharacterDto, CharacterPrivacySettingsDto, ClassLabel } from "../../types";
import { DndBeyondPanel } from "../dndbeyond/DndBeyondPanel";
import { PrivacySettingsPanel } from "../privacy/PrivacySettingsPanel";
import "./Characters.css";

interface Props {
  characters: CharacterDto[];
  currentUserId: string | null;
  isDm: boolean;
  onEdit?: (character: CharacterDto) => void;
  onDelete?: (id: string) => void;
  onDdbLink?: (characterId: string, dndBeyondCharacterId: number) => Promise<void>;
  onDdbSync?: (characterId: string) => Promise<void>;
  onDdbUploadJson?: (characterId: string, json: string) => Promise<void>;
  onDdbUnlink?: (characterId: string) => Promise<void>;
  onPrivacySave?: (characterId: string, settings: CharacterPrivacySettingsDto) => Promise<void>;
}

export function CharacterList({
  characters,
  currentUserId,
  isDm,
  onEdit,
  onDelete,
  onDdbLink,
  onDdbSync,
  onDdbUploadJson,
  onDdbUnlink,
  onPrivacySave,
}: Props) {
  // Track which card has its detail section expanded
  const [expandedId, setExpandedId] = useState<string | null>(null);

  if (characters.length === 0) {
    return <div className="empty-state">No adventurers have joined this campaign yet.</div>;
  }

  return (
    <div className="character-grid grid-2">
      {characters.map((char) => {
        const canEdit = char.isOwner || isDm;
        const isExpanded = expandedId === char.id;

        return (
          <div key={char.id} className="card character-card">
            {/* --- Header: always visible --- */}
            <div className="character-header">
              <div className="character-avatar">
                {char.imageUrl ? (
                  <img src={char.imageUrl} alt={char.name} />
                ) : (
                  <span className="avatar-placeholder">
                    {char.name.charAt(0).toUpperCase()}
                  </span>
                )}
              </div>
              <div className="character-identity">
                <h3 className="character-name">{char.name}</h3>
                <span className="character-meta">
                  Level {char.level} {char.race} {ClassLabel[char.class]}
                </span>
              </div>
              <button
                className="expand-btn"
                onClick={() => setExpandedId(isExpanded ? null : char.id)}
                title={isExpanded ? "Collapse" : "Expand details"}
              >
                {isExpanded ? "▾" : "▸"}
              </button>
            </div>

            <div className="character-player">
              <span className="player-label">Player</span>
              <span className="player-name">{char.playerDisplayName || "Unknown"}</span>
            </div>

            {/* --- Roleplay traits (privacy-gated — null = hidden) --- */}
            {(char.personalityTraits || char.ideals || char.bonds || char.flaws) && (
              <div className="character-traits">
                {char.personalityTraits && (
                  <TraitRow label="Personality" value={char.personalityTraits} />
                )}
                {char.ideals && <TraitRow label="Ideals" value={char.ideals} />}
                {char.bonds && <TraitRow label="Bonds" value={char.bonds} />}
                {char.flaws && <TraitRow label="Flaws" value={char.flaws} />}
              </div>
            )}

            {/* --- Expanded section --- */}
            {isExpanded && (
              <div className="character-expanded">
                {/* Backstory */}
                {char.backstory && (
                  <div className="character-backstory">
                    <span className="section-label">Backstory</span>
                    <p className="backstory-text">{char.backstory}</p>
                  </div>
                )}

                {/* Inventory (privacy-gated — null = hidden) */}
                {char.inventory !== null && char.inventory.length > 0 && (
                  <div className="character-inventory">
                    <span className="section-label">
                      Inventory ({char.inventory.length} items)
                    </span>
                    <div className="inventory-list">
                      {char.inventory.map((item) => (
                        <div
                          key={item.id}
                          className={`inventory-item ${item.isMagic ? "magic" : ""} ${item.isEquipped ? "equipped" : ""}`}
                        >
                          <span className="inv-name">
                            {item.name}
                            {item.quantity > 1 && (
                              <span className="inv-qty"> x{item.quantity}</span>
                            )}
                          </span>
                          <span className="inv-meta">
                            {item.isEquipped && <span className="inv-tag equipped">Equipped</span>}
                            {item.isAttuned && <span className="inv-tag attuned">Attuned</span>}
                            {item.isMagic && item.rarity && (
                              <span className="inv-tag magic">{item.rarity}</span>
                            )}
                            {item.itemType && (
                              <span className="inv-type">{item.itemType}</span>
                            )}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Hidden fields notice for non-owners */}
                {!char.isOwner && !isDm && (
                  <HiddenFieldsNotice char={char} />
                )}

                {/* D&D Beyond integration panel */}
                <DndBeyondPanel
                  character={char}
                  canEdit={canEdit}
                  onLink={(ddbId) => onDdbLink?.(char.id, ddbId) ?? Promise.resolve()}
                  onSync={() => onDdbSync?.(char.id) ?? Promise.resolve()}
                  onUploadJson={(json) => onDdbUploadJson?.(char.id, json) ?? Promise.resolve()}
                  onUnlink={() => onDdbUnlink?.(char.id) ?? Promise.resolve()}
                />

                {/* Privacy settings — only visible to the owning player */}
                {char.isOwner && char.privacySettings && onPrivacySave && (
                  <PrivacySettingsPanel
                    settings={char.privacySettings}
                    onSave={(s) => onPrivacySave(char.id, s)}
                  />
                )}
              </div>
            )}

            {canEdit && (
              <div className="character-actions">
                {onEdit && (
                  <button className="btn btn-sm" onClick={() => onEdit(char)}>
                    Edit
                  </button>
                )}
                {isDm && onDelete && (
                  <button className="btn btn-sm btn-danger" onClick={() => onDelete(char.id)}>
                    Remove
                  </button>
                )}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

function TraitRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="trait-row">
      <span className="trait-label">{label}</span>
      <span className="trait-value">{value}</span>
    </div>
  );
}

function HiddenFieldsNotice({ char }: { char: CharacterDto }) {
  const hidden: string[] = [];
  if (char.strength === null) hidden.push("ability scores");
  if (char.hitPoints === null) hidden.push("HP");
  if (char.armorClass === null) hidden.push("AC");
  if (char.inventory === null) hidden.push("inventory");
  if (char.personalityTraits === null) hidden.push("personality");
  if (char.ideals === null) hidden.push("ideals");
  if (char.bonds === null) hidden.push("bonds");
  if (char.flaws === null) hidden.push("flaws");
  if (char.backstory === null) hidden.push("backstory");

  if (hidden.length === 0) return null;

  return (
    <div className="hidden-notice">
      Some details are private: {hidden.join(", ")}
    </div>
  );
}
