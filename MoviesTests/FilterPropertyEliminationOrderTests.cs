using Newtonsoft.Json;

namespace MoviesTests
{
    [TestClass]
    public class FilterPropertyEliminationOrderTests
    {
        [TestMethod]
        public async Task SinglePredicateTests()
        {
            var single = DummyPredicate(Media.RUNTIME);

            await AssertOrder(single, Media.RUNTIME);
            await AssertOrder(new BooleanExpression { Predicates = { single } }, Media.RUNTIME);
        }

        [TestMethod]
        public void MultiplePredicatesTests()
        {
            var filter = FilterPredicate.TAUTOLOGY;
            var expression = DummyExpression(Movie.RELEASE_DATE, Media.RUNTIME, Movie.WATCH_PROVIDERS);
        }

        [TestMethod]
        public async Task CachedItems()
        {
            var inMemory = new PropertyDictionary
            {
                { Movie.RELEASE_DATE, Task.FromResult(DateTime.Now) },
                { Movie.WATCH_PROVIDERS, Task.FromResult(new WatchProvider()) },
                { TVShow.WATCH_PROVIDERS, Task.FromResult(new WatchProvider()) },
            };
            var persistent = new DummyCache
            {
                //{ Media.RUNTIME, new JsonResponse(System.Text.Json.JsonSerializer.Serialize(new TimeSpan(2, 10, 0))) }
            };

            await AssertOrder(DummyExpression(Movie.RELEASE_DATE, Media.RUNTIME), inMemory, Movie.RELEASE_DATE, Media.RUNTIME);
            await AssertOrder(DummyExpression(Media.RUNTIME, Movie.RELEASE_DATE), inMemory, Movie.RELEASE_DATE, Media.RUNTIME);

            await AssertOrder(DummyExpression(Media.RUNTIME, Movie.RELEASE_DATE, Media.KEYWORDS, Media.ORIGINAL_TITLE, Movie.WATCH_PROVIDERS), inMemory, Movie.RELEASE_DATE, Movie.WATCH_PROVIDERS, Media.RUNTIME, Media.KEYWORDS, Media.ORIGINAL_TITLE);
        }

        private Task AssertOrder(FilterPredicate filter, params Property[] expected) => AssertOrder(filter, null, (IEnumerable<Property>)expected);
        private Task AssertOrder(FilterPredicate filter, PropertyDictionary dict, params Property[] expected) => AssertOrder(filter, dict, (IEnumerable<Property>)expected);

        private async Task AssertOrder(FilterPredicate filter, PropertyDictionary dict, IEnumerable<Property> expected)
        {
            var actual = (await ItemHelpers.DefferedPredicates(filter, dict).ReadAll()).Select(predicate => predicate.LHS);
            Assert.IsTrue(actual.SequenceEqual(expected), $"Expected <{string.Join(',', expected)}>. Actual <{string.Join(',', actual)}>");
        }

        private string PrettyPrint(IEnumerable<FilterPredicate> predicates) => string.Join(',', predicates.Select(predicate => (predicate as OperatorPredicate)?.LHS ?? predicate));

        private OperatorPredicate DummyPredicate(Property property) => new OperatorPredicate
        {
            LHS = property,
            Operator = Operators.Equal,
            RHS = null
        };

        private BooleanExpression DummyExpression(params Property[] properties) => DummyExpression(properties.Select(DummyPredicate).ToArray());

        private BooleanExpression DummyExpression(params OperatorPredicate[] predicates)
        {
            var expression = new BooleanExpression();

            foreach (var predicate in predicates)
            {
                expression.Predicates.Add(predicate);
            }

            return expression;
        }
    }
}
