namespace MoviesTests
{
    public class DummyCache : Dictionary<string, JsonResponse>, IJsonCache
    {
        public DummyCache() : base() { }

        public DummyCache(IEnumerable<KeyValuePair<string, JsonResponse>> collection) : base(collection) { }

        public Task AddAsync(string url, JsonResponse response)
        {
            Add(url, response);
            return Task.CompletedTask;
        }

        public Task<bool> Expire(string url) => Task.FromResult(Remove(url));

        public IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default) => AsyncEnumerable.ToAsyncEnumerable(Task.FromResult<IEnumerable<KeyValuePair<string, JsonResponse>>>(this)).GetAsyncEnumerator();

        public Task<bool> IsCached(string url) => Task.FromResult(ContainsKey(url));

        public Task<JsonResponse> TryGetValueAsync(string url) => TryGetValue(url, out var value) ? Task.FromResult(value) : null;

        Task IJsonCache.Clear()
        {
            Clear();
            return Task.CompletedTask;
        }
    }

    [TestClass]
    public class FilterEvaluationTests
    {
        private Movie Movie = new Movie("movie").WithID(TMDB.IDKey, 0);
        private TVShow Show = new TVShow("show").WithID(TMDB.IDKey, 0);
        private Person Person = new Person("person").WithID(TMDB.IDKey, 0);

        public FilterEvaluationTests()
        {
#if DEBUG
            Movies.HttpClient.AllowLiveRequests = false;
            Movies.HttpClient.BreakOnRequest = false;
#endif
            new TMDB(string.Empty, string.Empty, new DummyCache());
        }

        [TestMethod]
        public async Task AllOrNoneFilterTests()
        {
            Assert.IsTrue(await ItemHelpers.Evaluate(Movie, FilterPredicate.TAUTOLOGY));
            Assert.IsTrue(await ItemHelpers.Evaluate(Show, FilterPredicate.TAUTOLOGY));
            Assert.IsTrue(await ItemHelpers.Evaluate(Person, FilterPredicate.TAUTOLOGY));

            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, FilterPredicate.CONTRADICTION));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, FilterPredicate.CONTRADICTION));
            Assert.IsFalse(await ItemHelpers.Evaluate(Person, FilterPredicate.CONTRADICTION));
        }

        [TestMethod]
        public async Task SingleEqualityTests()
        {
            var adventureGenre = new Genre { Id = 12, Name = "Adventure" };
            var adventureMovies = new OperatorPredicate
            {
                LHS = Movie.GENRES,
                Operator = Operators.Equal,
                RHS = adventureGenre
            };
            var filter = new BooleanExpression();
            filter.Predicates.Add(adventureMovies);

            Assert.IsTrue(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));
            adventureMovies.RHS = new Genre { Id = -1, Name = "No genre" };
            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));

            var comedyTV = new OperatorPredicate
            {
                LHS = TVShow.GENRES,
                Operator = Operators.Equal,
                RHS = new Genre { Id = 35, Name = "Comedy" }
            };
            filter.Predicates[0] = comedyTV;

            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsTrue(await ItemHelpers.Evaluate(Show, filter));
            comedyTV.RHS = new Genre { Id = -1, Name = "No genre" };
            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));

            comedyTV.RHS = adventureGenre;
            Assert.IsTrue(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));

            var keyword = new OperatorPredicate
            {
                LHS = Media.KEYWORDS,
                Operator = Operators.Equal,
                RHS = new Keyword { Id = 177912, Name = "wizard" }
            };
            filter.Predicates[0] = keyword;

            Assert.IsTrue(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));
        }

        [TestMethod]
        public async Task SingleComparisonTests()
        {
            var shortAssMovies = new OperatorPredicate
            {
                LHS = Media.RUNTIME,
                Operator = Operators.GreaterThan,
                RHS = new TimeSpan(0, 90, 0)
            };
            var filter = new BooleanExpression();
            filter.Predicates.Add(shortAssMovies);

            Assert.IsTrue(await ItemHelpers.Evaluate(Movie, filter));
            shortAssMovies.Operator = Operators.LessThan;
            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));
            shortAssMovies.RHS = new TimeSpan(2, 10, 0);
            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));

            Assert.IsTrue(await ItemHelpers.Evaluate(Show, filter));
            shortAssMovies.RHS = TimeSpan.Zero;
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));

            var oldMovies = new OperatorPredicate
            {
                LHS = Movie.RELEASE_DATE,
                Operator = Operators.LessThan,
                RHS = new DateTime(2000, 1, 1),
            };
            filter.Predicates[0] = oldMovies;

            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));
            oldMovies.Operator = Operators.GreaterThan;
            Assert.IsTrue(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));

            var goodMovies = new OperatorPredicate
            {
                LHS = TMDB.SCORE,
                Operator = Operators.GreaterThan,
                RHS = 9.0,
            };
            filter.Predicates[0] = goodMovies;
            Assert.IsFalse(await ItemHelpers.Evaluate(Movie, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Show, filter));
            Assert.IsFalse(await ItemHelpers.Evaluate(Person, filter));
        }

        [TestMethod]
        public async Task MultiplePredicatesSamePropertyTests()
        {

        }

        [TestMethod]
        public async Task MultiplePredicatesTests()
        {

        }
    }
}
