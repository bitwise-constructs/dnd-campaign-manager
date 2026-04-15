import { useState } from "react";
import { CharacterPrivacySettingsDto } from "../../types";
import "./Privacy.css";

interface Props {
  settings: CharacterPrivacySettingsDto;
  onSave: (settings: CharacterPrivacySettingsDto) => Promise<void>;
}

interface ToggleField {
  key: keyof CharacterPrivacySettingsDto;
  label: string;
  description: string;
}

const FIELDS: ToggleField[] = [
  { key: "showAbilityScores", label: "Ability scores", description: "STR, DEX, CON, INT, WIS, CHA" },
  { key: "showHitPoints", label: "Hit points", description: "Current and max HP" },
  { key: "showArmorClass", label: "Armor class", description: "AC value" },
  { key: "showInventory", label: "Inventory", description: "Held items and equipment" },
  { key: "showPersonalityTraits", label: "Personality traits", description: "How your character behaves" },
  { key: "showIdeals", label: "Ideals", description: "What your character believes in" },
  { key: "showBonds", label: "Bonds", description: "Connections and commitments" },
  { key: "showFlaws", label: "Flaws", description: "Weaknesses and vulnerabilities" },
  { key: "showWishlist", label: "Magic item wishlist", description: "Desired magic items" },
  { key: "showBackstory", label: "Backstory", description: "Character history and background" },
];

export function PrivacySettingsPanel({ settings, onSave }: Props) {
  const [local, setLocal] = useState<CharacterPrivacySettingsDto>(settings);
  const [saving, setSaving] = useState(false);
  const [dirty, setDirty] = useState(false);

  const toggle = (key: keyof CharacterPrivacySettingsDto) => {
    setLocal((prev) => {
      const next = { ...prev, [key]: !prev[key] };
      // If toggling off "showAll", keep individual settings as-is
      // If toggling on "showAll", we don't force individuals — showAll overrides at the server
      if (key === "showAll" && !prev.showAll) {
        // Turning on showAll
      }
      return next;
    });
    setDirty(true);
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      await onSave(local);
      setDirty(false);
    } finally {
      setSaving(false);
    }
  };

  const allIndividuallyOn = FIELDS.every((f) => local[f.key]);

  return (
    <div className="privacy-panel">
      <div className="privacy-header">
        <div>
          <h3 className="privacy-title">Character sheet privacy</h3>
          <p className="privacy-desc">
            Choose what other players can see. The DM always sees everything.
          </p>
        </div>
      </div>

      {/* Share all toggle */}
      <div className="privacy-toggle-row privacy-toggle-all">
        <div className="toggle-info">
          <span className="toggle-label">Share everything</span>
          <span className="toggle-desc">Make your entire character sheet public to the party</span>
        </div>
        <button
          className={`toggle-switch ${local.showAll ? "on" : ""}`}
          onClick={() => toggle("showAll")}
          role="switch"
          aria-checked={local.showAll}
        >
          <span className="toggle-thumb" />
        </button>
      </div>

      {!local.showAll && (
        <div className="privacy-fields">
          {FIELDS.map((field) => (
            <div key={field.key} className="privacy-toggle-row">
              <div className="toggle-info">
                <span className="toggle-label">{field.label}</span>
                <span className="toggle-desc">{field.description}</span>
              </div>
              <button
                className={`toggle-switch ${local[field.key] ? "on" : ""}`}
                onClick={() => toggle(field.key)}
                role="switch"
                aria-checked={local[field.key] as boolean}
              >
                <span className="toggle-thumb" />
              </button>
            </div>
          ))}
        </div>
      )}

      {local.showAll && (
        <p className="privacy-all-note">
          All fields are visible to other players. Toggle off to control individually.
        </p>
      )}

      {dirty && (
        <div className="privacy-save-bar">
          <span className="privacy-unsaved">Unsaved changes</span>
          <button
            className="btn btn-primary btn-sm"
            onClick={handleSave}
            disabled={saving}
          >
            {saving ? "Saving..." : "Save privacy settings"}
          </button>
        </div>
      )}
    </div>
  );
}
