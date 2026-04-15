import { Rarity, RarityLabel } from "../../types";

const rarityClassMap: Record<Rarity, string> = {
  [Rarity.Common]: "badge-common",
  [Rarity.Uncommon]: "badge-uncommon",
  [Rarity.Rare]: "badge-rare",
  [Rarity.VeryRare]: "badge-veryrare",
  [Rarity.Legendary]: "badge-legendary",
  [Rarity.Artifact]: "badge-artifact",
};

export function RarityBadge({ rarity }: { rarity: Rarity }) {
  return (
    <span className={`badge ${rarityClassMap[rarity]}`}>
      {RarityLabel[rarity]}
    </span>
  );
}
