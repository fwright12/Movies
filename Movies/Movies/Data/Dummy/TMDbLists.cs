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
        public static readonly string TMDB_PERSONAL_WATCHLIST_MOVIES_RESPONSE = @"{
    ""page"": 1,
    ""results"": [
        {
            ""adult"": false,
            ""backdrop_path"": ""/70AV2Xx5FQYj20labp0EGdbjI6E.jpg"",
            ""genre_ids"": [
                53,
                28,
                80
            ],
            ""id"": 637649,
            ""original_language"": ""en"",
            ""original_title"": ""Wrath of Man"",
            ""overview"": ""A cold and mysterious new security guard for a Los Angeles cash truck company surprises his co-workers when he unleashes precision skills during a heist. The crew is left wondering who he is and where he came from. Soon, the marksman's ultimate motive becomes clear as he takes dramatic and irrevocable steps to settle a score."",
            ""popularity"": 125.722,
            ""poster_path"": ""/M7SUK85sKjaStg4TKhlAVyGlz3.jpg"",
            ""release_date"": ""2021-04-22"",
            ""title"": ""Wrath of Man"",
            ""video"": false,
            ""vote_average"": 7.66,
            ""vote_count"": 4739
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/ewUqXnwiRLhgmGhuksOdLgh49Ch.jpg"",
            ""genre_ids"": [
                12,
                878
            ],
            ""id"": 696806,
            ""original_language"": ""en"",
            ""original_title"": ""The Adam Project"",
            ""overview"": ""After accidentally crash-landing in 2022, time-traveling fighter pilot Adam Reed teams up with his 12-year-old self on a mission to save the future."",
            ""popularity"": 46.859,
            ""poster_path"": ""/wFjboE0aFZNbVOF05fzrka9Fqyx.jpg"",
            ""release_date"": ""2022-03-11"",
            ""title"": ""The Adam Project"",
            ""video"": false,
            ""vote_average"": 7.054,
            ""vote_count"": 3875
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/8z5qtVE2N3AuA3bmvIQsI10Wjg8.jpg"",
            ""genre_ids"": [
                27,
                18,
                10770
            ],
            ""id"": 7342,
            ""original_language"": ""en"",
            ""original_title"": ""Carrie"",
            ""overview"": ""Carrie White is a lonely and painfully shy teenage girl with telekinetic powers who is slowly pushed to the edge of insanity by frequent bullying from both classmates at her school, and her own religious, but abusive, mother."",
            ""popularity"": 21.808,
            ""poster_path"": ""/knjeEeeyIwDkUtZwDfJOcUIuNdB.jpg"",
            ""release_date"": ""2002-11-04"",
            ""title"": ""Carrie"",
            ""video"": false,
            ""vote_average"": 6.104,
            ""vote_count"": 438
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/l6hQWH9eDksNJNiXWYRkWqikOdu.jpg"",
            ""genre_ids"": [
                14,
                18,
                80
            ],
            ""id"": 497,
            ""original_language"": ""en"",
            ""original_title"": ""The Green Mile"",
            ""overview"": ""A supernatural tale set on death row in a Southern prison, where gentle giant John Coffey possesses the mysterious power to heal people's ailments. When the cell block's head guard, Paul Edgecomb, recognizes Coffey's miraculous gift, he tries desperately to help stave off the condemned man's execution."",
            ""popularity"": 101.903,
            ""poster_path"": ""/8VG8fDNiy50H4FedGwdSVUPoaJe.jpg"",
            ""release_date"": ""1999-12-10"",
            ""title"": ""The Green Mile"",
            ""video"": false,
            ""vote_average"": 8.509,
            ""vote_count"": 16186
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/fbTxsnJcQwuwzCEu9VEiU9lV75Y.jpg"",
            ""genre_ids"": [
                28,
                53
            ],
            ""id"": 645788,
            ""original_language"": ""en"",
            ""original_title"": ""The Protégé"",
            ""overview"": ""Rescued as a child by the legendary assassin Moody and trained in the family business, Anna is the world’s most skilled contract killer. When Moody, the man who was like a father to her and taught her everything she needs to know about trust and survival, is brutally killed, Anna vows revenge. As she becomes entangled with an enigmatic killer whose attraction to her goes way beyond cat and mouse, their confrontation turns deadly and the loose ends of a life spent killing will weave themselves ever tighter."",
            ""popularity"": 46.473,
            ""poster_path"": ""/iQUj7MptHUlcXpaMLrqRNZRxGA9.jpg"",
            ""release_date"": ""2021-08-19"",
            ""title"": ""The Protégé"",
            ""video"": false,
            ""vote_average"": 6.6,
            ""vote_count"": 863
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/4OTYefcAlaShn6TGVK33UxLW9R7.jpg"",
            ""genre_ids"": [
                28,
                12,
                53
            ],
            ""id"": 476669,
            ""original_language"": ""en"",
            ""original_title"": ""The King's Man"",
            ""overview"": ""As a collection of history's worst tyrants and criminal masterminds gather to plot a war to wipe out millions, one man must race against time to stop them."",
            ""popularity"": 95.333,
            ""poster_path"": ""/mpOzVPDFDI15WaKV6dqpYp441rZ.jpg"",
            ""release_date"": ""2021-12-22"",
            ""title"": ""The King's Man"",
            ""video"": false,
            ""vote_average"": 6.793,
            ""vote_count"": 4016
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/4YzGG8IPduUQRVnQ0vN5laHA1rH.jpg"",
            ""genre_ids"": [
                35,
                18
            ],
            ""id"": 773,
            ""original_language"": ""en"",
            ""original_title"": ""Little Miss Sunshine"",
            ""overview"": ""A family loaded with quirky, colorful characters piles into an old van and road trips to California for little Olive to compete in a beauty pageant."",
            ""popularity"": 18.573,
            ""poster_path"": ""/cfVDxpSgP4J7wPavgZTm8KbEai6.jpg"",
            ""release_date"": ""2006-07-26"",
            ""title"": ""Little Miss Sunshine"",
            ""video"": false,
            ""vote_average"": 7.7,
            ""vote_count"": 6580
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/xXWT0je8dTFFNBq6P2CeTZkPUu2.jpg"",
            ""genre_ids"": [
                12,
                28,
                53,
                9648
            ],
            ""id"": 2059,
            ""original_language"": ""en"",
            ""original_title"": ""National Treasure"",
            ""overview"": ""Modern treasure hunters, led by archaeologist Ben Gates, search for a chest of riches rumored to have been stashed away by George Washington, Thomas Jefferson and Benjamin Franklin during the Revolutionary War. The chest's whereabouts may lie in secret clues embedded in the Constitution and the Declaration of Independence, and Gates is in a race to find the gold before his enemies do."",
            ""popularity"": 35.487,
            ""poster_path"": ""/pxL6E4GBOPUG6CdkO9cUQN5VMwI.jpg"",
            ""release_date"": ""2004-11-19"",
            ""title"": ""National Treasure"",
            ""video"": false,
            ""vote_average"": 6.616,
            ""vote_count"": 5915
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/ae25gpphxDPEYGUxvAGmR07oQqC.jpg"",
            ""genre_ids"": [
                27,
                35
            ],
            ""id"": 800497,
            ""original_language"": ""en"",
            ""original_title"": ""Werewolves Within"",
            ""overview"": ""When a proposed pipeline creates hostilities between residents of a small town, a newly-arrived forest ranger must keep the peace after a snowstorm confines the townspeople to an old lodge. But when a mysterious creature begins terrorizing the group, their worst tendencies and prejudices rise to the surface, and it is up to the ranger to keep the residents alive, both from each other and the monster which plagues them."",
            ""popularity"": 16.809,
            ""poster_path"": ""/5CGgbgyvmE39Yoqa80GKsgClbQm.jpg"",
            ""release_date"": ""2021-06-25"",
            ""title"": ""Werewolves Within"",
            ""video"": false,
            ""vote_average"": 6.048,
            ""vote_count"": 450
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/qnVL4BNPlREYq4HKl6L62hiicYE.jpg"",
            ""genre_ids"": [
                28,
                14,
                878
            ],
            ""id"": 9947,
            ""original_language"": ""en"",
            ""original_title"": ""Elektra"",
            ""overview"": ""Elektra the warrior survives a near-death experience, becomes an assassin-for-hire, and tries to protect her two latest targets, a single father and his young daughter, from a group of supernatural assassins."",
            ""popularity"": 25.441,
            ""poster_path"": ""/9Azi1GBNj3gPPwmQWAMcATg7JOl.jpg"",
            ""release_date"": ""2005-01-13"",
            ""title"": ""Elektra"",
            ""video"": false,
            ""vote_average"": 5.035,
            ""vote_count"": 2258
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/aCHn2TXYJfzPXQKA6r9mKPbMlUB.jpg"",
            ""genre_ids"": [
                35,
                18
            ],
            ""id"": 37165,
            ""original_language"": ""en"",
            ""original_title"": ""The Truman Show"",
            ""overview"": ""Truman Burbank is the star of The Truman Show, a 24-hour-a-day reality TV show that broadcasts every aspect of his life without his knowledge. His entire life has been an unending soap opera for consumption by the rest of the world. And everyone he knows, including his wife and his best friend, is really an actor, paid to be part of his life."",
            ""popularity"": 62.351,
            ""poster_path"": ""/vuza0WqY239yBXOadKlGwJsZJFE.jpg"",
            ""release_date"": ""1998-06-04"",
            ""title"": ""The Truman Show"",
            ""video"": false,
            ""vote_average"": 8.135,
            ""vote_count"": 17071
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/tQVJJ2kODGU0N7UnyPauv0qk8ag.jpg"",
            ""genre_ids"": [
                28,
                18
            ],
            ""id"": 8409,
            ""original_language"": ""en"",
            ""original_title"": ""A Man Apart"",
            ""overview"": ""When Vetter's wife is killed in a botched hit organized by Diablo, he seeks revenge against those responsible. But in the process, Vetter and Hicks have to fight their way up the chain to get to Diablo but it's easier said than done when all Vetter can focus on is revenge."",
            ""popularity"": 29.489,
            ""poster_path"": ""/z0JUBNk4BTBmeMOudrRH9GnOmK0.jpg"",
            ""release_date"": ""2003-04-04"",
            ""title"": ""A Man Apart"",
            ""video"": false,
            ""vote_average"": 6.1,
            ""vote_count"": 817
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/cdbjlFEW1slS3QHMQ2e1g6zgw7N.jpg"",
            ""genre_ids"": [
                18,
                80
            ],
            ""id"": 627,
            ""original_language"": ""en"",
            ""original_title"": ""Trainspotting"",
            ""overview"": ""Heroin addict Mark Renton stumbles through bad ideas and sobriety attempts with his unreliable friends -- Sick Boy, Begbie, Spud and Tommy. He also has an underage girlfriend, Diane, along for the ride. After cleaning up and moving from Edinburgh to London, Mark finds he can't escape the life he left behind when Begbie shows up at his front door on the lam, and a scheming Sick Boy follows."",
            ""popularity"": 26.045,
            ""poster_path"": ""/bhY62Dw8iW54DIhxPQerbuB9DOP.jpg"",
            ""release_date"": ""1996-02-23"",
            ""title"": ""Trainspotting"",
            ""video"": false,
            ""vote_average"": 7.969,
            ""vote_count"": 9107
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/mwlLjL3jTDmTdLWe2PyUVqYQTuK.jpg"",
            ""genre_ids"": [
                18,
                80,
                53
            ],
            ""id"": 22803,
            ""original_language"": ""en"",
            ""original_title"": ""Law Abiding Citizen"",
            ""overview"": ""A frustrated man decides to take justice into his own hands after a plea bargain sets one of his family's killers free. He targets not only the killer but also the district attorney and others involved in the deal."",
            ""popularity"": 47.895,
            ""poster_path"": ""/fcEXcip7v0O1ndV4VUdFqJSqbOg.jpg"",
            ""release_date"": ""2009-10-15"",
            ""title"": ""Law Abiding Citizen"",
            ""video"": false,
            ""vote_average"": 7.372,
            ""vote_count"": 4711
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/htbWYT0oQDsttfs8LLmz8I2pbve.jpg"",
            ""genre_ids"": [
                14,
                878,
                35
            ],
            ""id"": 424781,
            ""original_language"": ""en"",
            ""original_title"": ""Sorry to Bother You"",
            ""overview"": ""In an alternate present-day version of Oakland, black telemarketer Cassius Green discovers a magical key to professional success – which propels him into a macabre universe."",
            ""popularity"": 16.751,
            ""poster_path"": ""/peTl1V04E9ppvhgvNmSX0r2ALqO.jpg"",
            ""release_date"": ""2018-07-06"",
            ""title"": ""Sorry to Bother You"",
            ""video"": false,
            ""vote_average"": 6.839,
            ""vote_count"": 1415
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/6XGreAQomYnW7WcbOKvfRFYEW1D.jpg"",
            ""genre_ids"": [
                18,
                878
            ],
            ""id"": 62215,
            ""original_language"": ""en"",
            ""original_title"": ""Melancholia"",
            ""overview"": ""Two sisters find their already strained relationship challenged as a mysterious new planet threatens to collide with Earth."",
            ""popularity"": 24.982,
            ""poster_path"": ""/kd0PqvTsJd6DOY7Jxu0R4vEfpsj.jpg"",
            ""release_date"": ""2011-05-26"",
            ""title"": ""Melancholia"",
            ""video"": false,
            ""vote_average"": 7.203,
            ""vote_count"": 3250
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/k7lwdrpZ92BDahx6TLdkPE65cpJ.jpg"",
            ""genre_ids"": [
                18,
                53
            ],
            ""id"": 71859,
            ""original_language"": ""en"",
            ""original_title"": ""We Need to Talk About Kevin"",
            ""overview"": ""After her son Kevin commits a horrific act, troubled mother Eva reflects on her complicated relationship with her disturbed son as he grew from a toddler into a teenager."",
            ""popularity"": 22.735,
            ""poster_path"": ""/auAmiRmbBQ5QIYGpWgcGBoBQY3b.jpg"",
            ""release_date"": ""2011-09-28"",
            ""title"": ""We Need to Talk About Kevin"",
            ""video"": false,
            ""vote_average"": 7.6,
            ""vote_count"": 2617
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/kJwQXgAk440l2vZOspUJee6inim.jpg"",
            ""genre_ids"": [
                18,
                53,
                9648
            ],
            ""id"": 399057,
            ""original_language"": ""en"",
            ""original_title"": ""The Killing of a Sacred Deer"",
            ""overview"": ""Dr. Steven Murphy is a renowned cardiovascular surgeon who presides over a spotless household with his wife and two children. Lurking at the margins of his idyllic suburban existence is Martin, a fatherless teen who insinuates himself into the doctor's life in gradually unsettling ways."",
            ""popularity"": 29.957,
            ""poster_path"": ""/e4DGlsc9g0h5AyoyvvAuIRnofN7.jpg"",
            ""release_date"": ""2017-10-20"",
            ""title"": ""The Killing of a Sacred Deer"",
            ""video"": false,
            ""vote_average"": 7.014,
            ""vote_count"": 3475
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/xczDgm9F3uN3PqyP5yvs3JI9HrJ.jpg"",
            ""genre_ids"": [
                18,
                10749,
                10752
            ],
            ""id"": 15764,
            ""original_language"": ""en"",
            ""original_title"": ""Sophie's Choice"",
            ""overview"": ""Stingo, a young writer, moves to Brooklyn in 1947 to begin work on his first novel. As he becomes friendly with Sophie and her lover Nathan, he learns that she is a Holocaust survivor. Flashbacks reveal her harrowing story, from pre-war prosperity to Auschwitz. In the present, Sophie and Nathan's relationship increasingly unravels as Stingo grows closer to Sophie and Nathan's fragile mental state becomes ever more apparent."",
            ""popularity"": 20.461,
            ""poster_path"": ""/6zayvLCWvF8ImK5UqwRUbETcpVU.jpg"",
            ""release_date"": ""1982-12-08"",
            ""title"": ""Sophie's Choice"",
            ""video"": false,
            ""vote_average"": 7.336,
            ""vote_count"": 790
        },
        {
            ""adult"": false,
            ""backdrop_path"": ""/51UDa3IuGyiKGe2MgMcLGvJE2Fm.jpg"",
            ""genre_ids"": [
                53,
                80,
                18
            ],
            ""id"": 1213,
            ""original_language"": ""en"",
            ""original_title"": ""The Talented Mr. Ripley"",
            ""overview"": ""Tom Ripley is a calculating young man who believes it's better to be a fake somebody than a real nobody. Opportunity knocks in the form of a wealthy U.S. shipbuilder who hires Tom to travel to Italy to bring back his playboy son, Dickie. Ripley worms his way into the idyllic lives of Dickie and his girlfriend, plunging into a daring scheme of duplicity, lies and murder."",
            ""popularity"": 20.8,
            ""poster_path"": ""/6ojHgqtIR41O2qLKa7LFUVj0cZa.jpg"",
            ""release_date"": ""1999-12-25"",
            ""title"": ""The Talented Mr. Ripley"",
            ""video"": false,
            ""vote_average"": 7.181,
            ""vote_count"": 3297
        }
    ],
    ""total_pages"": 11,
    ""total_results"": 214
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
