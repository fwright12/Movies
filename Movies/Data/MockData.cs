#if DEBUG
using Movies.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Movies.Data
{
    public class MockData
    {
        public static readonly List<RatingTemplate> RATING_TEMPLATES = new List<RatingTemplate>
        {
            new RatingTemplate
            {
                Name = "Rotten Tomatoes",
                //LogoURL = "https://www.rottentomatoes.com/assets/pizza-pie/images/rottentomatoes_logo_40.336d6fe66ff.png",
                URLJavaScipt = @"
type = item.type === 'tv' ? 'tv' : 'm';
titles = [item.title.split(' ').join('_').split(/\W/g).join('').toLowerCase()];
if (titles[0].startsWith('the_')) titles.push(titles[0].substring(4));
titles.flatMap(title => [`https://www.rottentomatoes.com/${type}/${title}_${item.year}`, `https://www.rottentomatoes.com/${type}/${title}`])
",
                ScoreJavaScript = "JSON.parse(document.scripts['media-scorecard-json'].text).criticsScore.score + '%'"
            },
            new RatingTemplate
            {
                Name = "Rotten Tomatoes",
                URLJavaScipt = @"
type = item.type === 'tv' ? 'tv' : 'm';
titles = [item.title.split(' ').join('_').split(/\W/g).join('').toLowerCase()];
if (titles[0].startsWith('the_')) titles.push(titles[0].substring(4));
titles.flatMap(title => [`https://www.rottentomatoes.com/${type}/${title}_${item.year}`, `https://www.rottentomatoes.com/${type}/${title}`])
",
                ScoreJavaScript = "JSON.parse(document.scripts['media-scorecard-json'].text).audienceScore.score + '%'"
            },
            new RatingTemplate
            {
                Name = "Test",
                URLJavaScipt = @"[
`www.tmdb.com/${item.id.tmdb}`,
`www.imdb.com/${item.id.imdb}`,
`www.wikidata.com/${item.id.wikidata}`,
`www.facebook.com/${item.id.facebook}`,
`www.instagram.com/${item.id.instagram}`,
`www.twitter.com/${item.id.twitter}`,
]",
                ScoreJavaScript = "return 4;"
            },
            new RatingTemplate
            {
                Name = "Empty",
            }
        };
    }
}
#endif