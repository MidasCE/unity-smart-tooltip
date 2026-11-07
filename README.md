# Smart Tooltip for Unity

Tooltips that point at things in your game. A button, a sprite, a 3D character, a spot on the ground. The screen dims, a hole opens around the target, and a bubble with an arrow explains what to do.

Works in 2D and 3D, with any camera. Tested in the Built-in render pipeline. URP and HDRP should work too, because the overlay only uses a plain UI shader, but they are not tested yet. Free and open source (MIT).

<p align="center">
  <img src="https://github.com/user-attachments/assets/4747770c-a6c9-4df8-81a9-18a3ca3a8919" width="720" alt="A tip follows a runner off screen, then points at treasure behind the camera">
<img width="720" src="https://github.com/user-attachments/assets/96d37b8a-2861-43c0-8722-591a26b8a901" alt="A tooltip bind to UI in 2D"/>
</p>

* **Point at anything** by id. UI, sprites, meshes, colliders, or a plain spot in the world.
* **Follows moving targets** every frame. If the target leaves the screen or goes behind the camera, the bubble sticks to the screen edge and points toward it.
* **Taps pass through the hole**, so the player can press the highlighted button. Taps outside are blocked.
* **Rules decide what to show.** Describe your game state, write rules with conditions and priorities, and the best matching tip is picked for you. Show once tips are remembered.
* **A director runs it for you.** Call `Check()` when something happens. It waits for the UI to settle, moves on when the player has done what the tip asked, and pauses during cutscenes.
* **No art needed.** The default bubble is drawn in code. Bring your own prefab when you want your own look.

> Status is early (0.1.0). The rules, targets, overlay and director all work and are tested.

---

## Install

Needs **Unity 2022.3** or newer.

In Unity, open **Window → Package Manager → + → Add package from git URL…** and paste this.

```
https://github.com/MidasCE/unity-smart-tooltip.git?path=Packages/com.pattarapol.smart-tooltip
```

To lock a version, add a tag at the end, for example `#v0.1.0`.

Or add it to `Packages/manifest.json` yourself.

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

