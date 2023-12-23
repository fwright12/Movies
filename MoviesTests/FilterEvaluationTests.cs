namespace MoviesTests
{
    [TestClass]
    public class FilterEvaluationTests
    {
        private Movie Movie = new Movie("movie").WithID(TMDB.IDKey, 0);
        private TVShow Show = new TVShow("show").WithID(TMDB.IDKey, 0);
        private Person Person = new Person("person").WithID(TMDB.IDKey, 0);

        private UiiDictionaryDatastore InMemoryCache { get; }

        //private PropertyDictionary InMemoryCache1 = new PropertyDictionary
        //{
        //    { Movie.RELEASE_DATE, Task.FromResult(DateTime.Now) },
        //    { Movie.WATCH_PROVIDERS, Task.FromResult(new WatchProvider()) },
        //    { TVShow.WATCH_PROVIDERS, Task.FromResult(new WatchProvider()) },
        //};

        private static JsonResponse EmptyJSON = new JsonResponse(string.Empty);

        private IJsonCache PersistentCache = new DummyCache
        {
            { API.MOVIES.GET_WATCH_PROVIDERS.GetURL(), EmptyJSON },
            { API.TV.GET_WATCH_PROVIDERS.GetURL(), EmptyJSON },
            { API.MOVIES.GET_KEYWORDS.GetURL(), EmptyJSON }
        };

        private ChainLink<EventArgsAsyncWrapper<IEnumerable<ResourceReadArgs<Uri>>>> Chain;

        public FilterEvaluationTests()
        {
            //new TMDB(string.Empty, string.Empty, new DummyCache());
            var resolver = new TMDbResolver(TMDB.ITEM_PROPERTIES);
            var invoker = new HttpMessageInvoker(new BufferedHandler(new TMDbBufferedHandler(new MockHandler())));
            var RemoteTMDbHandlers = new TMDbHttpProcessor(invoker, resolver, TMDbApi.AutoAppend);

            Chain = AsyncCoRExtensions.Create<IEnumerable<ResourceReadArgs<Uri>>>(RemoteTMDbHandlers);
            DebugConfig.SimulatedDelay = 0;

            InMemoryCache = new UiiDictionaryDatastore();
            InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Movie.RELEASE_DATE), State.Create(DateTime.Now));
            InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.Movie, Movie.WATCH_PROVIDERS), State.Create(new WatchProvider()));
            InMemoryCache.CreateAsync(new UniformItemIdentifier(Constants.TVShow, TVShow.WATCH_PROVIDERS), State.Create(new WatchProvider()));
        }

        [TestMethod]
        public async Task FilterPropertyEliminationOrderTests()
        {
            await AssertOrderCached(Helpers.DummyExpression(
                Movie.RELEASE_DATE, Media.RUNTIME),
                Movie.RELEASE_DATE, Media.RUNTIME);

            await AssertOrder(Helpers.DummyExpression(
                Media.ORIGINAL_TITLE, Media.RUNTIME, Media.KEYWORDS, Movie.RELEASE_DATE, Movie.WATCH_PROVIDERS),
                Media.ORIGINAL_TITLE, Media.RUNTIME, Media.KEYWORDS, Movie.RELEASE_DATE, Movie.WATCH_PROVIDERS);
            await AssertOrderCached(Helpers.DummyExpression(
                Media.ORIGINAL_TITLE, Media.RUNTIME, Media.KEYWORDS, Movie.RELEASE_DATE, Movie.WATCH_PROVIDERS),
                Movie.RELEASE_DATE, Movie.WATCH_PROVIDERS, Media.KEYWORDS, Media.ORIGINAL_TITLE, Media.RUNTIME);
        }

        [TestMethod]
        public async Task AllOrNoneFilterTests()
        {
            Assert.IsTrue(await Evaluate(Movie, FilterPredicate.TAUTOLOGY));
            Assert.IsTrue(await Evaluate(Show, FilterPredicate.TAUTOLOGY));
            Assert.IsTrue(await Evaluate(Person, FilterPredicate.TAUTOLOGY));

            Assert.IsFalse(await Evaluate(Movie, FilterPredicate.CONTRADICTION));
            Assert.IsFalse(await Evaluate(Show, FilterPredicate.CONTRADICTION));
            Assert.IsFalse(await Evaluate(Person, FilterPredicate.CONTRADICTION));
        }

        private static readonly Genre ADVENTURE_GENRE = new Genre { Id = 12, Name = "Adventure" };
        private static readonly Genre COMEDY_GENRE = new Genre { Id = 35, Name = "Comedy" };
        private static readonly Keyword WIZARD_KEYWORD = new Keyword { Id = 177912, Name = "wizard" };
        private static readonly Person EVANNA_LYNCH = new Person("Evanna Lynch").WithID(TMDB.IDKey, 140367);
        private static readonly WatchProvider HBO = new WatchProvider { Id = 384 };
        private static readonly WatchProvider PEACOCK = new WatchProvider { Id = 386 };
        private static OperatorPredicate ADVENTURE_MOVIES => new OperatorPredicate
        {
            LHS = Movie.GENRES,
            Operator = Operators.Equal,
            RHS = ADVENTURE_GENRE
        };

        [TestMethod]
        public async Task SingleEqualityTests()
        {
            var filter = new BooleanExpression();
            var adventureMovies = ADVENTURE_MOVIES;
            filter.Predicates.Add(adventureMovies);

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            //Assert.IsFalse(await Evaluate(Person, filter));
            adventureMovies.RHS = new Genre { Id = -1, Name = "No genre" };
            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));

            var comedyTV = new OperatorPredicate
            {
                LHS = TVShow.GENRES,
                Operator = Operators.Equal,
                RHS = COMEDY_GENRE
            };
            filter.Predicates[0] = comedyTV;

            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsTrue(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));
            comedyTV.RHS = new Genre { Id = -1, Name = "No genre" };
            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));

            var keyword = new OperatorPredicate
            {
                LHS = Media.KEYWORDS,
                Operator = Operators.Equal,
                RHS = WIZARD_KEYWORD
            };
            filter.Predicates[0] = keyword;

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));
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

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Person, filter));
            shortAssMovies.Operator = Operators.LessThan;
            Assert.IsFalse(await Evaluate(Movie, filter));
            shortAssMovies.RHS = new TimeSpan(2, 10, 0);
            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsTrue(await Evaluate(Show, filter));
            shortAssMovies.RHS = TimeSpan.Zero;
            Assert.IsFalse(await Evaluate(Show, filter));

            var oldMovies = new OperatorPredicate
            {
                LHS = Movie.RELEASE_DATE,
                Operator = Operators.LessThan,
                RHS = new DateTime(2000, 1, 1),
            };
            filter.Predicates[0] = oldMovies;

            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));
            oldMovies.Operator = Operators.GreaterThan;
            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));

            var goodMovies = new OperatorPredicate
            {
                LHS = TMDB.SCORE,
                Operator = Operators.GreaterThan,
                RHS = 9.0,
            };
            filter.Predicates[0] = goodMovies;
            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));
        }

        [TestMethod]
        public async Task SamePropertyDifferentValuesTests()
        {
            var adventureMovies = new OperatorPredicate
            {
                LHS = TVShow.GENRES,
                Operator = Operators.Equal,
                RHS = ADVENTURE_GENRE
            };
            var filter = new BooleanExpression();
            filter.Predicates.Add(adventureMovies);

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));

            var comedyTV = new OperatorPredicate
            {
                LHS = Movie.GENRES,
                Operator = Operators.Equal,
                RHS = COMEDY_GENRE
            };
            filter.Predicates[0] = comedyTV;

            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsTrue(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));

            var hbo = new OperatorPredicate
            {
                LHS = TVShow.WATCH_PROVIDERS,
                Operator = Operators.Equal,
                RHS = HBO
            };
            filter.Predicates[0] = hbo;

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));

            var peacock = new OperatorPredicate
            {
                LHS = Movie.WATCH_PROVIDERS,
                Operator = Operators.Equal,
                RHS = PEACOCK
            };
            filter.Predicates[0] = peacock;

            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsTrue(await Evaluate(Show, filter));
        }

        [TestMethod]
        public async Task MultiplePredicatesSamePropertyTests()
        {
            var exp = Helpers.DummyExpression(Enumerable.Repeat(Media.KEYWORDS, 5).ToArray());
            exp.IsAnd = false;
            (exp.Predicates[2] as OperatorPredicate).RHS = WIZARD_KEYWORD;
            var filter = new BooleanExpression
            {
                Predicates = { ADVENTURE_MOVIES, exp }
            };

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));

            exp = Helpers.DummyExpression(Enumerable.Repeat(Movie.WATCH_PROVIDERS, 5).ToArray());
            exp.IsAnd = false;
            (exp.Predicates[2] as OperatorPredicate).RHS = HBO;
            filter.Predicates[0] = exp;

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));

            (exp.Predicates[2] as OperatorPredicate).RHS = null;
            Assert.IsFalse(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
        }

        [TestMethod]
        public async Task MultiplePredicatesSamePropertyHotfixTests()
        {
            var exp = Helpers.DummyExpression(Enumerable.Repeat(Media.KEYWORDS, 5).ToArray());
            exp.IsAnd = false;
            (exp.Predicates[2] as OperatorPredicate).RHS = WIZARD_KEYWORD;
            exp.Predicates.Insert(1, ADVENTURE_MOVIES);
            var filter = exp;

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));

            foreach (var i in Enumerable.Range(0, exp.Predicates.Count - 1).Reverse())
            {
                exp.Predicates.Insert(i, i == 0 ? ADVENTURE_MOVIES : Helpers.DummyPredicate(Movie.GENRES));
            }

            Assert.IsTrue(await Evaluate(Movie, filter));
            Assert.IsFalse(await Evaluate(Show, filter));
            Assert.IsFalse(await Evaluate(Person, filter));
        }

        //[TestMethod]
        public async Task MultiplePredicatesTests()
        {

        }

        [TestMethod]
        public async Task FilterPeople()
        {
            OperatorPredicate predicate = new OperatorPredicate
            {
                LHS = CollectionViewModel.People,
                Operator = Operators.Equal,
                RHS = new Person("alsdjfsf")
            };

            Assert.IsFalse(await Evaluate(Movie, predicate));
            predicate.RHS = EVANNA_LYNCH;
            Assert.IsTrue(await Evaluate(Movie, predicate));
        }

        private Task<bool> Evaluate(Item item, FilterPredicate filter) => ItemHelpers.Evaluate(Chain, item, filter);

        private Task AssertOrder(FilterPredicate filter, params Property[] expected) => AssertOrder(Constants.Movie, filter, null, null, expected);
        private Task AssertOrderCached(FilterPredicate filter, params Property[] expected) => AssertOrder(Constants.Movie, filter, InMemoryCache, PersistentCache, expected);
        //private Task AssertOrder(FilterPredicate filter, PropertyDictionary dict, IJsonCache cache, params Property[] expected) => AssertOrder(filter, dict, cache, (IEnumerable<Property>)expected);

        private async Task AssertOrder(Item item, FilterPredicate filter, UiiDictionaryDatastore datastore, IJsonCache cache, IEnumerable<Property> expected)
        {
            var actual = (await ItemHelpers.DefferedPredicates(item, filter, datastore, cache).ReadAll()).Select(predicate => predicate.LHS);
            Assert.IsTrue(actual.SequenceEqual(expected), $"Expected <[{string.Join(',', expected)}]>. Actual <[{string.Join(',', actual)}]>");
        }
    }
}
