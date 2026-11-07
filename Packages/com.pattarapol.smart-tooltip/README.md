# Smart Tooltip

Rule-driven tooltips and highlights for Unity. Tag anything (a UI button, a 2D sprite, a 3D mesh) with an id, describe when a tip should appear, and let the director pick what to show.

> Status is early. Rules, targets, the overlay and the director work.

## Install

In the Package Manager, choose **Add package from git URL…** and paste this.

```
https://github.com/MidasCE/unity-smart-tooltip.git?path=Packages/com.pattarapol.smart-tooltip
```

Requires Unity 2022.3 or newer.

## Quick start

1. Add a **TooltipTarget** component to the object you want to point at and give it an id, for example `play-button`. See [Targets](#targets).

2. Describe your rules. `conditions` say when a tip may show. `doneWhen` says when the player has done what it asks.

```csharp
using SmartTooltip;
using SmartTooltip.Core;

var rules = new[]
{
    new TooltipRule("first-play",
        new TooltipPrompt("play-button", "Tap here to start!", Placement.Above),
        conditions: new[] { Condition.Is("screen", "home") },
        doneWhen: Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1)),
};
```

3. Create the director once, for example when the game starts.

```csharp
var context = new TooltipContext()
    .Set("screen", "home")
    .Set("plays", plays);

var director = TooltipDirector.Create(rules, context, new PlayerPrefsProgressStore());
DontDestroyOnLoad(director.gameObject);
```

4. When your game state changes, update the context and call `Check`.

```csharp
plays++;
context.Set("plays", plays);
director.Check();
```

The director shows the best rule whose target is on screen. When `doneWhen` becomes true, the tip ends and counts as seen. If its `conditions` stop being true first, for example the player leaves the screen, it hides and comes back later.

* Leave out `doneWhen` for a tip that only explains something. Tapping the target then finishes it.
* Pass `requires: TargetRequirement.Exists` to start a tip for a target that is off screen. The bubble points toward it.
* Use `director.Hold()` during cutscenes and popups.

The scene needs an EventSystem so the overlay can block taps.

## Targets

Anything that shows on screen can be a target. Every target becomes a rect on screen, worked out again each time it is needed, so moving objects are fine.

**TooltipTarget component.** Add it and set an id. The **Shape** setting decides what area is highlighted.

| Shape | Uses |
|-------|------|
| Auto | The RectTransform if the object is on a canvas, else its renderers, else its colliders, else its position |
| Renderers | All renderers on the object and its children (sprites, meshes, skinned meshes, tilemaps, particles) |
| Colliders | All 3D and 2D colliders on the object and its children |
| Custom Box | A box you set in local space. It turns and scales with the object. Shown as a cyan gizmo. |

**Camera.** Leave it empty and the target uses `Camera.main` if that camera renders the object's layer. If not, it uses the camera with the highest depth that does. Cameras that render to a texture are skipped. For split screen, set the camera yourself.

**From code.** For objects spawned at runtime, attach a target.

```csharp
TooltipTarget.Attach(enemy, "boss");
```

For things with no GameObject of their own, like a tile or a spot on the ground, register a `WorldTarget`.

```csharp
var spot = WorldTarget.AtPoint("dig-here", tilemap.GetCellCenterWorld(cell));
TooltipTargetRegistry.Default.Register(spot);
// later
TooltipTargetRegistry.Default.Unregister(spot);
```

**Your own target.** Implement `ITooltipTarget` and use `ScreenProjection` to turn world points, bounds or boxes into a screen rect.

## Overlay

`TooltipOverlay` dims the screen, cuts a rounded hole around the target and shows a tooltip next to it.

* **Follows the target every frame.** It runs in `LateUpdate`, after cameras move, and uses unscaled time so it still works when the game is paused.
* **Off screen.** When the target leaves the screen, or goes behind the camera, the tooltip sticks to the nearest screen edge and the arrow points toward the target. When it comes back, the tooltip moves back next to it.
* **Placement.** Uses the prompt's `Placement`. If there is no room, it flips to the other side. It always stays inside `Screen.safeArea`.
* **Taps.** Taps inside the hole go to your game as normal, so the player can press the highlighted thing. A tap on the target raises `TargetTapped`. Taps outside are blocked and raise `OutsideTapped`. By default an outside tap also closes the tooltip.
* **Input read directly.** If your game reads input itself instead of through the EventSystem, check `overlay.IsBlocked(screenPosition)` first.
* **Render pipelines.** Tested in Built-in. The hole is a plain UI shader, so URP and HDRP should work too, but they are not tested yet.

To change the look, add `TooltipOverlay` to a GameObject in your scene and set its fields in the Inspector. For your own tooltip, make a prefab with a `TooltipBubble` component and assign it to **Bubble Prefab**. The arrow must point up with its pivot in the center. If you use TextMeshPro, subclass `TooltipBubble` and override `SetText`.

## Samples

In the Package Manager, select **Smart Tooltip** and open the **Samples** tab to import them.

| Sample | Shows |
|--------|-------|
| UI Tooltip | A small fake game menu that uses every part of the director, with a panel that lists each tip that starts and ends, and why. It is built from code, so the whole demo is in one script. |
| 3D World | The director with off screen targets. The tooltip follows a runner off screen, fits a bar turned in depth, and points at treasure behind the camera. Tap the target to finish a tip, or anywhere else to skip it. |

* The scenes use an EventSystem with the old Input Manager module. If your project uses the new Input System only, select the EventSystem and press **Replace with InputSystemUIInputModule**.
* The 3D sample uses Unity's default material. In URP or HDRP, convert it with the render pipeline converter or the objects show pink.

## Concepts

| Type | Role |
|------|------|
| `ITooltipContext` | Read-only game state. Implement it over your own game or use `TooltipContext`. |
| `ICondition` | A yes/no check against the context. Use `Condition.*` or write your own. |
| `TooltipRule` | A prompt, when it may show, when it is done, priority, show once, cooldown and whether the target must be on screen. |
| `TooltipEvaluator` | Returns rules that may show now, best first. |
| `TooltipDirector` | Runs the evaluator and the overlay for you. It checks on events, waits for UI to settle and holds while the game is busy. |
| `ITooltipProgressStore` | Remembers which show-once rules were seen. |
| `ITooltipTarget` | Anything that can report a screen rect. `TooltipTarget` and `WorldTarget` cover UI, 2D and 3D. |

`SmartTooltip.Core` has no Unity references, so rules can be unit-tested outside the editor.

## Roadmap

- A quiet hint style with no dimming, for tips during gameplay
- ScriptableObject and JSON rule authoring

## License

MIT. See [LICENSE.md](LICENSE.md).
