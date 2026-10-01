# Map modes

Maps and prefabs are embedded JSON, loaded when the catalog is first used. There is no in-game editor.

## Changing Big Map size or ratio

Edit the `BigMap` entry in `modes.json`, then rebuild and restart both API and frontend. Existing matches retain their original map.

- `width` / `height`: world dimensions. All twelve layouts, obstacle placements, terrain and spawn coordinates scale from their reference canvas automatically. Tanks and movement speeds stay the same.
- `viewWidth` / `viewHeight`: board size on screen. Big Map fits the entire world using the smaller width/height scale. A mismatched aspect ratio leaves unused space on the right or bottom.
- `maxPlayers` / `spawnCount`: capacity and expected spawn count. Changing spawn count also requires editing each layout's spawn list.

`big-map-presets.json` contains four ready-to-copy size/view combinations:

| Preset | World | View | Tank scale |
| --- | --- | --- | --- |
| Default | 1800 x 1400 | 900 x 700 | 0.5 |
| Wide | 2400 x 1400 | 1000 x 700 | 0.417 |
| Larger | 2250 x 1750 | 900 x 700 | 0.4 |
| Closer | 1800 x 1400 | 1080 x 840 | 0.6 |

Copy the four dimensions into `modes.json`; retain capacity and spawn count. These presets are examples, not an additional runtime selector. Run `dotnet test GameTest` after edits; catalog loading rejects blocked or out-of-bounds spawns. Dimension assertions in `MapModeTests` describe the current defaults and should change alongside an intentional default change.

## Layouts and shapes

`Layouts` holds four Standard, twelve Big Map and three Foggish maps. `prefabs.json` holds reusable obstacle shapes. A placement selects a prefab, position, uniform scale and optional earthy color. Ground patches share the same shapes but are visual only.

Coordinates describe a rectangle around each shape. Triangles point upward. Ellipses include circles. Arcs use `startAngle`, positive clockwise `sweepAngle` in degrees and an `innerRatio` between zero and one. Curves are polygons sampled every five degrees; collision and SVG rendering use the same polygon. Bullet reflections retain the existing horizontal/vertical behavior.

Spawn coordinates use the existing tank anchor; the sprite's top is 26 units above that anchor by default. Spawn selection leaves a full tank width between sprite rectangles. If no spawn is free, joins and respawns wait and retry each tick.

Foggish shows a following rectangular viewport without fog or explored-area memory. During respawn waits and elimination it follows a living player, retaining that player until they die. With nobody living it holds the last camera position until someone spawns. The server still sends the complete world state.
