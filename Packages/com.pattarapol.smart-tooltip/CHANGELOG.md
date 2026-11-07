# Changelog

## [0.1.0] - Unreleased

### Added
- Core rule engine with `TooltipRule`, `TooltipPrompt`, `TooltipEvaluator` and the built in `Condition` factory.
- `ITooltipContext` with a dictionary-backed `TooltipContext`.
- Progress stores `InMemoryProgressStore` and `PlayerPrefsProgressStore`.
- `TooltipTarget` component that resolves UI, 2D and 3D objects to a screen rect, plus `TooltipTargetRegistry`.
- `TargetShape` on `TooltipTarget`, with Auto, Renderers, Colliders and Custom Box.
- Automatic camera choice based on the object's layer. Cameras that render to a texture are skipped.
- World space canvases and world space TextMeshPro are supported.
- `TooltipTarget.Attach` and `WorldTarget` for targets created from code.
- `ScreenProjection` helper for custom targets.
- Targets report a rect even when off screen. Objects behind the camera point the right way.
- Tighter fit for rotated meshes, sprites and box colliders.
- `TooltipOverlay` with a dim layer and a rounded hole, tooltip bubble with arrow, follows the target, sticks to the screen edge when the target is off screen, flips side when there is no room, respects the safe area.
- `TooltipCutout` lets taps inside the hole reach the game and blocks the rest.
- `TooltipBubble` can be replaced with your own prefab.
- `TooltipDirector` runs the rules and the overlay for you. `Check()` is batched and waits for the UI to settle. `Hold()` pauses tips while the game is busy. `Started` and `Ended` report each tip with a `TooltipEndReason`.
- Tips count as seen when they end (completed or dismissed), not when they show.
- `doneWhen` on `TooltipRule` says when the player has done what the tip asks. Without it, tapping the target finishes the tip.
- `requires` on `TooltipRule` lets a tip start for a target that is off screen.
- `cooldown` on `TooltipRule` for repeatable tips.
- `TooltipTargetRegistry.TryFind` finds targets even when they are off screen.
- `TooltipOverlay.TargetTapped` fires when the player taps inside the hole. Works with both input systems.
- `TooltipOverlay.Hidden` reports a `TooltipHideReason`.
- Sample UI Tooltip, a small game menu built from code that shows priority, holds, settling, cooldowns, taps on the target and every end reason, with a live log.
- Sample 3D World, run by the director with off screen targets.
- PlayMode tests for targets, the overlay and the director.
