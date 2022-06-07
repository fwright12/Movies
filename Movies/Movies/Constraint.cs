using Movies.Models;
using System;
using System.Collections.Generic;

namespace Movies
{
    public enum Operators
    {
        Equal = 0,
        LessThan = -1,
        GreaterThan = 1,
        NotEqual = 2
    }

    public interface IBooleanValued<T>
    {
        T Item { get; }

        bool Evaluate();
        bool Evaluate(T value);
    }

    public class BooleanExpression
    {
        public IList<Constraint> Parts { get; } = new List<Constraint>();

        public BooleanExpression() { }
        public BooleanExpression(IEnumerable<Constraint> parts)
        {
            Parts = new List<Constraint>(parts);
        }
    }

    public struct Constraint
    {
        public Property Property { get; }
        public object Value { get; set; }
        public Operators Comparison { get; set; }

        public Constraint(Property property)
        {
            Property = property;
            Value = null;
            Comparison = Operators.Equal;
        }
    }


    public abstract class Constraint1
    {
        public Property Property { get; }
        public object Value { get; set; }
        public Operators Comparison { get; set; }

        public Constraint1(Property property)
        {
            Property = property;
        }

        public bool Evaluate(Item value) => IsAllowed((Item)value);
        public abstract bool IsAllowed(object value);

        public override bool Equals(object obj) => obj is Constraint1 constraint && constraint.Property == Property && Equals(constraint.Value, Value) && constraint.Comparison == Comparison;

        public override int GetHashCode() => Property?.GetHashCode() ?? base.GetHashCode();
    }

    public class Constraint1<T> : Constraint1
    {
        new public T Value
        {
            get => base.Value == null ? default : (T)base.Value;
            set => base.Value = value;
        }

        public Constraint1(Property property) : base(property) { }

        public override bool IsAllowed(object value)
        {
            if (!(value is T t))
            {
                return false;
            }

            if (t is IComparable comparable)
            {
                int compare = comparable.CompareTo(Value);
                return Comparison == Operators.NotEqual ? compare != 0 : compare == (int)Comparison;
            }

            if (Comparison == Operators.LessThan || Comparison == Operators.GreaterThan)
            {
                return false;
            }

            var equals = Equals(value, Value);
            return Comparison == Operators.Equal ? equals : !equals;
        }
    }
}