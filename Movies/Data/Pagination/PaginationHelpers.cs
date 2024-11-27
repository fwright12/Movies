using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading;

namespace Movies
{
    public static class Helpers
    {
        public static async Task<int?> GetTotalPages(HttpResponseMessage response, IJsonParser<int> parse) => parse?.TryGetValue(JsonNode.Parse(await response.Content.ReadAsStringAsync()), out var result) == true ? result : (int?)null;

        public static IEnumerable<int> LazyRange(int start, int step = 1)
        {
            while (true)
            {
                yield return (start += step) - step;
            }
        }

        private static IEnumerable<T> ToEnumerable<T>(IEnumerator<T> itr)
        {
            while (itr.MoveNext())
            {
                yield return itr.Current;
            }
        }

        private class PageEnumerator : IAsyncEnumerator<HttpResponseMessage>
        {
            public HttpResponseMessage Current { get; private set; }
            public HttpClient Client { get; }
            public IPagedRequest Request { get; }
            public string[] Parameters { get; }
            public int Page => PageItr.Current < 0 ? (TotalPages ?? 1) - ~PageItr.Current : PageItr.Current;

            private int? TotalPages;
            private IEnumerator<int> PageItr;

            public PageEnumerator(HttpClient client, IPagedRequest request, IEnumerable<int> pages, params string[] parameters)
            {
                Current = null;

                Client = client;
                Request = request;
                PageItr = pages.GetEnumerator();
                Parameters = parameters;

                TotalPages = null;
            }

            public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);

            public async ValueTask<bool> MoveNextAsync()
            {
                if (!PageItr.MoveNext() || Page < 0 || Page > TotalPages)
                {
                    return false;
                }

                if (PageItr.Current < 0 && !TotalPages.HasValue)
                {
                    var itr = new PageEnumerator(Client, Request, Enumerable.Range(1, 1), Parameters);
                    await itr.MoveNextAsync();
                    TotalPages = itr.TotalPages;

                    if (!TotalPages.HasValue)
                    {
                        return false;
                    }
                }

                var pageRequest = Request.GetRequest(Page, Parameters);
                Current = await Client.SendAsync(pageRequest);

                if (!Current.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(Current.ReasonPhrase);
                }

                TotalPages = await Request.GetTotalPages(Current);

                return true;
            }
        }

        public static async IAsyncEnumerable<HttpResponseMessage> GetPagesAsync(this HttpClient client, IPagedRequest request, IEnumerable<int> pages, CancellationToken cancellationToken = default, params string[] parameters)
        {
            var itr = new PageEnumerator(client, request, pages, parameters);

            while (await itr.MoveNextAsync())
            {
                yield return itr.Current;
            }
        }

        public static async IAsyncEnumerable<HttpResponseMessage> GetPagesAsync1(this HttpClient client, IPagedRequest request, IEnumerable<int> pages, CancellationToken cancellationToken = default, params string[] parameters)
        {
            int? totalPages = null;
            var pageItr = pages.GetEnumerator();
            int page;

            do
            {
                if (!pageItr.MoveNext())
                {
                    break;
                }

                if (pageItr.Current < 0)
                {
                    if (totalPages.HasValue)
                    {
                        page = totalPages.Value - ~pageItr.Current;
                    }
                    else
                    {
                        // Cause a query of page 0 so we can get total pages
                        page = 0;
                    }
                }
                else
                {
                    page = pageItr.Current;
                }

                var pageRequest = request.GetRequest(page, parameters);
                var response = await client.SendAsync(pageRequest, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(response.ReasonPhrase);
                }

                totalPages = await request.GetTotalPages(response);

                if (page == pageItr.Current || page == totalPages - ~pageItr.Current)
                {
                    yield return response;
                }
            }
            while (page >= 0 && page < totalPages);
        }

        public static async IAsyncEnumerable<T> FlattenPages<T>(IAsyncEnumerable<HttpResponseMessage> pages, IJsonParser<IEnumerable<T>> parse, bool reverse = false)
        {
            await foreach (var page in pages)
            {
                var json = JsonNode.Parse(await page.Content.ReadAsStringAsync());

                if (parse.TryGetValue(json, out var items))
                {
                    if (reverse)
                    {
                        items = items.Reverse();
                    }

                    foreach (var item in items)
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}
