# Changelog

## [0.1.0] - Unreleased

### Added
- Core rule engine: `TooltipRule`, `TooltipPrompt`, `TooltipEvaluator`, built-in `Condition` factory.
- `ITooltipContext` with a dictionary-backed `TooltipContext`.
- Progress stores: `InMemoryProgressStore`, `PlayerPrefsProgressStore`.
- `TooltipTarget` component that resolves UI, 2D and 3D objects to a screen rect, plus `TooltipTargetRegistry`.
- `TargetShape` on `TooltipTarget`: Auto, Renderers, Colliders, Custom Box.
- Automatic camera choice based on the object's layer. Cameras that render to a texture are skipped.
- World space canvases and world space TextMeshPro are supported.
- `TooltipTarget.Attach` and `WorldTarget` for targets created from code.
- `ScreenProjection` helper for custom targets.
- Targets report a rect even when off screen. Objects behind the camera point the right way.
- Tighter fit for rotated meshes, sprites and box colliders.
- `TooltipOverlay`: dim layer with a rounded hole, tooltip bubble with arrow, follows the target, sticks to the screen edge when the target is off screen, flips side when there is no room, respects the safe area.
- `TooltipCutout` lets taps inside the hole reach the game and blocks the rest.
- `TooltipBubble` can be replaced with your own prefab.
- Samples: UI Tooltip and 3D World.
- PlayMode tests for targets and the overlay.
