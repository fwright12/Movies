#if DEBUG
using System;
using System.Collections.Generic;
using System.Text;

namespace Movies
{
    public static class DUMMY_TMDB_LISTS
    {
        public static readonly string TMDB_ACCOUNT_LISTS_RESPONSE = @"{
    ""page"": 1,
    ""results"": [
        {
            ""adult"": 0,
            ""average_rating"": 8.16767,
            ""created_at"": ""2022-04-08 02:06:59"",
            ""description"": """",
            ""featured"": 0,
            ""id"": 8197823,
            ""iso_3166_1"": ""US"",
            ""iso_639_1"": ""en"",
            ""name"": ""Watched"",
            ""number_of_items"": 3,
            ""public"": 0,
            ""revenue"": ""905117392"",
            ""runtime"": 307,
            ""sort_by"": 1,
            ""updated_at"": ""2022-07-11 19:51:34""
        },
        {
            ""adult"": 0,
            ""average_rating"": 7.6135,
            ""created_at"": ""2022-07-11 18:53:16"",
            ""description"": """",
            ""featured"": 0,
            ""id"": 8210018,
            ""iso_3166_1"": ""US"",
            ""iso_639_1"": ""en"",
            ""name"": ""New List"",
            ""number_of_items"": 2,
            ""public"": 0,
            ""revenue"": ""156497322"",
            ""runtime"": 208,
            ""sort_by"": 1,
            ""updated_at"": ""2022-07-11 19:17:28""
        },
        {
            ""adult"": 0,
            ""average_rating"": 6.97822,
            ""created_at"": ""2022-04-21 19:24:35"",
            ""description"": """",
            ""featured"": 0,
            ""id"": 8199806,
            ""iso_3166_1"": ""US"",
            ""iso_639_1"": ""en"",
            ""name"": ""Big list [TMDb]"",
            ""number_of_items"": 46,
            ""public"": 0,
            ""revenue"": ""11047119608"",
            ""runtime"": 5613,
            ""sort_by"": 1,
            ""updated_at"": ""2022-04-21 19:31:26""
        }
    ],
    ""total_pages"": 1,
    ""total_results"": 3
}";
        public static readonly string TMDB_ACCOUNT_FAVORITE_MOVIES_RESPONSE = @"{
    ""page"": 1,
    ""results"": [
        {
            ""adult"": false,
            ""backdrop_path"": ""/o76ZDm8PS9791XiuieNB93UZcRV.jpg"",
            ""genre_ids"": [
                27,
                28,
                878
            ],
            ""id"": 460458,
            ""original_language"": ""en"",
            ""original_title"": ""Resident Evil: Welcome to Raccoon City"",
            ""overview"": ""Once the booming home of pharmaceutical giant Umbrella Corporation, Raccoon City is now a dying Midwestern town. The company’s exodus left the city a wasteland…with great evil brewing below the surface. When that evil is unleashed, the townspeople are forever…changed…and a small group of survivors must work together to uncover the truth behind Umbrella and make it through the night."",
            ""popularity"": 360.235,
            ""poster_path"": ""/7uRbWOXxpWDMtnsd2PF3clu65jc.jpg"",
            ""release_date"": ""2021-11-24"",
            ""title"": ""Resident Evil: Welcome to Raccoon City"",
            ""video"": false,
            ""vote_average"": 6.1,
            ""vote_count"": 1782
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/vIgyYkXkg6NC2whRbYjBD7eb3Er.jpg"",
            ""genre_ids"": [
                878,
                28,
                12
            ],
            ""id"": 580489,
            ""original_language"": ""en"",
            ""original_title"": ""Venom: Let There Be Carnage"",
            ""overview"": ""After finding a host body in investigative reporter Eddie Brock, the alien symbiote must face a new enemy, Carnage, the alter ego of serial killer Cletus Kasady."",
            ""popularity"": 460.644,
            ""poster_path"": ""/rjkmN1dniUHVYAtwuV3Tji7FsDO.jpg"",
            ""release_date"": ""2021-09-30"",
            ""title"": ""Venom: Let There Be Carnage"",
            ""video"": false,
            ""vote_average"": 7,
            ""vote_count"": 7869
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/eNI7PtK6DEYgZmHWP9gQNuff8pv.jpg"",
            ""genre_ids"": [
                878,
                28,
                12
            ],
            ""id"": 624860,
            ""original_language"": ""en"",
            ""original_title"": ""The Matrix Resurrections"",
            ""overview"": ""Plagued by strange memories, Neo's life takes an unexpected turn when he finds himself back inside the Matrix."",
            ""popularity"": 297.599,
            ""poster_path"": ""/8c4a8kE7PizaGQQnditMmI1xbRp.jpg"",
            ""release_date"": ""2021-12-16"",
            ""title"": ""The Matrix Resurrections"",
            ""video"": false,
            ""vote_average"": 6.6,
            ""vote_count"": 4029
        }
    ],
    ""total_pages"": 1,
    ""total_results"": 3
}";
        public static readonly string TMDB_ACCOUNT_FAVORITE_TV_RESPONSE = @"{
    ""page"": 1,
    ""results"": [
        {
            ""backdrop_path"": ""/vNpuAxGTl9HsUbHqam3E9CzqCvX.jpg"",
            ""first_air_date"": ""2005-03-24"",
            ""genre_ids"": [
                35
            ],
            ""id"": 2316,
            ""name"": ""The Office"",
            ""origin_country"": [
                ""US""
            ],
            ""original_language"": ""en"",
            ""original_name"": ""The Office"",
            ""overview"": ""The everyday lives of office employees in the Scranton, Pennsylvania branch of the fictional Dunder Mifflin Paper Company."",
            ""popularity"": 204.488,
            ""poster_path"": ""/qWnJzyZhyy74gjpSjIXWmuk0ifX.jpg"",
            ""vote_average"": 8.6,
            ""vote_count"": 2528
        }
    ],
    ""total_pages"": 1,
    ""total_results"": 1
}";
        public static readonly string TMDB_WATCHED_LIST_RESPONSE = @"{
    ""average_rating"": 8.16767,
    ""backdrop_path"": null,
    ""comments"": {
        ""movie:157336"": null,
        ""movie:329865"": null,
        ""tv:2316"": null
    },
    ""created_by"": {
        ""gravatar_hash"": ""05c2ac82a6f65015e186876e4a15ce05"",
        ""name"": """",
        ""username"": ""GreenMountainLabs""
    },
    ""description"": """",
    ""id"": 8197823,
    ""iso_3166_1"": ""US"",
    ""iso_639_1"": ""en"",
    ""name"": ""Watched"",
    ""object_ids"": {
        ""movie:157336"": ""50ef0cda760ee301bb074b57"",
        ""movie:329865"": ""54ff247cc3a3685ba6000005"",
        ""tv:2316"": ""52573079760ee3776a33fddd""
    },
    ""page"": 1,
    ""poster_path"": null,
    ""public"": false,
    ""results"": [
        {
            ""adult"": false,
            ""backdrop_path"": ""/xJHokMbljvjADYdit5fK5VQsXEG.jpg"",
            ""genre_ids"": [
                12,
                18,
                878
            ],
            ""id"": 157336,
            ""media_type"": ""movie"",
            ""original_language"": ""en"",
            ""original_title"": ""Interstellar"",
            ""overview"": ""The adventures of a group of explorers who make use of a newly discovered wormhole to surpass the limitations on human space travel and conquer the vast distances involved in an interstellar voyage."",
            ""popularity"": 169.81,
            ""poster_path"": ""/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg"",
            ""release_date"": ""2014-11-05"",
            ""title"": ""Interstellar"",
            ""video"": false,
            ""vote_average"": 8.4,
            ""vote_count"": 29195
        },
        {
            ""backdrop_path"": ""/vNpuAxGTl9HsUbHqam3E9CzqCvX.jpg"",
            ""first_air_date"": ""2005-03-24"",
            ""genre_ids"": [
                35
            ],
            ""id"": 2316,
            ""media_type"": ""tv"",
            ""name"": ""The Office"",
            ""origin_country"": [
                ""US""
            ],
            ""original_language"": ""en"",
            ""original_name"": ""The Office"",
            ""overview"": ""The everyday lives of office employees in the Scranton, Pennsylvania branch of the fictional Dunder Mifflin Paper Company."",
            ""popularity"": 204.488,
            ""poster_path"": ""/qWnJzyZhyy74gjpSjIXWmuk0ifX.jpg"",
            ""vote_average"": 8.6,
            ""vote_count"": 2528
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/lO5pFsXSwO7gkUbkhx4W6nooyVe.jpg"",
            ""genre_ids"": [
                18,
                878,
                9648
            ],
            ""id"": 329865,
            ""media_type"": ""movie"",
            ""original_language"": ""en"",
            ""original_title"": ""Arrival"",
            ""overview"": ""Taking place after alien crafts land around the world, an expert linguist is recruited by the military to determine whether they come in peace or are a threat."",
            ""popularity"": 68.225,
            ""poster_path"": ""/x2FJsf1ElAgr63Y3PNPtJrcmpoe.jpg"",
            ""release_date"": ""2016-11-10"",
            ""title"": ""Arrival"",
            ""video"": false,
            ""vote_average"": 7.6,
            ""vote_count"": 15368
        }
    ],
    ""revenue"": 905117392,
    ""runtime"": 307,
    ""sort_by"": ""original_order.asc"",
    ""total_pages"": 1,
    ""total_results"": 3
}";
        public static readonly string TMDB_NEW_LIST_RESPONSE = @"{
    ""average_rating"": 7.6135,
    ""backdrop_path"": null,
    ""comments"": {
        ""movie:624860"": null,
        ""tv:52814"": null
    },
    ""created_by"": {
        ""gravatar_hash"": ""05c2ac82a6f65015e186876e4a15ce05"",
        ""name"": """",
        ""username"": ""GreenMountainLabs""
    },
    ""description"": """",
    ""id"": 8210018,
    ""iso_3166_1"": ""US"",
    ""iso_639_1"": ""en"",
    ""name"": ""New List"",
    ""object_ids"": {
        ""movie:624860"": ""5d5c57c1a3d02700140aaefe"",
        ""tv:52814"": ""5259835b760ee34661aad7ed""
    },
    ""page"": 1,
    ""poster_path"": null,
    ""public"": false,
    ""results"": [
        {
            ""adult"": false,
            ""backdrop_path"": ""/eNI7PtK6DEYgZmHWP9gQNuff8pv.jpg"",
            ""genre_ids"": [
                878,
                28,
                12
            ],
            ""id"": 624860,
            ""media_type"": ""movie"",
            ""original_language"": ""en"",
            ""original_title"": ""The Matrix Resurrections"",
            ""overview"": ""Plagued by strange memories, Neo's life takes an unexpected turn when he finds himself back inside the Matrix."",
            ""popularity"": 297.599,
            ""poster_path"": ""/8c4a8kE7PizaGQQnditMmI1xbRp.jpg"",
            ""release_date"": ""2021-12-16"",
            ""title"": ""The Matrix Resurrections"",
            ""video"": false,
            ""vote_average"": 6.6,
            ""vote_count"": 4029
        },
        {
            ""backdrop_path"": ""/1qpUk27LVI9UoTS7S0EixUBj5aR.jpg"",
            ""first_air_date"": ""2022-03-24"",
            ""genre_ids"": [
                10759,
                10765
            ],
            ""id"": 52814,
            ""media_type"": ""tv"",
            ""name"": ""Halo"",
            ""origin_country"": [
                ""US""
            ],
            ""original_language"": ""en"",
            ""original_name"": ""Halo"",
            ""overview"": ""Depicting an epic 26th-century conflict between humanity and an alien threat known as the Covenant, the series weaves deeply drawn personal stories with action, adventure and a richly imagined vision of the future."",
            ""popularity"": 678.856,
            ""poster_path"": ""/nJUHX3XL1jMkk8honUZnUmudFb9.jpg"",
            ""vote_average"": 8.5,
            ""vote_count"": 1236
        }
    ],
    ""revenue"": 156497322,
    ""runtime"": 208,
    ""sort_by"": ""original_order.asc"",
    ""total_pages"": 1,
    ""total_results"": 2
}";
    }
}
#endif
