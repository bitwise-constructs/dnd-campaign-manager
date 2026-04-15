import { useState } from "react";
import { CharacterDto, DndBeyondSyncStatus, SyncStatusLabel } from "../../types";
import "./DndBeyond.css";

interface Props {
  character: CharacterDto;
  canEdit: boolean;
  onLink: (dndBeyondCharacterId: number) => Promise<void>;
  onSync: () => Promise<void>;
  onUploadJson: (json: string) => Promise<void>;
  onUnlink: () => Promise<void>;
}

export function DndBeyondPanel({
  character,
  canEdit,
  onLink,
  onSync,
  onUploadJson,
  onUnlink,
}: Props) {
  const [linkInput, setLinkInput] = useState("");
  const [jsonInput, setJsonInput] = useState("");
  const [showJsonUpload, setShowJsonUpload] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isLinked = character.dndBeyondSyncStatus !== DndBeyondSyncStatus.Unlinked;
  const isSynced = character.dndBeyondSyncStatus === DndBeyondSyncStatus.Synced;
  const isFailed = character.dndBeyondSyncStatus === DndBeyondSyncStatus.SyncFailed;

  const parseDdbId = (input: string): number | null => {
    // Accept full URL or just the numeric ID
    const urlMatch = input.match(/dndbeyond\.com\/characters\/(\d+)/);
    if (urlMatch) return parseInt(urlMatch[1], 10);
    const num = parseInt(input.trim(), 10);
    return isNaN(num) ? null : num;
  };

  const handleLink = async () => {
    const id = parseDdbId(linkInput);
    if (!id) {
      setError("Enter a valid D&D Beyond character ID or URL");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      await onLink(id);
      setLinkInput("");
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to link");
    } finally {
      setLoading(false);
    }
  };

  const handleSync = async () => {
    setLoading(true);
    setError(null);
    try {
      await onSync();
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Sync failed");
    } finally {
      setLoading(false);
    }
  };

  const handleUploadJson = async () => {
    if (!jsonInput.trim()) return;
    setLoading(true);
    setError(null);
    try {
      await onUploadJson(jsonInput);
      setJsonInput("");
      setShowJsonUpload(false);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Upload failed");
    } finally {
      setLoading(false);
    }
  };

  const timeSince = (isoDate: string): string => {
    const diff = Date.now() - new Date(isoDate).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return "just now";
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    return `${Math.floor(hrs / 24)}d ago`;
  };

  // --- Unlinked state: show the link form ---
  if (!isLinked) {
    if (!canEdit) return null;

    return (
      <div className="ddb-panel ddb-unlinked">
        <div className="ddb-brand-row">
          <span className="ddb-icon">🔗</span>
          <span className="ddb-label">Link D&D Beyond</span>
        </div>
        <div className="ddb-link-form">
          <input
            type="text"
            value={linkInput}
            onChange={(e) => setLinkInput(e.target.value)}
            placeholder="Character ID or URL"
            onKeyDown={(e) => e.key === "Enter" && handleLink()}
            disabled={loading}
          />
          <button className="btn btn-sm btn-primary" onClick={handleLink} disabled={loading}>
            {loading ? "Linking…" : "Link"}
          </button>
        </div>
        <p className="ddb-hint">
          Paste the URL from your D&D Beyond character page, or just the numeric ID.
        </p>

        {!showJsonUpload ? (
          <button
            className="ddb-json-toggle"
            onClick={() => setShowJsonUpload(true)}
          >
            Or paste character JSON manually
          </button>
        ) : (
          <div className="ddb-json-upload">
            <textarea
              value={jsonInput}
              onChange={(e) => setJsonInput(e.target.value)}
              placeholder={'Paste the JSON from:\ncharacter-service.dndbeyond.com/character/v5/character/{id}'}
              rows={4}
              disabled={loading}
            />
            <div className="ddb-json-actions">
              <button className="btn btn-sm btn-primary" onClick={handleUploadJson} disabled={loading || !jsonInput.trim()}>
                {loading ? "Importing…" : "Import JSON"}
              </button>
              <button className="btn btn-sm" onClick={() => setShowJsonUpload(false)}>
                Cancel
              </button>
            </div>
          </div>
        )}

        {error && <p className="ddb-error">{error}</p>}
      </div>
    );
  }

  // --- Linked state: show sync status and controls ---
  return (
    <div className={`ddb-panel ${isFailed ? "ddb-stale" : "ddb-linked"}`}>
      <div className="ddb-status-row">
        <div className="ddb-brand-row">
          <span className="ddb-icon">{isSynced ? "✓" : isFailed ? "⚠" : "↻"}</span>
          <span className="ddb-label">D&D Beyond</span>
          <span className={`ddb-status-badge ${isSynced ? "synced" : isFailed ? "failed" : "syncing"}`}>
            {SyncStatusLabel[character.dndBeyondSyncStatus]}
          </span>
        </div>

        {character.dndBeyondUrl && (
          <a
            href={character.dndBeyondUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="ddb-external-link"
          >
            View on DDB
          </a>
        )}
      </div>

      {character.dndBeyondLastSyncedAt && (
        <span className="ddb-sync-time">
          Last synced {timeSince(character.dndBeyondLastSyncedAt)}
        </span>
      )}

      {isFailed && character.dndBeyondLastSyncError && (
        <div className="ddb-error-banner">
          <p>{character.dndBeyondLastSyncError}</p>
          <p className="ddb-stale-note">Showing previously cached data.</p>
        </div>
      )}

      {/* Stat block from DDB */}
      {(character.strength != null) && (
        <div className="ddb-stats">
          <StatBox label="STR" value={character.strength} />
          <StatBox label="DEX" value={character.dexterity} />
          <StatBox label="CON" value={character.constitution} />
          <StatBox label="INT" value={character.intelligence} />
          <StatBox label="WIS" value={character.wisdom} />
          <StatBox label="CHA" value={character.charisma} />
        </div>
      )}

      {(character.hitPoints != null || character.armorClass != null) && (
        <div className="ddb-combat-stats">
          {character.hitPoints != null && (
            <div className="ddb-combat-stat">
              <span className="combat-value">{character.hitPoints}</span>
              <span className="combat-label">HP</span>
            </div>
          )}
          {character.armorClass != null && (
            <div className="ddb-combat-stat">
              <span className="combat-value">{character.armorClass}</span>
              <span className="combat-label">AC</span>
            </div>
          )}
        </div>
      )}

      {canEdit && (
        <div className="ddb-actions">
          <button className="btn btn-sm" onClick={handleSync} disabled={loading}>
            {loading ? "Syncing…" : "↻ Re-sync"}
          </button>
          <button
            className="ddb-json-toggle"
            onClick={() => setShowJsonUpload(!showJsonUpload)}
          >
            Upload JSON
          </button>
          <button className="btn btn-sm btn-danger" onClick={onUnlink} disabled={loading}>
            Unlink
          </button>
        </div>
      )}

      {showJsonUpload && canEdit && (
        <div className="ddb-json-upload">
          <textarea
            value={jsonInput}
            onChange={(e) => setJsonInput(e.target.value)}
            placeholder="Paste character JSON here as a fallback…"
            rows={3}
            disabled={loading}
          />
          <button className="btn btn-sm btn-primary" onClick={handleUploadJson} disabled={loading || !jsonInput.trim()}>
            Import
          </button>
        </div>
      )}

      {error && <p className="ddb-error">{error}</p>}
    </div>
  );
}

function StatBox({ label, value }: { label: string; value: number | null }) {
  if (value == null) return null;
  const mod = Math.floor((value - 10) / 2);
  const modStr = mod >= 0 ? `+${mod}` : `${mod}`;

  return (
    <div className="stat-box">
      <span className="stat-mod">{modStr}</span>
      <span className="stat-score">{value}</span>
      <span className="stat-label">{label}</span>
    </div>
  );
}
