Skin folders (each image file = one selectable skin in the main menu SKINS tab)

Place only PNG, JPG, JPEG, BMP, or GIF files. They must decode in-game and be between 4 and 4096 pixels per side (max 16 MB per file). Invalid files are hidden from the list.

Folders:
  Player/          — player ship
  EnemyShip/       — enemy ship
  Asteroid/        — asteroid (same sprite for all variants)
  Bullet/          — bullet
  Background/      — scrolling starfield backdrop
  PickupHealth/    — green health pickup
  PickupPositive/  — cyan positive weapon chip
  PickupNegative/  — red negative weapon chip

Inspector fields on GameBootstrap still override these choices (for development).

Note: Folder listing in the SKINS tab uses the file system. In Editor and desktop builds this matches your project folders. On some mobile builds StreamingAssets may be read-only or not listable the same way; bundled images can still load if referenced by known paths.
