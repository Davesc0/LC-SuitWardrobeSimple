# Changelog

## 0.2.1
- The suit rack blocks players again. Turning off the suits' own hitboxes had made it possible to
  walk through it.

## 0.2.0
- Controller support in the wardrobe: stick or d-pad to pick a suit, shoulder buttons to change
  tab, right stick to turn and zoom the preview.
- Scanning is blocked while the wardrobe is open, ShipLootCruiser's ship value included.
- New config option `UnlockAllSuits` (host only, off by default): unlocks every suit for free.

## 0.1.0
- First version: the suit rack opens a wardrobe menu with every suit, tabs per mod or author,
  search, favourites and a preview before you wear it.
- Thumbnails are taken once after a lobby loads and cached.
- Works with longer racks (BiggerShip). Suits that don't fit on the rail are hidden.
- TooManySuits is optional, its page arrows are hidden by default (`HidePageButtons`).
- `ShowBoots` can hide the boots under the rack.
