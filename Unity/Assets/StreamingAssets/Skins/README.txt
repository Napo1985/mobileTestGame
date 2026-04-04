Skin folders (each image file = one selectable skin in the main menu SKINS tab)

Place only PNG, JPG, JPEG, BMP, or GIF files. They must decode in-game (max 8192 px per side on disk, max 16 MB per file). At load time, images are automatically scaled to fit within 4096 px on the longest edge (and upscaled if below 4 px on the shortest edge). In gameplay, custom skins also get a world-size fit (default ~1.38 units on the longest edge for the player) so high-res PNGs are not huge on screen — tune on GameBootstrap under Custom images. Invalid files are hidden from the list.

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
