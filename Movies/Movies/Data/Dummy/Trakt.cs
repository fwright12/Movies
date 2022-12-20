#if DEBUG
using System;
using System.Collections.Generic;
using System.Text;

namespace Movies
{
    public partial class HttpClient
    {
        public static readonly string TRAKT_ACCOUNT_LAST_ACTIVITIES_RESPONSE = @"{
    ""all"": ""2022-10-16T20:53:26.000Z"",
    ""movies"": {
        ""watched_at"": ""2022-04-23T21:24:33.000Z"",
        ""collected_at"": ""2022-03-30T17:32:59.000Z"",
        ""rated_at"": ""2022-03-30T17:32:59.000Z"",
        ""watchlisted_at"": ""2022-04-23T21:01:04.000Z"",
        ""recommendations_at"": ""2022-03-30T17:32:59.000Z"",
        ""commented_at"": ""2022-03-30T17:32:59.000Z"",
        ""paused_at"": ""2022-03-30T17:32:59.000Z"",
        ""hidden_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""episodes"": {
        ""watched_at"": ""2022-04-16T21:11:02.000Z"",
        ""collected_at"": ""2022-03-30T17:32:59.000Z"",
        ""rated_at"": ""2022-03-30T17:32:59.000Z"",
        ""watchlisted_at"": ""2022-03-30T17:32:59.000Z"",
        ""commented_at"": ""2022-03-30T17:32:59.000Z"",
        ""paused_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""shows"": {
        ""rated_at"": ""2022-03-30T17:32:59.000Z"",
        ""watchlisted_at"": ""2022-04-21T19:55:20.000Z"",
        ""recommendations_at"": ""2022-03-30T17:32:59.000Z"",
        ""commented_at"": ""2022-03-30T17:32:59.000Z"",
        ""hidden_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""seasons"": {
        ""rated_at"": ""2022-03-30T17:32:59.000Z"",
        ""watchlisted_at"": ""2022-03-30T17:32:59.000Z"",
        ""commented_at"": ""2022-03-30T17:32:59.000Z"",
        ""hidden_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""comments"": {
        ""liked_at"": ""2022-03-30T17:32:59.000Z"",
        ""blocked_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""lists"": {
        ""liked_at"": ""2022-03-30T17:32:59.000Z"",
        ""updated_at"": ""2022-10-16T20:53:26.000Z"",
        ""commented_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""watchlist"": {
        ""updated_at"": ""2022-04-23T21:01:04.000Z""
    },
    ""recommendations"": {
        ""updated_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""collaborations"": {
        ""updated_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""account"": {
        ""settings_at"": ""2022-03-30T17:33:51.000Z"",
        ""followed_at"": ""2022-03-30T17:32:59.000Z"",
        ""following_at"": ""2022-03-30T17:32:59.000Z"",
        ""pending_at"": ""2022-03-30T17:32:59.000Z"",
        ""requested_at"": ""2022-03-30T17:32:59.000Z""
    },
    ""saved_filters"": {
        ""updated_at"": ""2022-03-30T17:32:59.000Z""
    }
}";
        public static readonly string TRAKT_ACCOUNT_LISTS_RESPONSE = @"[
            {
                ""name"": ""Big list [Trakt]"",
                ""description"": """",
                ""privacy"": ""private"",
                ""type"": ""personal"",
                ""display_numbers"": false,
                ""allow_comments"": true,
                ""sort_by"": ""rank"",
                ""sort_how"": ""asc"",
                ""created_at"": ""2022-04-06T21:16:07.000Z"",
                ""updated_at"": ""2022-04-17T20:32:35.000Z"",
                ""item_count"": 46,
                ""comment_count"": 0,
                ""likes"": 0,
                ""ids"": {
                    ""trakt"": 23321509,
                    ""slug"": ""big-list-trakt""
                },
                ""user"": {
                    ""username"": ""GreenMountainLabs"",
                    ""private"": false,
                    ""name"": """",
                    ""vip"": false,
                    ""vip_ep"": false,
                    ""ids"": {
                        ""slug"": ""greenmountainlabs""
                    }
                }
            },
            {
                ""name"": ""Favorites"",
                ""description"": """",
                ""privacy"": ""private"",
                ""type"": ""personal"",
                ""display_numbers"": false,
                ""allow_comments"": true,
                ""sort_by"": ""rank"",
                ""sort_how"": ""asc"",
                ""created_at"": ""2022-04-08T02:53:47.000Z"",
                ""updated_at"": ""2022-10-16T17:37:23.000Z"",
                ""item_count"": 4,
                ""comment_count"": 0,
                ""likes"": 0,
                ""ids"": {
                    ""trakt"": 23327889,
                    ""slug"": ""favorites""
                },
                ""user"": {
                    ""username"": ""GreenMountainLabs"",
                    ""private"": false,
                    ""name"": """",
                    ""vip"": false,
                    ""vip_ep"": false,
                    ""ids"": {
                        ""slug"": ""greenmountainlabs""
                    }
                }
            }
        ]";
        public static readonly string TRAKT_ACCOUNT_FAVORITES_RESPONSE = @"[
            {
                ""rank"": 1,
                ""id"": 689140778,
                ""listed_at"": ""2022-04-23T21:01:26.000Z"",
                ""notes"": null,
                ""type"": ""movie"",
                ""movie"": {
                    ""title"": ""The Matrix Resurrections"",
                    ""year"": 2021,
                    ""ids"": {
                        ""trakt"": 466465,
                        ""slug"": ""the-matrix-resurrections-2021"",
                        ""imdb"": ""tt10838180"",
                        ""tmdb"": 624860
                    }
                }
            },
            {
                ""rank"": 2,
                ""id"": 751635643,
                ""listed_at"": ""2022-10-13T23:33:21.000Z"",
                ""notes"": null,
                ""type"": ""show"",
                ""show"": {
                    ""title"": ""The Office"",
                    ""year"": 2005,
                    ""ids"": {
                        ""trakt"": 2302,
                        ""slug"": ""the-office"",
                        ""tvdb"": 73244,
                        ""imdb"": ""tt0386676"",
                        ""tmdb"": 2316,
                        ""tvrage"": 6061
                    }
                }
            },
            {
                ""rank"": 3,
                ""id"": 689140779,
                ""listed_at"": ""2022-04-23T21:01:26.000Z"",
                ""notes"": null,
                ""type"": ""movie"",
                ""movie"": {
                    ""title"": ""Venom: Let There Be Carnage"",
                    ""year"": 2021,
                    ""ids"": {
                        ""trakt"": 429764,
                        ""slug"": ""venom-let-there-be-carnage-2021"",
                        ""imdb"": ""tt7097896"",
                        ""tmdb"": 580489
                    }
                }
            },
            {
                ""rank"": 4,
                ""id"": 689140780,
                ""listed_at"": ""2022-04-23T21:01:26.000Z"",
                ""notes"": null,
                ""type"": ""movie"",
                ""movie"": {
                    ""title"": ""Resident Evil: Welcome to Raccoon City"",
                    ""year"": 2021,
                    ""ids"": {
                        ""trakt"": 307489,
                        ""slug"": ""resident-evil-welcome-to-raccoon-city-2021"",
                        ""imdb"": ""tt6920084"",
                        ""tmdb"": 460458
                    }
                }
            }
        ]";
    }
}
#endif
