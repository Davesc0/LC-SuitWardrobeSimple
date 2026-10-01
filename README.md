# Suit Wardrobe Simple

Changes how the suit rack works. You can't change suits by clicking the ones on the rack
anymore. Instead, the rack opens a wardrobe menu with every suit the crew owns.

![The suit rack](https://raw.githubusercontent.com/Davesc0/LC-SuitWardrobeSimple/main/docs/rack.webp)
![The wardrobe menu](https://raw.githubusercontent.com/Davesc0/LC-SuitWardrobeSimple/main/docs/menu.webp)

## Features

- **Tabs per mod or author**, plus a search box.
- **Preview before you wear it.** Click a suit to try it on: only you see it, on your own
  character (model replacements too). Drag to turn, scroll to zoom. Press Wear to change for
  real, or close to go back to your old suit.
- **Favourites.** Star a suit to keep it in the Favourites tab.
- **Thumbnails.** Each suit has a picture in the list. The only way I found to make them was to
  photograph every suit on your character, and this can only happen after a lobby loads. It only
  needs to happen once: the pictures are cached and load almost instantly after that. While they
  are being taken the rack can't be opened, but it shows how far along it is.

Changing suits uses the game's own code, so players without the mod see the change too. In
principle the mod is client side.

## Compatibility

- **More_Suits** and every suit pack loaded through it. The tabs are named after the pack.
- **ModelReplacementAPI**: replacement models are tried on and shown in the preview.
- **TooManySuits**: optional. Without it, the suits that don't fit on the rail are hidden. With
  it, the page arrows are hidden by default and the rack stays on the first page. Every suit is
  in the wardrobe either way.
- **BiggerShip**: its longer rack works end to end and holds as many suits as fit on it.
- **SuitSaver**: saves the suit you wear, not the ones you only try on.

## Note

Made with AI assistance, but tested in a 260-mod pack.
Still found an issue? Report it on the mod's GitHub page.

I originally wanted to make a proper standalone furniture mod, a wardrobe of its own. This one
came out of it to keep things simple. The other one might get finished some day.
