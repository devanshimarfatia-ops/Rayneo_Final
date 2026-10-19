# Feature launcher (Plan B)

Self-contained home screen that runs the existing features in one scene, in case our feature code cannot be
merged into the team's Canvas UI. Scene: `Assets/Scenes/Test-Launcher.unity` = RayNeo "XR Plugin" prefab +
one `FeatureLauncher`. The feature bootstraps (`AnchorZoneBootstrap`, `VoiceTodoTestBootstrap`) are created as
inactive children and built on first open.

## Controls
| Where | Input | Action |
|---|---|---|
| Home | Swipe forward / back (Editor: Left / Right) | Focus left / right tile (clamped, no wrap) |
| Home | Tap (Editor: Space) | Open ANCHOR / TODO, or QUIT the app |
| Inside a feature | Double tap / Android back (Editor: Escape) | Back to home (ignored while the anchor menu is open: there it cancels the menu) |

All other input inside a feature belongs to that feature (see its own README / class comments).

## How switching works
Only one feature has input and a visible HUD at a time. Going home disables its front components
(`AnchorPlacementInput`, `ZoneDebugHUD`, `ZoneAnchorMarkers` or `VoiceTodoTestPanel`); each hides its own
head-locked canvas in `OnDisable`. `Keep Anchor Running` (default ON) keeps SLAM + zone detection alive in the
background, so `ModeManager.OnModeChanged` still fires and anchors stay valid.
Team UI can reuse the same pattern: a button enables one feature's front components and disables the others.

## Layout tuning
All home screen layout values are on the `FeatureLauncher` Inspector under "Layout" (tile size, gap, distance
from the bottom, title / subtitle / hint toggles, font sizes, frame thickness). Changes apply live in Play mode.
Play-mode edits are reset when Play stops: Copy Component, stop Play, Paste Component Values, save the scene.
Fonts below 16 px and lines below 2 px are raised to the spec minimum.
