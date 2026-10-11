# Static map files

Static maps are stored as JSON in this directory. If a map file is missing, the
game creates it from its built-in starting layout on first launch. Map files
are copied beside the game executable when the project is built.

`overworld.json` and `city_haven.json` are the initial examples. The top-level
fields are:

- `Version`: map-file format version; currently `1`.
- `MapId` and `DisplayName`: stable map key and UI label.
- `Width` and `Height`: dimensions, up to the map console size.
- `LightingEnabled`: `true` enables field-of-view and darkens explored tiles
  outside the player's view; `false` reveals the whole map.
- `PaletteName`: `Dawnbringer`, `Monochrome`, or `Ember`.
- `Tiles`: terrain definitions. Colors are RGB channel values from 0 to 255.
  `Walkable` and `Transparent` affect movement and field-of-view.
- `Rows`: each row is a sequence of four-digit hexadecimal tile IDs, one per
  map cell, from left to right. `ExploredRows` uses `1` and `0` per cell.
- `Exits`: source coordinates and glyph, plus `TargetMapId` (or `TargetLayer`
  for legacy dungeon stairs). `TargetX` and `TargetY` are optional, but must
  be specified together; omitted coordinates use the destination's default
  arrival point.
- `Npcs`: neutral NPC name, location, glyph, color, and `Wanders` setting.

Use `StaticMapFileService.Load(path)` and `Save(path, map)` to read or write
map files from code. NPC interaction callbacks are code behavior and are not
serialized; the file stores only their static map data.
