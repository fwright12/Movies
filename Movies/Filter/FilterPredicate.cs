using System;
using System.Collections.Generic;
using System.Linq;

namespace Movies
{
    public enum Operators
    {
        Equal = 0,
        LessThan = -1,
        GreaterThan = 1,
        NotEqual = 2
    }

    public abstract class FilterPredicate
    {
        public static readonly FilterPredicate TAUTOLOGY = new Tautology();
        public static readonly FilterPredicate CONTRADICTION = new Contradiction();

        private class Tautology : FilterPredicate
        {
            public override bool Evaluate() => true;
        }

        private class Contradiction : FilterPredicate
        {
            public override bool Evaluate() => false;
        }

        //public static bool operator true(FilterPredicate<T> predicate) => predicate.Evaluate
        //public static bool operator false(FilterPredicate<T> predicate) => x == Green || x == Yellow;

        public static BooleanExpression operator &(FilterPredicate first, FilterPredicate second) => new BooleanExpression
        {
            Predicates =
            {
                //first,
                //second
            }
        };

        public static FilterPredicate operator |(FilterPredicate first, FilterPredicate second)
        {
            if (first == FilterPredicate.TAUTOLOGY)
            {
                return first;
            }
            else if (first == FilterPredicate.CONTRADICTION)
            {
                return second;
            }
            else
            {
                return Operate(first, second, (a, b) => a | b);
            }
        }

        private static FilterPredicate Operate(FilterPredicate first, FilterPredicate second, Func<FilterPredicate, FilterPredicate, FilterPredicate> operate)
        {
            if (first is BooleanExpression expression)
            {
                return expression | second;
            }
            else
            {
                return null;
            }
        }

        public abstract bool Evaluate();
    }

    public class ExpressionPredicate : FilterPredicate
    {
        private IEnumerable<object> Predicates { get; }

        public ExpressionPredicate() : this(Enumerable.Empty<object>()) { }
        public ExpressionPredicate(params object[] predicates) : this((IEnumerable<object>)predicates) { }
        public ExpressionPredicate(IEnumerable<object> predicates)
        {
            Predicates = predicates;
        }

        public override bool Evaluate() => true;
    }

    public class OperatorPredicate : FilterPredicate
    {
        public object LHS { get; set; }
        public Operators Operator { get; set; }
        public object RHS { get; set; }

        public override bool Evaluate()
        {
            var value = LHS;

            if (Operator == Operators.GreaterThan || Operator == Operators.LessThan)
            {
                if (!(value is IComparable comparable) || comparable.CompareTo(RHS) != (int)Operator)
                {
                    return false;
                }
            }
            else if (Operator == Operators.Equal || Operator == Operators.NotEqual)
            {
                bool result = Equals(value, RHS);

                if (Operator == Operators.NotEqual)
                {
                    result = !result;
                }

                if (!result)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class BooleanExpression : FilterPredicate
    {
        public IList<FilterPredicate> Predicates { get; } = new List<FilterPredicate>();
        public bool IsAnd { get; set; } = true;

        public override bool Evaluate() => IsAnd ? Predicates.All(predicate => predicate.Evaluate()) : Predicates.Any(predicate => predicate.Evaluate());

        public static BooleanExpression operator &(BooleanExpression expression, FilterPredicate predicate)
        {
            expression.Predicates.Add(predicate);
            return expression;
        }
    }

    public class SearchPredicate : FilterPredicate
    {
        public string Query { get; set; }

        public override bool Evaluate() => true;
    }
}