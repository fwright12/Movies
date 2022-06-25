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
        public static readonly FilterPredicate CONTRADICTION = new Tautology();

        private class Tautology : FilterPredicate
        {
            public override bool Evaluate(object item) => true;
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

        public virtual bool Evaluate(object item) => true;
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

        //public override bool Evaluate(T item) => Predicates.OfType<FilterPredicate<T>>().All(predicate => predicate.Evaluate(item));
    }

    public class OperatorPredicate : FilterPredicate
    {
        public object LHS { get; set; }
        public Operators Operator { get; set; }
        public object RHS { get; set; }

        public override bool Evaluate(object item)
        {
            if (Operator == Operators.LessThan || Operator == Operators.GreaterThan)
            {
                int compare;

                if (LHS is IComparable lhs)
                {
                    compare = lhs.CompareTo(RHS);
                }
                else if (RHS is IComparable rhs)
                {
                    compare = rhs.CompareTo(LHS) * -1;
                }
                else
                {
                    return true;
                }

                return compare == (int)Operator;
            }
            else
            {
                var equal = Equals(LHS, RHS);

                if (Operator == Operators.NotEqual)
                {
                    equal = !equal;
                }

                return equal;
            }
        }
    }

    public class BooleanExpression : FilterPredicate
    {
        public IList<FilterPredicate> Predicates { get; } = new List<FilterPredicate>();

        //public override bool Evaluate(T item) => true;// Predicates.All(predicate => predicate.Evaluate(item));

        public static BooleanExpression operator &(BooleanExpression expression, FilterPredicate predicate)
        {
            expression.Predicates.Add(predicate);
            return expression;
        }
    }

    public class SearchPredicate : FilterPredicate
    {
        public string Query { get; set; }
    }
}