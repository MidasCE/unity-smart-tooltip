using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartTooltip.Core
{
    public static class Condition
    {
        public static ICondition When(Func<ITooltipContext, bool> predicate) => new Predicate(predicate);

        public static ICondition Not(ICondition inner) => new Predicate(c => !inner.Evaluate(c));

        public static ICondition All(params ICondition[] conditions) => All((IEnumerable<ICondition>)conditions);
        public static ICondition All(IEnumerable<ICondition> conditions)
        {
            var list = conditions.ToArray();
            return new Predicate(c => list.All(x => x.Evaluate(c)));
        }

        public static ICondition Any(params ICondition[] conditions) => Any((IEnumerable<ICondition>)conditions);
        public static ICondition Any(IEnumerable<ICondition> conditions)
        {
            var list = conditions.ToArray();
            return new Predicate(c => list.Any(x => x.Evaluate(c)));
        }

        public static ICondition Is<T>(string key, T expected) =>
            new Predicate(c => c.TryGet<T>(key, out var actual) && EqualityComparer<T>.Default.Equals(actual, expected));

        public static ICondition Has(string key) => new Predicate(c => c.TryGet<object>(key, out _));

        /// <summary>Missing keys or values that are not numbers never match.</summary>
        public static ICondition Compare(string key, ComparisonOperator op, double operand) =>
            new Predicate(c => TryGetNumber(c, key, out var actual) && Apply(op, actual, operand));

        static bool TryGetNumber(ITooltipContext context, string key, out double number)
        {
            number = 0;
            if (!context.TryGet<object>(key, out var raw) || raw == null)
                return false;

            switch (raw)
            {
                case int i: number = i; return true;
                case long l: number = l; return true;
                case float f: number = f; return true;
                case double d: number = d; return true;
                default: return false;
            }
        }

        static bool Apply(ComparisonOperator op, double actual, double operand)
        {
            switch (op)
            {
                case ComparisonOperator.Equal: return actual == operand;
                case ComparisonOperator.NotEqual: return actual != operand;
                case ComparisonOperator.Less: return actual < operand;
                case ComparisonOperator.LessOrEqual: return actual <= operand;
                case ComparisonOperator.Greater: return actual > operand;
                case ComparisonOperator.GreaterOrEqual: return actual >= operand;
                default: throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        sealed class Predicate : ICondition
        {
            readonly Func<ITooltipContext, bool> predicate;

            public Predicate(Func<ITooltipContext, bool> predicate) =>
                this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

            public bool Evaluate(ITooltipContext context) => predicate(context);
        }
    }
}
