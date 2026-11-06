using System;
using System.Linq;
using SmartTooltip.Core;
using NUnit.Framework;

namespace SmartTooltip.Tests
{
    public class TooltipEvaluatorTests
    {
        static TooltipRule Rule(string id, int priority = 0, bool showOnce = true, params ICondition[] conditions) =>
            new TooltipRule(id, new TooltipPrompt("target", id), conditions, priority, showOnce);

        [Test]
        public void Candidates_OrderedByPriority_ThenDeclarationOrder()
        {
            var evaluator = new TooltipEvaluator(
                new[] { Rule("a"), Rule("b", priority: 5), Rule("c") },
                new InMemoryProgressStore());

            var ids = evaluator.Candidates(new TooltipContext()).Select(r => r.Id);

            CollectionAssert.AreEqual(new[] { "b", "a", "c" }, ids);
        }

        [Test]
        public void Candidates_SkipUnsatisfiedRules()
        {
            var evaluator = new TooltipEvaluator(
                new[] { Rule("needs-home", conditions: Condition.Is("screen", "home")) },
                new InMemoryProgressStore());

            Assert.IsEmpty(evaluator.Candidates(new TooltipContext().Set("screen", "shop")));
            Assert.IsNotEmpty(evaluator.Candidates(new TooltipContext().Set("screen", "home")));
        }

        [Test]
        public void ShowOnce_HiddenAfterMarkShown_RepeatableStays()
        {
            var once = Rule("once");
            var repeat = Rule("repeat", showOnce: false);
            var evaluator = new TooltipEvaluator(new[] { once, repeat }, new InMemoryProgressStore());
            var context = new TooltipContext();

            evaluator.MarkShown(once);
            evaluator.MarkShown(repeat);

            CollectionAssert.AreEqual(new[] { "repeat" }, evaluator.Candidates(context).Select(r => r.Id));
        }

        [Test]
        public void DuplicateIds_Throw()
        {
            Assert.Throws<ArgumentException>(() =>
                new TooltipEvaluator(new[] { Rule("x"), Rule("x") }, new InMemoryProgressStore()));
        }
    }
}
