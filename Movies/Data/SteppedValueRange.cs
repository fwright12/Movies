using System.Collections;
using System.Linq;

namespace Movies
{
    public static class Enumerator
    {
        public static IEnumerable AsEnumerable(this IEnumerator itr)
        {
            while (itr.MoveNext())
            {
                yield return itr.Current;
            }
        }
    }

    public interface IReverseEnumerable
    {
        public IEnumerator GetReverseEnumerator();
    }

    public class SteppedValueRange : IEnumerable, IReverseEnumerable
    {
        public object First { get; set; }
        public object Last { get; set; }
        public object Step { get; set; }

        private class Enumerator : IEnumerator
        {
            public object Current { get; private set; }

            public SteppedValueRange Range { get; }
            private int Direction { get; }

            private static readonly object ENUMERATION_NOT_STARTED = new object();

            public Enumerator(SteppedValueRange range, bool reverse = false)
            {
                Range = range;
                Direction = reverse ? -1 : 1;
                Reset();
            }

            public bool MoveNext()
            {
                if (Current == ENUMERATION_NOT_STARTED)
                {
                    Current = Direction == -1 ? Range.Last : Range.First;
                    return true;
                }

                object end;
                try
                {
                    if (Direction == -1)
                    {
                        Current = (dynamic)Current - (dynamic)Range.Step;
                        end = Range.First;
                    }
                    else
                    {
                        Current = (dynamic)Current + (dynamic)Range.Step;
                        end = Range.Last;
                    }
                }
                catch
                {
                    return false;
                }

                return Current == end;
            }

            public void Reset()
            {
                Current = ENUMERATION_NOT_STARTED;
            }
        }


        public IEnumerator GetEnumerator() => new Enumerator(this);
        public IEnumerator GetReverseEnumerator() => new Enumerator(this, true);
    }
}