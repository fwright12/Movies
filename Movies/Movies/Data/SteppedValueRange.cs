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

            public Enumerator(SteppedValueRange range, bool reverse = false)
            {
                Range = range;
                Direction = reverse ? -1 : 1;
            }

            public bool MoveNext()
            {
                try
                {
                    Current += (dynamic)Range.Step * Direction;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public void Reset()
            {
                Current = Direction == -1 ? Range.Last : Range.First;
            }
        }


        public IEnumerator GetEnumerator() => new Enumerator(this);
        public IEnumerator GetReverseEnumerator() => new Enumerator(this, true);
    }
}