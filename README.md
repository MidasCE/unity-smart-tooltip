# Smart Tooltip for Unity

Tooltips that point at things in your game. A button, a sprite, a 3D character, a spot on the ground. The screen dims, a hole opens around the target, and a bubble with an arrow explains what to do.

Works in 2D and 3D, with any camera, in Built-in, URP and HDRP. Free and open source (MIT).

* **Point at anything** on screen by id: UI, sprites, meshes, colliders, or a plain world position.
* **Follows moving targets** every frame. If the target leaves the screen or goes behind the camera, the bubble sticks to the screen edge and points toward it.
* **Taps pass through the hole**, so the player can press the highlighted button. Taps outside are blocked.
* **Rules decide what to show.** Describe your game state, write rules with conditions and priorities, and the best matching tip is picked for you. Show once tips are remembered.
* **No art needed.** The default bubble is drawn in code. Bring your own prefab when you want your own look.

> Status: early (0.1.0). The rules, targets and overlay all work and are tested. A driver that runs the rules for you automatically is planned.

---

## Install

Needs **Unity 2022.3** or newer.

In Unity: **Window → Package Manager → + → Add package from git URL…** and paste:

```
https://github.com/MidasCE/unity-smart-tooltip.git?path=Packages/com.pattarapol.smart-tooltip
```

To lock a version, add a tag at the end, for example `#v0.1.0`.

Or add it to `Packages/manifest.json` yourself:

```json
"com.pattarapol.smart-tooltip": "https://github.com/MidasCE/unity-smart-tooltip.git?path=Packages/com.pattarapol.smart-tooltip#v0.1.0"
```

Your scene needs an **EventSystem** (Unity adds one when you create any UI). Without it, the overlay can't block or detect taps.

---

## Your first tooltip in 2 minutes

**1. Tag the thing you want to point at.** Select it, click **Add Component → Tooltip Target**, and type an id, for example `play-button`.

It works on a UI button, a `SpriteRenderer`, a 3D mesh, or anything with a collider.

**2. Show a tip from any script.**

```csharp
using SmartTooltip;
using SmartTooltip.Core;
using UnityEngine;

public class FirstTip : MonoBehaviour
{
    TooltipOverlay overlay;

    void Start()
    {
        overlay = TooltipOverlay.Create();

        if (TooltipTargetRegistry.Default.TryResolve("play-button", out var target))
            overlay.Show(new TooltipPrompt("play-button", "Tap here to start!", Placement.Above), target);
    }
}
```

Press Play. That's it.

`TryResolve` only returns a target that is **on screen right now**, so you never start a tip for something the player can't see.

> **Tip:** If the target is on a canvas that is still laying itself out on the first frame, wait one frame (`yield return null`) before showing.

---

## Using rules

For a real game you usually have many tips, and which one makes sense depends on the game state. Rules handle that.

```csharp
using SmartTooltip;
using SmartTooltip.Core;

var rules = new[]
{
    new TooltipRule("first-play",
        new TooltipPrompt("play-button", "Tap Play to start your first game.", Placement.Above),
        new[] { Condition.Compare("plays", ComparisonOperator.Equal, 0) }),

    new TooltipRule("visit-shop",
        new TooltipPrompt("shop-button", "You earned coins! Spend them in the Shop."),
        new[]
        {
            Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1),
            Condition.Not(Condition.Is("visitedShop", true)),
        },
        priority: 10),
};

var evaluator = new TooltipEvaluator(rules, new PlayerPrefsProgressStore());
```

Whenever something changes (a screen opens, a level ends), describe the state and show the best tip:

```csharp
var context = new TooltipContext()
    .Set("plays", plays)
    .Set("visitedShop", visitedShop);

foreach (var rule in evaluator.Candidates(context))
{
    if (!TooltipTargetRegistry.Default.TryResolve(rule.Prompt.TargetId, out var target))
        continue; // target not on screen, try the next rule

    overlay.Show(rule.Prompt, target);
    evaluator.MarkShown(rule);
    break;
}
```

`Candidates` gives you the rules that match right now, **highest priority first**. Rules with the same priority keep the order you wrote them in.

### Conditions

| Condition | True when |
|---|---|
| `Condition.Is("screen", "home")` | the value equals this (same type) |
| `Condition.Has("tutorialDone")` | the key exists |
| `Condition.Compare("level", ComparisonOperator.Less, 5)` | the number compares true. Works with int, long, float and double. A missing key is always false. |
| `Condition.Not(c)` | `c` is false |
| `Condition.All(a, b, ...)` | all are true |
| `Condition.Any(a, b, ...)` | at least one is true |
| `Condition.When(ctx => ...)` | your own code says true |

