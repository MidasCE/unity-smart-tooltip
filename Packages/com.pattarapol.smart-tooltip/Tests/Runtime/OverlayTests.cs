using System.Collections;
using System.IO;
using SmartTooltip.Core;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmartTooltip.Tests
{
    public class OverlayTests
    {
        const float SettleTime = 0.6f;

        static readonly string OutputFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/SmartTooltipTests"));

        Camera camera;
        GameObject cube;
        GameObject light;
        TooltipTarget target;
        TooltipOverlay overlay;

        RectTransform Bubble => (RectTransform)overlay.transform.Find("Bubble");
        RectTransform Arrow => (RectTransform)Bubble.Find("Arrow");
        Vector2 CubeOnScreen => camera.WorldToScreenPoint(cube.transform.position);
        Rect ScreenRect => new Rect(0, 0, Screen.width, Screen.height);

        [OneTimeSetUp]
        public void CleanOutput()
        {
            if (Directory.Exists(OutputFolder))
                Directory.Delete(OutputFolder, true);
            Directory.CreateDirectory(OutputFolder);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            camera = new GameObject("Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.35f, 0.5f, 0.7f);

            light = new GameObject("Light");
            light.AddComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target = TooltipTarget.Attach(cube, "cube");
            overlay = TooltipOverlay.Create();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(overlay.gameObject);
            Object.Destroy(cube);
            Object.Destroy(light);
            Object.Destroy(camera.gameObject);

            var result = TestContext.CurrentContext.Result;
            var line = $"{TestContext.CurrentContext.Test.Name}: {result.Outcome.Status}";
            if (result.Outcome.Status != TestStatus.Passed)
                line += $"\n    {result.Message}";
            File.AppendAllText(Path.Combine(OutputFolder, "results.txt"), line + "\n");
        }

        [Test]
        public void Target_OnScreen_RectCoversCube()
        {
            Assert.IsTrue(target.TryGetScreenRect(out var rect));
            Assert.IsTrue(ScreenProjection.IsOnScreen(rect));
            Assert.IsTrue(rect.Contains(CubeOnScreen));
            Assert.Greater(rect.width, 10f);
        }

        [Test]
        public void Target_BehindCameraOnTheRight_PointsRight()
        {
            cube.transform.position = new Vector3(5, 0, -20);

            Assert.IsTrue(target.TryGetScreenRect(out var rect));
            Assert.IsFalse(ScreenProjection.IsOnScreen(rect));
            Assert.Greater(rect.center.x, Screen.width, $"rect {rect}");
        }

        [Test]
        public void Target_TurnedInDepth_FitsTighterThanWorldBox()
        {
            cube.transform.localScale = new Vector3(6, 0.5f, 0.5f);
            cube.transform.rotation = Quaternion.Euler(35, 45, 20);

            Assert.IsTrue(target.TryGetScreenRect(out var tight));
            Assert.IsTrue(ScreenProjection.TryProjectBounds(camera, cube.GetComponent<Renderer>().bounds, out var loose));
            var ratio = tight.width * tight.height / (loose.width * loose.height);
            Assert.Less(ratio, 0.85f, $"tight {tight} loose {loose}");
        }

        [Test]
        public void Registry_OnlyResolvesTargetsOnScreen()
        {
            Assert.IsTrue(TooltipTargetRegistry.Default.TryResolve("cube", out _));

            cube.transform.position = new Vector3(1000, 0, 0);
            Assert.IsFalse(TooltipTargetRegistry.Default.TryResolve("cube", out _));
            Assert.IsTrue(TooltipTargetRegistry.Default.TryFind("cube", out _), "TryFind also finds it off screen");

            cube.SetActive(false);
            Assert.IsFalse(TooltipTargetRegistry.Default.TryFind("cube", out _));
        }

        [UnityTest]
        public IEnumerator Overlay_TapInsideHole_RaisesTargetTapped()
        {
            var taps = 0;
            overlay.TargetTapped += () => taps++;
            overlay.Show(new TooltipPrompt("cube", "Tap me", Placement.Above), target);
            yield return new WaitForSecondsRealtime(SettleTime);

            overlay.OnPointerPressed(new Vector2(5, 5));
            overlay.OnPointerReleased(CubeOnScreen);
            Assert.AreEqual(0, taps, "a drag into the hole is not a tap");

            overlay.OnPointerPressed(CubeOnScreen);
            overlay.OnPointerReleased(CubeOnScreen);
            Assert.AreEqual(1, taps);
        }

        [UnityTest]
        public IEnumerator Overlay_OnScreen_HoleAroundTargetAndBubbleAbove()
        {
            overlay.Show(new TooltipPrompt("cube", "Tap the cube to start!", Placement.Above), target);
            yield return new WaitForSecondsRealtime(SettleTime);
            yield return Capture("1_on_screen");

            Assert.IsFalse(overlay.IsBlocked(CubeOnScreen), "tap on the cube should reach the game");
            Assert.IsTrue(overlay.IsBlocked(new Vector2(5, 5)), "tap in the corner should be blocked");

            target.TryGetScreenRect(out var rect);
            Assert.Greater(Bubble.position.y, rect.yMax, "bubble should be above the cube");
            Assert.AreEqual(180f, Arrow.localEulerAngles.z, 1f, "arrow should point down");
            Assert.IsTrue(ScreenRect.Contains(Bubble.position));
        }

        [UnityTest]
        public IEnumerator Overlay_NoRoomAbove_FlipsBelow()
        {
            cube.transform.position = new Vector3(0, 4.6f, 0);
            overlay.Show(new TooltipPrompt("cube", "Not enough room above me", Placement.Above), target);
            yield return new WaitForSecondsRealtime(SettleTime);
            yield return Capture("2_flipped_below");

            target.TryGetScreenRect(out var rect);
            Assert.Less(Bubble.position.y, rect.yMin, "bubble should flip below the cube");
            Assert.AreEqual(0f, Arrow.localEulerAngles.z, 1f, "arrow should point up");
        }

        [UnityTest]
        public IEnumerator Overlay_FollowsMovingTarget()
        {
            overlay.Show(new TooltipPrompt("cube", "I follow you", Placement.Above), target);
            yield return new WaitForSecondsRealtime(SettleTime);

            for (var i = 0; i < 30; i++)
            {
                cube.transform.position += new Vector3(0.05f, 0.02f, 0);
                yield return null;
                Assert.IsFalse(overlay.IsBlocked(CubeOnScreen), $"hole lost the cube on frame {i}");
            }

            yield return Capture("3_moved");
        }

        [UnityTest]
        public IEnumerator Overlay_TargetOffScreenRight_SticksToRightEdge()
        {
            overlay.Show(new TooltipPrompt("cube", "I am over here", Placement.Above), target);
            yield return new WaitForSecondsRealtime(SettleTime);

            cube.transform.position = new Vector3(30, 0, 0);
            yield return new WaitForSecondsRealtime(SettleTime);
            yield return Capture("4_off_screen_right");

            Assert.IsTrue(overlay.IsShowing);
            Assert.IsTrue(ScreenRect.Contains(Bubble.position), "bubble should stay on screen");
            Assert.Greater(Bubble.position.x, Screen.width * 0.6f, "bubble should be at the right edge");
            Assert.AreEqual(270f, Arrow.localEulerAngles.z, 1f, "arrow should point right");

            cube.transform.position = Vector3.zero;
            yield return new WaitForSecondsRealtime(SettleTime);
            yield return Capture("5_back_on_screen");

            Assert.IsFalse(overlay.IsBlocked(CubeOnScreen), "hole should come back around the cube");
        }

        [UnityTest]
        public IEnumerator Overlay_TargetBehindCamera_PointsTowardIt()
        {
            overlay.Show(new TooltipPrompt("cube", "Turn around", Placement.Above), target);
            yield return new WaitForSecondsRealtime(SettleTime);

            cube.transform.position = new Vector3(-5, 0, -20);
            yield return new WaitForSecondsRealtime(SettleTime);
            yield return Capture("6_behind_camera_left");

            Assert.IsTrue(overlay.IsShowing);
            Assert.Less(Bubble.position.x, Screen.width * 0.4f, "bubble should be at the left edge");
            Assert.AreEqual(90f, Arrow.localEulerAngles.z, 1f, "arrow should point left");
        }

        [UnityTest]
        public IEnumerator Overlay_TargetDisabled_Hides()
        {
            TooltipHideReason? hidden = null;
            overlay.Hidden += reason => hidden = reason;
            overlay.Show(new TooltipPrompt("cube", "Bye", Placement.Above), target);
            yield return new WaitForSecondsRealtime(SettleTime);

            cube.SetActive(false);
            yield return new WaitForSecondsRealtime(SettleTime);

            Assert.IsFalse(overlay.IsShowing);
            Assert.AreEqual(TooltipHideReason.TargetLost, hidden);
        }

        IEnumerator Capture(string name)
        {
            if (Application.isBatchMode)
                yield break;

            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(OutputFolder, name + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}
