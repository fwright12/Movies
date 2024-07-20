namespace MoviesTests.Data
{
    internal class ResourceAssert
    {
        public static void InMemoryCacheCount(UiiDictionaryDataStore cache, int expected) => CacheCount(cache.Keys, expected, null);
        public static void InMemoryCacheCount(UiiDictionaryDataStore cache, Item item)
        {
            var expectedResources = ExpectedResources(item);
            if (item.TryGetID(TMDB.ID, out var id))
            {
                if (item is Movie)
                {
                    expectedResources.Add(new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
                else if (item is TVShow)
                {
                    expectedResources.Add(new Uri(string.Format(API.TV.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
            }

            CacheCount(cache.Keys, expectedResources.Count, expectedResources);
        }

        public static void DiskCacheCount(DummyDatastore<Uri> cache, int expected) => CacheCount(cache.Keys, expected, null);
        public static void DiskCacheCount(DummyDatastore<Uri> cache, Item item)
        {
            var expectedResources = ExpectedResources(item);
            if (item.TryGetID(TMDB.ID, out var id))
            {
                if (item is Movie)
                {
                    expectedResources.Add(new Uri(string.Format(API.MOVIES.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
                else if (item is TVShow)
                {
                    //uris.Add(new Uri(string.Format(API.TV.GET_EXTERNAL_IDS.GetURL(), id), UriKind.RelativeOrAbsolute));
                }
            }

            CacheCount(cache.Keys, expectedResources.Count, expectedResources);
        }
        private static void CacheCount(ICollection<Uri> cache, int expectedCount, ICollection<Uri> expectedUris = null)
        {
            Assert.AreEqual(expectedCount, cache.Count, expectedUris == null ? "" : $"\n{ReportAsymmetricSetDiff(cache, expectedUris)}");
        }

        private static string ReportAsymmetricSetDiff<T>(ICollection<T> collection1, ICollection<T> collection2)
        {
            var set1 = new HashSet<T>(collection1);
            set1.ExceptWith(collection2);

            var set2 = new HashSet<T>(collection2);
            set2.ExceptWith(collection1);

            var report = new List<string>();
            if (set2.Count > 0)
            {
                report.Add(string.Join("\n\t\t", set2.Select(uri => uri.ToString()).Prepend("\tMissing:")));
            }
            if (set1.Count > 0)
            {
                report.Add(string.Join("\n\t\t", set1.Select(uri => uri.ToString()).Prepend("\tExtra:")));
            }

            return string.Join("\n", report);
        }

        public static int ExpectedMovieResourceCount => GetExpectedResourceCount(ItemType.Movie);
        public static int ExpectedTVResourceCount => GetExpectedResourceCount(ItemType.TVShow);

        private static IDictionary<ItemType, int> ExpectedResourceCounts = new Dictionary<ItemType, int>();
        private static int GetExpectedResourceCount(ItemType type) => ComputeIfAbsent(ExpectedResourceCounts, type, key => ComputeExpectedResourceCount(key));
        private static TValue ComputeIfAbsent<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> compute)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return dictionary[key] = compute(key);
        }

        public static ISet<Uri> ExpectedResources(Item item)
        {
            if (!TMDB.ITEM_PROPERTIES.TryGetValue(item.ItemType, out var properties) || !item.TryGetID(TMDB.ID, out var id))
            {
                return new HashSet<Uri>();
            }

            return properties.PropertyLookup.Keys.Select(property => new UniformItemIdentifier(item, property)).ToHashSet<Uri>();
        }

        private static int ComputeExpectedResourceCount(ItemType type)
        {
            if (!TMDB.ITEM_PROPERTIES.TryGetValue(type, out var properties))
            {
                return 0;
            }

            int count = properties.PropertyLookup.Count;
            if (type == ItemType.Movie)
            {
                // external ids endpoint
                count++;
            }
            else if (type == ItemType.TVShow)
            {

            }

            return count;
        }
    }
}
