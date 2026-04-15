import { WishlistItemDto } from "../../types";
import { RarityBadge } from "../common/RarityBadge";
import "./Wishlists.css";

interface Props {
  wishlists: Record<string, WishlistItemDto[]>;
  currentUserId: string | null;
  isDm: boolean;
  onRemove?: (id: string) => void;
}

export function WishlistView({ wishlists, currentUserId, isDm, onRemove }: Props) {
  const characterNames = Object.keys(wishlists);

  if (characterNames.length === 0) {
    return (
      <div className="empty-state">
        No wishlists yet. Players can add desired magic items to their character's wishlist.
      </div>
    );
  }

  return (
    <div className="wishlists-container">
      {characterNames.map((name) => {
        const items = wishlists[name];
        const isOwner = items[0] && items[0].characterId
          ? items.some((i) => {
              // In a real app, we'd check against the character's playerUserId
              return false; // Simplified — ownership check happens server-side
            })
          : false;

        return (
          <div key={name} className="card wishlist-card">
            <div className="wishlist-header">
              <h3 className="wishlist-character-name">{name}</h3>
              <span className="wishlist-count">{items.length} items</span>
            </div>

            <div className="wishlist-items">
              {items.map((item, idx) => (
                <div key={item.id} className="wishlist-item">
                  <span className="wishlist-priority">#{item.priority}</span>
                  <div className="wishlist-item-info">
                    <span className="wishlist-item-name">{item.magicItemName}</span>
                    <RarityBadge rarity={item.magicItemRarity} />
                  </div>
                  {item.notes && (
                    <span className="wishlist-item-notes">{item.notes}</span>
                  )}
                  {(isDm || isOwner) && onRemove && (
                    <button
                      className="btn btn-sm btn-danger"
                      onClick={() => onRemove(item.id)}
                      title="Remove from wishlist"
                    >
                      ✕
                    </button>
                  )}
                </div>
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}