> **Tip.** If the target is on a canvas that is still laying itself out on the first frame, wait one frame (`yield return null`) before showing. The [director](#the-director) does this for you.

---

## Using rules

A real game has many tips, and which one makes sense depends on what the player is doing. Rules handle that.

Each rule answers two questions.

* **When may it show?** These are its `conditions`. All of them must be true.
* **When is it done?** This is its `doneWhen` condition. Once it is true, the player has done what the tip asked and the tip goes away for good.

```csharp
using SmartTooltip;
using SmartTooltip.Core;

var rules = new[]
{
    new TooltipRule("first-play",
        new TooltipPrompt("play-button", "Tap Play to start your first game.", Placement.Above),
        conditions: new[] { Condition.Is("screen", "home") },
        doneWhen: Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1)),

    new TooltipRule("visit-shop",
        new TooltipPrompt("shop-button", "You earned coins! Spend them in the Shop."),
        conditions: new[]
        {
            Condition.Is("screen", "home"),
            Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1),
        },
        doneWhen: Condition.Is("visitedShop", true),
        priority: 10),
};

var context = new TooltipContext()
    .Set("screen", "home")
    .Set("plays", plays)
    .Set("visitedShop", visitedShop);

var director = TooltipDirector.Create(rules, context, new PlayerPrefsProgressStore());
```

Whenever something changes in your game, update the context and call `Check`. A screen opening, a level ending and a button press are all good moments.

```csharp
plays++;
context.Set("plays", plays);
director.Check();
```

The director shows the best rule that may show and whose target is on screen. Higher `priority` wins. Rules with the same priority keep the order you wrote them in.

Keeping the two questions apart matters. If the player opens Settings instead of the Shop, `screen` changes and "visit-shop" may no longer show. That does not mean the player visited the Shop, so the tip hides and comes back the next time they are home.

### Tips that only explain something

Some tips have nothing to do, like "This is your coin counter". Leave out `doneWhen`, and the tip is done when the player taps the highlighted thing.

```csharp
new TooltipRule("coins",
    new TooltipPrompt("coin-counter", "These are your coins."),
    conditions: new[] { Condition.Is("screen", "home") })
```

### Targets that are off screen

By default a tip only starts when its target is on screen. That suits UI. In a 2D or 3D world you often want to point at something the player can't see yet. Pass `requires: TargetRequirement.Exists`, and the tip starts as long as the target exists. The bubble sticks to the screen edge and points toward it.

```csharp
new TooltipRule("find-blacksmith",
    new TooltipPrompt("blacksmith", "Talk to the blacksmith to fix your sword."),
    conditions: new[] { Condition.Is("swordBroken", true) },
    doneWhen: Condition.Is("talkedToBlacksmith", true),
    requires: TargetRequirement.Exists)
```

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

### Show once

Rules are **show once** by default. After the player finishes or dismisses a tip, it never shows again.

For a tip that should come back, pass `showOnce: false`. Add a `cooldown` in seconds too, so it doesn't come back the moment it closes.

```csharp
new TooltipRule("daily-reward",
    new TooltipPrompt("reward-button", "Your daily reward is ready!"),
    conditions: new[] { Condition.Is("rewardReady", true) },
    doneWhen: Condition.Is("rewardClaimed", true),
    showOnce: false, cooldown: 60)
```

The progress store remembers which tips were seen.

| Store | Use for |
|---|---|
| `PlayerPrefsProgressStore()` | most games. Saved on the device. Pass a different key per account if one device can have many players. |
| `InMemoryProgressStore()` | tests and demos. Forgotten on quit. |
| your own `ITooltipProgressStore` | cloud saves. It has three methods, `HasSeen`, `MarkSeen` and `ResetAll`. |

### Your own game state

`TooltipContext` is a simple bag of keys and values. If you'd rather not copy values into it, implement `ITooltipContext` over your own state.

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

## The director

`TooltipDirector` runs the rules for you. You tell it that something changed, and it decides what to show.

### Check

`director.Check()` is safe to call as often as you like, from any event.

* Calls in the same frame become one check. It runs **next frame**, after new UI has laid itself out.
* If the best tip's target isn't there yet, for example a panel that is still sliding in, it waits up to `SettleTime` (half a second). After that it shows the next best tip it can.
* A tip that is showing stays until it ends. A higher priority rule does not push it away.

The director also checks by itself when a tip ends, when a hold is released and when a scene loads. For state that has no events, like a health bar, set `director.PollInterval = 0.5f` and it checks on a timer.

### How a tip ends

| Reason | When | Counts as seen |
|---|---|---|
| `Completed` | its `doneWhen` became true, or the player tapped the target of a tip without `doneWhen` | yes |
| `Dismissed` | the player tapped outside the hole | yes |
| `TargetLost` | the target was disabled or destroyed | no, it can show again |
| `Interrupted` | its `conditions` stopped being true, a hold started, `Stop()` was called, or other code hid the overlay | no, it can show again |

A tip is saved as seen when it **ends**, not when it shows. If the game quits while a tip is up, the player sees it again next time.

### Hold

When the game is busy with a cutscene, a reward popup or loading, hold the director. The current tip hides, and it comes back once every hold is released. If the tip was already done when the hold started, it ends as `Completed` instead. That happens when the same tap that finishes a tip also opens a popup.

```csharp
using (director.Hold())
{
    yield return PlayCutscene();
}

// Or keep the hold and release it later.
var hold = director.Hold();
...
hold.Dispose();
```

### Events

```csharp
director.Started += rule => Debug.Log("Showing " + rule.Id);
director.Ended += (rule, reason) => analytics.Log("tip", rule.Id, reason.ToString());

if (director.Active != null) { ... }   // the rule showing now
```

### Your own overlay

`Create` makes an overlay with the default look. To use your own look, put a **Tooltip Overlay** in your scene and pass it in.

```csharp
var director = TooltipDirector.Create(rules, context, store, myOverlay);
```

The director destroys the overlay it made, but never one you passed in. Don't call `Show` or `Hide` on that overlay yourself. Use `Stop()` or `Hold()` on the director instead.

### Driving it yourself

You don't have to use the director. `TooltipEvaluator` gives you the rules that may show, best first, and you show them yourself.

```csharp
var evaluator = new TooltipEvaluator(rules, store);

foreach (var rule in evaluator.Candidates(context))
{
    if (!TooltipTargetRegistry.Default.TryResolve(rule.Prompt.TargetId, out var target))
        continue; // target not on screen, try the next rule

    overlay.Show(rule.Prompt, target);
    break;
}

// Later, when the player has done it.
evaluator.MarkShown(rule);
```

`TryResolve` only finds targets on screen. `TryFind` also finds them off screen.

---

## Targets

### The Tooltip Target component

| Shape | Highlights |
|---|---|
| **Auto** (default) | the RectTransform if on a canvas, else the renderers, else the colliders, else the object's position |
| **Renderers** | all renderers on the object and its children |
| **Colliders** | all 3D and 2D colliders on the object and its children |
| **Custom Box** | a box you set in local space. Turns and scales with the object. Shown as a cyan gizmo when selected. |

**Camera.** Leave it empty and it picks `Camera.main` if that camera sees the object's layer, otherwise the highest depth camera that does. Cameras that render to a texture (minimaps) are skipped. For split screen, set the camera yourself.

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
* **Off screen.** The bubble sticks to the nearest edge and the arrow points toward the target. It slides back when the target returns.
* **Placement.** It uses the prompt's `Placement` (Auto, Above, Below, Left, Right). If there is no room, it flips to the other side. It stays inside `Screen.safeArea` (notches are fine).
* **Switching.** Calling `Show` while a tip is up slides the hole and bubble to the new target.
* **Closing.** Call `overlay.Hide()`. By default, tapping outside the hole closes it too. If the target is disabled or destroyed, it closes by itself.

### Events and checks

```csharp
overlay.OutsideTapped += () => Debug.Log("Player tapped outside");
overlay.TargetTapped += () => Debug.Log("Player tapped the target");
overlay.Hidden += reason => ShowNextTip();  // after the fade out. Requested, OutsideTap or TargetLost

if (overlay.IsShowing) { ... }

// Only if your game reads input directly instead of through the EventSystem.
if (overlay.IsBlocked(Input.mousePosition)) return;
```

### Changing the look

`TooltipOverlay.Create()` uses the default settings. To change them, add the **Tooltip Overlay** component to a GameObject in your scene (or a prefab) and set it in the Inspector.

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

Using TextMeshPro? Subclass and override `SetText`. The overlay reads the bubble's size every frame, so you only need to size it.

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
| **UI Tooltip** | A small fake game menu that uses every part of the director. A panel on the right lists each tip that starts and ends, and why. |
| **3D World** | The director with `TargetRequirement.Exists`. An orbiting camera, a runner that leaves the screen, a bar turned in depth, and treasure that ends up behind the camera. Tap the target to finish a tip, or anywhere else to skip it. |

The UI sample is built from code when the scene starts, so the whole demo is in one script you can read top to bottom. This is what it shows.

| Try this | You see |
|---|---|
| Tap Play | The first tip is done. A popup holds the director, and the next tip waits until you close it. |
| Close the popup | Two tips match. The Shop tip wins because it has a higher priority. |
| Go to the Shop | The Shop tip is done, and the buy tip shows. It only shows while you have 10 coins. |
| Go back Home | The coins tip shows. It has no `doneWhen`, so tapping the coins finishes it. |
| Tap outside any tip | `Dismissed` |
| Press New day | The reward panel slides in, and the director waits for it instead of showing a lower tip. |
| Let the offer run out | `Interrupted`. The tip may no longer show, but it isn't seen, so it shows again on the next new day. |
| Close the reward with X | `TargetLost` |
| Claim, then press New day again | The reward tip is repeatable with a 10 second cooldown. It comes back once the cooldown ends. |
| Stop and start Play mode | Seen tips stay seen, because the sample uses `PlayerPrefsProgressStore`. Press Reset demo to start over. |

* Tapping the target works with both the old Input Manager and the new Input System.
* The scenes use the old Input Manager on their EventSystem. If your project uses only the new Input System, select the EventSystem and press **Replace with InputSystemUIInputModule**.
* The 3D sample uses Unity's default material. In URP or HDRP, convert it with the Render Pipeline Converter, or the objects show pink.

---

## Not done yet

* **Every tip dims the screen and blocks taps outside the hole.** That suits menus and tutorials. It is wrong for a hint in the middle of gameplay, like "drink a potion" during a fight. A quiet hint style with no dimming is planned.
* **One tip at a time.**
* **No priority interrupts.** A more important tip waits until the current one ends.

---

## Troubleshooting

| Problem | Likely cause |
|---|---|
| Nothing shows | `TryResolve` returned false. The target is disabled, has no id, or is not on screen yet. Wait a frame after loading. With the director, make sure you called `Check()` after changing the state, and that it isn't held. |
| A tip comes straight back after it closes | It is repeatable and still matches. Add a `cooldown`. |
| A tip never goes away | Its `doneWhen` never becomes true. Check that your game updates that value and calls `Check()`. |
| A tip comes back after the player did it | Its `conditions` changed before its `doneWhen` became true, so it ended as `Interrupted`. Make sure the action also sets the `doneWhen` value. |
| A tip for a world object never starts | Its target is off screen. Use `requires: TargetRequirement.Exists`. |
| Taps go through everywhere | No EventSystem in the scene (there is a warning in the console). |
| The hole is in the wrong place on a 3D object | The wrong camera is picked. Set **World Camera** on the target. |
| The hole is too big on a rotated object | Use **Custom Box**, or a Box collider with **Colliders** shape. A flat (screen) rotation always needs an upright rect. |
| `Is` never matches | Types must match exactly, `3` (int) is not `3L` (long). Use `Compare` for numbers. |
| A tip shows only once | That is the default. Use `showOnce: false`, or `ResetAll()` on the store to test again. |
| Tapping the target does nothing | The tip has a `doneWhen`, so the tap doesn't finish it by itself. Or the tap started outside the hole. |

---

## Working on this repo

This repo is a Unity project. The package itself lives in `Packages/com.pattarapol.smart-tooltip`.

```
Packages/com.pattarapol.smart-tooltip/
├── Runtime/Core       rules, no Unity references
├── Runtime/Targets    turning objects into screen rects
├── Runtime/Overlay    dim, hole, bubble, layout
├── Runtime/Director   runs the rules and the overlay for you
├── Runtime/Progress   PlayerPrefs store
├── Tests/Editor       rule tests
├── Tests/Runtime      PlayMode tests
└── Samples~           the two samples
Docs/How It Works.md   how everything works inside
```

The samples live in `Samples~`, which Unity hides. To open and edit them in this project, make a local link. Git ignores it.

```bash
cd Assets && ln -s "../Packages/com.pattarapol.smart-tooltip/Samples~" SmartTooltipSamples
```

Run tests from **Window → General → Test Runner** (both EditMode and PlayMode).

Curious how it works inside? Read [Docs/How It Works.md](Docs/How%20It%20Works.md).

## License

MIT. See [LICENSE.md](Packages/com.pattarapol.smart-tooltip/LICENSE.md).
