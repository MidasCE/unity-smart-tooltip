# Smart Tooltip

Rule-driven tooltips and highlights for Unity. Tag anything (a UI button, a 2D sprite, a 3D mesh) with an id, describe when a tip should appear, and let the evaluator pick what to show.

> Status: early. Rules, targets and the overlay work. A driver that runs rules for you is still to come.

## Install

Package Manager → **Add package from git URL…**

```
https://github.com/MidasCE/unity-smart-tooltip.git?path=Packages/com.pattarapol.smart-tooltip
```

Requires Unity 2022.3 or newer.

## Quick start

1. Add a **TooltipTarget** component to the object you want to point at and give it an id, for example `play-button`. See [Targets](#targets).

2. Describe your rules:

```csharp
using SmartTooltip;
using SmartTooltip.Core;

var rules = new[]
{
    new TooltipRule("first-play",
        new TooltipPrompt("play-button", "Tap here to start!", Placement.Above),
        new[]
        {
            Condition.Is("screen", "home"),
            Condition.Compare("level", ComparisonOperator.Less, 2),
        }),
};

var evaluator = new TooltipEvaluator(rules, new PlayerPrefsProgressStore());
```

3. Create the overlay once, for example when the game starts:

```csharp
var overlay = TooltipOverlay.Create();
DontDestroyOnLoad(overlay.gameObject);
```

4. When your game state changes, build a context and show the best rule whose target is on screen:

```csharp
var context = new TooltipContext()
    .Set("screen", "home")
    .Set("level", player.Level);

foreach (var rule in evaluator.Candidates(context))
{
    if (!TooltipTargetRegistry.Default.TryResolve(rule.Prompt.TargetId, out var target))
        continue; // not on screen, try the next rule

    overlay.Show(rule.Prompt, target);
    evaluator.MarkShown(rule);
    break;
}
```

The scene needs an EventSystem so the overlay can block taps.

## Targets

Anything that shows on screen can be a target. Every target becomes a rect on screen, worked out again each time it is needed, so moving objects are fine.

**TooltipTarget component.** Add it and set an id. The **Shape** setting decides what area is highlighted:

| Shape | Uses |
|-------|------|
| Auto | The RectTransform if the object is on a canvas, else its renderers, else its colliders, else its position |
| Renderers | All renderers on the object and its children (sprites, meshes, skinned meshes, tilemaps, particles) |
| Colliders | All 3D and 2D colliders on the object and its children |
| Custom Box | A box you set in local space. It turns and scales with the object. Shown as a cyan gizmo. |

**Camera.** Leave it empty and the target uses `Camera.main` if that camera renders the object's layer. If not, it uses the camera with the highest depth that does. Cameras that render to a texture are skipped. For split screen, set the camera yourself.

**From code.** For objects spawned at runtime:

```csharp
TooltipTarget.Attach(enemy, "boss");
```

For things with no GameObject of their own, like a tile or a spot on the ground:

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
* **Taps.** Taps inside the hole go to your game as normal, so the player can press the highlighted thing. Taps outside are blocked and raise `OutsideTapped`. By default an outside tap also closes the tooltip.
* **Input read directly.** If your game reads input itself instead of through the EventSystem, check `overlay.IsBlocked(screenPosition)` first.
* **Render pipelines.** The hole is a simple UI shader, so it works in Built-in, URP and HDRP.

To change the look, add `TooltipOverlay` to a GameObject in your scene and set its fields in the Inspector. For your own tooltip, make a prefab with a `TooltipBubble` component and assign it to **Bubble Prefab**. The arrow must point up with its pivot in the center. If you use TextMeshPro, subclass `TooltipBubble` and override `SetText`.

## Samples

In the Package Manager, select **Smart Tooltip** and open the **Samples** tab to import them.

| Sample | Shows |
|--------|-------|
| UI Tooltip | Rules and a context. Tap the highlighted button and the next tip appears. |
| 3D World | Overlay only, no rules. The tooltip follows a runner off screen, fits a bar turned in depth, and points at treasure behind the camera. Tap anywhere for the next tip. |

Notes:

* The scenes use an EventSystem with the old Input Manager module. If your project uses the new Input System only, select the EventSystem and press **Replace with InputSystemUIInputModule**.
* The 3D sample uses Unity's default material. In URP or HDRP, convert it with the render pipeline converter or the objects show pink.

## Concepts

| Type | Role |
|------|------|
| `ITooltipContext` | Read-only game state. Implement it over your own game or use `TooltipContext`. |
| `ICondition` | A yes/no check against the context. Use `Condition.*` or write your own. |
| `TooltipRule` | Conditions + prompt + priority + show-once. |
| `TooltipEvaluator` | Returns rules that may show now, best first. |
| `ITooltipProgressStore` | Remembers which show-once rules were seen. |
| `ITooltipTarget` | Anything that can report a screen rect. `TooltipTarget` and `WorldTarget` cover UI, 2D and 3D. |

`SmartTooltip.Core` has no Unity references, so rules can be unit-tested outside the editor.

## Roadmap

- Runtime driver (re-evaluate on events, wait for UI to settle, busy gate)
- ScriptableObject and JSON rule authoring

## License

MIT, see [LICENSE.md](LICENSE.md).