All conditions in a rule must be true for the rule to match.

### Show once

Rules are **show once** by default. After `MarkShown`, the rule never matches again. For a tip that can repeat, pass `showOnce: false`.

Where "seen" is saved:

| Store | Use for |
|---|---|
| `PlayerPrefsProgressStore()` | most games. Saved on the device. Pass a different key per account if one device can have many players. |
| `InMemoryProgressStore()` | tests and demos. Forgotten on quit. |
| your own `ITooltipProgressStore` | cloud saves. Three methods: `HasSeen`, `MarkSeen`, `ResetAll`. |

### Your own game state

`TooltipContext` is a simple key value bag. If you'd rather not copy values into it, implement `ITooltipContext` over your own state:

```csharp
class GameContext : ITooltipContext
{
    readonly Player player;
    public GameContext(Player player) => this.player = player;

    public bool TryGet<T>(string key, out T value)
    {
        object raw = key switch
        {
            "level" => player.Level,
            "coins" => player.Coins,
            _ => null,
        };

        if (raw is T typed) { value = typed; return true; }
        value = default;
        return false;
    }
}
```

---

## Targets

### The Tooltip Target component

| Shape | Highlights |
|---|---|
| **Auto** (default) | the RectTransform if on a canvas, else the renderers, else the colliders, else the object's position |
| **Renderers** | all renderers on the object and its children |
| **Colliders** | all 3D and 2D colliders on the object and its children |
| **Custom Box** | a box you set in local space. Turns and scales with the object. Shown as a cyan gizmo when selected. |

**Camera:** leave it empty and it picks `Camera.main` if that camera sees the object's layer, otherwise the highest depth camera that does. Cameras that render to a texture (minimaps) are skipped. For split screen, set the camera yourself.

Several objects can share an id. The newest one that is on screen is used.

### From code

```csharp
// Something spawned at runtime
TooltipTarget.Attach(boss, "boss");

// Something with no GameObject: a tile, a spot on the ground
var spot = WorldTarget.AtPoint("dig-here", tilemap.GetCellCenterWorld(cell));
TooltipTargetRegistry.Default.Register(spot);
// when done
TooltipTargetRegistry.Default.Unregister(spot);

// A moving area
var area = new WorldTarget("drop-zone", () => dropZone.bounds);
```

If you add renderers or colliders deep inside a target at runtime, call `target.Refresh()`.

### Your own target type

Anything that can say where it is on screen can be a target. Return a rect in screen pixels, (0, 0) at the bottom left. Return `false` only if the target is gone right now. A rect that is off screen is fine, the bubble will point at it.

```csharp
sealed class CardInHandTarget : ITooltipTarget
{
    readonly HandView hand;
    readonly int index;

    public CardInHandTarget(HandView hand, int index) { this.hand = hand; this.index = index; }

    public string Id => "card-" + index;

    public bool TryGetScreenRect(out Rect rect)
    {
        rect = default;
        var card = hand.CardAt(index);
        return card != null && ScreenProjection.TryProjectBounds(hand.Camera, card.Bounds, out rect);
    }
}
```

`ScreenProjection` has helpers for points, bounds, local boxes and UI rects. It also handles things behind the camera for you.

---

## The overlay

### Behaviour

* **Follows** the target every frame, after cameras move. Uses unscaled time, so it works while the game is paused.
* **Off screen:** the bubble sticks to the nearest edge and the arrow points toward the target. It slides back when the target returns.
* **Placement:** uses the prompt's `Placement` (Auto, Above, Below, Left, Right). If there is no room, it flips to the other side. It stays inside `Screen.safeArea` (notches are fine).
* **Switching:** calling `Show` while a tip is up slides the hole and bubble to the new target.
* **Closing:** `overlay.Hide()`. By default, tapping outside the hole closes it too. If the target is disabled or destroyed, it closes by itself.

### Events and checks

```csharp
overlay.OutsideTapped += () => Debug.Log("Player tapped outside");
overlay.Hidden += ShowNextTip;              // after the fade out ends

if (overlay.IsShowing) { ... }

// Only if your game reads input directly instead of through the EventSystem:
if (overlay.IsBlocked(Input.mousePosition)) return;
```

### Changing the look

