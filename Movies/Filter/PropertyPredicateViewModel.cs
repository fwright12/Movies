using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies.ViewModels
{
    public class PropertyPredicateBuilder : OperatorPredicateBuilder
    {
        public Property Property => LHS as Property;

        public override FilterPredicate BuildPredicate()
        {
            if (Property?.Values is SteppedValueRange range)
            {
                if (RHS is IComparable comparable)
                {
                    var first = (range.First as IComparable)?.CompareTo(comparable);
                    var last = (range.Last as IComparable)?.CompareTo(comparable);

                    if ((Operator == Operators.LessThan && first == 1) || (Operator == Operators.GreaterThan && last == -1))
                    {
                        return FilterPredicate.CONTRADICTION;
                    }
                    else if ((Operator == Operators.LessThan && last == 0) || (Operator == Operators.GreaterThan && first == 0))
                    {
                        return FilterPredicate.TAUTOLOGY;
                    }
                }
                else
                {
                    return FilterPredicate.CONTRADICTION;
                }

                //bool isAbsoluteMin = Operator != Operators.Equal && Equals(RHS, range.First);
                //bool isAbsoluteMax = Operator != Operators.Equal && Equals(RHS, range.Last);

                //return RHS != null && !isAbsoluteMin && !isAbsoluteMax;
                //return !(Equals(constraint.Constraint, constraint.Default) || isAbsoluteMin || isAbsoluteMax);
            }
            else if (Property?.Values != null)
            {
                if (RHS == null)//!Property.Values.OfType<object>().Contains(RHS))
                {
                    return FilterPredicate.CONTRADICTION;
                }
            }

            return base.BuildPredicate();
        }
    }
}