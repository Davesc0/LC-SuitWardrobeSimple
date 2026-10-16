# Suit Wardrobe Simple

Changes how the suit rack works. You can't change suits by clicking the ones on the rack
anymore. Instead, the rack opens a wardrobe menu with every suit the crew owns.

| The rack | The wardrobe |
| :---: | :---: |
| ![The suit rack](https://raw.githubusercontent.com/Davesc0/LC-SuitWardrobeSimple/main/docs/rack.webp) | ![The wardrobe menu](https://raw.githubusercontent.com/Davesc0/LC-SuitWardrobeSimple/main/docs/menu.webp) |

## Features

- Tabs per mod or author
- Preview before you wear a suit
- Favourites
- Thumbnail for every suit
- Controller support

> **Note:** the thumbnails are made by photographing every suit on your character after a lobby
> loads. This only happens once, they are cached after that. While it runs the rack can't be
> opened, but it shows the progress.

Changing suits uses the game's own code, so players without the mod see the change too.
Optional: unlock every suit for free (`UnlockAllSuits` in the config, host only)

## Recommended

- **SuitSaver**: saves the suit you wear, not the ones you only try on.
- **InputUtils**: with it the controller hints are button icons, without it button
  names.
- **CorrectControllerIcon**: optional. The icons follow the controller picked there.

## Compatibility

- **More_Suits** and every suit pack loaded through it.
- **ModelReplacementAPI**: replacement models are tried on and shown in the preview.
- **TooManySuits**: without it, the suits that don't fit on the rail are hidden. With
  it, the page arrows are hidden by default and the rack stays on the first page. Every suit is
  in the wardrobe either way.
- **BiggerShip**: its longer rack works end to end and holds as many suits as fit on it.

## Note

Made with AI assistance, but tested in a 260-mod pack.
Still found an issue? Report it on the mod's GitHub page.

I originally wanted to make a proper standalone furniture mod, a wardrobe of its own. This one
came out of it to keep things simple. The other one might get finished some day.
