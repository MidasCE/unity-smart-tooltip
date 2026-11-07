using System;
using System.Collections.Generic;
using SmartTooltip.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SmartTooltip.Samples
{
    /// <summary>
    /// A tiny fake game menu that shows every part of the director.
    /// The panel on the right lists what the director did and why.
    /// </summary>
    public sealed class UiTooltipDemo : MonoBehaviour
    {
        const string StateKey = "smarttooltip.sample.ui.state";
        const string SeenKey = "smarttooltip.sample.ui.seen";
        const float OfferSeconds = 8f;
        const int MaxLogLines = 10;

        static readonly Color Accent = new Color(0.96f, 0.62f, 0.2f);
        static readonly Color Muted = new Color(0.3f, 0.34f, 0.45f);
        static readonly Color Selected = new Color(0.25f, 0.5f, 0.85f);

        [Serializable]
        sealed class SaveData
        {
            public int plays;
            public int coins;
            public bool visitedShop;
            public bool hasSword;
        }

        readonly TooltipContext context = new TooltipContext();
        readonly List<string> logLines = new List<string>();
        PlayerPrefsProgressStore progress;
        TooltipDirector director;
        SaveData save;

        string screen = "home";
        bool rewardReady;
        bool rewardClaimed;
        float offerLeft;
        IDisposable popupHold;

        GameObject homeScreen;
        GameObject shopScreen;
        Button homeTab;
        Button shopTab;
        Text coinsLabel;
        Text playsLabel;
        Button buyButton;
        SlidePanel rewardPanel;
        Text offerLabel;
        GameObject popup;
        Text logLabel;

        void Start()
        {
            save = Load();
            progress = new PlayerPrefsProgressStore(SeenKey);
            BuildUi();
            UpdateContext();

            director = TooltipDirector.Create(Rules(), context, progress);
            // The reward cooldown has no event that ends it, so check on a timer too.
            director.PollInterval = 1f;
            director.Started += rule => Log($"show  {rule.Id}");
            director.Ended += (rule, reason) => Log($"end   {rule.Id}, {Explain(reason)}");

            Log(Note("Tips you already saw are remembered, even after you stop Play mode. Press Reset demo to start over."));
        }

        static TooltipRule[] Rules() => new[]
        {
            // Repeatable, so it has a cooldown. Highest priority, so it wins over the others.
            new TooltipRule("daily-reward",
                new TooltipPrompt("reward", "A free gift! Claim it before it's gone.", Placement.Left),
                conditions: new[] { Condition.Is("screen", "home"), Condition.Is("rewardReady", true) },
                doneWhen: Condition.Is("rewardClaimed", true),
                priority: 20, showOnce: false, cooldown: 10f),

            new TooltipRule("first-play",
                new TooltipPrompt("play", "Tap Play to start your first game.", Placement.Above),
                conditions: new[] { Condition.Is("screen", "home") },
                doneWhen: Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1)),

            new TooltipRule("visit-shop",
                new TooltipPrompt("shop-tab", "You earned coins! Spend them in the Shop.", Placement.Above),
                conditions: new[] { Condition.Is("screen", "home"), Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1) },
                doneWhen: Condition.Is("visitedShop", true),
                priority: 10),

            new TooltipRule("buy-sword",
                new TooltipPrompt("buy-sword", "Buy the sword with your coins.", Placement.Below),
                conditions: new[] { Condition.Is("screen", "shop"), Condition.Compare("coins", ComparisonOperator.GreaterOrEqual, 10) },
                doneWhen: Condition.Is("hasSword", true)),

            // No done condition. It only explains something, so tapping the coins finishes it.
            new TooltipRule("coins",
                new TooltipPrompt("coins", "These are your coins. Tap them to continue.", Placement.Below),
                conditions: new[] { Condition.Is("screen", "home"), Condition.Compare("plays", ComparisonOperator.GreaterOrEqual, 1) }),
        };

        void Update()
        {
            if (!rewardReady || rewardClaimed)
                return;

            offerLeft -= Time.unscaledDeltaTime;
            offerLabel.text = $"Ends in {Mathf.CeilToInt(Mathf.Max(offerLeft, 0f))} s";
            if (offerLeft <= 0f)
            {
                Log(Note("The offer ran out before you claimed it."));
                rewardReady = false;
                rewardPanel.Close();
                StateChanged();
            }
        }

        void OnPlay()
        {
            save.plays++;
            save.coins += 10;
            // Update the state before the hold, so a tip that is now done ends as Completed.
            StateChanged();
            popup.SetActive(true);
            popupHold = director.Hold();
            Log(Note("A popup opened, so the director is held."));
        }

        void OnPopupClosed()
        {
            popup.SetActive(false);
            popupHold?.Dispose();
            popupHold = null;
            Log(Note("The popup closed, so the hold is released."));
        }

        void ShowScreen(string name)
        {
            screen = name;
            if (name == "shop")
                save.visitedShop = true;
            StateChanged();
        }

        void OnBuy()
        {
            if (save.hasSword || save.coins < 10)
                return;
            save.coins -= 10;
            save.hasSword = true;
            StateChanged();
        }

        void OnNewDay()
        {
            rewardReady = true;
            rewardClaimed = false;
            offerLeft = OfferSeconds;
            rewardPanel.Open();
            Log(Note("New day. The reward panel slides in, and the director waits for it."));
            StateChanged();
        }

        void OnClaim()
        {
            rewardClaimed = true;
            save.coins += 5;
            rewardPanel.Close();
            StateChanged();
        }

        void OnCloseReward()
        {
            rewardReady = false;
            rewardPanel.CloseNow();
            Log(Note("You closed the reward panel, so the tip's target is gone."));
            StateChanged();
        }

        void OnReset()
        {
            director.Stop();
            progress.ResetAll();
            save = new SaveData();
            rewardReady = false;
            rewardPanel.CloseNow();
            if (popupHold != null)
                OnPopupClosed();
            logLines.Clear();
            Log(Note("Reset. Everything is new again."));
            ShowScreen("home");
        }

        void StateChanged()
        {
            PlayerPrefs.SetString(StateKey, JsonUtility.ToJson(save));
            UpdateContext();
            director.Check();
        }

        void UpdateContext()
        {
            context.Set("screen", screen)
                .Set("plays", save.plays)
                .Set("coins", save.coins)
                .Set("visitedShop", save.visitedShop)
                .Set("hasSword", save.hasSword)
                .Set("rewardReady", rewardReady)
                .Set("rewardClaimed", rewardClaimed);

            homeScreen.SetActive(screen == "home");
            shopScreen.SetActive(screen == "shop");
            homeTab.image.color = screen == "home" ? Selected : Muted;
            shopTab.image.color = screen == "shop" ? Selected : Muted;
            coinsLabel.text = $"{save.coins} coins";
            playsLabel.text = $"Games played {save.plays}";
            buyButton.SetText(save.hasSword ? "You own it" : "Buy for 10 coins");
        }

        static SaveData Load()
        {
            var json = PlayerPrefs.GetString(StateKey, "");
            return string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
        }

        static string Explain(TooltipEndReason reason) => reason switch
        {
            TooltipEndReason.Completed => "Completed. You did it, saved as seen.",
            TooltipEndReason.Dismissed => "Dismissed. You tapped outside, saved as seen.",
            TooltipEndReason.TargetLost => "TargetLost. Its target went away, it can show again.",
            _ => "Interrupted. It can't show right now, it can show again."
        };

        static string Note(string text) => $"<color=#9AA3B8>{text}</color>";

        void Log(string line)
        {
            logLines.Add(line);
            if (logLines.Count > MaxLogLines)
                logLines.RemoveAt(0);
            logLabel.text = string.Join("\n", logLines);
        }

        void BuildUi()
        {
            var root = DemoUi.CreateCanvas("Demo Canvas").transform;
            DemoUi.Panel("Background", root, new Color(0.12f, 0.14f, 0.2f)).rectTransform.Fill();

            var game = DemoUi.Rect("Game", root).Fill(right: 600f);
            BuildTopBar(game);
            BuildHome(game);
            BuildShop(game);
            BuildTabs(game);
            BuildSidePanel(root);
            BuildRewardPanel(root);
            BuildPopup(root);
        }

        void BuildTopBar(Transform game)
        {
            var bar = DemoUi.Panel("Top Bar", game, new Color(0.17f, 0.2f, 0.28f)).rectTransform;
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 120f);

            DemoUi.Label("Title", bar, "My Game", 48, TextAnchor.MiddleLeft).rectTransform.Fill(left: 40f);

            var coins = DemoUi.Panel("Coins", bar, new Color(0.85f, 0.7f, 0.2f));
            coins.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(240f, 76f));
            coinsLabel = DemoUi.Label("Label", coins.transform, "", 34, TextAnchor.MiddleCenter, Color.black);
            coinsLabel.rectTransform.Fill();
            TooltipTarget.Attach(coins.gameObject, "coins");
        }

        void BuildHome(Transform game)
        {
            var home = DemoUi.Rect("Home", game).Fill(bottom: 140f, top: 120f);
            homeScreen = home.gameObject;

            var play = DemoUi.Button("Play", home, "Play", Accent, OnPlay);
            ((RectTransform)play.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(380f, 150f));
            play.SetText("Play");
            play.GetComponentInChildren<Text>().fontSize = 56;
            TooltipTarget.Attach(play.gameObject, "play");

            playsLabel = DemoUi.Label("Plays", home, "", 30);
            playsLabel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(600f, 50f));
        }

        void BuildShop(Transform game)
        {
            var shop = DemoUi.Rect("Shop", game).Fill(bottom: 140f, top: 120f);
            shopScreen = shop.gameObject;

            var card = DemoUi.Panel("Sword Card", shop, new Color(0.2f, 0.24f, 0.33f)).rectTransform;
            card.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 380f));
            DemoUi.Label("Name", card, "Sword", 52).rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(560f, 70f));
            DemoUi.Label("About", card, "A shiny new sword.", 32).rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(560f, 50f));

            buyButton = DemoUi.Button("Buy", card, "", Accent, OnBuy);
            ((RectTransform)buyButton.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(400f, 110f));
            TooltipTarget.Attach(buyButton.gameObject, "buy-sword");
        }

        void BuildTabs(Transform game)
        {
            var tabs = DemoUi.Rect("Tabs", game);
            tabs.anchorMin = Vector2.zero;
            tabs.anchorMax = new Vector2(1f, 0f);
            tabs.pivot = new Vector2(0.5f, 0f);
            tabs.sizeDelta = new Vector2(0f, 140f);

            homeTab = DemoUi.Button("Home Tab", tabs, "Home", Muted, () => ShowScreen("home"));
            Half((RectTransform)homeTab.transform, 0f);
            shopTab = DemoUi.Button("Shop Tab", tabs, "Shop", Muted, () => ShowScreen("shop"));
            Half((RectTransform)shopTab.transform, 0.5f);
            TooltipTarget.Attach(shopTab.gameObject, "shop-tab");

            static void Half(RectTransform rect, float from)
            {
                rect.anchorMin = new Vector2(from, 0f);
                rect.anchorMax = new Vector2(from + 0.5f, 1f);
                rect.offsetMin = new Vector2(20f, 20f);
                rect.offsetMax = new Vector2(-20f, -20f);
            }
        }

        void BuildSidePanel(Transform root)
        {
            var side = DemoUi.Panel("Side Panel", root, new Color(0.08f, 0.09f, 0.13f)).rectTransform;
            side.anchorMin = new Vector2(1f, 0f);
            side.anchorMax = Vector2.one;
            side.pivot = new Vector2(1f, 0.5f);
            side.sizeDelta = new Vector2(600f, 0f);

            DemoUi.Label("Heading", side, "What the director did", 36, TextAnchor.UpperLeft)
                .rectTransform.Fill(left: 30f, right: 30f, top: 30f);

            logLabel = DemoUi.Label("Log", side, "", 24, TextAnchor.UpperLeft);
            logLabel.rectTransform.Fill(left: 30f, bottom: 520f, right: 30f, top: 90f);

            const string tryThis =
                "Things to try\n" +
                "• Tap outside a tip to dismiss it.\n" +
                "• In the Shop, the buy tip only shows while you have 10 coins.\n" +
                "• Press New day, then let the offer run out.\n" +
                "• Press New day, then close the reward with X.\n" +
                "• Claim a reward, then press New day again. That tip waits 10 s.\n" +
                "• Stop and start Play mode. Seen tips stay seen.";
            DemoUi.Label("Try This", side, tryThis, 24, TextAnchor.UpperLeft, new Color(0.8f, 0.84f, 0.92f))
                .rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(540f, 340f));

            var newDay = DemoUi.Button("New Day", side, "New day", Muted, OnNewDay);
            ((RectTransform)newDay.transform).Place(Vector2.zero, new Vector2(30f, 30f), new Vector2(255f, 90f));
            var reset = DemoUi.Button("Reset", side, "Reset demo", Muted, OnReset);
            ((RectTransform)reset.transform).Place(new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(255f, 90f));
        }

        void BuildRewardPanel(Transform root)
        {
            var panel = DemoUi.Panel("Reward Panel", root, new Color(0.22f, 0.5f, 0.35f)).rectTransform;
            panel.Place(new Vector2(1f, 0.5f), Vector2.zero, new Vector2(420f, 330f));

            DemoUi.Label("Title", panel, "Daily reward", 40).rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(360f, 60f));
            DemoUi.Label("Amount", panel, "+5 coins", 34).rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(360f, 50f));
            offerLabel = DemoUi.Label("Offer", panel, "", 26, TextAnchor.MiddleCenter, new Color(0.85f, 0.95f, 0.88f));
            offerLabel.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(360f, 40f));

            var claim = DemoUi.Button("Claim", panel, "Claim", Accent, OnClaim);
            ((RectTransform)claim.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(300f, 100f));
            var close = DemoUi.Button("Close", panel, "X", new Color(0.15f, 0.35f, 0.25f), OnCloseReward);
            ((RectTransform)close.transform).Place(Vector2.one, new Vector2(-12f, -12f), new Vector2(64f, 64f));

            // The whole panel is the target, so Claim and X are both inside the hole.
            TooltipTarget.Attach(panel.gameObject, "reward");
            rewardPanel = panel.gameObject.AddComponent<SlidePanel>();
            rewardPanel.Setup(shown: new Vector2(-640f, 40f), hidden: new Vector2(470f, 40f));
        }

        void BuildPopup(Transform root)
        {
            var dim = DemoUi.Panel("Popup", root, new Color(0f, 0f, 0f, 0.6f));
            dim.rectTransform.Fill();
            popup = dim.gameObject;

            var box = DemoUi.Panel("Box", dim.transform, new Color(0.2f, 0.24f, 0.33f)).rectTransform;
            box.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 380f));
            DemoUi.Label("Text", box, "You won!\n+10 coins", 48).rectTransform.Fill(bottom: 140f, top: 30f);

            var ok = DemoUi.Button("OK", box, "OK", Accent, OnPopupClosed);
            ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(260f, 100f));
            popup.SetActive(false);
        }
    }
}
