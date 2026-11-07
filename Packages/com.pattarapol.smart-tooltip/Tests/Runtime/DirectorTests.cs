using System.Collections;
using System.Collections.Generic;
using SmartTooltip.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmartTooltip.Tests
{
    public class DirectorTests
    {
        // Longer than the overlay fade.
        const float FadeTime = 0.4f;

        Camera camera;
        GameObject cube;
        TooltipContext context;
        InMemoryProgressStore progress;
        TooltipDirector director;
        List<string> log;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            TooltipTarget.Attach(cube, "cube");

            context = new TooltipContext().Set("step", 0).Set("done", false);
            progress = new InMemoryProgressStore();
            log = new List<string>();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (director != null)
                Object.Destroy(director.gameObject);
            Object.Destroy(cube);
            Object.Destroy(camera.gameObject);
        }

        static readonly ICondition Done = Condition.Is("done", true);

        static TooltipRule Step(string id, int step, string targetId = "cube", int priority = 0,
            bool showOnce = true, float cooldown = 0f, ICondition doneWhen = null,
            TargetRequirement requires = TargetRequirement.OnScreen) =>
            new TooltipRule(id, new TooltipPrompt(targetId, id),
                new[] { Condition.Compare("step", ComparisonOperator.Equal, step) },
                priority, showOnce, cooldown, doneWhen, requires);

        Vector2 CubeOnScreen => camera.WorldToScreenPoint(cube.transform.position);

        void TapAt(Vector2 screenPoint)
        {
            director.Overlay.OnPointerPressed(screenPoint);
            director.Overlay.OnPointerReleased(screenPoint);
        }

        void CreateDirector(params TooltipRule[] rules)
        {
            director = TooltipDirector.Create(rules, context, progress);
            director.Started += rule => log.Add("start " + rule.Id);
            director.Ended += (rule, reason) => log.Add($"end {rule.Id} {reason}");
        }

        static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
                yield return null;
        }

        [UnityTest]
        public IEnumerator Check_ShowsBestRuleNextFrame_AndCallsInOneFrameBecomeOne()
        {
            CreateDirector(Step("low", 0), Step("high", 0, priority: 5));
            director.Check();
            director.Check();
            Assert.IsNull(director.Active, "should wait for the next frame");

            yield return Frames(2);

            Assert.AreEqual("high", director.Active?.Id);
            CollectionAssert.AreEqual(new[] { "start high" }, log);
            Assert.IsFalse(progress.HasSeen("high"), "showing is not seeing");
        }

        [UnityTest]
        public IEnumerator DoneConditionTrue_CompletesMarksSeenAndShowsNext()
        {
            CreateDirector(Step("first", 0, priority: 1, doneWhen: Done), Step("second", 0));
            yield return Frames(2);

            context.Set("done", true);
            director.Check();
            yield return Frames(2);

            Assert.AreEqual("second", director.Active?.Id);
            CollectionAssert.AreEqual(new[] { "start first", "end first Completed", "start second" }, log);
            Assert.IsTrue(progress.HasSeen("first"));
        }

        [UnityTest]
        public IEnumerator ShowConditionsStopMatching_InterruptsAndShowsAgainLater()
        {
            CreateDirector(Step("first", 0, doneWhen: Done));
            yield return Frames(2);

            context.Set("step", 1);
            director.Check();
            yield return Frames(2);

            Assert.IsNull(director.Active);
            CollectionAssert.AreEqual(new[] { "start first", "end first Interrupted" }, log);
            Assert.IsFalse(progress.HasSeen("first"), "leaving is not finishing");

            context.Set("step", 0);
            director.Check();
            yield return Frames(2);
            Assert.AreEqual("first", director.Active?.Id);
        }

        [UnityTest]
        public IEnumerator TargetTap_CompletesRuleWithoutDoneCondition()
        {
            CreateDirector(Step("info", 0));
            yield return new WaitForSecondsRealtime(FadeTime);

            TapAt(new Vector2(5, 5));
            Assert.AreEqual("info", director.Active?.Id, "a tap outside the hole is not a tap on the target");

            TapAt(CubeOnScreen);
            yield return Frames(2);

            Assert.IsNull(director.Active);
            CollectionAssert.AreEqual(new[] { "start info", "end info Completed" }, log);
            Assert.IsTrue(progress.HasSeen("info"));
        }

        [UnityTest]
        public IEnumerator TargetTap_RuleWithDoneCondition_Stays()
        {
            CreateDirector(Step("action", 0, doneWhen: Done));
            yield return new WaitForSecondsRealtime(FadeTime);

            TapAt(CubeOnScreen);
            yield return Frames(2);

            Assert.AreEqual("action", director.Active?.Id);
        }

        [UnityTest]
        public IEnumerator OffScreenTarget_StartsOnlyWhenRuleAllowsIt()
        {
            cube.transform.position = new Vector3(30, 0, 0);
            CreateDirector(Step("on-screen", 0, priority: 1), Step("anywhere", 0, requires: TargetRequirement.Exists));
            yield return new WaitForSecondsRealtime(director.SettleTime + 0.1f);

            Assert.AreEqual("anywhere", director.Active?.Id);
        }

        [UnityTest]
        public IEnumerator CheckWhileStillMatching_KeepsTheTip()
        {
            CreateDirector(Step("first", 0));
            yield return Frames(2);

            director.Check();
            yield return Frames(2);

            Assert.AreEqual("first", director.Active?.Id);
            CollectionAssert.AreEqual(new[] { "start first" }, log);
        }

        [UnityTest]
        public IEnumerator OutsideTap_Dismisses()
        {
            CreateDirector(Step("first", 0));
            yield return Frames(2);

            director.Overlay.GetComponentInChildren<TooltipCutout>().OnPointerClick(null);
            yield return new WaitForSecondsRealtime(FadeTime);

            Assert.IsNull(director.Active);
            CollectionAssert.AreEqual(new[] { "start first", "end first Dismissed" }, log);
            Assert.IsTrue(progress.HasSeen("first"));
        }

        [UnityTest]
        public IEnumerator TargetLost_NotMarkedSeen()
        {
            CreateDirector(Step("first", 0));
            yield return Frames(2);

            cube.SetActive(false);
            yield return new WaitForSecondsRealtime(FadeTime);

            Assert.IsNull(director.Active);
            CollectionAssert.AreEqual(new[] { "start first", "end first TargetLost" }, log);
            Assert.IsFalse(progress.HasSeen("first"));

            cube.SetActive(true);
            director.Check();
            yield return Frames(2);
            Assert.AreEqual("first", director.Active?.Id, "should show again once the target is back");
        }

        [UnityTest]
        public IEnumerator Hold_InterruptsAndShowsAgainAfterRelease()
        {
            CreateDirector(Step("first", 0));
            yield return Frames(2);

            var outer = director.Hold();
            var inner = director.Hold();
            director.Check();
            yield return Frames(3);

            Assert.IsNull(director.Active);
            CollectionAssert.AreEqual(new[] { "start first", "end first Interrupted" }, log);
            Assert.IsFalse(progress.HasSeen("first"));

            inner.Dispose();
            yield return Frames(3);
            Assert.IsNull(director.Active, "one hold is still active");

            outer.Dispose();
            outer.Dispose();
            yield return Frames(2);
            Assert.AreEqual("first", director.Active?.Id);
        }

        [UnityTest]
        public IEnumerator Hold_WhenTipIsAlreadyDone_CountsAsCompleted()
        {
            CreateDirector(Step("first", 0, doneWhen: Done));
            yield return Frames(2);

            context.Set("done", true);
            using (director.Hold())
            {
                CollectionAssert.AreEqual(new[] { "start first", "end first Completed" }, log);
                Assert.IsTrue(progress.HasSeen("first"));
            }
        }

        [UnityTest]
        public IEnumerator BestTargetMissing_WaitsThenFallsBack()
        {
            CreateDirector(Step("best", 0, targetId: "missing", priority: 5), Step("fallback", 0));
            yield return Frames(2);

            Assert.IsNull(director.Active, "should wait for the best target first");

            yield return new WaitForSecondsRealtime(director.SettleTime + 0.1f);
            Assert.AreEqual("fallback", director.Active?.Id);
        }

        [UnityTest]
        public IEnumerator Cooldown_DelaysRepeatableRule()
        {
            CreateDirector(Step("repeat", 0, showOnce: false, cooldown: 0.5f, doneWhen: Done));
            yield return Frames(2);

            context.Set("done", true);
            director.Check();
            yield return Frames(2);
            Assert.IsNull(director.Active);
            CollectionAssert.AreEqual(new[] { "start repeat", "end repeat Completed" }, log);

            context.Set("done", false);
            director.Check();
            yield return Frames(2);
            Assert.IsNull(director.Active, "still cooling down");

            yield return new WaitForSecondsRealtime(0.6f);
            director.Check();
            yield return Frames(2);
            Assert.AreEqual("repeat", director.Active?.Id);
        }
    }
}
