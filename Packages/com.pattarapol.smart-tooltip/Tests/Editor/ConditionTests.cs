using SmartTooltip.Core;
using NUnit.Framework;

namespace SmartTooltip.Tests
{
    public class ConditionTests
    {
        [Test]
        public void Compare_MatchesNumericTypes()
        {
            var context = new TooltipContext().Set("level", 5).Set("coins", 12.5f);

            Assert.IsTrue(Condition.Compare("level", ComparisonOperator.GreaterOrEqual, 5).Evaluate(context));
            Assert.IsFalse(Condition.Compare("level", ComparisonOperator.Greater, 5).Evaluate(context));
            Assert.IsTrue(Condition.Compare("coins", ComparisonOperator.Less, 20).Evaluate(context));
        }

        [Test]
        public void MissingKey_NeverMatches()
        {
            var context = new TooltipContext();

            Assert.IsFalse(Condition.Compare("level", ComparisonOperator.NotEqual, 1).Evaluate(context));
            Assert.IsFalse(Condition.Is("screen", "home").Evaluate(context));
            Assert.IsFalse(Condition.Has("screen").Evaluate(context));
        }

        [Test]
        public void Composites()
        {
            var context = new TooltipContext().Set("screen", "home").Set("level", 2);
            var onHome = Condition.Is("screen", "home");
            var high = Condition.Compare("level", ComparisonOperator.Greater, 10);

            Assert.IsFalse(Condition.All(onHome, high).Evaluate(context));
            Assert.IsTrue(Condition.Any(onHome, high).Evaluate(context));
            Assert.IsTrue(Condition.Not(high).Evaluate(context));
        }
    }
}