`TooltipOverlay.Create()` uses the default settings. To change them, add the **Tooltip Overlay** component to a GameObject in your scene (or a prefab) and set it in the Inspector:

| Field | Default | What it does |
|---|---|---|
| Dim Color | black, 60% | color of the dark layer |
| Hole Padding | 12 | space around the target |
| Hole Radius | 16 | rounded corners of the hole |
| Hole Softness | 2 | edge blur |
| Bubble Prefab | empty | your own bubble, see below |
| Gap | 6 | space between hole and arrow |
| Screen Margin | 16 | space the bubble keeps from the screen edge |
| Reference Resolution | 1920 × 1080 | for the overlay's canvas scaler |
| Sorting Order | 32000 | draws above your own canvases |
| Close On Outside Tap | on | |
| Transition Time | 0.25 s | fade and slide speed |

Mark it `DontDestroyOnLoad` if it should live across scenes.

### Your own bubble

Make a UI prefab with a **Tooltip Bubble** component. Assign its **Arrow** (an image pointing **up**, pivot in the center; the overlay turns it) and **Text**. Then drag the prefab into **Bubble Prefab**.

Using TextMeshPro? Subclass and override `SetText`. The overlay reads the bubble's size every frame, so you only need to size it:

```csharp
using SmartTooltip;
using TMPro;
using UnityEngine;

public class TmpBubble : TooltipBubble
{
    [SerializeField] TMP_Text label;
    [SerializeField] float maxTextWidth = 560f;
    [SerializeField] Vector2 margin = new Vector2(28f, 20f);

    public override void SetText(string value)
    {
        label.text = value;
        var width = Mathf.Min(label.GetPreferredValues(value).x, maxTextWidth);
        var height = label.GetPreferredValues(value, width, 0f).y;
        label.rectTransform.sizeDelta = new Vector2(width, height);
        Rect.sizeDelta = new Vector2(width, height) + margin * 2f;
    }
}
```

---

## Samples

In the Package Manager, select **Smart Tooltip**, open the **Samples** tab, and press **Import**.

| Sample | Shows |
|---|---|
| **UI Tooltip** | Rules and a context. Three buttons. Tap the highlighted one and the next tip appears. |
| **3D World** | Overlay only. An orbiting camera, a runner that leaves the screen, a bar turned in depth, and treasure that ends up behind the camera. Tap anywhere for the next tip. |

* The scenes use the old Input Manager on their EventSystem. If your project uses only the new Input System, select the EventSystem and press **Replace with InputSystemUIInputModule**.
* The 3D sample uses Unity's default material. In URP or HDRP, convert it with the Render Pipeline Converter, or the objects show pink.

---

## Troubleshooting

| Problem | Likely cause |
|---|---|
| Nothing shows | `TryResolve` returned false. The target is disabled, has no id, or is not on screen yet. Wait a frame after loading. |
| Taps go through everywhere | No EventSystem in the scene (there is a warning in the console). |
| The hole is in the wrong place on a 3D object | The wrong camera is picked. Set **World Camera** on the target. |
| The hole is too big on a rotated object | Use **Custom Box**, or a Box collider with **Colliders** shape. A flat (screen) rotation always needs an upright rect. |
| `Is` never matches | Types must match exactly, `3` (int) is not `3L` (long). Use `Compare` for numbers. |
| A tip shows only once | That is the default. Use `showOnce: false`, or `ResetAll()` on the store to test again. |

---

## Working on this repo

This repo is a Unity project. The package itself lives in `Packages/com.pattarapol.smart-tooltip`.

```
Packages/com.pattarapol.smart-tooltip/
├── Runtime/Core       rules, no Unity references
├── Runtime/Targets    turning objects into screen rects
├── Runtime/Overlay    dim, hole, bubble, layout
├── Runtime/Progress   PlayerPrefs store
├── Tests/Editor       rule tests
├── Tests/Runtime      PlayMode tests
└── Samples~           the two samples
Docs/How It Works.md   how everything works inside
```

The samples live in `Samples~`, which Unity hides. To open and edit them in this project, make a local link (it is gitignored):

```bash
cd Assets && ln -s "../Packages/com.pattarapol.smart-tooltip/Samples~" SmartTooltipSamples
```

Run tests from **Window → General → Test Runner** (both EditMode and PlayMode).

Curious how it works inside? Read [Docs/How It Works.md](Docs/How%20It%20Works.md).

## License

MIT. See [LICENSE.md](Packages/com.pattarapol.smart-tooltip/LICENSE.md).
