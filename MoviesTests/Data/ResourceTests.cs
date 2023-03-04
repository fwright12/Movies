namespace MoviesTests.Data
{
    public static class DataHelpers
    {
        public static Task<(bool Success, T Resource)> Get<T>(this IControllerLink link, string url) => Get<T>(link, new Uri(url, UriKind.Relative));
        public static Task<(bool Success, T Resource)> Get<T>(this IControllerLink link, Uri uri)
        {
            var controller = new Controller().AddLast(link);
            return controller.Get<T>(uri);
        }
    }
}
