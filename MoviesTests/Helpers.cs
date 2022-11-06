using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoviesTests
{
    public static class Helpers
    {
        public static OperatorPredicate DummyPredicate(this Property property) => new OperatorPredicate
        {
            LHS = property,
            Operator = Operators.Equal,
            RHS = null
        };

        public static BooleanExpression DummyExpression(params Property[] properties) => DummyExpression(properties.Select(DummyPredicate).ToArray());

        public static BooleanExpression DummyExpression(params OperatorPredicate[] predicates)
        {
            var expression = new BooleanExpression();

            foreach (var predicate in predicates)
            {
                expression.Predicates.Add(predicate);
            }

            return expression;
        }

        public static string PrettyPrint<T>(this IEnumerable<T> list) => "[" + string.Join(',', list) + "]";

        public static Movie ToMovie(this int id) => new Movie(id.ToString()).WithID(TMDB.IDKey, id);

        public static IEnumerable<T> Scramble<T>(this IEnumerable<T> source, Random generator)
        {
            var list = source.ToList();

            foreach (var _ in source)
            {
                int index = generator.Next(0, list.Count - 1);
                var item = list[index];
                list.RemoveAt(index);

                yield return item;
            }
        }

        public static IEnumerable<IEnumerable<T>> NoisyLists<T>(Random random, IEnumerable<IEnumerable<T>> lists) => lists.SelectMany(list => new List<IEnumerable<T>> { list, list.Reverse(), list.Scramble(random) }).Scramble(random);

        public static IEnumerable<IEnumerable<T>> RandomChunks<T>(IReadOnlyList<T> list, Random random) => RandomChunks(random.Next(0, 5), list, random);
        public static IEnumerable<IEnumerable<T>> RandomChunks<T>(int iterations, IReadOnlyList<T> list, Random random)
        {
            for (int i = 0; i < iterations; i++)
            {
                int count = random.Next(0, Math.Min(list.Count, 33));
                int index = random.Next(0, list.Count - count);

                yield return Enumerable.Range(0, count).Select(j => list[index + j]);
            }
        }
    }
}
