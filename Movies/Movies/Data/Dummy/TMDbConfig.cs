#if DEBUG
using System;
using System.Collections.Generic;
using System.Text;

namespace Movies
{
    public partial class HttpClient
    {
        public static readonly string MOVIE_GENRE_VALUES = @"{
    ""genres"": [
        {
            ""id"": 28,
            ""name"": ""Action""
        },
        {
            ""id"": 12,
            ""name"": ""Adventure""
        },
        {
            ""id"": 16,
            ""name"": ""Animation""
        },
        {
            ""id"": 35,
            ""name"": ""Comedy""
        },
        {
            ""id"": 80,
            ""name"": ""Crime""
        },
        {
            ""id"": 99,
            ""name"": ""Documentary""
        },
        {
            ""id"": 18,
            ""name"": ""Drama""
        },
        {
            ""id"": 10751,
            ""name"": ""Family""
        },
        {
            ""id"": 14,
            ""name"": ""Fantasy""
        },
        {
            ""id"": 36,
            ""name"": ""History""
        },
        {
            ""id"": 27,
            ""name"": ""Horror""
        },
        {
            ""id"": 10402,
            ""name"": ""Music""
        },
        {
            ""id"": 9648,
            ""name"": ""Mystery""
        },
        {
            ""id"": 10749,
            ""name"": ""Romance""
        },
        {
            ""id"": 878,
            ""name"": ""Science Fiction""
        },
        {
            ""id"": 10770,
            ""name"": ""TV Movie""
        },
        {
            ""id"": 53,
            ""name"": ""Thriller""
        },
        {
            ""id"": 10752,
            ""name"": ""War""
        },
        {
            ""id"": 37,
            ""name"": ""Western""
        }
    ]
}";
        public static readonly string TV_GENRE_VALUES = @"{
    ""genres"": [
        {
            ""id"": 10759,
            ""name"": ""Action \u0026 Adventure""
        },
        {
            ""id"": 16,
            ""name"": ""Animation""
        },
        {
            ""id"": 35,
            ""name"": ""Comedy""
        },
        {
            ""id"": 80,
            ""name"": ""Crime""
        },
        {
            ""id"": 99,
            ""name"": ""Documentary""
        },
        {
            ""id"": 18,
            ""name"": ""Drama""
        },
        {
            ""id"": 10751,
            ""name"": ""Family""
        },
        {
            ""id"": 10762,
            ""name"": ""Kids""
        },
        {
            ""id"": 9648,
            ""name"": ""Mystery""
        },
        {
            ""id"": 10763,
            ""name"": ""News""
        },
        {
            ""id"": 10764,
            ""name"": ""Reality""
        },
        {
            ""id"": 10765,
            ""name"": ""Sci-Fi \u0026 Fantasy""
        },
        {
            ""id"": 10766,
            ""name"": ""Soap""
        },
        {
            ""id"": 10767,
            ""name"": ""Talk""
        },
        {
            ""id"": 10768,
            ""name"": ""War \u0026 Politics""
        },
        {
            ""id"": 37,
            ""name"": ""Western""
        }
    ]
}";
        public static readonly string MOVIE_CERTIFICATION_VALUES = @"{
    ""certifications"": {
        ""CA"": [
            {
                ""certification"": ""G"",
                ""meaning"": ""All ages."",
                ""order"": 1
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Parental guidance advised. There is no age restriction but some material may not be suitable for all children."",
                ""order"": 2
            },
            {
                ""certification"": ""A"",
                ""meaning"": ""Admittance restricted to people 18 years of age or older. Sole purpose of the film is the portrayal of sexually explicit activity and/or explicit violence."",
                ""order"": 5
            },
            {
                ""certification"": ""18A"",
                ""meaning"": ""Persons under 18 years of age must be accompanied by an adult. In the Maritimes \u0026 Manitoba, children under the age of 14 are prohibited from viewing the film."",
                ""order"": 4
            },
            {
                ""certification"": ""14A"",
                ""meaning"": ""Persons under 14 years of age must be accompanied by an adult."",
                ""order"": 3
            }
        ],
        ""NL"": [
            {
                ""certification"": ""AL"",
                ""meaning"": ""All ages."",
                ""order"": 1
            },
            {
                ""certification"": ""6"",
                ""meaning"": ""Potentially harmful to children under 6 years."",
                ""order"": 2
            },
            {
                ""certification"": ""9"",
                ""meaning"": ""Potentially harmful to children under 9 years."",
                ""order"": 3
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Potentially harmful to children under 16 years; broadcasting is not allowed before 10:00 pm."",
                ""order"": 5
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Potentially harmful to children under 12 years; broadcasting is not allowed before 8:00 pm."",
                ""order"": 4
            }
        ],
        ""GB"": [
            {
                ""certification"": ""U"",
                ""meaning"": ""All ages admitted, there is nothing unsuitable for children."",
                ""order"": 1
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Home media only since 2002. 12A-rated films are usually given a 12 certificate for the VHS/DVD version unless extra material has been added that requires a higher rating. Nobody younger than 12 can rent or buy a 12-rated VHS, DVD, Blu-ray Disc, UMD or game. The content guidelines are identical to those used for the 12A certificate."",
                ""order"": 4
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""Only those over 15 years are admitted. Nobody younger than 15 can rent or buy a 15-rated VHS, DVD, Blu-ray Disc, UMD or game, or watch a film in the cinema with this rating. Films under this category can contain adult themes, hard drugs, frequent strong language and limited use of very strong language, strong violence and strong sex references, and nudity without graphic detail. Sexual activity may be portrayed but without any strong detail. Sexual violence may be shown if discreet and justified by context."",
                ""order"": 5
            },
            {
                ""certification"": ""12A"",
                ""meaning"": ""Films under this category are considered to be unsuitable for very young people. Those aged under 12 years are only admitted if accompanied by an adult, aged at least 18 years, at all times during the motion picture. However, it is generally not recommended that children under 12 years should watch the film. Films under this category can contain mature themes, discrimination, soft drugs, moderate swear words, infrequent strong language and moderate violence, sex references and nudity. Sexual activity may be briefly and discreetly portrayed. Sexual violence may be implied or briefly indicated."",
                ""order"": 3
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""All ages admitted, but certain scenes may be unsuitable for young children. May contain mild language and sex/drugs references. May contain moderate violence if justified by context (e.g. fantasy)."",
                ""order"": 2
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Only adults are admitted. Nobody younger than 18 can rent or buy an 18-rated VHS, DVD, Blu-ray Disc, UMD or game, or watch a film in the cinema with this rating. Films under this category do not have limitation on the bad language that is used. Hard drugs are generally allowed, and explicit sex references along with detailed sexual activity are also allowed. Scenes of strong real sex may be permitted if justified by the context. Very strong, gory, and/or sadistic violence is usually permitted. Strong sexual violence is permitted unless it is eroticised or excessively graphic."",
                ""order"": 6
            },
            {
                ""certification"": ""R18"",
                ""meaning"": ""Can only be shown at licensed adult cinemas or sold at licensed sex shops, and only to adults, those aged 18 or over. Films under this category are always hard-core pornography, defined as material intended for sexual stimulation and containing clear images of real sexual activity, strong fetish material, explicit animated images, or sight of certain acts such as triple simultaneous penetration and snowballing. There remains a range of material that is often cut from the R18 rating: strong images of injury in BDSM or spanking works, urolagnia, scenes suggesting incest even if staged, references to underage sex or childhood sexual development and aggressive behaviour such as hair-pulling or spitting on a performer are not permitted. More cuts are demanded in this category than any other category."",
                ""order"": 7
            }
        ],
        ""AU"": [
            {
                ""certification"": ""M"",
                ""meaning"": ""Recommended for mature audiences. There are no age restrictions. The content is moderate in impact."",
                ""order"": 4
            },
            {
                ""certification"": ""X18\u002B"",
                ""meaning"": ""Restricted to 18 years and over. Films with this rating have pornographic content. Films classified as X18\u002B are banned from being sold or rented in all Australian states and are only legally available in the Australian Capital Territory and the Northern Territory. However, importing X18\u002B material from the two territories to any of the Australian states is legal.The content is sexually explicit in impact."",
                ""order"": 7
            },
            {
                ""certification"": ""RC"",
                ""meaning"": ""Refused Classification. Banned from sale or hire in Australia; also generally applies to importation (if inspected by and suspicious to Customs). Private Internet viewing is unenforced and attempts to legally censor such online material has resulted in controversy. Films are rated RC if their content exceeds the guidelines. The content is very high in impact."",
                ""order"": 8
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""General. The content is very mild in impact."",
                ""order"": 2
            },
            {
                ""certification"": ""R18\u002B"",
                ""meaning"": ""Restricted to 18 years and over. Adults only. The content is high in impact."",
                ""order"": 6
            },
            {
                ""certification"": ""E"",
                ""meaning"": ""Exempt from classification. Films that are exempt from classification must not contain contentious material (i.e. material that would ordinarily be rated M or higher)."",
                ""order"": 1
            },
            {
                ""certification"": ""MA15\u002B"",
                ""meaning"": ""Mature Accompanied. Unsuitable for children younger than 15. Children younger than 15 years must be accompanied by a parent or guardian. The content is strong in impact."",
                ""order"": 5
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Parental guidance recommended. There are no age restrictions. The content is mild in impact."",
                ""order"": 3
            }
        ],
        ""FR"": [
            {
                ""certification"": ""10"",
                ""meaning"": ""(D\u00E9conseill\u00E9 aux moins de 10 ans) unsuitable for children younger than 10 (this rating is only used for TV); equivalent in theatres : \u0022avertissement\u0022 (warning), some scenes may be disturbing to young children and sensitive people; equivalent on video : \u0022accord parental\u0022 (parental guidance)."",
                ""order"": 2
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""(Interdit aux moins de 12 ans) unsuitable for children younger than 12 or forbidden in cinemas for under 12."",
                ""order"": 3
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""(Interdit aux mineurs) unsuitable for children younger than 18 or forbidden in cinemas for under 18."",
                ""order"": 5
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""(Interdit aux moins de 16 ans) unsuitable for children younger than 16 or forbidden in cinemas for under 16."",
                ""order"": 4
            },
            {
                ""certification"": ""U"",
                ""meaning"": ""(Tous publics) valid for all audiences."",
                ""order"": 1
            }
        ],
        ""NZ"": [
            {
                ""certification"": ""18"",
                ""meaning"": ""Restricted to persons 18 years of age and over."",
                ""order"": 7
            },
            {
                ""certification"": ""M"",
                ""meaning"": ""Suitable for (but not restricted to) mature audiences 16 years and up."",
                ""order"": 3
            },
            {
                ""certification"": ""R"",
                ""meaning"": ""Restricted to a particular class of persons, or for particular purposes, or both."",
                ""order"": 8
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""Restricted to persons 15 years of age and over."",
                ""order"": 5
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Parental guidance recommended for younger viewers."",
                ""order"": 2
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""Suitable for general audiences."",
                ""order"": 1
            },
            {
                ""certification"": ""13"",
                ""meaning"": ""Restricted to persons 13 years of age and over."",
                ""order"": 4
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Restricted to persons 16 years of age and over."",
                ""order"": 6
            }
        ],
        ""BR"": [
            {
                ""certification"": ""14"",
                ""meaning"": ""Not recommended for minors under fourteen. More violent material, stronger sex references and/or nudity."",
                ""order"": 4
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Not recommended for minors under sixteen. Scenes featuring production, trafficking and/or use of illegal drugs, hyper-realistic sex, sexual violence, abortion, torture, mutilation, suicide, trivialization of violence and death penalty."",
                ""order"": 5
            },
            {
                ""certification"": ""L"",
                ""meaning"": ""General Audiences. Do not expose children to potentially harmful content."",
                ""order"": 1
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Not recommended for minors under twelve. Scenes can include physical aggression, use of legal drugs and sexual innuendo."",
                ""order"": 3
            },
            {
                ""certification"": ""10"",
                ""meaning"": ""Not recommended for minors under ten. Violent content or inappropriate language to children, even if of a less intensity."",
                ""order"": 2
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Not recommended for minors under eighteen. Scenes featuring explicit sex, incest, pedophilia, praising of the use of illegal drugs and violence of a strong imagery impact."",
                ""order"": 6
            }
        ],
        ""PT"": [
            {
                ""certification"": ""M/18"",
                ""meaning"": ""Passed for viewers aged 18 and older."",
                ""order"": 7
            },
            {
                ""certification"": ""M/6"",
                ""meaning"": ""Passed for viewers aged 6 and older."",
                ""order"": 3
            },
            {
                ""certification"": ""P\u00FAblicos"",
                ""meaning"": ""For all the public (especially designed for children under 3 years of age)."",
                ""order"": 1
            },
            {
                ""certification"": ""M/3"",
                ""meaning"": ""Passed for viewers aged 3 and older."",
                ""order"": 2
            },
            {
                ""certification"": ""M/12"",
                ""meaning"": ""Passed for viewers aged 12 and older."",
                ""order"": 4
            },
            {
                ""certification"": ""M/16"",
                ""meaning"": ""Passed for viewers aged 16 and older."",
                ""order"": 6
            },
            {
                ""certification"": ""P"",
                ""meaning"": ""Special rating supplementary to the M/18 age rating denoting pornography."",
                ""order"": 8
            },
            {
                ""certification"": ""M/14"",
                ""meaning"": ""Passed for viewers aged 14 and older."",
                ""order"": 5
            }
        ],
        ""NO"": [
            {
                ""certification"": ""6"",
                ""meaning"": ""6 years (no restriction for children accompanied by an adult)."",
                ""order"": 2
            },
            {
                ""certification"": ""9"",
                ""meaning"": ""9 years (children down to 6 years accompanied by an adult)."",
                ""order"": 3
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""18"",
                ""meaning"": "" 18 years (absolute lower limit)."",
                ""order"": 6
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""15 years (young down to 12 years accompanied by an adult)."",
                ""order"": 5
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""12 years (children down to 9 years accompanied by an adult)."",
                ""order"": 4
            },
            {
                ""certification"": ""A"",
                ""meaning"": ""Suitable for all."",
                ""order"": 1
            }
        ],
        ""RU"": [
            {
                ""certification"": ""6\u002B"",
                ""meaning"": ""(For children above 6) \u2013 Unsuitable for children under 6."",
                ""order"": 2
            },
            {
                ""certification"": ""0\u002B"",
                ""meaning"": ""All ages are admitted."",
                ""order"": 1
            },
            {
                ""certification"": ""16\u002B"",
                ""meaning"": ""(For children above 16) \u2013 Unsuitable for children under 16."",
                ""order"": 4
            },
            {
                ""certification"": ""18\u002B"",
                ""meaning"": ""(Prohibited for children) \u2013 Prohibited for children under 18."",
                ""order"": 5
            },
            {
                ""certification"": ""12\u002B"",
                ""meaning"": ""(For children above 12) \u2013 Unsuitable for children under 12."",
                ""order"": 3
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            }
        ],
        ""IT"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""VM14"",
                ""meaning"": ""No admittance for children under 14."",
                ""order"": 2
            },
            {
                ""certification"": ""T"",
                ""meaning"": ""All ages admitted."",
                ""order"": 1
            },
            {
                ""certification"": ""VM18"",
                ""meaning"": ""No admittance for children under 18."",
                ""order"": 3
            }
        ],
        ""DE"": [
            {
                ""certification"": ""12"",
                ""meaning"": ""Children 12 or older admitted, children between 6 and 11 only when accompanied by parent or a legal guardian."",
                ""order"": 3
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""No youth admitted, only adults."",
                ""order"": 5
            },
            {
                ""certification"": ""0"",
                ""meaning"": ""No age restriction."",
                ""order"": 1
            },
            {
                ""certification"": ""6"",
                ""meaning"": ""No children younger than 6 years admitted."",
                ""order"": 2
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Children 16 or older admitted, nobody under this age admitted."",
                ""order"": 4
            }
        ],
        ""CA-QC"": [
            {
                ""certification"": ""G"",
                ""meaning"": ""General Rating \u2013 May be viewed, rented or purchased by persons of all ages. If a film carrying a \u0022G\u0022 rating might offend the sensibilities of a child under 8 years of age, \u0022Not suitable for young children\u0022 is appended to the classification."",
                ""order"": 1
            },
            {
                ""certification"": ""18\u002B"",
                ""meaning"": ""18 years and over \u2013 May be viewed, rented or purchased only by adults 18 years of age or over. If a film contains real and explicit sexual activity \u0022Explicit sexuality\u0022 is appended to the classification, and in the retail video industry storeowners are required to place the film in a room reserved for adults."",
                ""order"": 4
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""16\u002B"",
                ""meaning"": ""16 years and over \u2013 May be viewed, rented or purchased only by children 16 years of age or over."",
                ""order"": 3
            },
            {
                ""certification"": ""13\u002B"",
                ""meaning"": ""13 years and over \u2013 May be viewed, rented or purchased only by children 13 years of age or over. Children under 13 may be admitted only if accompanied by an adult."",
                ""order"": 2
            }
        ],
        ""DK"": [
            {
                ""certification"": ""11"",
                ""meaning"": ""For ages 11 and up."",
                ""order"": 3
            },
            {
                ""certification"": ""7"",
                ""meaning"": ""Not recommended for children under 7."",
                ""order"": 2
            },
            {
                ""certification"": ""A"",
                ""meaning"": ""Suitable for a general audience."",
                ""order"": 1
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""For ages 15 and up."",
                ""order"": 4
            },
            {
                ""certification"": ""F"",
                ""meaning"": ""Exempt from classification."",
                ""order"": 5
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            }
        ],
        ""US"": [
            {
                ""certification"": ""PG-13"",
                ""meaning"": ""Some material may be inappropriate for children under 13. Films given this rating may contain sexual content, brief or partial nudity, some strong language and innuendo, humor, mature themes, political themes, terror and/or intense action violence. However, bloodshed is rarely present. This is the minimum rating at which drug content is present."",
                ""order"": 3
            },
            {
                ""certification"": ""R"",
                ""meaning"": ""Under 17 requires accompanying parent or adult guardian 21 or older. The parent/guardian is required to stay with the child under 17 through the entire movie, even if the parent gives the child/teenager permission to see the film alone. These films may contain strong profanity, graphic sexuality, nudity, strong violence, horror, gore, and strong drug use. A movie rated R for profanity often has more severe or frequent language than the PG-13 rating would permit. An R-rated movie may have more blood, gore, drug use, nudity, or graphic sexuality than a PG-13 movie would admit."",
                ""order"": 4
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""All ages admitted. There is no content that would be objectionable to most parents. This is one of only two ratings dating back to 1968 that still exists today."",
                ""order"": 1
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Some material may not be suitable for children under 10. These films may contain some mild language, crude/suggestive humor, scary moments and/or violence. No drug content is present. There are a few exceptions to this rule. A few racial insults may also be heard."",
                ""order"": 2
            },
            {
                ""certification"": ""NC-17"",
                ""meaning"": ""These films contain excessive graphic violence, intense or explicit sex, depraved, abhorrent behavior, explicit drug abuse, strong language, explicit nudity, or any other elements which, at present, most parents would consider too strong and therefore off-limits for viewing by their children and teens. NC-17 does not necessarily mean obscene or pornographic in the oft-accepted or legal meaning of those words."",
                ""order"": 5
            }
        ],
        ""FI"": [
            {
                ""certification"": ""K-16"",
                ""meaning"": ""Over 16 years."",
                ""order"": 4
            },
            {
                ""certification"": ""K-12"",
                ""meaning"": ""Over 12 years."",
                ""order"": 3
            },
            {
                ""certification"": ""K-7"",
                ""meaning"": ""Over 7 years."",
                ""order"": 2
            },
            {
                ""certification"": ""S"",
                ""meaning"": ""For all ages."",
                ""order"": 1
            },
            {
                ""certification"": ""K-18"",
                ""meaning"": ""Adults only."",
                ""order"": 5
            },
            {
                ""certification"": ""KK"",
                ""meaning"": ""Banned from commercial distribution."",
                ""order"": 6
            }
        ],
        ""ES"": [
            {
                ""certification"": ""12"",
                ""meaning"": ""Not recommended for audiences under 12."",
                ""order"": 3
            },
            {
                ""certification"": ""7"",
                ""meaning"": ""Not recommended for audiences under 7."",
                ""order"": 2
            },
            {
                ""certification"": ""X"",
                ""meaning"": ""Prohibited for audiences under 18."",
                ""order"": 6
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Not recommended for audiences under 16."",
                ""order"": 4
            },
            {
                ""certification"": ""APTA"",
                ""meaning"": ""General admission."",
                ""order"": 1
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Not recommended for audiences under 18."",
                ""order"": 5
            }
        ],
        ""LT"": [
            {
                ""certification"": ""N-7"",
                ""meaning"": ""Movies for viewers from 7 years old. Younger than 7 years of age, viewers of this index have been featured only together with accompanying adult persons."",
                ""order"": 2
            },
            {
                ""certification"": ""V"",
                ""meaning"": ""Movies for the audience of all ages."",
                ""order"": 1
            },
            {
                ""certification"": ""N-13"",
                ""meaning"": ""Movies for viewers from 13 years of age. The viewers from 7 to 13 years of age are allowed to enter this index only together with accompanying adult persons."",
                ""order"": 3
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""N-16"",
                ""meaning"": ""Movies for viewers from 16 years of age."",
                ""order"": 4
            },
            {
                ""certification"": ""N-18"",
                ""meaning"": ""Movies for viewers from 18 years of age."",
                ""order"": 5
            }
        ],
        ""PH"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Viewers below 13 years old must be accompanied by a parent or supervising adult."",
                ""order"": 2
            },
            {
                ""certification"": ""X"",
                ""meaning"": ""\u201CX-rated\u201D films are not suitable for public exhibition."",
                ""order"": 6
            },
            {
                ""certification"": ""R-18"",
                ""meaning"": ""Only viewers who are 18 years old and above can be admitted."",
                ""order"": 5
            },
            {
                ""certification"": ""R-16"",
                ""meaning"": ""Only viewers who are 16 years old and above can be admitted."",
                ""order"": 4
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""Viewers of all ages are admitted."",
                ""order"": 1
            },
            {
                ""certification"": ""R-13"",
                ""meaning"": ""Only viewers who are 13 years old and above can be admitted."",
                ""order"": 3
            }
        ],
        ""MY"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""18SG"",
                ""meaning"": ""(Seram, Ganas: \u0022Graphic Violence and Horror/Terror\u0022) - Film may contain strong violence, gore or horror/terror people may find objectionable."",
                ""order"": 3
            },
            {
                ""certification"": ""18PA"",
                ""meaning"": ""(Politik, Agama: \u0022Strong Religious or Political Elements\u0022) - Film may contain elements which include religious, social or political aspects people may find objectionable. Rarely used."",
                ""order"": 5
            },
            {
                ""certification"": ""18PL"",
                ""meaning"": ""(Pelbagai: \u0022Various\u0022) - Film may contain strong violence, gore, horror/terror, sex scenes, nudity, sexual dialogues/references, religious, social or political aspects people may find objectionable. The majority of the 18\u002B movies use this rating. For example, a film with sex scenes and strong violence will be classified as 18PL, despite scenes of sex and nudity being strictly censored off by the LPF."",
                ""order"": 6
            },
            {
                ""certification"": ""U"",
                ""meaning"": ""(Umum: \u0022General Audiences\u0022) - For general audiences. (Used by the majority of films screened in Malaysia until 2008 but it continues only for television, notably for RTM.)"",
                ""order"": 1
            },
            {
                ""certification"": ""P13"",
                ""meaning"": ""(Penjaga 13 : \u0022Parental Guidance 13\u0022) - Children under 13 not admitted unless accompanied by an adult. (Introduced in 2006, this became the official Malaysian motion picture rating system in 2008. The \u0022PG-13\u0022 rating was revised to \u0022P13\u0022 from April 2012 onwards to emphasize the use of Malay language instead of English.) Passionate kissing scenes are not allowed under a P13 rating."",
                ""order"": 2
            },
            {
                ""certification"": ""18SX"",
                ""meaning"": ""(Seks: \u0022Sexual Content\u0022) - Film may contain sex scenes, nudity or sexual dialogue/references people may find objectionable (despite scenes of sex and nudity being strictly censored off by the LPF.)"",
                ""order"": 4
            }
        ],
        ""SE"": [
            {
                ""certification"": ""11"",
                ""meaning"": ""Children over the age of 7, who are accompanied by an adult, are admitted to films that have been passed for children from the age of 11."",
                ""order"": 3
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""Children over the age of 7, who are accompanied by an adult, are admitted to films that have been passed for children from the age of 11. Updated on March 1, 2017."",
                ""order"": 4
            },
            {
                ""certification"": ""7"",
                ""meaning"": ""Children under the age of 7, who are accompanied by an adult (a person aged 18 or over), are admitted to films that have been passed for children from the age of 7."",
                ""order"": 2
            },
            {
                ""certification"": ""Btl"",
                ""meaning"": ""All ages."",
                ""order"": 1
            }
        ],
        ""HU"": [
            {
                ""certification"": ""6"",
                ""meaning"": ""Not recommended below age of 6."",
                ""order"": 2
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Not recommended below age of 16."",
                ""order"": 4
            },
            {
                ""certification"": ""KN"",
                ""meaning"": ""Without age restriction."",
                ""order"": 1
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Not recommended below age of 18."",
                ""order"": 5
            },
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Not recommended below age of 12."",
                ""order"": 3
            },
            {
                ""certification"": ""X"",
                ""meaning"": ""Restricted below 18, for adults only."",
                ""order"": 6
            }
        ],
        ""IN"": [
            {
                ""certification"": ""UA"",
                ""meaning"": ""All ages admitted, but it is advised that children below 12 be accompanied by a parent as the theme or content may be considered intense or inappropriate for young children. Films under this category may contain mature themes, sexual references, mild sex scenes, violence with brief gory images and/or infrequent use of crude language."",
                ""order"": 1
            },
            {
                ""certification"": ""U"",
                ""meaning"": ""Unrestricted Public Exhibition throughout India, suitable for all age groups. Films under this category should not upset children over 4. Such films may contain educational, social or family-oriented themes. Films under this category may also contain fantasy violence and/or mild bad language."",
                ""order"": 0
            },
            {
                ""certification"": ""A"",
                ""meaning"": ""Restricted to adult audiences (18 years or over). Nobody below the age of 18 may buy/rent an A-rated DVD, VHS, UMD or watch a film in the cinema with this rating. Films under this category may contain adult/disturbing themes, frequent crude language, brutal violence with blood and gore, strong sex scenes and/or scenes of drug abuse which is considered unsuitable for minors."",
                ""order"": 2
            }
        ],
        ""BG"": [
            {
                ""certification"": ""A"",
                ""meaning"": ""Recommended for children."",
                ""order"": 1
            },
            {
                ""certification"": ""B"",
                ""meaning"": ""Without age restrictions."",
                ""order"": 2
            },
            {
                ""certification"": ""X"",
                ""meaning"": ""Prohibited for persons under 18."",
                ""order"": 5
            },
            {
                ""certification"": ""D"",
                ""meaning"": ""Prohibited for persons under 16."",
                ""order"": 4
            },
            {
                ""certification"": ""C"",
                ""meaning"": ""Not recommended for children under 12."",
                ""order"": 3
            }
        ]
    }
}";
        public static readonly string TV_CERTIFICATION_VALUES = @"{
    ""certifications"": {
        ""RU"": [
            {
                ""certification"": ""18\u002B"",
                ""meaning"": ""Restricted to People 18 or Older."",
                ""order"": 5
            },
            {
                ""certification"": ""0\u002B"",
                ""meaning"": ""Can be watched by Any Age."",
                ""order"": 1
            },
            {
                ""certification"": ""6\u002B"",
                ""meaning"": ""Only kids the age of 6 or older can watch."",
                ""order"": 2
            },
            {
                ""certification"": ""12\u002B"",
                ""meaning"": ""Only kids the age of 12 or older can watch."",
                ""order"": 3
            },
            {
                ""certification"": ""16\u002B"",
                ""meaning"": ""Only teens the age of 16 or older can watch."",
                ""order"": 4
            }
        ],
        ""US"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""TV-Y"",
                ""meaning"": ""This program is designed to be appropriate for all children."",
                ""order"": 1
            },
            {
                ""certification"": ""TV-Y7"",
                ""meaning"": ""This program is designed for children age 7 and above."",
                ""order"": 2
            },
            {
                ""certification"": ""TV-G"",
                ""meaning"": ""Most parents would find this program suitable for all ages."",
                ""order"": 3
            },
            {
                ""certification"": ""TV-PG"",
                ""meaning"": ""This program contains material that parents may find unsuitable for younger children."",
                ""order"": 4
            },
            {
                ""certification"": ""TV-14"",
                ""meaning"": ""This program contains some material that many parents would find unsuitable for children under 14 years of age."",
                ""order"": 5
            },
            {
                ""certification"": ""TV-MA"",
                ""meaning"": ""This program is specifically designed to be viewed by adults and therefore may be unsuitable for children under 17."",
                ""order"": 6
            }
        ],
        ""CA"": [
            {
                ""certification"": ""Exempt"",
                ""meaning"": ""Shows which are exempt from ratings (such as news and sports programming) will not display an on-screen rating at all."",
                ""order"": 0
            },
            {
                ""certification"": ""C"",
                ""meaning"": ""Programming suitable for children ages of 2\u20137 years. No profanity or sexual content of any level allowed. Contains little violence."",
                ""order"": 1
            },
            {
                ""certification"": ""C8"",
                ""meaning"": ""Suitable for children ages 8\u002B. Low level violence and fantasy horror is allowed. No foul language is allowed, but occasional \u0022socially offensive and discriminatory\u0022 language is allowed if in the context of the story. No sexual content of any level allowed."",
                ""order"": 2
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""Suitable for general audiences. Programming suitable for the entire family with mild violence, and mild profanity and/or censored language."",
                ""order"": 3
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Parental guidance. Moderate violence and moderate profanity is allowed, as is brief nudity and sexual references if important to the context of the story."",
                ""order"": 4
            },
            {
                ""certification"": ""14\u002B"",
                ""meaning"": ""Programming intended for viewers ages 14 and older. May contain strong violence and strong profanity, and depictions of sexual activity as long as they are within the context of a story."",
                ""order"": 5
            },
            {
                ""certification"": ""18\u002B"",
                ""meaning"": ""Programming intended for viewers ages 18 and older. May contain explicit violence and sexual activity. Programming with this rating cannot air before the watershed (9:00 p.m. to 6:00 a.m.)."",
                ""order"": 6
            }
        ],
        ""AU"": [
            {
                ""certification"": ""P"",
                ""meaning"": ""Programming is intended for younger children 2\u201311; commercial stations must show at least 30 minutes of P-rated content each weekday and weekends at all times. No advertisements may be shown during P-rated programs."",
                ""order"": 1
            },
            {
                ""certification"": ""C"",
                ""meaning"": ""Programming intended for older children 5\u201314; commercial stations must show at least 30 minutes of C-rated content each weekday between 7 a.m. and 8 a.m. or between 4 p.m. and 8:30 p.m. A further 2 and a half ours a week must also be shown either within these time bands or between 7 a.m. and 8:30 p.m. on weekends and school holidays, for a total of five hours a week (averaged as 260 hours over the course of a year). C-rated content is subject to certain restrictions and limitations on advertising (typically five minutes maximum per 30-minute period or seven minutes including promotions and community announcements)."",
                ""order"": 2
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""For general exhibition; all ages are permitted to watch programming with this rating."",
                ""order"": 3
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Parental guidance is recommended for young viewers; PG-rated content may air at any time on digital-only channels, otherwise, it should only be broadcast between 8:30 a.m. and 4:00 p.m. and between 7:00 p.m. and 6:00 a.m. on weekdays, and between 10:00 a.m. and 6:00 a.m. on weekends."",
                ""order"": 4
            },
            {
                ""certification"": ""M"",
                ""meaning"": ""Recommended for mature audiences; M-rated content may only be broadcast between 8:30 p.m. and 5:00 a.m. on any day, and additionally between 12:00 p.m. and 3:00 p.m. on schooldays."",
                ""order"": 5
            },
            {
                ""certification"": ""MA15\u002B"",
                ""meaning"": ""Not suitable for children and teens under 15; MA15\u002B-rated programming may only be broadcast between 9:00 p.m. and 5:00 a.m. on any given day. Consumer advice is mandatory. Some R18\u002B rated movies on DVD/Blu-ray are often re-edited on free TV/cable channels to secure a more \u0022appropriate\u0022 MA15\u002B rating. Some movies that were rated R18 on DVD have since been aired in Australian TV with MA15\u002B rating."",
                ""order"": 6
            },
            {
                ""certification"": ""AV15\u002B"",
                ""meaning"": ""Not suitable for children and teens under 15; this is the same as the MA15\u002B rating, except the \u0022AV\u0022 stands for \u0022Adult Violence\u0022 meaning that anything that is Classified \u0022MA15\u002B\u0022 with the consumer advice \u0022Frequent Violence\u0022 or \u0022Strong Violence\u0022 will automatically become AV15\u002B (with that same consumer advice.) The AV rating is still allowed to exceed any MA15\u002B content, in particular \u2013 \u0027Violence\u0027. AV15\u002B content may only be broadcast between 9:30 p.m. and 5:00 a.m. on any day. Consumer advice is mandatory."",
                ""order"": 7
            },
            {
                ""certification"": ""R18\u002B"",
                ""meaning"": ""Not for children under 18; this is limited to Adult \u0022Pay Per View\u0022 VC 196 and 197. Content may include graphic violence, sexual situations, coarse language and explicit drug use."",
                ""order"": 8
            }
        ],
        ""FR"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""10"",
                ""meaning"": ""Not recommended for children under 10. Not allowed in children\u0027s television series."",
                ""order"": 1
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Not recommended for children under 12. Not allowed air before 10:00 p.m. Some channels and programs are subject to exception."",
                ""order"": 2
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Not recommended for children under 16. Not allowed air before 10:30 p.m. Some channels and programs are subject to exception."",
                ""order"": 3
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Not recommended for persons under 18. Allowed between midnight and 5 a.m. and only in some channels, access to these programs is locked by a personal password."",
                ""order"": 4
            }
        ],
        ""DE"": [
            {
                ""certification"": ""0"",
                ""meaning"": ""Can be aired at any time."",
                ""order"": 0
            },
            {
                ""certification"": ""6"",
                ""meaning"": ""Can be aired at any time."",
                ""order"": 1
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""The broadcaster must take the decision about the air time by taking in consideration the impact on young children in the timeframe from 6:00am to 8:00pm."",
                ""order"": 2
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Can be aired only from 10:00pm Uhr to 6:00am."",
                ""order"": 3
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Can be aired only from 11:00pm Uhr to 6:00am."",
                ""order"": 4
            }
        ],
        ""TH"": [
            {
                ""certification"": ""\u0E2A"",
                ""meaning"": ""Sor - Educational movies which the public should be encouraged to see."",
                ""order"": 0
            },
            {
                ""certification"": ""\u0E17"",
                ""meaning"": ""Tor - G Movies appropriate for the general public. No sex, abusive language or violence."",
                ""order"": 1
            },
            {
                ""certification"": ""\u0E19 13\u002B"",
                ""meaning"": ""Nor 13\u002B Movies appropriate for audiences aged 13 and older."",
                ""order"": 2
            },
            {
                ""certification"": ""\u0E19 15\u002B"",
                ""meaning"": ""Nor 15\u002B Movies appropriate for audiences aged 15 and older. Some violence, brutality, inhumanity, bad language or indecent gestures allowed."",
                ""order"": 3
            },
            {
                ""certification"": ""\u0E19 18\u002B"",
                ""meaning"": ""Nor 18\u002B Movies appropriate for audiences aged 18 and older."",
                ""order"": 4
            },
            {
                ""certification"": ""\u0E09 20-"",
                ""meaning"": ""Chor 20 - Movies prohibited for audiences aged below 20."",
                ""order"": 5
            },
            {
                ""certification"": ""-"",
                ""meaning"": ""Banned."",
                ""order"": 6
            }
        ],
        ""KR"": [
            {
                ""certification"": ""Exempt"",
                ""meaning"": ""This rating is only for knowledge based game shows; lifestyle shows; documentary shows; news; current topic discussion shows; education/culture shows; sports that excludes MMA or other violent sports; and other programs that Korea Communications Standards Commission recognizes."",
                ""order"": 0
            },
            {
                ""certification"": ""ALL"",
                ""meaning"": ""This rating is for programming that is appropriate for all ages. This program usually involves programs designed for children or families."",
                ""order"": 1
            },
            {
                ""certification"": ""7"",
                ""meaning"": ""This rating is for programming that may contain material inappropriate for children younger than 7, and parental discretion should be used. Some cartoon programming not deemed strictly as \u0022educational\u0022, and films rated \u0022G\u0022 or \u0022PG\u0022 in North America may fall into the 7 category."",
                ""order"": 2
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""This rating is for programs that may deemed inappropriate for those younger than 12, and parental discretion should be used. Usually used for animations that have stronger themes or violence then those designed for children, or for reality shows that have mild violence, themes, or language."",
                ""order"": 3
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""This rating is for programs that contain material that may be inappropriate for children under 15, and that parental discretion should be used. Examples include most dramas, and talk shows on OTA (over-the-air) TV (KBS, MBC, SBS), and many American TV shows/dramas on Cable TV channels like OCN and OnStyle. The programs that have this rating may include moderate or strong adult themes, language, sexual inference, and violence. As with the TV-MA rating in North America, this rating is commonly applied to live events where the occurrence of inappropriate dialogue is unpredictable. Since 2007, this rating is the most used rating for TV."",
                ""order"": 4
            },
            {
                ""certification"": ""19"",
                ""meaning"": ""This rating is for programs that are intended for adults only. 19-rated programming cannot air during the hours of 7:00AM to 9:00AM, and 1:00PM to 10:00PM. Programmes that receive this rating will almost certainly have adult themes, sexual situations, frequent use of strong language and disturbing scenes of violence."",
                ""order"": 5
            }
        ],
        ""GB"": [
            {
                ""certification"": ""U"",
                ""meaning"": ""The U symbol stands for Universal. A U film should be suitable for audiences aged four years and over."",
                ""order"": 0
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""PG stands for Parental Guidance. This means a film is suitable for general viewing, but some scenes may be unsuitable for young children. A PG film should not unsettle a child aged around eight or older."",
                ""order"": 1
            },
            {
                ""certification"": ""12A"",
                ""meaning"": ""Films classified 12A and video works classified 12 contain material that is not generally suitable for children aged under 12. 12A requires an adult to accompany any child under 12 seeing a 12A film at the cinema."",
                ""order"": 2
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Films classified 12A and video works classified 12 contain material that is not generally suitable for children aged under 12."",
                ""order"": 3
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""No-one under 15 is allowed to see a 15 film at the cinema or buy/rent a 15 rated video. 15 rated works are not suitable for children under 15 years of age."",
                ""order"": 4
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Films rated 18 are for adults. No-one under 18 is allowed to see an 18 film at the cinema or buy / rent an 18 rated video. No 18 rated works are suitable for children."",
                ""order"": 5
            },
            {
                ""certification"": ""R18"",
                ""meaning"": ""The R18 category is a special and legally-restricted classification primarily for explicit works of consenting sex or strong fetish material involving adults."",
                ""order"": 6
            }
        ],
        ""BR"": [
            {
                ""certification"": ""L"",
                ""meaning"": ""Content is suitable for all audiences."",
                ""order"": 0
            },
            {
                ""certification"": ""10"",
                ""meaning"": ""Content suitable for viewers over the age of 10."",
                ""order"": 1
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Content suitable for viewers over the age of 12."",
                ""order"": 2
            },
            {
                ""certification"": ""14"",
                ""meaning"": ""Content suitable for viewers over the age of 14."",
                ""order"": 3
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Content suitable for viewers over the age of 16."",
                ""order"": 4
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Content suitable for viewers over the age of 18."",
                ""order"": 5
            }
        ],
        ""NL"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""AL"",
                ""meaning"": ""Not harmful / All Ages."",
                ""order"": 1
            },
            {
                ""certification"": ""6"",
                ""meaning"": ""Take care with children under 6."",
                ""order"": 2
            },
            {
                ""certification"": ""9"",
                ""meaning"": ""Take care with children under 9."",
                ""order"": 3
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Take care with children under 12."",
                ""order"": 4
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Take care with children under 16."",
                ""order"": 5
            }
        ],
        ""PT"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""T"",
                ""meaning"": ""Todos (suitable for all)."",
                ""order"": 1
            },
            {
                ""certification"": ""10AP"",
                ""meaning"": ""Acompanhamento Parental (may not be suitable for children under 10, parental guidance advised)."",
                ""order"": 2
            },
            {
                ""certification"": ""12AP"",
                ""meaning"": ""Acompanhamento Parental (may not be suitable for children under 12, parental guidance advised)."",
                ""order"": 3
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Not suitable for children under 16, access to these programs is locked by a personal password."",
                ""order"": 4
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Not suitable for children under 18."",
                ""order"": 5
            }
        ],
        ""CA-QC"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""Appropriate for all ages and must contain little or no violence and little to no sexual content."",
                ""order"": 1
            },
            {
                ""certification"": ""8\u002B"",
                ""meaning"": ""Appropriate for children 8 and up may contain with little violence, language, and little to no sexual situations."",
                ""order"": 2
            },
            {
                ""certification"": ""13\u002B"",
                ""meaning"": ""Appropriate \u2013 suitable for children 13 and up and may contain with moderate violence, language, and some sexual situations."",
                ""order"": 3
            },
            {
                ""certification"": ""16\u002B"",
                ""meaning"": ""Recommended for children over the age of 16 and may contain with strong violence, strong language, and strong sexual content."",
                ""order"": 4
            },
            {
                ""certification"": ""18\u002B"",
                ""meaning"": ""Only to be viewed by adults and may contain extreme violence and graphic sexual content. It is mostly used for 18\u002B movies and pornography."",
                ""order"": 5
            }
        ],
        ""HU"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""Unrated"",
                ""meaning"": ""Without age restriction."",
                ""order"": 1
            },
            {
                ""certification"": ""Children"",
                ""meaning"": ""Programs recommended for children. It is an optional rating, there is no obligation for broadcasters to indicate it."",
                ""order"": 2
            },
            {
                ""certification"": ""6"",
                ""meaning"": ""Programs not recommended for children below the age of 6, may not contain any violence or sexual content. A yellow circle with the number 6 written inside is used for this rating."",
                ""order"": 3
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Programs not recommended for children below the age of 12, may contain light sexual content or explicit language. Most films without serious violence or sexual content fit into this category as well. A yellow circle with the number 12 written inside is used for this rating."",
                ""order"": 4
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Programs not recommended for teens and children below the age of 16, may contain more intensive violence and sexual content. A yellow circle with the number 16 written inside is used for this rating."",
                ""order"": 5
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""The program is recommended only for adult viewers (for ages 18 and up), may contain explicit violence and explicit sexual content. A red circle with the number 18 written inside is used for this rating (the red circle was also used until 2002, but it did not contain any number in it)."",
                ""order"": 6
            }
        ],
        ""LT"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""N-7"",
                ""meaning"": ""Intended for viewers from 7 years old."",
                ""order"": 1
            },
            {
                ""certification"": ""N-14"",
                ""meaning"": ""Intended for viewers from 14 years of age and broadcast from 21 (9pm) to 6 (6am) hours."",
                ""order"": 2
            },
            {
                ""certification"": ""S"",
                ""meaning"": ""Intended for adult viewers from the age of 18 (corresponding to the age-appropriate index N-18) and broadcast between 23 (11pm) and 6 (6am) hours; Limited to minors and intended for adult audiences."",
                ""order"": 3
            }
        ],
        ""PH"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""G"",
                ""meaning"": ""Suitable for all ages. Material for television, which in the judgment of the Board does not contain anything unsuitable for children."",
                ""order"": 1
            },
            {
                ""certification"": ""PG"",
                ""meaning"": ""Parental guidance suggested. Material for television, which, in the judgment of the Board, may contain some adult material that may be permissible for children to watch but only under the guidance and supervision of a parent or adult."",
                ""order"": 2
            },
            {
                ""certification"": ""SPG"",
                ""meaning"": ""Stronger and more vigilant parental guidance is suggested. Programs classified as \u201CSPG\u201D may contain more serious topic and theme, which may not be advisable for children to watch except under the very vigilant guidance and presence of a parent or an adult."",
                ""order"": 3
            },
            {
                ""certification"": ""X"",
                ""meaning"": ""Any television program that does not conform to the \u201CG\u201D, \u201CPG\u201D, and \u201CSPG\u201D classification shall be disapproved for television broadcast."",
                ""order"": 4
            }
        ],
        ""ES"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""Infantil"",
                ""meaning"": ""Specially recommended for younger children."",
                ""order"": 1
            },
            {
                ""certification"": ""TP"",
                ""meaning"": ""For general viewing."",
                ""order"": 2
            },
            {
                ""certification"": ""7"",
                ""meaning"": ""Not recommended for viewers under the age of 7."",
                ""order"": 3
            },
            {
                ""certification"": ""10"",
                ""meaning"": ""Not recommended for viewers under the age of 10."",
                ""order"": 4
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Not recommended for viewers under the age of 12."",
                ""order"": 5
            },
            {
                ""certification"": ""13"",
                ""meaning"": ""Not recommended for viewers under the age of 13."",
                ""order"": 6
            },
            {
                ""certification"": ""16"",
                ""meaning"": ""Not recommended for viewers under the age of 16."",
                ""order"": 7
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Not recommended for viewers under the age of 18."",
                ""order"": 8
            }
        ],
        ""SK"": [
            {
                ""certification"": ""NR"",
                ""meaning"": ""No rating information."",
                ""order"": 0
            },
            {
                ""certification"": ""7"",
                ""meaning"": ""Content suitable for children over 6 years."",
                ""order"": 1
            },
            {
                ""certification"": ""12"",
                ""meaning"": ""Content suitable for children over 12 years."",
                ""order"": 2
            },
            {
                ""certification"": ""15"",
                ""meaning"": ""Content suitable for teens over 15 years."",
                ""order"": 3
            },
            {
                ""certification"": ""18"",
                ""meaning"": ""Content exclusively for adults."",
                ""order"": 4
            }
        ]
    }
}";
        public static readonly string MOVIE_WATCH_PROVIDER_VALUES = @"{
    ""results"": [
        {
            ""display_priority"": 0,
            ""logo_path"": ""/t2yyOv40HZeVlLjYsCsPHnWLk4W.jpg"",
            ""provider_name"": ""Netflix"",
            ""provider_id"": 8
        },
        {
            ""display_priority"": 0,
            ""logo_path"": ""/7Fl8ylPDclt3ZYgNbW2t7rbZE9I.jpg"",
            ""provider_name"": ""Hotstar"",
            ""provider_id"": 122
        },
        {
            ""display_priority"": 0,
            ""logo_path"": ""/w1T8s7FqakcfucR8cgOvbe6UeXN.jpg"",
            ""provider_name"": ""Okko"",
            ""provider_id"": 115
        },
        {
            ""display_priority"": 0,
            ""logo_path"": ""/bKy2YjC0QxViRnd8ayd2pv2ugJZ.jpg"",
            ""provider_name"": ""Fetch TV"",
            ""provider_id"": 436
        },
        {
            ""display_priority"": 1,
            ""logo_path"": ""/emthp39XA2YScoYL1p0sdbAH2WA.jpg"",
            ""provider_name"": ""Amazon Prime Video"",
            ""provider_id"": 119
        },
        {
            ""display_priority"": 1,
            ""logo_path"": ""/emthp39XA2YScoYL1p0sdbAH2WA.jpg"",
            ""provider_name"": ""Amazon Prime Video"",
            ""provider_id"": 9
        },
        {
            ""display_priority"": 1,
            ""logo_path"": ""/nlgoXBQCMSnGZrhAnyIZ7vSQ3vs.jpg"",
            ""provider_name"": ""Amediateka"",
            ""provider_id"": 116
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/peURlLlr8jggOwK53fJ5wdQl05y.jpg"",
            ""provider_name"": ""Apple iTunes"",
            ""provider_id"": 2
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/wTF37o4jOkQfjnWe41gmeuASYZA.jpg"",
            ""provider_name"": ""O2 TV"",
            ""provider_id"": 308
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/z3XAGCCbDD3KTZFvc96Ytr3XR56.jpg"",
            ""provider_name"": ""blutv"",
            ""provider_id"": 341
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/fyZObCfyY6mNVZOaBqgm7UMlHt.jpg"",
            ""provider_name"": ""iflix"",
            ""provider_id"": 160
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/ajbCmwvZ8HiePHZaOVEgm9MzyuA.jpg"",
            ""provider_name"": ""Zee5"",
            ""provider_id"": 232
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/g8jqHtXJsMlc8B1Gb0Rt8AvUJMn.jpg"",
            ""provider_name"": ""dTV"",
            ""provider_id"": 85
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/od4YNSSLgOP3p8EtQTnEYfrPa77.jpg"",
            ""provider_name"": ""Neon TV"",
            ""provider_id"": 273
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/9mmdMwWkDh12sIeSbEvsFWzmDX2.jpg"",
            ""provider_name"": ""FlixOl\u00E9"",
            ""provider_id"": 393
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/tbEdFQDwx5LEVr8WpSeXQSIirVq.jpg"",
            ""provider_name"": ""Google Play Movies"",
            ""provider_id"": 3
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/2LS6g6iE5DiAIDiZTAK8mbQQTuE.jpg"",
            ""provider_name"": ""SwissCom"",
            ""provider_id"": 150
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/2u1uElmpm4lProS7C9RYcaYLYt1.jpg"",
            ""provider_name"": ""Voot"",
            ""provider_id"": 121
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/uW4dPCcbXaaFTyfL5HwhuDt5akK.jpg"",
            ""provider_name"": ""Sun Nxt"",
            ""provider_id"": 309
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/2ioan5BX5L9tz4fIGU93blTeFhv.jpg"",
            ""provider_name"": ""wavve"",
            ""provider_id"": 356
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/dUeHhim2WUZz8S7EWjv0Ws6anRP.jpg"",
            ""provider_name"": ""Meo"",
            ""provider_id"": 242
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/3namPdisFuyTbB8BX2PxT3OdVCG.jpg"",
            ""provider_name"": ""puhutv"",
            ""provider_id"": 342
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/hGvUo8KZTRLDZWcfFJS3gA8aenB.jpg"",
            ""provider_name"": ""Canal\u002B"",
            ""provider_id"": 381
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/d3ixI1no0EpTj2i7u0Sd2DBXVlG.jpg"",
            ""provider_name"": ""BINGE"",
            ""provider_id"": 385
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/bVR4Z1LCHY7gidXAJF5pMa4QrDS.jpg"",
            ""provider_name"": ""Mubi"",
            ""provider_id"": 11
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/pq8p1umEnJjdFAP1nFvNArTR61X.jpg"",
            ""provider_name"": ""Be TV Go"",
            ""provider_id"": 311
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/gJ3yVMWouaVj6iHd59TISJ1TlM5.jpg"",
            ""provider_name"": ""Crave"",
            ""provider_id"": 230
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/riPZYc1ILIbubFaxYSdVfc7K6bm.jpg"",
            ""provider_name"": ""OCS Go"",
            ""provider_id"": 56
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/jRpQbuHbGR0MzSIBxJjxZxpXhqC.jpg"",
            ""provider_name"": ""Jio Cinema"",
            ""provider_id"": 220
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/9pS9Y3xkCLJnti9pi1AyrD5KbZe.jpg"",
            ""provider_name"": ""U-NEXT"",
            ""provider_id"": 84
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/vXXZx0aWQtDv2klvObNugm4dQMN.jpg"",
            ""provider_name"": ""Watcha"",
            ""provider_id"": 97
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/A4RUf7OEPnQ3mhaFIZkJkJMrYW2.jpg"",
            ""provider_name"": ""VRT nu"",
            ""provider_id"": 312
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/mPDlxHokGsEc84OOhp9qjeynq2U.jpg"",
            ""provider_name"": ""Looke"",
            ""provider_id"": 47
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/sB5vHrmYmliwUvBwZe8HpXo9r8m.jpg"",
            ""provider_name"": ""Crave Starz"",
            ""provider_id"": 305
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/jPXksae158ukMLFhhlNvzsvaEyt.jpg"",
            ""provider_name"": ""fuboTV"",
            ""provider_id"": 257
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/zES0qHTB5ZU1Cb3NYwZ56mgZ3Bc.jpg"",
            ""provider_name"": ""GYAO"",
            ""provider_id"": 86
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/8ARqfv7c3eD48NxHfjdNdoop1b0.jpg"",
            ""provider_name"": ""Ziggo TV"",
            ""provider_id"": 297
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/kplaFNfZXsdyqsz4TAK8xaKU9Qa.jpg"",
            ""provider_name"": ""VOD Poland"",
            ""provider_id"": 245
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/v8vA6WnPVTOE1o39waNFVmAqEJj.jpg"",
            ""provider_name"": ""HBO Poland"",
            ""provider_id"": 244
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/o9ExgOSLF3OTwR6T3DJOuwOKJgq.jpg"",
            ""provider_name"": ""Ivi"",
            ""provider_id"": 113
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/6MG0j8Z5d67Y06J7PZC8l7z58DX.jpg"",
            ""provider_name"": ""BoxOffice"",
            ""provider_id"": 54
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/iaMw6nOyxUzXSacrLQ0Au6CfZkc.jpg"",
            ""provider_name"": ""Classix"",
            ""provider_id"": 445
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/tRbUL8V91FdvIUuTYpdHFscyHVM.jpg"",
            ""provider_name"": ""Foxtel Now"",
            ""provider_id"": 134
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/pCIkSBek0aZfPQzOn9gfazuYaLV.jpg"",
            ""provider_name"": ""C More"",
            ""provider_id"": 77
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/jH5dm7aqU9DxS4yPfprz6e3jmHU.jpg"",
            ""provider_name"": ""Yle Areena"",
            ""provider_id"": 323
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/zxrVdFjIjLqkfnwyghnfywTn3Lh.jpg"",
            ""provider_name"": ""Hulu"",
            ""provider_id"": 15
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/a4ciTQc27FsgdUp7PCrToHPygcw.jpg"",
            ""provider_name"": ""Naver Store"",
            ""provider_id"": 96
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/7Jn4Vx4tbSnMQwQXuJFPwV0P5n1.jpg"",
            ""provider_name"": ""Videoland"",
            ""provider_id"": 72
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/hR9vWd8hWEVQKD6eOnBneKRFEW3.jpg"",
            ""provider_name"": ""Star Plus"",
            ""provider_id"": 619
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/iX0pvJ2GFATbVIH5IHMwG0ffIdV.jpg"",
            ""provider_name"": ""GuideDoc"",
            ""provider_id"": 100
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/sHP8XLo4Ac4WMbziRyAdRQdb76q.jpg"",
            ""provider_name"": ""Sky"",
            ""provider_id"": 210
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/1TsJJCXAScFxVsCesCdYnAex30x.jpg"",
            ""provider_name"": ""Sky Ticket"",
            ""provider_id"": 30
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/l5Wxbsgral716BOtZsGyPVNn8GC.jpg"",
            ""provider_name"": ""Horizon"",
            ""provider_id"": 250
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/llmnYOyknekZsXtkCaazKjhTLvG.jpg"",
            ""provider_name"": ""Path\u00E9 Thuis"",
            ""provider_id"": 71
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/xbhHHa1YgtpwhC8lb1NQ3ACVcLd.jpg"",
            ""provider_name"": ""Paramount Plus"",
            ""provider_id"": 531
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/liEIj6CkvojVDiMWeexGvflSPZT.jpg"",
            ""provider_name"": ""Public Domain Movies"",
            ""provider_id"": 638
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/rDd7IEBnJB0gPagFvagP1kK3pDu.jpg"",
            ""provider_name"": ""Stan"",
            ""provider_id"": 21
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/j2OLGxyy0gKbPVI0DYFI2hJxP6y.jpg"",
            ""provider_name"": ""Netflix Kids"",
            ""provider_id"": 175
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/fBHHXKC34ffxAsQvDe0ZJbvmTEQ.jpg"",
            ""provider_name"": ""Sky Go"",
            ""provider_id"": 29
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/2pCbao1J9s0DMak2KKnEzmzHni8.jpg"",
            ""provider_name"": ""Sky Store"",
            ""provider_id"": 130
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/5GEbAhFW2S5T8zVc1MNvz00pIzM.jpg"",
            ""provider_name"": ""Rakuten TV"",
            ""provider_id"": 35
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/jmyYN1124dDIzqMysLekixy3AzF.jpg"",
            ""provider_name"": ""Hollystar"",
            ""provider_id"": 164
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/cZkT6PrmJs5mfVscQf2PNF7xrF.jpg"",
            ""provider_name"": ""Ruutu"",
            ""provider_id"": 338
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/Ajqyt5aNxNGjmF9uOfxArGrdf3X.jpg"",
            ""provider_name"": ""HBO Max"",
            ""provider_id"": 384
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/tgKw3lckZULebs3cMLAbMRqir7G.jpg"",
            ""provider_name"": ""Argo"",
            ""provider_id"": 534
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/fadQYOyKL0tqfyj012nYJxm3N2I.jpg"",
            ""provider_name"": ""Eventive"",
            ""provider_id"": 677
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/8Gt1iClBlzTeQs8WQm8UrCoIxnQ.jpg"",
            ""provider_name"": ""Crunchyroll"",
            ""provider_id"": 283
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/52MBOsXXqm0wuLHxKR1FHymef66.jpg"",
            ""provider_name"": ""WAKANIM"",
            ""provider_id"": 354
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/ppycrWdkR3pefMYYK79e481PULm.jpg"",
            ""provider_name"": ""Sixplay"",
            ""provider_id"": 147
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/rsXaDmBzlHgYrtv1o2NsRFctM5t.jpg"",
            ""provider_name"": ""Now TV"",
            ""provider_id"": 39
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/zY5SmHyAy1CoA3AfQpf58QnShnw.jpg"",
            ""provider_name"": ""BBC iPlayer"",
            ""provider_id"": 38
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/cEFDMwXFueD1II3lwcTawSnmOaj.jpg"",
            ""provider_name"": ""Volta"",
            ""provider_id"": 53
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/cDzkhgvozSr4GW2aRdV22uDuFpw.jpg"",
            ""provider_name"": ""Movistar Play"",
            ""provider_id"": 339
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/jNdDSUCyzk2wOwct9vXAaoX4Ypx.jpg"",
            ""provider_name"": ""DocPlay"",
            ""provider_id"": 357
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/jgD3gxzW39UhJ7wZsxst75bN8Ck.jpg"",
            ""provider_name"": ""Go3"",
            ""provider_id"": 373
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/8VCV78prwd9QzZnEm0ReO6bERDa.jpg"",
            ""provider_name"": ""Peacock"",
            ""provider_id"": 386
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/m6p38R4AlEo1ub7QnZtirXDIUF5.jpg"",
            ""provider_name"": ""CONtv"",
            ""provider_id"": 428
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/59azlQKUgFdYq6QI5QEAxIeecyL.jpg"",
            ""provider_name"": ""Cultpix"",
            ""provider_id"": 692
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/lTgfdT2r558ytJN8cZp19zd6DKO.jpg"",
            ""provider_name"": ""Quickflix"",
            ""provider_id"": 22
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/5P99DkK1jVs95KcE8bYG9MBtGQ.jpg"",
            ""provider_name"": ""Acorn TV"",
            ""provider_id"": 87
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/mT9kIe6JVz72ikWJ58x0q8ckUW3.jpg"",
            ""provider_name"": ""Chili"",
            ""provider_id"": 40
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/lJT7r1nprk1Z8t1ywiIa8h9d3rc.jpg"",
            ""provider_name"": ""Claro video"",
            ""provider_id"": 167
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/5jdN9E9Ftxxbg711crjOyCagTy8.jpg"",
            ""provider_name"": ""Telecine Play"",
            ""provider_id"": 227
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/oBoWstXQFHAlPApyxIQ31CIbNQk.jpg"",
            ""provider_name"": ""Globoplay"",
            ""provider_id"": 307
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/gZTgebCNny3MHvUxFde7gueVgT1.jpg"",
            ""provider_name"": ""Movistar Plus"",
            ""provider_id"": 149
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/ddWcbe8fYAfcQMjighzWGLjjyip.jpg"",
            ""provider_name"": ""Orange VOD"",
            ""provider_id"": 61
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/okiQZMXnqwv0aD3QDYmu5DBNLce.jpg"",
            ""provider_name"": ""ShowMax"",
            ""provider_id"": 55
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/j9iLCZMMgXP3jaYPkxCicx5Zmx3.jpg"",
            ""provider_name"": ""Mediaset Play"",
            ""provider_id"": 359
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/yBrCoCGMIiHPHuoyh1mg82Pwlhx.jpg"",
            ""provider_name"": ""TV 2"",
            ""provider_id"": 383
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/xTHltMrZPAJFLQ6qyCBjAnXSmZt.jpg"",
            ""provider_name"": ""Peacock Premium"",
            ""provider_id"": 387
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/zEG5OsS8ZJHJ6RTuAtLUyCSb6De.jpg"",
            ""provider_name"": ""Sooner"",
            ""provider_id"": 389
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/kV8XFGI5OLJKl72dI8DtnKplfFr.jpg"",
            ""provider_name"": ""DIRECTV GO"",
            ""provider_id"": 467
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/zEuYa2328KQlbpOr4W0tVNpCGtZ.jpg"",
            ""provider_name"": ""iWantTFC"",
            ""provider_id"": 511
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/6HtR4lwikdriuJi86cZa3nXjB3d.jpg"",
            ""provider_name"": ""Quickflix Store"",
            ""provider_id"": 24
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/6uhKBfmtzFqOcLousHwZuzcrScK.jpg"",
            ""provider_name"": ""Apple TV Plus"",
            ""provider_id"": 350
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/5NyLm42TmCqCMOZFvH4fcoSNKEW.jpg"",
            ""provider_name"": ""Amazon Video"",
            ""provider_id"": 10
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/366UvWIQMqvKI6SyinCmvQx2B2j.jpg"",
            ""provider_name"": ""iciTouTV"",
            ""provider_id"": 146
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/kTpJjBn08IBluCPpFQekU9qdwRt.jpg"",
            ""provider_name"": ""Joyn"",
            ""provider_id"": 304
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/3L9ReVOwpiWgKyBw9ApkrDUWepy.jpg"",
            ""provider_name"": ""France TV"",
            ""provider_id"": 236
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/xVsZYrrmmqFJh3MkH98aFjMHnSf.jpg"",
            ""provider_name"": ""Filmin Latino"",
            ""provider_id"": 66
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/4FqTBYsUSZgS9z9UGKgxSDBbtc8.jpg"",
            ""provider_name"": ""FilmBox\u002B"",
            ""provider_id"": 701
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/vjsvYNPgq6BpUoubXR1wNkokoBb.jpg"",
            ""provider_name"": ""Yelo Play"",
            ""provider_id"": 313
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/sN8B7EweqmOmWm5ALdOAxCquuv1.jpg"",
            ""provider_name"": ""Cineplex"",
            ""provider_id"": 140
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/8T2jS3TdKCAsCrH0Kvl2NCwQ0ym.jpg"",
            ""provider_name"": ""Arte"",
            ""provider_id"": 234
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/9dielJNGTSKO7Lp6NKAuNOLw2jP.jpg"",
            ""provider_name"": ""Atres Player"",
            ""provider_id"": 62
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/32Pe7XfsubjbmvnZveBH5HfBQOm.jpg"",
            ""provider_name"": ""BFI Player"",
            ""provider_id"": 224
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/odTur9CmVtzsRUAZ9910tPM4XwL.jpg"",
            ""provider_name"": ""Sony Liv"",
            ""provider_id"": 237
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/58aUMVWJRolhWpi4aJCkGHwfKdg.jpg"",
            ""provider_name"": ""VIX "",
            ""provider_id"": 457
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/uXc2fJqhtXfuNq6ha8tTLL9VnXj.jpg"",
            ""provider_name"": ""Player"",
            ""provider_id"": 505
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/zLM7f1w2L8TU2Fspzns72m6h3yY.jpg"",
            ""provider_name"": ""Wink"",
            ""provider_id"": 501
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/r4q36I2Xts1SqhLWd6XsnbSAQJ4.jpg"",
            ""provider_name"": ""IROKOTV"",
            ""provider_id"": 704
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/oIkQkEkwfmcG7IGpRR1NB8frZZM.jpg"",
            ""provider_name"": ""YouTube"",
            ""provider_id"": 192
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/3hI22hp7YDZXyrmXVqDGnVivNTI.jpg"",
            ""provider_name"": ""RTL\u002B"",
            ""provider_id"": 298
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/ftxHS1anAWTYgtDtIDv8VLXoepH.jpg"",
            ""provider_name"": ""Timvision"",
            ""provider_id"": 109
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/67Ee4E6qOkQGHeUTArdJ1qRxzR2.jpg"",
            ""provider_name"": ""Curiosity Stream"",
            ""provider_id"": 190
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/v8vA6WnPVTOE1o39waNFVmAqEJj.jpg"",
            ""provider_name"": ""Disney Plus"",
            ""provider_id"": 390
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/eCRNttY7Zd75L5syA52AF8rCEuq.jpg"",
            ""provider_name"": ""TVNZ"",
            ""provider_id"": 395
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/oSJqnUUeoHfUj86Wsu2oq6VXLXE.jpg"",
            ""provider_name"": ""RTPplay"",
            ""provider_id"": 452
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/m3NWxxR23l1w1e156fyTuw931gx.jpg"",
            ""provider_name"": ""aha"",
            ""provider_id"": 532
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/ApALy1g1c9piZkivc9yrb30BGfn.jpg"",
            ""provider_name"": ""Nova Play"",
            ""provider_id"": 580
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/uurfHKuprPDeKfIs7FYd5lQzw0L.jpg"",
            ""provider_name"": ""Shahid VIP"",
            ""provider_id"": 1715
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/7khYVzFljIj9FpXIP7AEvxC8Nk6.jpg"",
            ""provider_name"": ""Infinity"",
            ""provider_id"": 110
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/iM0P8o5S1hDahB41kIY5voQWXtU.jpg"",
            ""provider_name"": ""QubitTV"",
            ""provider_id"": 274
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/9KiRtQNFyMaYau9bLHZgqlnUTCA.jpg"",
            ""provider_name"": ""Bioskop Online"",
            ""provider_id"": 466
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/xN97FFkFAdY1JvHhS4zyPD4URgD.jpg"",
            ""provider_name"": ""Spamflix"",
            ""provider_id"": 521
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/wKAPdeGjoejE3pPZp3RdElIbfl7.jpg"",
            ""provider_name"": ""Play Suisse"",
            ""provider_id"": 691
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/2PTFxgrswnEuK0szl87iSd8Yszz.jpg"",
            ""provider_name"": ""maxdome Store"",
            ""provider_id"": 20
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/s1QWuiBbZhLGSFzYOglPTVye7td.jpg"",
            ""provider_name"": ""Rai Play"",
            ""provider_id"": 222
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/o252SN51PdMx8UvyUkX00MAtooX.jpg"",
            ""provider_name"": ""Funimation Now"",
            ""provider_id"": 269
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/dBRq5BYYhK1ZXIgP68z5PYekPD3.jpg"",
            ""provider_name"": ""Vodacom Video Play"",
            ""provider_id"": 450
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/qw1BwnbWKs7AXLVR05eRpi3YdD9.jpg"",
            ""provider_name"": ""RTBF"",
            ""provider_id"": 461
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/iJGVfWTDddgipZ7mJCCEYzmRYrP.jpg"",
            ""provider_name"": ""Spuul"",
            ""provider_id"": 545
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/goKrzBxDNYxKgeeT2yoHtLXuIol.jpg"",
            ""provider_name"": ""Videobuster"",
            ""provider_id"": 133
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/dqlwg963xlz7jLN5Akdg6gbJ5To.jpg"",
            ""provider_name"": ""Watchbox"",
            ""provider_id"": 171
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/rll0yTCjrSY6hcJqIyMatv9B2iR.jpg"",
            ""provider_name"": ""NetMovies"",
            ""provider_id"": 19
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/x36C6aseF5l4uX99Kpse9dbPwBo.jpg"",
            ""provider_name"": ""Starz Play Amazon Channel"",
            ""provider_id"": 194
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/45eTLxznKGY9xq50NBWjN4adVng.jpg"",
            ""provider_name"": ""Catchplay"",
            ""provider_id"": 159
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/9xKAZFyhkZVewWxJJhR41AJO0D3.jpg"",
            ""provider_name"": ""genflix"",
            ""provider_id"": 468
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/bvcdVO7SDHKEa6D40g1jntXKNj.jpg"",
            ""provider_name"": ""DOCSVILLE"",
            ""provider_id"": 475
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/b6hJjWPa7h8VCpaCVJCSu8EPlqT.jpg"",
            ""provider_name"": ""Filmo TV"",
            ""provider_id"": 138
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/kJ9GcmYk5zJ9nJtVX8XjDo9geIM.jpg"",
            ""provider_name"": ""All 4"",
            ""provider_id"": 103
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/ugFkAFzcZm6I1ftFumwAmhtFaVO.jpg"",
            ""provider_name"": ""Curzon Home Cinema"",
            ""provider_id"": 189
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/uOooYc5OsAq68QcrCZnRhY1rrXo.jpg"",
            ""provider_name"": ""Bookmyshow"",
            ""provider_id"": 124
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/mgD0T960hnYU4gBxbPPBrcDfgWg.jpg"",
            ""provider_name"": ""WOW Presents Plus"",
            ""provider_id"": 546
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/bZNXgd8fwVTD68aAGlElkpAtu7b.jpg"",
            ""provider_name"": ""IPLA"",
            ""provider_id"": 549
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/krABGbxTRmPtUA10fkwhwUdCd4I.jpg"",
            ""provider_name"": ""tvzavr"",
            ""provider_id"": 556
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/3E0RkIEQrrGYazs63NMsn3XONT6.jpg"",
            ""provider_name"": ""Paramount\u002B Amazon Channel"",
            ""provider_id"": 582
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/dSAEkpy0IhZpTLixrMq9z24oEPC.jpg"",
            ""provider_name"": ""7plus"",
            ""provider_id"": 246
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/knpqBvBQjyHnFrYPJ9bbtUCv6uo.jpg"",
            ""provider_name"": ""Canal VOD"",
            ""provider_id"": 58
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/gekkP93StjYdiMAInViVmrnldNY.jpg"",
            ""provider_name"": ""Magellan TV"",
            ""provider_id"": 551
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/Aduyz3yAGMXTmd2N6NiIOYCmWF3.jpg"",
            ""provider_name"": ""More TV"",
            ""provider_id"": 557
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/tfI7CcurXCS2CZnLLHrBWS8CGHk.jpg"",
            ""provider_name"": ""EPIX Amazon Channel"",
            ""provider_id"": 583
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/98gXEOnALxMcSTuAkzrx8OKKErx.jpg"",
            ""provider_name"": ""Nexo Plus"",
            ""provider_id"": 641
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/vFjk9B5bZ1ranNLnjE6Z4RY3VxM.jpg"",
            ""provider_name"": ""ABC iview"",
            ""provider_id"": 135
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/cDwMvtLqnReORuXJAOKUCTcyc5f.jpg"",
            ""provider_name"": ""Alleskino"",
            ""provider_id"": 33
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/ulTa4e9ysKwMwNpg7EfhYnvAj8q.jpg"",
            ""provider_name"": ""Bbox VOD"",
            ""provider_id"": 59
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/wrg4g92EvgzMOcnh1pWbUbnPdGA.jpg"",
            ""provider_name"": ""ITV Hub"",
            ""provider_id"": 41
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/5kffg7iSNcJKyQdi9TEn463cK3T.jpg"",
            ""provider_name"": ""Film1"",
            ""provider_id"": 396
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/66vV4PIX6aiDvmprUYli7vnBEEA.jpg"",
            ""provider_name"": ""Beamafilm"",
            ""provider_id"": 448
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/dFnG5G2YxrYjv9YiVu9Bq7Wj5Ds.jpg"",
            ""provider_name"": ""Believe"",
            ""provider_id"": 465
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/6IdiH2yMRYCtB7XoIQ36wZig9gZ.jpg"",
            ""provider_name"": ""Vidio"",
            ""provider_id"": 489
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/xLu1rkZNOKuNnRNr70wySosfTBf.jpg"",
            ""provider_name"": ""BroadwayHD"",
            ""provider_id"": 554
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/a2OcajC4bM5ItniQdjyOV7tgthW.jpg"",
            ""provider_name"": ""Discovery\u002B Amazon Channel"",
            ""provider_id"": 584
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/5kcixh15fZo0yUwQB2ug2ZfvCVc.jpg"",
            ""provider_name"": ""SBS On Demand"",
            ""provider_id"": 132
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/73igBrpTdAhEGwuYxhmnhTK5Srs.jpg"",
            ""provider_name"": ""NPO Start"",
            ""provider_id"": 360
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/zJKmYhZ5jn8nfQ36Dtk6MgQnoy6.jpg"",
            ""provider_name"": ""ThreeNow"",
            ""provider_id"": 440
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/y1PDXoEMqReA1uX1aF8rnVgSYBS.jpg"",
            ""provider_name"": ""NRK TV"",
            ""provider_id"": 442
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/fy4svqyray3cnkuEqGIXL3i2WQw.jpg"",
            ""provider_name"": ""Belas Artes \u00E0 La Carte"",
            ""provider_id"": 447
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/9nyK6XeCSe1fmK9B9H2xHgOYDlj.jpg"",
            ""provider_name"": ""CINE"",
            ""provider_id"": 491
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/eDFIGvn1PImm9kmZ83ugaqdWapy.jpg"",
            ""provider_name"": ""MAX Stream"",
            ""provider_id"": 483
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/olmH7t5tEng8Yuq33KmvpvaaVIg.jpg"",
            ""provider_name"": ""Filmzie"",
            ""provider_id"": 559
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/lDaVaiNwOU8JHCcZ6fANzugVvtT.jpg"",
            ""provider_name"": ""Das Erste Mediathek"",
            ""provider_id"": 219
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/xiqTOBxOnlMy1nvppZcFhCDsP0f.jpg"",
            ""provider_name"": ""Alt Balaji"",
            ""provider_id"": 319
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/zoL69abPHiVC1Qzd4kM6hwLSo0j.jpg"",
            ""provider_name"": ""Showtime Amazon Channel"",
            ""provider_id"": 203
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/3QsJbibv5dFW2IYuXbTjxDmGGRZ.jpg"",
            ""provider_name"": ""Blockbuster"",
            ""provider_id"": 423
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/vqybB1exnaQ3UOlKaw4t6OgzFIu.jpg"",
            ""provider_name"": ""Filmstriben"",
            ""provider_id"": 443
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/u2H29LCxRzjZVUoZUQAHKm5P8Zc.jpg"",
            ""provider_name"": ""Dekkoo"",
            ""provider_id"": 444
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/47iDHK3CykgXuZ20FN6QRAEcFBY.jpg"",
            ""provider_name"": ""Mitele "",
            ""provider_id"": 456
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/yHXKdLK7kfHo907L2W8fTalXltQ.jpg"",
            ""provider_name"": ""Picl"",
            ""provider_id"": 451
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/bmU37kpSMbcTgwwUrbxByk7x8h3.jpg"",
            ""provider_name"": ""HBO Go"",
            ""provider_id"": 31
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/rpwa6Tjghh1DF4iNfP5g4Rn6MGQ.jpg"",
            ""provider_name"": ""VVVVID"",
            ""provider_id"": 414
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/dNcz2AZHPEgt4BIKJe56r4visuK.jpg"",
            ""provider_name"": ""SF Anytime"",
            ""provider_id"": 426
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/4QEQsvCBnORNIg9EDnrRSiEw61D.jpg"",
            ""provider_name"": ""Hungama Play"",
            ""provider_id"": 437
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/1EVaN5FaXnheqQSVB5kn4zDJKZa.jpg"",
            ""provider_name"": ""Draken Films"",
            ""provider_id"": 435
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/9edKQczyuMmQM1yS520hgmJbcaC.jpg"",
            ""provider_name"": ""AMC\u002B Amazon Channel"",
            ""provider_id"": 528
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/osREemsc9uUB2J8VTkQeAVk2fu9.jpg"",
            ""provider_name"": ""True Story"",
            ""provider_id"": 567
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/dUGPd8eg651seqculYtaM3AE9O9.jpg"",
            ""provider_name"": ""Premier"",
            ""provider_id"": 570
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/y0kyIFElN5sJAsmW8Txj69wzrD2.jpg"",
            ""provider_name"": ""Sky X"",
            ""provider_id"": 321
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/3LQzaSBH1kjQB9oKc4n72dKj8oY.jpg"",
            ""provider_name"": ""HBO Now"",
            ""provider_id"": 27
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/8jzbtiXz0eZ6aPjxdmGW3ceqjon.jpg"",
            ""provider_name"": ""Hollywood Suite"",
            ""provider_id"": 182
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/5nECaP8nhtrzZfx7oG0yoFMfqiA.jpg"",
            ""provider_name"": ""SumoTV"",
            ""provider_id"": 431
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/mMb0rksAc7Cmom5pEYaLNDkbitE.jpg"",
            ""provider_name"": ""Kirjastokino"",
            ""provider_id"": 463
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/xKUlNQjy7dpfI8Nj8BjgSTdYnqH.jpg"",
            ""provider_name"": ""CONTAR"",
            ""provider_id"": 543
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/aQ1ritN00jXc7RAFfUoQKGAAfp7.jpg"",
            ""provider_name"": ""DocAlliance Films"",
            ""provider_id"": 569
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/3jJtMOIwtvcrCyeRMUvv4wsfhJk.jpg"",
            ""provider_name"": ""TvIgle"",
            ""provider_id"": 577
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/s90cXW0NE709rLYRQ8YzMYjMmU3.jpg"",
            ""provider_name"": ""LaCinetek"",
            ""provider_id"": 310
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/kIbbhgfOWTHNp0xpcFC5uJUAwHj.jpg"",
            ""provider_name"": ""Viu"",
            ""provider_id"": 158
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/d4vHcXY9rwnr763wQns2XJThclt.jpg"",
            ""provider_name"": ""Hoichoi"",
            ""provider_id"": 315
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/egNBibGpNroklenRFE0EiRYZqaf.jpg"",
            ""provider_name"": ""Blim"",
            ""provider_id"": 67
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/z0h7mBHwm5KfMB2MKeoQDD2ngEZ.jpg"",
            ""provider_name"": ""The Roku Channel"",
            ""provider_id"": 207
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/h5R7lrSpyuejhjqd1L1a9uCSIB4.jpg"",
            ""provider_name"": ""Horrify"",
            ""provider_id"": 460
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/q03pok7xSxYJaENuYs547qa6upY.jpg"",
            ""provider_name"": ""EPIC ON"",
            ""provider_id"": 476
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/jblaJCpe4cDnaFNZg90qGF1UkZF.jpg"",
            ""provider_name"": ""SVT"",
            ""provider_id"": 493
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/uHv6Y4YSsr4cj7q4cBbAg7WXKEI.jpg"",
            ""provider_name"": ""KoreaOnDemand"",
            ""provider_id"": 575
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/4iegia9VpdceQpbUNqZ5ZP9jdgh.jpg"",
            ""provider_name"": ""Arrow Video Amazon Channel"",
            ""provider_id"": 596
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/ubIndtI8qN9sE7iXdpYO41ktW4v.jpg"",
            ""provider_name"": ""Flimmit"",
            ""provider_id"": 142
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/aGIS8maihUm60A3moKYD9gfYHYT.jpg"",
            ""provider_name"": ""BritBox"",
            ""provider_id"": 151
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/qjtOUIUnk4kRpcZmaddjqDHM0dR.jpg"",
            ""provider_name"": ""Rakuten Viki"",
            ""provider_id"": 344
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/cvl65OJnz14LUlC3yGK1KHj8UYs.jpg"",
            ""provider_name"": ""Viaplay"",
            ""provider_id"": 76
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/3BxFGDZ1q8CxbUv5tSvMT28AC0X.jpg"",
            ""provider_name"": ""Cinemas a la Demande"",
            ""provider_id"": 324
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/dpm29atq9clfBL38NgGxsj2CCe3.jpg"",
            ""provider_name"": ""Hotstar Disney\u002B"",
            ""provider_id"": 377
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/vIhSFgmp0HXZbUHDscuhpU6S2Z6.jpg"",
            ""provider_name"": ""ShemarooMe"",
            ""provider_id"": 474
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/m8wib2YFVWHaY0SvnExvXZFusz9.jpg"",
            ""provider_name"": ""NLZIET"",
            ""provider_id"": 472
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/cQQYtdaCg7vDo28JPru4v8Ypi8x.jpg"",
            ""provider_name"": ""NOW"",
            ""provider_id"": 484
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/676SkNeXbHTfrtTdvEGpxGtMlKk.jpg"",
            ""provider_name"": ""Viafree"",
            ""provider_id"": 494
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/1KAux0lBEHpLnQcvaf1Qf1uKcIP.jpg"",
            ""provider_name"": ""Kinopoisk"",
            ""provider_id"": 117
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/qMf2zirM2w0sO0mdAIIoP5XnQn8.jpg"",
            ""provider_name"": ""Showtime Roku Premium Channel"",
            ""provider_id"": 632
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/lMGjx9hi6Kb4nQvFLGhBfk6nHZV.jpg"",
            ""provider_name"": ""Netzkino"",
            ""provider_id"": 28
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/gqdajHmtr6qtutL7kkmEgleGfV9.jpg"",
            ""provider_name"": ""Filmin"",
            ""provider_id"": 63
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/t6N57S17sdXRXmZDAkaGP0NHNG0.jpg"",
            ""provider_name"": ""Pluto TV"",
            ""provider_id"": 300
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/1OUHXH3R6waN0ojQWX9LcrO1mNY.jpg"",
            ""provider_name"": ""VOD Club"",
            ""provider_id"": 370
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/1bbExrGyEuUFAEWMBSN76bwacQ0.jpg"",
            ""provider_name"": ""Oldflix"",
            ""provider_id"": 499
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/f3RCRmZWiUzg2CjxUqWJ881WmcS.jpg"",
            ""provider_name"": ""rtve"",
            ""provider_id"": 541
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/ooeXdXICZNnDFHAq596xQwN4A15.jpg"",
            ""provider_name"": ""Viddla"",
            ""provider_id"": 539
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/jiGIvlZafckhqy0Ya9zGp60eWS8.jpg"",
            ""provider_name"": ""QFT Player"",
            ""provider_id"": 552
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/8MXYXzZGoPAEQU13GWk1GVvKNUS.jpg"",
            ""provider_name"": ""iQIYI"",
            ""provider_id"": 581
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/qlVSrZgfXlFw0Jj6hsYq2zi70JD.jpg"",
            ""provider_name"": ""Paramount\u002B Roku Premium Channel"",
            ""provider_id"": 633
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/gGTgmDv9fa8uSd2sEybDOzfYfHK.jpg"",
            ""provider_name"": ""Kividoo"",
            ""provider_id"": 89
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/4k49M5oMFewREZLfCw6jNAn0dOo.jpg"",
            ""provider_name"": ""Filmin Plus"",
            ""provider_id"": 64
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/fN1aQj1gpWXGW0gwcGlHJYQHUeS.jpg"",
            ""provider_name"": ""Anime Digital Networks"",
            ""provider_id"": 415
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/73ms51HSpkD0OOXwj2EeiZeSqSt.jpg"",
            ""provider_name"": ""History Play"",
            ""provider_id"": 478
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/vuAxCPW4tlZ7Dg9EshAdPoHZFBo.jpg"",
            ""provider_name"": ""Comhem Play"",
            ""provider_id"": 497
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/8ugSQ1g7E8fXFnKFT5G8XOMcmS0.jpg"",
            ""provider_name"": ""TVF Play"",
            ""provider_id"": 500
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/ihE8Z4jZcGsmQsGRj6q06oxD2Wd.jpg"",
            ""provider_name"": ""Elisa Viihde"",
            ""provider_id"": 540
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/5OAb2w7D9C2VHa0k5PaoAYeFYFE.jpg"",
            ""provider_name"": ""Starz Roku Premium Channel"",
            ""provider_id"": 634
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/vMxtOESmrNWkM9Y9jAebETexPAc.jpg"",
            ""provider_name"": ""OSN"",
            ""provider_id"": 629
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/uULoezj2skPc6amfwru72UPjYXV.jpg"",
            ""provider_name"": ""MagentaTV"",
            ""provider_id"": 178
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/dUokaRky9vs1u2PFRzFDV4iIx6A.jpg"",
            ""provider_name"": ""TNTGo"",
            ""provider_id"": 512
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/qLR6qzB1IcANZUqMEkLf6Sh8Y8s.jpg"",
            ""provider_name"": ""Tata Play"",
            ""provider_id"": 502
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/yqdmrKY4D0WuB9Q06EQvBoOOgKP.jpg"",
            ""provider_name"": ""TriArt Play"",
            ""provider_id"": 517
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/ni2NgPmIqqJRXeiA8Zdj4UhBZnU.jpg"",
            ""provider_name"": ""AMC\u002B Roku Premium Channel"",
            ""provider_id"": 635
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/uOTEObCZtolNGDA7A4Wrb47cxNn.jpg"",
            ""provider_name"": ""STARZPLAY"",
            ""provider_id"": 630
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/plbVK1EXpz7PpyddpI0U1cZIYYK.jpg"",
            ""provider_name"": ""GOSPEL PLAY"",
            ""provider_id"": 477
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/thucdaw2gnOE0g478AHVZw5UeYm.jpg"",
            ""provider_name"": ""Cineasterna"",
            ""provider_id"": 496
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/wYRiUqIgWcfUvO6OPcXuUNd4tc2.jpg"",
            ""provider_name"": ""Discovery Plus"",
            ""provider_id"": 510
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/5uUdbTzTj4N2Wso1FTt2rRoJ5Da.jpg"",
            ""provider_name"": ""SALTO"",
            ""provider_id"": 564
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/96JqcynVUOkkIfpyffjczff5NZb.jpg"",
            ""provider_name"": ""Klik Film"",
            ""provider_id"": 576
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/yiU8I1FrrXJkq4bVpjmoVqBXDuc.jpg"",
            ""provider_name"": ""iBAKATV"",
            ""provider_id"": 702
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/xlonQMSmhtA2HHwK3JKF9ghx7M8.jpg"",
            ""provider_name"": ""AMC\u002B"",
            ""provider_id"": 526
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/7rwgEs15tFwyR9NPQ5vpzxTj19Q.jpg"",
            ""provider_name"": ""Disney Plus"",
            ""provider_id"": 337
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/iRv3wbUEPuwYYPSKwUxPaMPKGM4.jpg"",
            ""provider_name"": ""ManoramaMax"",
            ""provider_id"": 482
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/zQZE8lzSXsXl1K3aPa7kYRp4I6r.jpg"",
            ""provider_name"": ""Epix Roku Premium Channel"",
            ""provider_id"": 636
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/h9vCGR4GF42HjXNvGQoBcuiZAvG.jpg"",
            ""provider_name"": ""HRTi"",
            ""provider_id"": 631
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/zXDDsD9M5vO7lqoqlBQCOcZtKBS.jpg"",
            ""provider_name"": ""Telstra TV"",
            ""provider_id"": 429
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/n3BIqc0mojP85bJSKjsIwZUOVya.jpg"",
            ""provider_name"": ""Libreflix"",
            ""provider_id"": 544
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/60PpYTEeU4F1r5ndl1VbdYq5r7F.jpg"",
            ""provider_name"": ""IFFR Unleashed"",
            ""provider_id"": 548
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/aTUaeAdFmNfjcm7FRWaM49Ds7Gj.jpg"",
            ""provider_name"": ""Filmoteket"",
            ""provider_id"": 560
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/xrHrIraInfRXnrz1zHhY1tXJowg.jpg"",
            ""provider_name"": ""RTL Play"",
            ""provider_id"": 572
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/9CHdbyMXYgFk9oM7H4t1FlrULHs.jpg"",
            ""provider_name"": ""Otta"",
            ""provider_id"": 626
        },
        {
            ""display_priority"": 32,
            ""logo_path"": ""/6IPjvnYl6WWkIwN158qBFXCr2Ne.jpg"",
            ""provider_name"": ""YouTube Premium"",
            ""provider_id"": 188
        },
        {
            ""display_priority"": 32,
            ""logo_path"": ""/z4vfN7KoOn6zruoCDRITnDZTdAx.jpg"",
            ""provider_name"": ""OzFlix"",
            ""provider_id"": 434
        },
        {
            ""display_priority"": 32,
            ""logo_path"": ""/pICdAIrQp0JRR4polBXhlVg8bO.jpg"",
            ""provider_name"": ""Shadowz"",
            ""provider_id"": 513
        },
        {
            ""display_priority"": 32,
            ""logo_path"": ""/kHx8k4ixfSZdj45FAYP2P9r4FUO.jpg"",
            ""provider_name"": ""Pickbox NOW"",
            ""provider_id"": 637
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/i8tj86LAQ2MdCvZgTSRbD3E5ySg.jpg"",
            ""provider_name"": ""BFI Player Amazon Channel"",
            ""provider_id"": 287
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/4SCmZgf7AeJLKKRPcbf5VFkGpBj.jpg"",
            ""provider_name"": ""YouTube Free"",
            ""provider_id"": 235
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/dH4BZucVyb5lW97TEbZ7RTAugjg.jpg"",
            ""provider_name"": ""MX Player"",
            ""provider_id"": 515
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/l0SGkSW80SFWshxT2tvafv9dzkp.jpg"",
            ""provider_name"": ""La Toile"",
            ""provider_id"": 518
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/qJxuBkjkXWYmuTKk7hxvbmqvrNc.jpg"",
            ""provider_name"": ""Cin\u00E9polis KLIC"",
            ""provider_id"": 558
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/ujkJsUqk59ZdN6fCslGV3dZDSHr.jpg"",
            ""provider_name"": ""Voyo"",
            ""provider_id"": 627
        },
        {
            ""display_priority"": 34,
            ""logo_path"": ""/aJ0b9BLU1Cvv5hIz9fEhKKc1x1D.jpg"",
            ""provider_name"": ""Hoopla"",
            ""provider_id"": 212
        },
        {
            ""display_priority"": 34,
            ""logo_path"": ""/7RJrotCrvD0oUjG0udv9on6CDKX.jpg"",
            ""provider_name"": ""Hayu Amazon Channel"",
            ""provider_id"": 296
        },
        {
            ""display_priority"": 34,
            ""logo_path"": ""/3OYkWKdWFgmRNiAp2kPgRN9wWd3.jpg"",
            ""provider_name"": ""Edisonline"",
            ""provider_id"": 628
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/6Y6w3F5mYoRHCcNAG0ZD2AndLJ2.jpg"",
            ""provider_name"": ""The CW"",
            ""provider_id"": 83
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/bVClgB5bpaTRM3sVPlboaxkFD0U.jpg"",
            ""provider_name"": ""KPN"",
            ""provider_id"": 563
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/gKno1uvHwHyhQTKMflDvEqj5oGJ.jpg"",
            ""provider_name"": ""Strim"",
            ""provider_id"": 578
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/cInE5cdEs1yOKVbNaqlGbeZeAnN.jpg"",
            ""provider_name"": ""Dansk Filmskat"",
            ""provider_id"": 621
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/fDZtWPwSiKjVbbuZOVtlZAiH0rE.jpg"",
            ""provider_name"": ""WeTV"",
            ""provider_id"": 623
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/7cXdK4ExORmhkJl9wX1q3Yqs8lV.jpg"",
            ""provider_name"": ""ZDF Herzkino Amazon Channel"",
            ""provider_id"": 286
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/fWqVPYArdFwBc6vYqoyQB6XUl85.jpg"",
            ""provider_name"": ""HBO"",
            ""provider_id"": 118
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/xM2A6jTb4895MIuqPa6W6ooEcJS.jpg"",
            ""provider_name"": ""My5"",
            ""provider_id"": 333
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/rWYJ9mMvxs0p57Nd1BKEtKtpRvD.jpg"",
            ""provider_name"": ""Supo Mungam Plus"",
            ""provider_id"": 530
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/9nYphuoVD2doYP1Fc0Xij1j3Qdm.jpg"",
            ""provider_name"": ""Tenk"",
            ""provider_id"": 550
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/xTVM8uXT9QocigQ07LE7Irc65W2.jpg"",
            ""provider_name"": ""Telia Play"",
            ""provider_id"": 553
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/dpqap8iY6bsSqQf4xrkAG2j43gS.jpg"",
            ""provider_name"": ""DRTV"",
            ""provider_id"": 620
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/wUe8sI0PyRNNaWTSIDUoRADytvR.jpg"",
            ""provider_name"": ""BBC Player Amazon Channel"",
            ""provider_id"": 285
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/21dEscfO8n1tL35k4DANixhffsR.jpg"",
            ""provider_name"": ""Vudu"",
            ""provider_id"": 7
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/dtU2zKZvtdKgSKjyKekp8t0Ryd1.jpg"",
            ""provider_name"": ""BritBox"",
            ""provider_id"": 380
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/mAehaBHcatpbaYgZ0G6Z1czkXax.jpg"",
            ""provider_name"": ""Discovery Plus"",
            ""provider_id"": 524
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/iGDZ6zPbVcngc0BQEsZX13Z7I07.jpg"",
            ""provider_name"": ""KKTV"",
            ""provider_id"": 624
        },
        {
            ""display_priority"": 38,
            ""logo_path"": ""/nVly1ywNU2hMYLaieL6ixhEFTWh.jpg"",
            ""provider_name"": ""CBC Gem"",
            ""provider_id"": 314
        },
        {
            ""display_priority"": 38,
            ""logo_path"": ""/xzfVRl1CgJPYa9dOoyVI3TDSQo2.jpg"",
            ""provider_name"": ""VUDU Free"",
            ""provider_id"": 332
        },
        {
            ""display_priority"": 38,
            ""logo_path"": ""/qEFO4pJhL6IyHbKUqaefsOA0kWJ.jpg"",
            ""provider_name"": ""Filme Filme"",
            ""provider_id"": 566
        },
        {
            ""display_priority"": 38,
            ""logo_path"": ""/wLZCjEAlCKjEkQQM75bITfqL7D0.jpg"",
            ""provider_name"": ""LINE TV"",
            ""provider_id"": 625
        },
        {
            ""display_priority"": 39,
            ""logo_path"": ""/hNO6rEpZ9l2LQEkjacrpeoocKbX.jpg"",
            ""provider_name"": ""CTV"",
            ""provider_id"": 326
        },
        {
            ""display_priority"": 39,
            ""logo_path"": ""/zVJhpmIEgdDVbDt5TB72sZu3qdO.jpg"",
            ""provider_name"": ""Starz"",
            ""provider_id"": 43
        },
        {
            ""display_priority"": 40,
            ""logo_path"": ""/4TJTNWd2TT1kYj6ocUEsQc8WRgr.jpg"",
            ""provider_name"": ""Criterion Channel"",
            ""provider_id"": 258
        },
        {
            ""display_priority"": 40,
            ""logo_path"": ""/75mU4aWHPnMxSl95VT5O4lCR64U.jpg"",
            ""provider_name"": ""STUDIOCANAL PRESENTS Apple TV Channel"",
            ""provider_id"": 642
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/lwefE4yPpCQGhH2LotPuhGA8gCV.jpg"",
            ""provider_name"": ""Universcine"",
            ""provider_id"": 239
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/9TteoCidgcwT28Zs9yeAX7HdkwO.jpg"",
            ""provider_name"": ""Animax Plus Amazon Channel"",
            ""provider_id"": 195
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/4kL33LoKd99YFIaSOoOPMQOSw1A.jpg"",
            ""provider_name"": ""Showtime"",
            ""provider_id"": 37
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/bxdNcDbk1ohVeOMmM3eusAAiTLw.jpg"",
            ""provider_name"": ""HBO Go"",
            ""provider_id"": 425
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/gzHzhgt6cVSn4yy6UnJvLGbOSwL.jpg"",
            ""provider_name"": ""KinoPop"",
            ""provider_id"": 573
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/v3PhM1pr6omrcffUoBBZkiVeApH.jpg"",
            ""provider_name"": ""STV Player"",
            ""provider_id"": 593
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/5OtaT8STJ8ZMkKt994C5XxrEAaP.jpg"",
            ""provider_name"": ""UPC TV"",
            ""provider_id"": 622
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/gmXeSpaYVJcb49SAzYcVHgQKQWM.jpg"",
            ""provider_name"": ""Filmtastic Amazon Channel"",
            ""provider_id"": 334
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/2BPU00vSfCZ4XI2CnQCBv8rZk2f.jpg"",
            ""provider_name"": ""CBS"",
            ""provider_id"": 78
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/bbxgdl6B5T75wJE713BiTCIBXyS.jpg"",
            ""provider_name"": ""PBS"",
            ""provider_id"": 209
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/vrFpju3t7kplDbFsN5GLJpG0obj.jpg"",
            ""provider_name"": ""Lionsgate Play"",
            ""provider_id"": 561
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/xbdgLcQ6kRrcVe1uJAG9lzlkSbY.jpg"",
            ""provider_name"": ""Oi Play"",
            ""provider_id"": 574
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/hRqG400ljOAbbQkoos4W4gq2uPN.jpg"",
            ""provider_name"": ""CineMember"",
            ""provider_id"": 639
        },
        {
            ""display_priority"": 43,
            ""logo_path"": ""/2tAjxjo1n3H7fsXqMsxWFMeFUWp.jpg"",
            ""provider_name"": ""Pantaflix"",
            ""provider_id"": 177
        },
        {
            ""display_priority"": 43,
            ""logo_path"": ""/bCQVIO5iEjfstObco3fuhFB7sbs.jpg"",
            ""provider_name"": ""OUTtv Amazon Channel"",
            ""provider_id"": 607
        },
        {
            ""display_priority"": 44,
            ""logo_path"": ""/twV9iQPYeaoBzwsfRFGMGoMIUg8.jpg"",
            ""provider_name"": ""FXNow"",
            ""provider_id"": 123
        },
        {
            ""display_priority"": 44,
            ""logo_path"": ""/tQL30UKe7OykrtkYQCmYEFrdIMC.jpg"",
            ""provider_name"": ""Love Nature Amazon Channel"",
            ""provider_id"": 608
        },
        {
            ""display_priority"": 45,
            ""logo_path"": ""/w2TDH9TRI7pltf5LjN3vXzs7QbN.jpg"",
            ""provider_name"": ""Tubi TV"",
            ""provider_id"": 73
        },
        {
            ""display_priority"": 45,
            ""logo_path"": ""/mpdAy9QFQHA4OpRzykGUXsXqltN.jpg"",
            ""provider_name"": ""wedotv"",
            ""provider_id"": 392
        },
        {
            ""display_priority"": 46,
            ""logo_path"": ""/wbCleYwRFpUtWcNi7BLP3E1f6VI.jpg"",
            ""provider_name"": ""Kanopy"",
            ""provider_id"": 191
        },
        {
            ""display_priority"": 46,
            ""logo_path"": ""/awgDmkHSfGEcoIVpeQKwaE2OgLM.jpg"",
            ""provider_name"": ""Global TV"",
            ""provider_id"": 449
        },
        {
            ""display_priority"": 46,
            ""logo_path"": ""/fUUgfrOfvvPKx9vhFBd6IMdkfLy.jpg"",
            ""provider_name"": ""MGM Amazon Channel"",
            ""provider_id"": 588
        },
        {
            ""display_priority"": 47,
            ""logo_path"": ""/sXixZNwjBjMoBR97alHOKVerKf4.jpg"",
            ""provider_name"": ""Kino on Demand"",
            ""provider_id"": 349
        },
        {
            ""display_priority"": 47,
            ""logo_path"": ""/h1PNHFp50cceDZ8jXUMnuVVMIw2.jpg"",
            ""provider_name"": ""VI movies and tv"",
            ""provider_id"": 614
        },
        {
            ""display_priority"": 47,
            ""logo_path"": ""/yJflQpWbgaiqYEVsFrE18lIEbaG.jpg"",
            ""provider_name"": ""FlixOl\u00E9 Amazon Channel"",
            ""provider_id"": 684
        },
        {
            ""display_priority"": 48,
            ""logo_path"": ""/shq88b09gTBYC4hA7K7MUL8Q4zP.jpg"",
            ""provider_name"": ""Microsoft Store"",
            ""provider_id"": 68
        },
        {
            ""display_priority"": 48,
            ""logo_path"": ""/3gTVbIj15Amgz5Qqg5dPDpgMW9V.jpg"",
            ""provider_name"": ""Looke Amazon Channel"",
            ""provider_id"": 683
        },
        {
            ""display_priority"": 48,
            ""logo_path"": ""/yELuKENa93S31x4Mlkk4jp5PThi.jpg"",
            ""provider_name"": ""TVCortos Amazon Channel"",
            ""provider_id"": 689
        },
        {
            ""display_priority"": 49,
            ""logo_path"": ""/2joD3S2goOB6lmepX35A8dmaqgM.jpg"",
            ""provider_name"": ""Joyn Plus"",
            ""provider_id"": 421
        },
        {
            ""display_priority"": 49,
            ""logo_path"": ""/yXAjdxUTdehG4YUUEevvaeRhZl7.jpg"",
            ""provider_name"": ""NFB"",
            ""provider_id"": 441
        },
        {
            ""display_priority"": 49,
            ""logo_path"": ""/l6boVLijqAZLYXlZpkzzeNC4mvg.jpg"",
            ""provider_name"": ""Pongalo Amazon Channel  "",
            ""provider_id"": 690
        },
        {
            ""display_priority"": 49,
            ""logo_path"": ""/jWKX6kO7JqQbqVnu9QtEO6FC85n.jpg"",
            ""provider_name"": ""meJane"",
            ""provider_id"": 697
        },
        {
            ""display_priority"": 50,
            ""logo_path"": ""/gbyLHzl4eYP0oP9oJZ2oKbpkhND.jpg"",
            ""provider_name"": ""Redbox"",
            ""provider_id"": 279
        },
        {
            ""display_priority"": 50,
            ""logo_path"": ""/42Cj5KNEteBRpfWnGWQbTJpJDGV.jpg"",
            ""provider_name"": ""OCS Amazon Channel "",
            ""provider_id"": 685
        },
        {
            ""display_priority"": 51,
            ""logo_path"": ""/iB415dMHBjdTZOOZA06FA1sxDWN.jpg"",
            ""provider_name"": ""Moviedome Plus Amazon Channel"",
            ""provider_id"": 706
        },
        {
            ""display_priority"": 51,
            ""logo_path"": ""/wdbbz8SsamximWXn0AR5f5U3fOw.jpg"",
            ""provider_name"": ""Acontra Plus"",
            ""provider_id"": 1717
        },
        {
            ""display_priority"": 52,
            ""logo_path"": ""/AhaVozbDe3SPHXTKyd6Crdt720S.jpg"",
            ""provider_name"": ""Max Go"",
            ""provider_id"": 139
        },
        {
            ""display_priority"": 52,
            ""logo_path"": ""/xB6NQNF0vlRlRh4KNPFE1Vlqn1Q.jpg"",
            ""provider_name"": ""Aniverse Amazon Channel"",
            ""provider_id"": 707
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/l9BRdAgQ3MkooOalsuu3yFQv2XP.jpg"",
            ""provider_name"": ""ABC"",
            ""provider_id"": 148
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/6FWwq6rayak6g6rvzVVP1NnX9gf.jpg"",
            ""provider_name"": ""Club Illico"",
            ""provider_id"": 469
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/3xIBSZdL2pZCJR2saHwDPhKW2aZ.jpg"",
            ""provider_name"": ""Home of Horror"",
            ""provider_id"": 479
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/pHJhFw6XXRCTnIKK06MT0m1Vll4.jpg"",
            ""provider_name"": ""Superfresh Amazon Channel"",
            ""provider_id"": 708
        },
        {
            ""display_priority"": 54,
            ""logo_path"": ""/xL9SUR63qrEjFZAhtsipskeAMR7.jpg"",
            ""provider_name"": ""DIRECTV"",
            ""provider_id"": 358
        },
        {
            ""display_priority"": 54,
            ""logo_path"": ""/u04LR9vGEhc8B1ml4HSj1RCbqTG.jpg"",
            ""provider_name"": ""Filmtastic"",
            ""provider_id"": 480
        },
        {
            ""display_priority"": 55,
            ""logo_path"": ""/7P2JHkfv4AmU2MgSPGaJ0z6nNLG.jpg"",
            ""provider_name"": ""Crackle"",
            ""provider_id"": 12
        },
        {
            ""display_priority"": 55,
            ""logo_path"": ""/xtfU2pO6YjnU0qrPaDi30IjaQGR.jpg"",
            ""provider_name"": ""ArthouseCNMA"",
            ""provider_id"": 481
        },
        {
            ""display_priority"": 55,
            ""logo_path"": ""/dCO5ge3nDm4LdnWSPe6jHPciE7U.jpg"",
            ""provider_name"": ""tvo"",
            ""provider_id"": 488
        },
        {
            ""display_priority"": 56,
            ""logo_path"": ""/sTwowAulL7eZpgJORBKPKepIbxw.jpg"",
            ""provider_name"": ""Popcorntimes"",
            ""provider_id"": 522
        },
        {
            ""display_priority"": 56,
            ""logo_path"": ""/iPK2kpaKnGYvSdEcRerIbkqWVPh.jpg"",
            ""provider_name"": ""Knowledge Network"",
            ""provider_id"": 525
        },
        {
            ""display_priority"": 57,
            ""logo_path"": ""/pGk6V35szQnJVq2OoJLnRpjifb3.jpg"",
            ""provider_name"": ""ILLICO"",
            ""provider_id"": 492
        },
        {
            ""display_priority"": 58,
            ""logo_path"": ""/eAhAUvV2ouai3cGti5y70YOtrBN.jpg"",
            ""provider_name"": ""Fandor"",
            ""provider_id"": 25
        },
        {
            ""display_priority"": 60,
            ""logo_path"": ""/3ISpW4LBSKAaCyIZI3cxHiox8dI.jpg"",
            ""provider_name"": ""Noovo"",
            ""provider_id"": 516
        },
        {
            ""display_priority"": 60,
            ""logo_path"": ""/1h8etYGesCuldkQGoUDyDJr92EB.jpg"",
            ""provider_name"": ""Amazon Arthaus Channel"",
            ""provider_id"": 533
        },
        {
            ""display_priority"": 61,
            ""logo_path"": ""/c7nw5oRfx5iZfyX0QmtOK0pbVaJ.jpg"",
            ""provider_name"": ""Epix"",
            ""provider_id"": 34
        },
        {
            ""display_priority"": 61,
            ""logo_path"": ""/3OoJykZgg9frZwIta01EJAocKjY.jpg"",
            ""provider_name"": ""Arthouse CNMA Amazon Channel"",
            ""provider_id"": 687
        },
        {
            ""display_priority"": 62,
            ""logo_path"": ""/rgpmwMkXqFYch9cway9qWMw0uXu.jpg"",
            ""provider_name"": ""Freeform"",
            ""provider_id"": 211
        },
        {
            ""display_priority"": 62,
            ""logo_path"": ""/dKH9TB94EIbnaWnjO6vX0snaNVP.jpg"",
            ""provider_name"": ""ZDF"",
            ""provider_id"": 537
        },
        {
            ""display_priority"": 62,
            ""logo_path"": ""/nqGY5wuSv14vbY7NYOs8stJ6ZBF.jpg"",
            ""provider_name"": ""Now TV Cinema"",
            ""provider_id"": 591
        },
        {
            ""display_priority"": 63,
            ""logo_path"": ""/m6pLJ0l6MQJiKg1yxEs1holRSiq.jpg"",
            ""provider_name"": ""History"",
            ""provider_id"": 155
        },
        {
            ""display_priority"": 63,
            ""logo_path"": ""/o6li3XZrBKXSqyNRS39UQEfPTCH.jpg"",
            ""provider_name"": ""Virgin TV Go"",
            ""provider_id"": 594
        },
        {
            ""display_priority"": 64,
            ""logo_path"": ""/f7iqKjWYdVoYVIvKP3nboULcrM2.jpg"",
            ""provider_name"": ""Syfy"",
            ""provider_id"": 215
        },
        {
            ""display_priority"": 64,
            ""logo_path"": ""/aNvHP7E7X4hEGW7aT5tyo1xfnFN.jpg"",
            ""provider_name"": ""CuriosityStream Amazon Channel"",
            ""provider_id"": 603
        },
        {
            ""display_priority"": 65,
            ""logo_path"": ""/q6hCkmhpK5cDUURb4i6yWXNfpZz.jpg"",
            ""provider_name"": ""filmfriend"",
            ""provider_id"": 542
        },
        {
            ""display_priority"": 65,
            ""logo_path"": ""/jdQu3zBkbZCKnVUZwm63jBxAATk.jpg"",
            ""provider_name"": ""DocuBay Amazon Channel"",
            ""provider_id"": 604
        },
        {
            ""display_priority"": 66,
            ""logo_path"": ""/3wJNOOCbvqi7fJAdgf1QpL7Wwe2.jpg"",
            ""provider_name"": ""Lifetime"",
            ""provider_id"": 157
        },
        {
            ""display_priority"": 67,
            ""logo_path"": ""/pheENW1BxlexXX1CKJ4GyWudyMA.jpg"",
            ""provider_name"": ""Shudder"",
            ""provider_id"": 99
        },
        {
            ""display_priority"": 67,
            ""logo_path"": ""/3Jz6S84ovoY8oohXrxrD3DtaBMD.jpg"",
            ""provider_name"": ""digital TIFF Bell Lightbox"",
            ""provider_id"": 535
        },
        {
            ""display_priority"": 68,
            ""logo_path"": ""/c2Ey5Q3uUjZgfWWQQIdVIjVfxE4.jpg"",
            ""provider_name"": ""Screambox"",
            ""provider_id"": 185
        },
        {
            ""display_priority"": 68,
            ""logo_path"": ""/shdbLP97nPOXYVUmw7yfxXcbWFT.jpg"",
            ""provider_name"": ""Full Moon Amazon Channel"",
            ""provider_id"": 597
        },
        {
            ""display_priority"": 70,
            ""logo_path"": ""/pZ9TSk3wlRYwiwwRxTsQJ7t2but.jpg"",
            ""provider_name"": ""Sundance Now"",
            ""provider_id"": 143
        },
        {
            ""display_priority"": 71,
            ""logo_path"": ""/olvOut34aWUFf1YoOqiqtjidiTK.jpg"",
            ""provider_name"": ""Popcornflix"",
            ""provider_id"": 241
        },
        {
            ""display_priority"": 71,
            ""logo_path"": ""/itokSbqrAKttZajeuUQlwDOy3zS.jpg"",
            ""provider_name"": ""Pok\u00E9mon Amazon Channel"",
            ""provider_id"": 599
        },
        {
            ""display_priority"": 72,
            ""logo_path"": ""/6qlN4ra7T69JynYVbs9SXVUlkLk.jpg"",
            ""provider_name"": ""Shout! Factory Amazon Channel"",
            ""provider_id"": 600
        },
        {
            ""display_priority"": 74,
            ""logo_path"": ""/10BQc1kYmgjXFrFKb3xsRcDDn14.jpg"",
            ""provider_name"": ""realeyz"",
            ""provider_id"": 14
        },
        {
            ""display_priority"": 74,
            ""logo_path"": ""/1K9ZUrerCHALa0dyZ1OxidsJ28u.jpg"",
            ""provider_name"": ""Eros Now Amazon Channel"",
            ""provider_id"": 595
        },
        {
            ""display_priority"": 75,
            ""logo_path"": ""/kgyeCy524FHBpL0oWWKnjGArt9h.jpg"",
            ""provider_name"": ""FilmBox Live Amazon Channel"",
            ""provider_id"": 602
        },
        {
            ""display_priority"": 76,
            ""logo_path"": ""/ihCOKHIsNdqzhftgiJld6YVwr1G.jpg"",
            ""provider_name"": ""MotorTrend Amazon Channel"",
            ""provider_id"": 601
        },
        {
            ""display_priority"": 77,
            ""logo_path"": ""/94IdHexespnJs96kmGiJlflfiwU.jpg"",
            ""provider_name"": ""Pantaya"",
            ""provider_id"": 247
        },
        {
            ""display_priority"": 77,
            ""logo_path"": ""/kvn50K9EIdwJhpLwnFFE1D2rOIZ.jpg"",
            ""provider_name"": ""IFC Amazon Channel"",
            ""provider_id"": 587
        },
        {
            ""display_priority"": 77,
            ""logo_path"": ""/hTXE4J8fB7KDYkhY6KbwJIfHMtM.jpg"",
            ""provider_name"": ""W4free"",
            ""provider_id"": 615
        },
        {
            ""display_priority"": 77,
            ""logo_path"": ""/wblLDecRE2PG2c2rdSkGtkqhgHr.jpg"",
            ""provider_name"": ""Filmlegenden Amazon Channel"",
            ""provider_id"": 678
        },
        {
            ""display_priority"": 78,
            ""logo_path"": ""/oRXiHzPl2HJMXXFR4eebsb8F5Oc.jpg"",
            ""provider_name"": ""Boomerang"",
            ""provider_id"": 248
        },
        {
            ""display_priority"": 78,
            ""logo_path"": ""/xDGeUEkg6Vkud7lhZOyhsi3EIYj.jpg"",
            ""provider_name"": ""Nick\u002B Amazon Channel"",
            ""provider_id"": 589
        },
        {
            ""display_priority"": 78,
            ""logo_path"": ""/u0x5jHjdhvlNtFrfb63XDJvFwFu.jpg"",
            ""provider_name"": ""Cinema of Hearts Amazon Channel"",
            ""provider_id"": 679
        },
        {
            ""display_priority"": 79,
            ""logo_path"": ""/5uTsmZnDQmIOjZPEv8TNTy7GRJB.jpg"",
            ""provider_name"": ""Urban Movie Channel"",
            ""provider_id"": 251
        },
        {
            ""display_priority"": 79,
            ""logo_path"": ""/chOSZtRhgwzrMyMa5Hx8QG0Vwx7.jpg"",
            ""provider_name"": ""Paus"",
            ""provider_id"": 618
        },
        {
            ""display_priority"": 79,
            ""logo_path"": ""/jTvkFUHvHsUoOzZRZ8WpmkiZD1v.jpg"",
            ""provider_name"": ""Home of Horror Amazon Channel"",
            ""provider_id"": 686
        },
        {
            ""display_priority"": 80,
            ""logo_path"": ""/3bm7P1O8WRqK6CYqfffJv4fba2p.jpg"",
            ""provider_name"": ""History Vault"",
            ""provider_id"": 268
        },
        {
            ""display_priority"": 80,
            ""logo_path"": ""/97AOkkJyYajWLHgKGpK5NZOQ22O.jpg"",
            ""provider_name"": ""Bloody Movies Amazon Channel"",
            ""provider_id"": 680
        },
        {
            ""display_priority"": 81,
            ""logo_path"": ""/cBCzPOX6ir5L8hCoJlfIWycxauh.jpg"",
            ""provider_name"": ""Dove Channel"",
            ""provider_id"": 254
        },
        {
            ""display_priority"": 81,
            ""logo_path"": ""/jqByg3hw9LsuKTxgpAQPbO9b1ZQ.jpg"",
            ""provider_name"": ""Super Channel Amazon Channel"",
            ""provider_id"": 605
        },
        {
            ""display_priority"": 81,
            ""logo_path"": ""/4Q3nrYFVeOaSB76JlH2Ep08AQxA.jpg"",
            ""provider_name"": ""Film Total Amazon Channel"",
            ""provider_id"": 681
        },
        {
            ""display_priority"": 82,
            ""logo_path"": ""/8qNJcPBHZ4qewHrDJ7C7s2DBQ3V.jpg"",
            ""provider_name"": ""Yupp TV"",
            ""provider_id"": 255
        },
        {
            ""display_priority"": 82,
            ""logo_path"": ""/h8sud4kBfHnTni7G7pTnOGcArco.jpg"",
            ""provider_name"": ""StackTV Amazon Channel"",
            ""provider_id"": 606
        },
        {
            ""display_priority"": 82,
            ""logo_path"": ""/7BVWcGgkBBVpqg7RdoxlhIgb3s4.jpg"",
            ""provider_name"": ""Turk On Video Amazon Channel"",
            ""provider_id"": 693
        },
        {
            ""display_priority"": 83,
            ""logo_path"": ""/4XYI2rzRm34skcvamytegQx7Dmu.jpg"",
            ""provider_name"": ""Eros Now"",
            ""provider_id"": 218
        },
        {
            ""display_priority"": 84,
            ""logo_path"": ""/foT1TtL67MgEOWR6Cib8dKyCvJI.jpg"",
            ""provider_name"": ""Magnolia Selects"",
            ""provider_id"": 259
        },
        {
            ""display_priority"": 85,
            ""logo_path"": ""/rDYZ9v3Y09fuFyan51tHKE1mFId.jpg"",
            ""provider_name"": ""WWE Network"",
            ""provider_id"": 260
        },
        {
            ""display_priority"": 85,
            ""logo_path"": ""/rZALpU2NvloNDBuUWX7BVBPFLDG.jpg"",
            ""provider_name"": ""Smithsonian Channel Amazon Channel"",
            ""provider_id"": 609
        },
        {
            ""display_priority"": 86,
            ""logo_path"": ""/xGUwoyO5LlHKEQGGYSMoLxo7c6D.jpg"",
            ""provider_name"": ""BBC Earth Amazon Channel"",
            ""provider_id"": 610
        },
        {
            ""display_priority"": 87,
            ""logo_path"": ""/oMwjMgYiT2jcR7ELqCH3TPzpgTX.jpg"",
            ""provider_name"": ""Nickhits Amazon Channel"",
            ""provider_id"": 261
        },
        {
            ""display_priority"": 88,
            ""logo_path"": ""/yxBUPUBFzHE72uFXvFr1l0fnMJA.jpg"",
            ""provider_name"": ""Noggin Amazon Channel"",
            ""provider_id"": 262
        },
        {
            ""display_priority"": 88,
            ""logo_path"": ""/e07gcWq5OWhJ8MxZncJrDuoJAp2.jpg"",
            ""provider_name"": ""UMC Amazon Channel"",
            ""provider_id"": 612
        },
        {
            ""display_priority"": 90,
            ""logo_path"": ""/w4GTJ1EDrgJku49XKSnRag9kKCT.jpg"",
            ""provider_name"": ""Laugh Out Loud"",
            ""provider_id"": 275
        },
        {
            ""display_priority"": 91,
            ""logo_path"": ""/UAZ2lJBWszijybQD4frqw2jxRO.jpg"",
            ""provider_name"": ""Smithsonian Channel"",
            ""provider_id"": 276
        },
        {
            ""display_priority"": 92,
            ""logo_path"": ""/orsVBNvPWxJNOVSEHMOk2h8R1wA.jpg"",
            ""provider_name"": ""Pure Flix"",
            ""provider_id"": 278
        },
        {
            ""display_priority"": 93,
            ""logo_path"": ""/llEJ6av9kAniTQUR9hF9mhVbzlB.jpg"",
            ""provider_name"": ""Hallmark Movies"",
            ""provider_id"": 281
        },
        {
            ""display_priority"": 94,
            ""logo_path"": ""/p1v0UKH13xQsMjumRgCGmCdlgKm.jpg"",
            ""provider_name"": ""Lifetime Movie Club"",
            ""provider_id"": 284
        },
        {
            ""display_priority"": 95,
            ""logo_path"": ""/tU4tamrqRjbg3Lbmkryp3EiLPQJ.jpg"",
            ""provider_name"": ""PBS Kids Amazon Channel"",
            ""provider_id"": 293
        },
        {
            ""display_priority"": 96,
            ""logo_path"": ""/1zfRJQc14uEzZThdwNvxtxeWJw6.jpg"",
            ""provider_name"": ""Boomerang Amazon Channel"",
            ""provider_id"": 288
        },
        {
            ""display_priority"": 97,
            ""logo_path"": ""/kEnyHRflZPNWEOIXroZPhfdGi46.jpg"",
            ""provider_name"": ""Cinemax Amazon Channel"",
            ""provider_id"": 289
        },
        {
            ""display_priority"": 97,
            ""logo_path"": ""/fTc12wQdF3tOgKE16Eai4vjOFPg.jpg"",
            ""provider_name"": ""Hollywood Suite Amazon Channel"",
            ""provider_id"": 705
        },
        {
            ""display_priority"": 98,
            ""logo_path"": ""/fvSJ17mOt3MxKfnSgQVrtXTuepq.jpg"",
            ""provider_name"": ""Pantaya Amazon Channel"",
            ""provider_id"": 292
        },
        {
            ""display_priority"": 99,
            ""logo_path"": ""/6L2wLiZz3IG2X4MRbdRlGLgftMK.jpg"",
            ""provider_name"": ""Hallmark Movies Now Amazon Channel"",
            ""provider_id"": 290
        },
        {
            ""display_priority"": 100,
            ""logo_path"": ""/mMALQK52OFGoYUKOSCZILZkfGWs.jpg"",
            ""provider_name"": ""PBS Masterpiece Amazon Channel"",
            ""provider_id"": 294
        },
        {
            ""display_priority"": 101,
            ""logo_path"": ""/mlH42JbZMrapSF6zc8iTYURcZlH.jpg"",
            ""provider_name"": ""Viewster Amazon Channel"",
            ""provider_id"": 295
        },
        {
            ""display_priority"": 102,
            ""logo_path"": ""/72tiOIjZQPqm7MGhqoqyjyTJzSv.jpg"",
            ""provider_name"": ""MZ Choice Amazon Channel"",
            ""provider_id"": 291
        },
        {
            ""display_priority"": 104,
            ""logo_path"": ""/aLQ8rZhO7vgPFy1tae5vxvb81Wl.jpg"",
            ""provider_name"": ""Sling TV"",
            ""provider_id"": 299
        },
        {
            ""display_priority"": 106,
            ""logo_path"": ""/9baY98ZKyDaNArp1H9fAWqiR3Zi.jpg"",
            ""provider_name"": ""HiDive"",
            ""provider_id"": 430
        },
        {
            ""display_priority"": 108,
            ""logo_path"": ""/ba8l0e5CkpVnrdFgzBySP7ckZnZ.jpg"",
            ""provider_name"": ""Night Flight Plus"",
            ""provider_id"": 455
        },
        {
            ""display_priority"": 109,
            ""logo_path"": ""/ubWucXFn34TrVlJBaJFgPaC4tOP.jpg"",
            ""provider_name"": ""Topic"",
            ""provider_id"": 454
        },
        {
            ""display_priority"": 111,
            ""logo_path"": ""/9ONs8SMAXtkiyaEIKATTpbwckx8.jpg"",
            ""provider_name"": ""Retrocrush"",
            ""provider_id"": 446
        },
        {
            ""display_priority"": 114,
            ""logo_path"": ""/ju3T8MFGNIoPiYpwHFpNlrYNyG7.jpg"",
            ""provider_name"": ""Shout! Factory TV"",
            ""provider_id"": 439
        },
        {
            ""display_priority"": 115,
            ""logo_path"": ""/3tCqvc5hPm5nl8Hm8o2koDRZlPo.jpg"",
            ""provider_name"": ""Chai Flicks"",
            ""provider_id"": 438
        },
        {
            ""display_priority"": 116,
            ""logo_path"": ""/nXi2nRDPMNivJyFOifEa2t15Xuu.jpg"",
            ""provider_name"": ""OVID"",
            ""provider_id"": 433
        },
        {
            ""display_priority"": 118,
            ""logo_path"": ""/rOwEnT8oDSTZ5rDKmyaa3O4gUnc.jpg"",
            ""provider_name"": ""The Film Detective"",
            ""provider_id"": 470
        },
        {
            ""display_priority"": 120,
            ""logo_path"": ""/sc5pTTCFbx7GQyOst5SG4U7nkPH.jpg"",
            ""provider_name"": ""Shudder Amazon Channel"",
            ""provider_id"": 204
        },
        {
            ""display_priority"": 121,
            ""logo_path"": ""/aJUiN18NZFbpSkHZQV1C1cTpz8H.jpg"",
            ""provider_name"": ""Mubi Amazon Channel"",
            ""provider_id"": 201
        },
        {
            ""display_priority"": 122,
            ""logo_path"": ""/8WWD7t5Irwq9kAH4rufQ4Pe1Dog.jpg"",
            ""provider_name"": ""AcornTV Amazon Channel"",
            ""provider_id"": 196
        },
        {
            ""display_priority"": 123,
            ""logo_path"": ""/xTfyFZqWv8c8sxlFooUzemi6WRM.jpg"",
            ""provider_name"": ""BritBox Amazon Channel"",
            ""provider_id"": 197
        },
        {
            ""display_priority"": 124,
            ""logo_path"": ""/8vBJZkwkrUDYMSfmw5R0ZENd7yw.jpg"",
            ""provider_name"": ""Fandor Amazon Channel"",
            ""provider_id"": 199
        },
        {
            ""display_priority"": 125,
            ""logo_path"": ""/naqM14qSfg2q0S2zDylM5zQQ3jn.jpg"",
            ""provider_name"": ""Screambox Amazon Channel"",
            ""provider_id"": 202
        },
        {
            ""display_priority"": 126,
            ""logo_path"": ""/xImSZRKRYzIMPr4COgJNsEHdd2T.jpg"",
            ""provider_name"": ""Sundance Now Amazon Channel"",
            ""provider_id"": 205
        },
        {
            ""display_priority"": 129,
            ""logo_path"": ""/ldU2RCgdvkcSEBWWbttCpVO450z.jpg"",
            ""provider_name"": ""USA Network"",
            ""provider_id"": 322
        },
        {
            ""display_priority"": 131,
            ""logo_path"": ""/4U02VrbgLfUKJAUCHKzxWFtnPx4.jpg"",
            ""provider_name"": ""FlixFling"",
            ""provider_id"": 331
        },
        {
            ""display_priority"": 132,
            ""logo_path"": ""/obBJU4ak4XvAOUM5iVmSUxDvqC3.jpg"",
            ""provider_name"": ""Bet\u002B Amazon Channel"",
            ""provider_id"": 343
        },
        {
            ""display_priority"": 134,
            ""logo_path"": ""/kJlVJLgbNPvKDYC0YMp3yA2OKq2.jpg"",
            ""provider_name"": ""AMC on Demand"",
            ""provider_id"": 352
        },
        {
            ""display_priority"": 135,
            ""logo_path"": ""/x4AFz5koB2R8BRn8WNh6EqXUGHc.jpg"",
            ""provider_name"": ""Darkmatter TV"",
            ""provider_id"": 355
        },
        {
            ""display_priority"": 136,
            ""logo_path"": ""/8TbsXATKVD4Humjzi6a8SVaSY7o.jpg"",
            ""provider_name"": ""TCM"",
            ""provider_id"": 361
        },
        {
            ""display_priority"": 137,
            ""logo_path"": ""/cezAIHmsUVvgAahfCR7J0z30y1N.jpg"",
            ""provider_name"": ""Bravo TV"",
            ""provider_id"": 365
        },
        {
            ""display_priority"": 138,
            ""logo_path"": ""/gJnQ40Z6T7HyY6fbmmI6qKE0zmK.jpg"",
            ""provider_name"": ""TNT"",
            ""provider_id"": 363
        },
        {
            ""display_priority"": 140,
            ""logo_path"": ""/ukSXbR5qFjO2qCHpc6ZhcGPSjTJ.jpg"",
            ""provider_name"": ""BBC America"",
            ""provider_id"": 397
        },
        {
            ""display_priority"": 141,
            ""logo_path"": ""/2NRn6OApVKfDTKLuHDRN8UadLRw.jpg"",
            ""provider_name"": ""IndieFlix"",
            ""provider_id"": 368
        },
        {
            ""display_priority"": 156,
            ""logo_path"": ""/sa10pK4Jwr5aA7rvafFP2zyLFjh.jpg"",
            ""provider_name"": ""Here TV"",
            ""provider_id"": 417
        },
        {
            ""display_priority"": 160,
            ""logo_path"": ""/6fX0J6x7zXsUCvPFczgOW4oD34D.jpg"",
            ""provider_name"": ""Flix Premiere"",
            ""provider_id"": 432
        },
        {
            ""display_priority"": 162,
            ""logo_path"": ""/rcebVnRvZvPXauK4353Jgiu4DWI.jpg"",
            ""provider_name"": ""TBS"",
            ""provider_id"": 506
        },
        {
            ""display_priority"": 163,
            ""logo_path"": ""/3VxDqUk25KU5860XxHKwV9cy3L8.jpg"",
            ""provider_name"": ""AsianCrush"",
            ""provider_id"": 514
        },
        {
            ""display_priority"": 164,
            ""logo_path"": ""/mEiBVz62M9j3TCebmOspMfqkIn.jpg"",
            ""provider_name"": ""FILMRISE"",
            ""provider_id"": 471
        },
        {
            ""display_priority"": 165,
            ""logo_path"": ""/r1UgUKmt83FSDOIHBdRWKooZPNx.jpg"",
            ""provider_name"": ""Revry"",
            ""provider_id"": 473
        },
        {
            ""display_priority"": 168,
            ""logo_path"": ""/79mRAYq40lcYiXkQm6N7YErSSHd.jpg"",
            ""provider_name"": ""Spectrum On Demand"",
            ""provider_id"": 486
        },
        {
            ""display_priority"": 170,
            ""logo_path"": ""/mB2eDIncwSAlyl8WAtfV24qEIkk.jpg"",
            ""provider_name"": ""Hi-YAH"",
            ""provider_id"": 503
        },
        {
            ""display_priority"": 171,
            ""logo_path"": ""/rtTqPKRrVVXxvPV0T9OmSXhwXnY.jpg"",
            ""provider_name"": ""VRV"",
            ""provider_id"": 504
        },
        {
            ""display_priority"": 172,
            ""logo_path"": ""/pg4bIFyUsSIhFChqOz5Up1BxuIU.jpg"",
            ""provider_name"": ""tru TV"",
            ""provider_id"": 507
        },
        {
            ""display_priority"": 175,
            ""logo_path"": ""/v8vA6WnPVTOE1o39waNFVmAqEJj.jpg"",
            ""provider_name"": ""Discovery Plus"",
            ""provider_id"": 520
        },
        {
            ""display_priority"": 176,
            ""logo_path"": ""/4UfmxLzph9Aso9pr9bXohp0V3sr.jpg"",
            ""provider_name"": ""ARROW"",
            ""provider_id"": 529
        },
        {
            ""display_priority"": 177,
            ""logo_path"": ""/wDWvnupneMbY6RhBTHQC9zU0SCX.jpg"",
            ""provider_name"": ""Plex"",
            ""provider_id"": 538
        },
        {
            ""display_priority"": 179,
            ""logo_path"": ""/1UP7ysjKolfD0rmp2fLmvyRHkdn.jpg"",
            ""provider_name"": ""Alamo on Demand"",
            ""provider_id"": 547
        },
        {
            ""display_priority"": 184,
            ""logo_path"": ""/fdWE8jpmQqkZrwg2ZMuCLz6ms5P.jpg"",
            ""provider_name"": ""MovieSaints"",
            ""provider_id"": 562
        },
        {
            ""display_priority"": 185,
            ""logo_path"": ""/9sk88OAxDZSdMOzg8VuqtGpgWQ3.jpg"",
            ""provider_name"": ""Dogwoof On Demand"",
            ""provider_id"": 536
        },
        {
            ""display_priority"": 192,
            ""logo_path"": ""/tKJdVrC0fjEtQtYYjlVwX9rmqrj.jpg"",
            ""provider_name"": ""Film Movement Plus"",
            ""provider_id"": 579
        },
        {
            ""display_priority"": 194,
            ""logo_path"": ""/8PmpsrVDLJ3m8I37W6UNFEymhm7.jpg"",
            ""provider_name"": ""Metrograph"",
            ""provider_id"": 585
        },
        {
            ""display_priority"": 195,
            ""logo_path"": ""/iFZdWKZr6kzkNRzZ3oRis0vfWob.jpg"",
            ""provider_name"": ""Freevee Amazon Channel"",
            ""provider_id"": 613
        },
        {
            ""display_priority"": 196,
            ""logo_path"": ""/5FuVJSVSy60JQ58m6fmTyOsiJSC.jpg"",
            ""provider_name"": ""Curia"",
            ""provider_id"": 617
        },
        {
            ""display_priority"": 198,
            ""logo_path"": ""/ttxbDVmHMuNTKcSLOyIHFs7TdRh.jpg"",
            ""provider_name"": ""Kino Now"",
            ""provider_id"": 640
        },
        {
            ""display_priority"": 201,
            ""logo_path"": ""/7EKsM2afDq6P0yUmTHDKQj3wHkQ.jpg"",
            ""provider_name"": ""Cinessance"",
            ""provider_id"": 694
        },
        {
            ""display_priority"": 204,
            ""logo_path"": ""/m0mvKlSjn38S9w7WVNV7a7XyPIe.jpg"",
            ""provider_name"": ""ShortsTV Amazon Channel"",
            ""provider_id"": 688
        }
    ]
}";
        public static readonly string TV_WATCH_PROVIDER_VALUES = @"{
    ""results"": [
        {
            ""display_priority"": 0,
            ""logo_path"": ""/t2yyOv40HZeVlLjYsCsPHnWLk4W.jpg"",
            ""provider_name"": ""Netflix"",
            ""provider_id"": 8
        },
        {
            ""display_priority"": 0,
            ""logo_path"": ""/7Fl8ylPDclt3ZYgNbW2t7rbZE9I.jpg"",
            ""provider_name"": ""Hotstar"",
            ""provider_id"": 122
        },
        {
            ""display_priority"": 0,
            ""logo_path"": ""/w1T8s7FqakcfucR8cgOvbe6UeXN.jpg"",
            ""provider_name"": ""Okko"",
            ""provider_id"": 115
        },
        {
            ""display_priority"": 0,
            ""logo_path"": ""/bKy2YjC0QxViRnd8ayd2pv2ugJZ.jpg"",
            ""provider_name"": ""Fetch TV"",
            ""provider_id"": 436
        },
        {
            ""display_priority"": 1,
            ""logo_path"": ""/emthp39XA2YScoYL1p0sdbAH2WA.jpg"",
            ""provider_name"": ""Amazon Prime Video"",
            ""provider_id"": 119
        },
        {
            ""display_priority"": 1,
            ""logo_path"": ""/emthp39XA2YScoYL1p0sdbAH2WA.jpg"",
            ""provider_name"": ""Amazon Prime Video"",
            ""provider_id"": 9
        },
        {
            ""display_priority"": 1,
            ""logo_path"": ""/nlgoXBQCMSnGZrhAnyIZ7vSQ3vs.jpg"",
            ""provider_name"": ""Amediateka"",
            ""provider_id"": 116
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/peURlLlr8jggOwK53fJ5wdQl05y.jpg"",
            ""provider_name"": ""Apple iTunes"",
            ""provider_id"": 2
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/wTF37o4jOkQfjnWe41gmeuASYZA.jpg"",
            ""provider_name"": ""O2 TV"",
            ""provider_id"": 308
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/z3XAGCCbDD3KTZFvc96Ytr3XR56.jpg"",
            ""provider_name"": ""blutv"",
            ""provider_id"": 341
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/fyZObCfyY6mNVZOaBqgm7UMlHt.jpg"",
            ""provider_name"": ""iflix"",
            ""provider_id"": 160
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/ajbCmwvZ8HiePHZaOVEgm9MzyuA.jpg"",
            ""provider_name"": ""Zee5"",
            ""provider_id"": 232
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/od4YNSSLgOP3p8EtQTnEYfrPa77.jpg"",
            ""provider_name"": ""Neon TV"",
            ""provider_id"": 273
        },
        {
            ""display_priority"": 2,
            ""logo_path"": ""/9mmdMwWkDh12sIeSbEvsFWzmDX2.jpg"",
            ""provider_name"": ""FlixOl\u00E9"",
            ""provider_id"": 393
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/tbEdFQDwx5LEVr8WpSeXQSIirVq.jpg"",
            ""provider_name"": ""Google Play Movies"",
            ""provider_id"": 3
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/3HOI5D6DV8WtL8o1zntNEfi8DLe.jpg"",
            ""provider_name"": ""hayu"",
            ""provider_id"": 223
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/2LS6g6iE5DiAIDiZTAK8mbQQTuE.jpg"",
            ""provider_name"": ""SwissCom"",
            ""provider_id"": 150
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/2u1uElmpm4lProS7C9RYcaYLYt1.jpg"",
            ""provider_name"": ""Voot"",
            ""provider_id"": 121
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/uW4dPCcbXaaFTyfL5HwhuDt5akK.jpg"",
            ""provider_name"": ""Sun Nxt"",
            ""provider_id"": 309
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/2ioan5BX5L9tz4fIGU93blTeFhv.jpg"",
            ""provider_name"": ""wavve"",
            ""provider_id"": 356
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/3namPdisFuyTbB8BX2PxT3OdVCG.jpg"",
            ""provider_name"": ""puhutv"",
            ""provider_id"": 342
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/hGvUo8KZTRLDZWcfFJS3gA8aenB.jpg"",
            ""provider_name"": ""Canal\u002B"",
            ""provider_id"": 381
        },
        {
            ""display_priority"": 3,
            ""logo_path"": ""/d3ixI1no0EpTj2i7u0Sd2DBXVlG.jpg"",
            ""provider_name"": ""BINGE"",
            ""provider_id"": 385
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/pq8p1umEnJjdFAP1nFvNArTR61X.jpg"",
            ""provider_name"": ""Be TV Go"",
            ""provider_id"": 311
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/gJ3yVMWouaVj6iHd59TISJ1TlM5.jpg"",
            ""provider_name"": ""Crave"",
            ""provider_id"": 230
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/riPZYc1ILIbubFaxYSdVfc7K6bm.jpg"",
            ""provider_name"": ""OCS Go"",
            ""provider_id"": 56
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/jRpQbuHbGR0MzSIBxJjxZxpXhqC.jpg"",
            ""provider_name"": ""Jio Cinema"",
            ""provider_id"": 220
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/9pS9Y3xkCLJnti9pi1AyrD5KbZe.jpg"",
            ""provider_name"": ""U-NEXT"",
            ""provider_id"": 84
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/vXXZx0aWQtDv2klvObNugm4dQMN.jpg"",
            ""provider_name"": ""Watcha"",
            ""provider_id"": 97
        },
        {
            ""display_priority"": 4,
            ""logo_path"": ""/pbhaYgvH94ZI2G4r7duAUBqcP2e.jpg"",
            ""provider_name"": ""Canal\u002B S\u00E9ries"",
            ""provider_id"": 345
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/A4RUf7OEPnQ3mhaFIZkJkJMrYW2.jpg"",
            ""provider_name"": ""VRT nu"",
            ""provider_id"": 312
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/mPDlxHokGsEc84OOhp9qjeynq2U.jpg"",
            ""provider_name"": ""Looke"",
            ""provider_id"": 47
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/sB5vHrmYmliwUvBwZe8HpXo9r8m.jpg"",
            ""provider_name"": ""Crave Starz"",
            ""provider_id"": 305
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/jPXksae158ukMLFhhlNvzsvaEyt.jpg"",
            ""provider_name"": ""fuboTV"",
            ""provider_id"": 257
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/zES0qHTB5ZU1Cb3NYwZ56mgZ3Bc.jpg"",
            ""provider_name"": ""GYAO"",
            ""provider_id"": 86
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/8ARqfv7c3eD48NxHfjdNdoop1b0.jpg"",
            ""provider_name"": ""Ziggo TV"",
            ""provider_id"": 297
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/kplaFNfZXsdyqsz4TAK8xaKU9Qa.jpg"",
            ""provider_name"": ""VOD Poland"",
            ""provider_id"": 245
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/o9ExgOSLF3OTwR6T3DJOuwOKJgq.jpg"",
            ""provider_name"": ""Ivi"",
            ""provider_id"": 113
        },
        {
            ""display_priority"": 5,
            ""logo_path"": ""/iaMw6nOyxUzXSacrLQ0Au6CfZkc.jpg"",
            ""provider_name"": ""Classix"",
            ""provider_id"": 445
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/tRbUL8V91FdvIUuTYpdHFscyHVM.jpg"",
            ""provider_name"": ""Foxtel Now"",
            ""provider_id"": 134
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/pCIkSBek0aZfPQzOn9gfazuYaLV.jpg"",
            ""provider_name"": ""C More"",
            ""provider_id"": 77
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/jH5dm7aqU9DxS4yPfprz6e3jmHU.jpg"",
            ""provider_name"": ""Yle Areena"",
            ""provider_id"": 323
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/zxrVdFjIjLqkfnwyghnfywTn3Lh.jpg"",
            ""provider_name"": ""Hulu"",
            ""provider_id"": 15
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/a4ciTQc27FsgdUp7PCrToHPygcw.jpg"",
            ""provider_name"": ""Naver Store"",
            ""provider_id"": 96
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/7Jn4Vx4tbSnMQwQXuJFPwV0P5n1.jpg"",
            ""provider_name"": ""Videoland"",
            ""provider_id"": 72
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/3MflXNopMv3EFKbVgJGoEkJEnnF.jpg"",
            ""provider_name"": ""Rooster Teeth"",
            ""provider_id"": 485
        },
        {
            ""display_priority"": 6,
            ""logo_path"": ""/hR9vWd8hWEVQKD6eOnBneKRFEW3.jpg"",
            ""provider_name"": ""Star Plus"",
            ""provider_id"": 619
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/sHP8XLo4Ac4WMbziRyAdRQdb76q.jpg"",
            ""provider_name"": ""Sky"",
            ""provider_id"": 210
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/1TsJJCXAScFxVsCesCdYnAex30x.jpg"",
            ""provider_name"": ""Sky Ticket"",
            ""provider_id"": 30
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/l5Wxbsgral716BOtZsGyPVNn8GC.jpg"",
            ""provider_name"": ""Horizon"",
            ""provider_id"": 250
        },
        {
            ""display_priority"": 7,
            ""logo_path"": ""/xbhHHa1YgtpwhC8lb1NQ3ACVcLd.jpg"",
            ""provider_name"": ""Paramount Plus"",
            ""provider_id"": 531
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/rDd7IEBnJB0gPagFvagP1kK3pDu.jpg"",
            ""provider_name"": ""Stan"",
            ""provider_id"": 21
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/j2OLGxyy0gKbPVI0DYFI2hJxP6y.jpg"",
            ""provider_name"": ""Netflix Kids"",
            ""provider_id"": 175
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/fBHHXKC34ffxAsQvDe0ZJbvmTEQ.jpg"",
            ""provider_name"": ""Sky Go"",
            ""provider_id"": 29
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/2pCbao1J9s0DMak2KKnEzmzHni8.jpg"",
            ""provider_name"": ""Sky Store"",
            ""provider_id"": 130
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/5GEbAhFW2S5T8zVc1MNvz00pIzM.jpg"",
            ""provider_name"": ""Rakuten TV"",
            ""provider_id"": 35
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/jmyYN1124dDIzqMysLekixy3AzF.jpg"",
            ""provider_name"": ""Hollystar"",
            ""provider_id"": 164
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/cZkT6PrmJs5mfVscQf2PNF7xrF.jpg"",
            ""provider_name"": ""Ruutu"",
            ""provider_id"": 338
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/qOLzLcOnngiTBeYXa8zquWRDtsB.jpg"",
            ""provider_name"": ""SFR Play"",
            ""provider_id"": 193
        },
        {
            ""display_priority"": 8,
            ""logo_path"": ""/Ajqyt5aNxNGjmF9uOfxArGrdf3X.jpg"",
            ""provider_name"": ""HBO Max"",
            ""provider_id"": 384
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/8Gt1iClBlzTeQs8WQm8UrCoIxnQ.jpg"",
            ""provider_name"": ""Crunchyroll"",
            ""provider_id"": 283
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/52MBOsXXqm0wuLHxKR1FHymef66.jpg"",
            ""provider_name"": ""WAKANIM"",
            ""provider_id"": 354
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/ppycrWdkR3pefMYYK79e481PULm.jpg"",
            ""provider_name"": ""Sixplay"",
            ""provider_id"": 147
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/cPWCQBEscHpJIgmZLCAKDtYuCZe.jpg"",
            ""provider_name"": ""MyTF1vod"",
            ""provider_id"": 145
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/rsXaDmBzlHgYrtv1o2NsRFctM5t.jpg"",
            ""provider_name"": ""Now TV"",
            ""provider_id"": 39
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/zY5SmHyAy1CoA3AfQpf58QnShnw.jpg"",
            ""provider_name"": ""BBC iPlayer"",
            ""provider_id"": 38
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/cDzkhgvozSr4GW2aRdV22uDuFpw.jpg"",
            ""provider_name"": ""Movistar Play"",
            ""provider_id"": 339
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/jNdDSUCyzk2wOwct9vXAaoX4Ypx.jpg"",
            ""provider_name"": ""DocPlay"",
            ""provider_id"": 357
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/jgD3gxzW39UhJ7wZsxst75bN8Ck.jpg"",
            ""provider_name"": ""Go3"",
            ""provider_id"": 373
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/8VCV78prwd9QzZnEm0ReO6bERDa.jpg"",
            ""provider_name"": ""Peacock"",
            ""provider_id"": 386
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/m6p38R4AlEo1ub7QnZtirXDIUF5.jpg"",
            ""provider_name"": ""CONtv"",
            ""provider_id"": 428
        },
        {
            ""display_priority"": 9,
            ""logo_path"": ""/59azlQKUgFdYq6QI5QEAxIeecyL.jpg"",
            ""provider_name"": ""Cultpix"",
            ""provider_id"": 692
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/5P99DkK1jVs95KcE8bYG9MBtGQ.jpg"",
            ""provider_name"": ""Acorn TV"",
            ""provider_id"": 87
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/mT9kIe6JVz72ikWJ58x0q8ckUW3.jpg"",
            ""provider_name"": ""Chili"",
            ""provider_id"": 40
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/lJT7r1nprk1Z8t1ywiIa8h9d3rc.jpg"",
            ""provider_name"": ""Claro video"",
            ""provider_id"": 167
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/oBoWstXQFHAlPApyxIQ31CIbNQk.jpg"",
            ""provider_name"": ""Globoplay"",
            ""provider_id"": 307
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/gZTgebCNny3MHvUxFde7gueVgT1.jpg"",
            ""provider_name"": ""Movistar Plus"",
            ""provider_id"": 149
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/ddWcbe8fYAfcQMjighzWGLjjyip.jpg"",
            ""provider_name"": ""Orange VOD"",
            ""provider_id"": 61
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/okiQZMXnqwv0aD3QDYmu5DBNLce.jpg"",
            ""provider_name"": ""ShowMax"",
            ""provider_id"": 55
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/j9iLCZMMgXP3jaYPkxCicx5Zmx3.jpg"",
            ""provider_name"": ""Mediaset Play"",
            ""provider_id"": 359
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/yBrCoCGMIiHPHuoyh1mg82Pwlhx.jpg"",
            ""provider_name"": ""TV 2"",
            ""provider_id"": 383
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/xTHltMrZPAJFLQ6qyCBjAnXSmZt.jpg"",
            ""provider_name"": ""Peacock Premium"",
            ""provider_id"": 387
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/zEG5OsS8ZJHJ6RTuAtLUyCSb6De.jpg"",
            ""provider_name"": ""Sooner"",
            ""provider_id"": 389
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/kV8XFGI5OLJKl72dI8DtnKplfFr.jpg"",
            ""provider_name"": ""DIRECTV GO"",
            ""provider_id"": 467
        },
        {
            ""display_priority"": 10,
            ""logo_path"": ""/zEuYa2328KQlbpOr4W0tVNpCGtZ.jpg"",
            ""provider_name"": ""iWantTFC"",
            ""provider_id"": 511
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/6HtR4lwikdriuJi86cZa3nXjB3d.jpg"",
            ""provider_name"": ""Quickflix Store"",
            ""provider_id"": 24
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/6uhKBfmtzFqOcLousHwZuzcrScK.jpg"",
            ""provider_name"": ""Apple TV Plus"",
            ""provider_id"": 350
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/5NyLm42TmCqCMOZFvH4fcoSNKEW.jpg"",
            ""provider_name"": ""Amazon Video"",
            ""provider_id"": 10
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/366UvWIQMqvKI6SyinCmvQx2B2j.jpg"",
            ""provider_name"": ""iciTouTV"",
            ""provider_id"": 146
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/kTpJjBn08IBluCPpFQekU9qdwRt.jpg"",
            ""provider_name"": ""Joyn"",
            ""provider_id"": 304
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/3L9ReVOwpiWgKyBw9ApkrDUWepy.jpg"",
            ""provider_name"": ""France TV"",
            ""provider_id"": 236
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/xVsZYrrmmqFJh3MkH98aFjMHnSf.jpg"",
            ""provider_name"": ""Filmin Latino"",
            ""provider_id"": 66
        },
        {
            ""display_priority"": 11,
            ""logo_path"": ""/4FqTBYsUSZgS9z9UGKgxSDBbtc8.jpg"",
            ""provider_name"": ""FilmBox\u002B"",
            ""provider_id"": 701
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/vjsvYNPgq6BpUoubXR1wNkokoBb.jpg"",
            ""provider_name"": ""Yelo Play"",
            ""provider_id"": 313
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/8T2jS3TdKCAsCrH0Kvl2NCwQ0ym.jpg"",
            ""provider_name"": ""Arte"",
            ""provider_id"": 234
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/9dielJNGTSKO7Lp6NKAuNOLw2jP.jpg"",
            ""provider_name"": ""Atres Player"",
            ""provider_id"": 62
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/odTur9CmVtzsRUAZ9910tPM4XwL.jpg"",
            ""provider_name"": ""Sony Liv"",
            ""provider_id"": 237
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/58aUMVWJRolhWpi4aJCkGHwfKdg.jpg"",
            ""provider_name"": ""VIX "",
            ""provider_id"": 457
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/uXc2fJqhtXfuNq6ha8tTLL9VnXj.jpg"",
            ""provider_name"": ""Player"",
            ""provider_id"": 505
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/zLM7f1w2L8TU2Fspzns72m6h3yY.jpg"",
            ""provider_name"": ""Wink"",
            ""provider_id"": 501
        },
        {
            ""display_priority"": 12,
            ""logo_path"": ""/r4q36I2Xts1SqhLWd6XsnbSAQJ4.jpg"",
            ""provider_name"": ""IROKOTV"",
            ""provider_id"": 704
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/oIkQkEkwfmcG7IGpRR1NB8frZZM.jpg"",
            ""provider_name"": ""YouTube"",
            ""provider_id"": 192
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/3hI22hp7YDZXyrmXVqDGnVivNTI.jpg"",
            ""provider_name"": ""RTL\u002B"",
            ""provider_id"": 298
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/ftxHS1anAWTYgtDtIDv8VLXoepH.jpg"",
            ""provider_name"": ""Timvision"",
            ""provider_id"": 109
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/67Ee4E6qOkQGHeUTArdJ1qRxzR2.jpg"",
            ""provider_name"": ""Curiosity Stream"",
            ""provider_id"": 190
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/v8vA6WnPVTOE1o39waNFVmAqEJj.jpg"",
            ""provider_name"": ""Disney Plus"",
            ""provider_id"": 390
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/eCRNttY7Zd75L5syA52AF8rCEuq.jpg"",
            ""provider_name"": ""TVNZ"",
            ""provider_id"": 395
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/oSJqnUUeoHfUj86Wsu2oq6VXLXE.jpg"",
            ""provider_name"": ""RTPplay"",
            ""provider_id"": 452
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/m3NWxxR23l1w1e156fyTuw931gx.jpg"",
            ""provider_name"": ""aha"",
            ""provider_id"": 532
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/ApALy1g1c9piZkivc9yrb30BGfn.jpg"",
            ""provider_name"": ""Nova Play"",
            ""provider_id"": 580
        },
        {
            ""display_priority"": 13,
            ""logo_path"": ""/uurfHKuprPDeKfIs7FYd5lQzw0L.jpg"",
            ""provider_name"": ""Shahid VIP"",
            ""provider_id"": 1715
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/7khYVzFljIj9FpXIP7AEvxC8Nk6.jpg"",
            ""provider_name"": ""Infinity"",
            ""provider_id"": 110
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/zyX0rRd986t2iKXUCvEsW7or4KN.jpg"",
            ""provider_name"": ""Kocowa"",
            ""provider_id"": 464
        },
        {
            ""display_priority"": 14,
            ""logo_path"": ""/wKAPdeGjoejE3pPZp3RdElIbfl7.jpg"",
            ""provider_name"": ""Play Suisse"",
            ""provider_id"": 691
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/2PTFxgrswnEuK0szl87iSd8Yszz.jpg"",
            ""provider_name"": ""maxdome Store"",
            ""provider_id"": 20
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/s1QWuiBbZhLGSFzYOglPTVye7td.jpg"",
            ""provider_name"": ""Rai Play"",
            ""provider_id"": 222
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/o252SN51PdMx8UvyUkX00MAtooX.jpg"",
            ""provider_name"": ""Funimation Now"",
            ""provider_id"": 269
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/eniTWVEhUi5h4lqwnOD2fkvTbAQ.jpg"",
            ""provider_name"": ""9Now"",
            ""provider_id"": 378
        },
        {
            ""display_priority"": 15,
            ""logo_path"": ""/qw1BwnbWKs7AXLVR05eRpi3YdD9.jpg"",
            ""provider_name"": ""RTBF"",
            ""provider_id"": 461
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/goKrzBxDNYxKgeeT2yoHtLXuIol.jpg"",
            ""provider_name"": ""Videobuster"",
            ""provider_id"": 133
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/dqlwg963xlz7jLN5Akdg6gbJ5To.jpg"",
            ""provider_name"": ""Watchbox"",
            ""provider_id"": 171
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/rll0yTCjrSY6hcJqIyMatv9B2iR.jpg"",
            ""provider_name"": ""NetMovies"",
            ""provider_id"": 19
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/x36C6aseF5l4uX99Kpse9dbPwBo.jpg"",
            ""provider_name"": ""Starz Play Amazon Channel"",
            ""provider_id"": 194
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/45eTLxznKGY9xq50NBWjN4adVng.jpg"",
            ""provider_name"": ""Catchplay"",
            ""provider_id"": 159
        },
        {
            ""display_priority"": 16,
            ""logo_path"": ""/9xKAZFyhkZVewWxJJhR41AJO0D3.jpg"",
            ""provider_name"": ""genflix"",
            ""provider_id"": 468
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/kJ9GcmYk5zJ9nJtVX8XjDo9geIM.jpg"",
            ""provider_name"": ""All 4"",
            ""provider_id"": 103
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/mgD0T960hnYU4gBxbPPBrcDfgWg.jpg"",
            ""provider_name"": ""WOW Presents Plus"",
            ""provider_id"": 546
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/bZNXgd8fwVTD68aAGlElkpAtu7b.jpg"",
            ""provider_name"": ""IPLA"",
            ""provider_id"": 549
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/krABGbxTRmPtUA10fkwhwUdCd4I.jpg"",
            ""provider_name"": ""tvzavr"",
            ""provider_id"": 556
        },
        {
            ""display_priority"": 17,
            ""logo_path"": ""/3E0RkIEQrrGYazs63NMsn3XONT6.jpg"",
            ""provider_name"": ""Paramount\u002B Amazon Channel"",
            ""provider_id"": 582
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/dSAEkpy0IhZpTLixrMq9z24oEPC.jpg"",
            ""provider_name"": ""7plus"",
            ""provider_id"": 246
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/knpqBvBQjyHnFrYPJ9bbtUCv6uo.jpg"",
            ""provider_name"": ""Canal VOD"",
            ""provider_id"": 58
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/gekkP93StjYdiMAInViVmrnldNY.jpg"",
            ""provider_name"": ""Magellan TV"",
            ""provider_id"": 551
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/Aduyz3yAGMXTmd2N6NiIOYCmWF3.jpg"",
            ""provider_name"": ""More TV"",
            ""provider_id"": 557
        },
        {
            ""display_priority"": 18,
            ""logo_path"": ""/tfI7CcurXCS2CZnLLHrBWS8CGHk.jpg"",
            ""provider_name"": ""EPIX Amazon Channel"",
            ""provider_id"": 583
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/vFjk9B5bZ1ranNLnjE6Z4RY3VxM.jpg"",
            ""provider_name"": ""ABC iview"",
            ""provider_id"": 135
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/ulTa4e9ysKwMwNpg7EfhYnvAj8q.jpg"",
            ""provider_name"": ""Bbox VOD"",
            ""provider_id"": 59
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/wrg4g92EvgzMOcnh1pWbUbnPdGA.jpg"",
            ""provider_name"": ""ITV Hub"",
            ""provider_id"": 41
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/6IdiH2yMRYCtB7XoIQ36wZig9gZ.jpg"",
            ""provider_name"": ""Vidio"",
            ""provider_id"": 489
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/xLu1rkZNOKuNnRNr70wySosfTBf.jpg"",
            ""provider_name"": ""BroadwayHD"",
            ""provider_id"": 554
        },
        {
            ""display_priority"": 19,
            ""logo_path"": ""/a2OcajC4bM5ItniQdjyOV7tgthW.jpg"",
            ""provider_name"": ""Discovery\u002B Amazon Channel"",
            ""provider_id"": 584
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/5kcixh15fZo0yUwQB2ug2ZfvCVc.jpg"",
            ""provider_name"": ""SBS On Demand"",
            ""provider_id"": 132
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/73igBrpTdAhEGwuYxhmnhTK5Srs.jpg"",
            ""provider_name"": ""NPO Start"",
            ""provider_id"": 360
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/zJKmYhZ5jn8nfQ36Dtk6MgQnoy6.jpg"",
            ""provider_name"": ""ThreeNow"",
            ""provider_id"": 440
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/y1PDXoEMqReA1uX1aF8rnVgSYBS.jpg"",
            ""provider_name"": ""NRK TV"",
            ""provider_id"": 442
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/9nyK6XeCSe1fmK9B9H2xHgOYDlj.jpg"",
            ""provider_name"": ""CINE"",
            ""provider_id"": 491
        },
        {
            ""display_priority"": 20,
            ""logo_path"": ""/eDFIGvn1PImm9kmZ83ugaqdWapy.jpg"",
            ""provider_name"": ""MAX Stream"",
            ""provider_id"": 483
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/lDaVaiNwOU8JHCcZ6fANzugVvtT.jpg"",
            ""provider_name"": ""Das Erste Mediathek"",
            ""provider_id"": 219
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/xiqTOBxOnlMy1nvppZcFhCDsP0f.jpg"",
            ""provider_name"": ""Alt Balaji"",
            ""provider_id"": 319
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/zoL69abPHiVC1Qzd4kM6hwLSo0j.jpg"",
            ""provider_name"": ""Showtime Amazon Channel"",
            ""provider_id"": 203
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/3QsJbibv5dFW2IYuXbTjxDmGGRZ.jpg"",
            ""provider_name"": ""Blockbuster"",
            ""provider_id"": 423
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/vqybB1exnaQ3UOlKaw4t6OgzFIu.jpg"",
            ""provider_name"": ""Filmstriben"",
            ""provider_id"": 443
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/u2H29LCxRzjZVUoZUQAHKm5P8Zc.jpg"",
            ""provider_name"": ""Dekkoo"",
            ""provider_id"": 444
        },
        {
            ""display_priority"": 21,
            ""logo_path"": ""/47iDHK3CykgXuZ20FN6QRAEcFBY.jpg"",
            ""provider_name"": ""Mitele "",
            ""provider_id"": 456
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/n0A2DUp7BPrz5mBoTN9cYV8oGhG.jpg"",
            ""provider_name"": ""tenplay"",
            ""provider_id"": 82
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/bmU37kpSMbcTgwwUrbxByk7x8h3.jpg"",
            ""provider_name"": ""HBO Go"",
            ""provider_id"": 31
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/rpwa6Tjghh1DF4iNfP5g4Rn6MGQ.jpg"",
            ""provider_name"": ""VVVVID"",
            ""provider_id"": 414
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/dNcz2AZHPEgt4BIKJe56r4visuK.jpg"",
            ""provider_name"": ""SF Anytime"",
            ""provider_id"": 426
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/4QEQsvCBnORNIg9EDnrRSiEw61D.jpg"",
            ""provider_name"": ""Hungama Play"",
            ""provider_id"": 437
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/9edKQczyuMmQM1yS520hgmJbcaC.jpg"",
            ""provider_name"": ""AMC\u002B Amazon Channel"",
            ""provider_id"": 528
        },
        {
            ""display_priority"": 22,
            ""logo_path"": ""/dUGPd8eg651seqculYtaM3AE9O9.jpg"",
            ""provider_name"": ""Premier"",
            ""provider_id"": 570
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/y0kyIFElN5sJAsmW8Txj69wzrD2.jpg"",
            ""provider_name"": ""Sky X"",
            ""provider_id"": 321
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/3LQzaSBH1kjQB9oKc4n72dKj8oY.jpg"",
            ""provider_name"": ""HBO Now"",
            ""provider_id"": 27
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/8jzbtiXz0eZ6aPjxdmGW3ceqjon.jpg"",
            ""provider_name"": ""Hollywood Suite"",
            ""provider_id"": 182
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/5nECaP8nhtrzZfx7oG0yoFMfqiA.jpg"",
            ""provider_name"": ""SumoTV"",
            ""provider_id"": 431
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/mMb0rksAc7Cmom5pEYaLNDkbitE.jpg"",
            ""provider_name"": ""Kirjastokino"",
            ""provider_id"": 463
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/xKUlNQjy7dpfI8Nj8BjgSTdYnqH.jpg"",
            ""provider_name"": ""CONTAR"",
            ""provider_id"": 543
        },
        {
            ""display_priority"": 23,
            ""logo_path"": ""/3jJtMOIwtvcrCyeRMUvv4wsfhJk.jpg"",
            ""provider_name"": ""TvIgle"",
            ""provider_id"": 577
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/kIbbhgfOWTHNp0xpcFC5uJUAwHj.jpg"",
            ""provider_name"": ""Viu"",
            ""provider_id"": 158
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/d4vHcXY9rwnr763wQns2XJThclt.jpg"",
            ""provider_name"": ""Hoichoi"",
            ""provider_id"": 315
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/egNBibGpNroklenRFE0EiRYZqaf.jpg"",
            ""provider_name"": ""Blim"",
            ""provider_id"": 67
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/z0h7mBHwm5KfMB2MKeoQDD2ngEZ.jpg"",
            ""provider_name"": ""The Roku Channel"",
            ""provider_id"": 207
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/q03pok7xSxYJaENuYs547qa6upY.jpg"",
            ""provider_name"": ""EPIC ON"",
            ""provider_id"": 476
        },
        {
            ""display_priority"": 24,
            ""logo_path"": ""/jblaJCpe4cDnaFNZg90qGF1UkZF.jpg"",
            ""provider_name"": ""SVT"",
            ""provider_id"": 493
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/ubIndtI8qN9sE7iXdpYO41ktW4v.jpg"",
            ""provider_name"": ""Flimmit"",
            ""provider_id"": 142
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/aGIS8maihUm60A3moKYD9gfYHYT.jpg"",
            ""provider_name"": ""BritBox"",
            ""provider_id"": 151
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/qjtOUIUnk4kRpcZmaddjqDHM0dR.jpg"",
            ""provider_name"": ""Rakuten Viki"",
            ""provider_id"": 344
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/cvl65OJnz14LUlC3yGK1KHj8UYs.jpg"",
            ""provider_name"": ""Viaplay"",
            ""provider_id"": 76
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/dpm29atq9clfBL38NgGxsj2CCe3.jpg"",
            ""provider_name"": ""Hotstar Disney\u002B"",
            ""provider_id"": 377
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/m8wib2YFVWHaY0SvnExvXZFusz9.jpg"",
            ""provider_name"": ""NLZIET"",
            ""provider_id"": 472
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/cQQYtdaCg7vDo28JPru4v8Ypi8x.jpg"",
            ""provider_name"": ""NOW"",
            ""provider_id"": 484
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/676SkNeXbHTfrtTdvEGpxGtMlKk.jpg"",
            ""provider_name"": ""Viafree"",
            ""provider_id"": 494
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/1KAux0lBEHpLnQcvaf1Qf1uKcIP.jpg"",
            ""provider_name"": ""Kinopoisk"",
            ""provider_id"": 117
        },
        {
            ""display_priority"": 25,
            ""logo_path"": ""/qMf2zirM2w0sO0mdAIIoP5XnQn8.jpg"",
            ""provider_name"": ""Showtime Roku Premium Channel"",
            ""provider_id"": 632
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/gqdajHmtr6qtutL7kkmEgleGfV9.jpg"",
            ""provider_name"": ""Filmin"",
            ""provider_id"": 63
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/wPLt8WPk6wGfDb4FD5YlG5C9vi7.jpg"",
            ""provider_name"": ""UKTV Play"",
            ""provider_id"": 137
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/t6N57S17sdXRXmZDAkaGP0NHNG0.jpg"",
            ""provider_name"": ""Pluto TV"",
            ""provider_id"": 300
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/1bbExrGyEuUFAEWMBSN76bwacQ0.jpg"",
            ""provider_name"": ""Oldflix"",
            ""provider_id"": 499
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/f3RCRmZWiUzg2CjxUqWJ881WmcS.jpg"",
            ""provider_name"": ""rtve"",
            ""provider_id"": 541
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/8MXYXzZGoPAEQU13GWk1GVvKNUS.jpg"",
            ""provider_name"": ""iQIYI"",
            ""provider_id"": 581
        },
        {
            ""display_priority"": 26,
            ""logo_path"": ""/qlVSrZgfXlFw0Jj6hsYq2zi70JD.jpg"",
            ""provider_name"": ""Paramount\u002B Roku Premium Channel"",
            ""provider_id"": 633
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/gGTgmDv9fa8uSd2sEybDOzfYfHK.jpg"",
            ""provider_name"": ""Kividoo"",
            ""provider_id"": 89
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/fN1aQj1gpWXGW0gwcGlHJYQHUeS.jpg"",
            ""provider_name"": ""Anime Digital Networks"",
            ""provider_id"": 415
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/73ms51HSpkD0OOXwj2EeiZeSqSt.jpg"",
            ""provider_name"": ""History Play"",
            ""provider_id"": 478
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/qFScrsCdMy1dtUz5pJAH0LpIOEd.jpg"",
            ""provider_name"": ""Arte1 Play "",
            ""provider_id"": 490
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/vuAxCPW4tlZ7Dg9EshAdPoHZFBo.jpg"",
            ""provider_name"": ""Comhem Play"",
            ""provider_id"": 497
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/8ugSQ1g7E8fXFnKFT5G8XOMcmS0.jpg"",
            ""provider_name"": ""TVF Play"",
            ""provider_id"": 500
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/ihE8Z4jZcGsmQsGRj6q06oxD2Wd.jpg"",
            ""provider_name"": ""Elisa Viihde"",
            ""provider_id"": 540
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/5OAb2w7D9C2VHa0k5PaoAYeFYFE.jpg"",
            ""provider_name"": ""Starz Roku Premium Channel"",
            ""provider_id"": 634
        },
        {
            ""display_priority"": 27,
            ""logo_path"": ""/vMxtOESmrNWkM9Y9jAebETexPAc.jpg"",
            ""provider_name"": ""OSN"",
            ""provider_id"": 629
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/uULoezj2skPc6amfwru72UPjYXV.jpg"",
            ""provider_name"": ""MagentaTV"",
            ""provider_id"": 178
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/dUokaRky9vs1u2PFRzFDV4iIx6A.jpg"",
            ""provider_name"": ""TNTGo"",
            ""provider_id"": 512
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/qLR6qzB1IcANZUqMEkLf6Sh8Y8s.jpg"",
            ""provider_name"": ""Tata Play"",
            ""provider_id"": 502
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/ni2NgPmIqqJRXeiA8Zdj4UhBZnU.jpg"",
            ""provider_name"": ""AMC\u002B Roku Premium Channel"",
            ""provider_id"": 635
        },
        {
            ""display_priority"": 28,
            ""logo_path"": ""/uOTEObCZtolNGDA7A4Wrb47cxNn.jpg"",
            ""provider_name"": ""STARZPLAY"",
            ""provider_id"": 630
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/thucdaw2gnOE0g478AHVZw5UeYm.jpg"",
            ""provider_name"": ""Cineasterna"",
            ""provider_id"": 496
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/wYRiUqIgWcfUvO6OPcXuUNd4tc2.jpg"",
            ""provider_name"": ""Discovery Plus"",
            ""provider_id"": 510
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/5uUdbTzTj4N2Wso1FTt2rRoJ5Da.jpg"",
            ""provider_name"": ""SALTO"",
            ""provider_id"": 564
        },
        {
            ""display_priority"": 29,
            ""logo_path"": ""/xlonQMSmhtA2HHwK3JKF9ghx7M8.jpg"",
            ""provider_name"": ""AMC\u002B"",
            ""provider_id"": 526
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/7rwgEs15tFwyR9NPQ5vpzxTj19Q.jpg"",
            ""provider_name"": ""Disney Plus"",
            ""provider_id"": 337
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/iRv3wbUEPuwYYPSKwUxPaMPKGM4.jpg"",
            ""provider_name"": ""ManoramaMax"",
            ""provider_id"": 482
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/rIvQ4zuxvVirsNNVarYmOTunBD2.jpg"",
            ""provider_name"": ""HBO Max Free"",
            ""provider_id"": 616
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/zQZE8lzSXsXl1K3aPa7kYRp4I6r.jpg"",
            ""provider_name"": ""Epix Roku Premium Channel"",
            ""provider_id"": 636
        },
        {
            ""display_priority"": 30,
            ""logo_path"": ""/h9vCGR4GF42HjXNvGQoBcuiZAvG.jpg"",
            ""provider_name"": ""HRTi"",
            ""provider_id"": 631
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/zXDDsD9M5vO7lqoqlBQCOcZtKBS.jpg"",
            ""provider_name"": ""Telstra TV"",
            ""provider_id"": 429
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/n3BIqc0mojP85bJSKjsIwZUOVya.jpg"",
            ""provider_name"": ""Libreflix"",
            ""provider_id"": 544
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/xrHrIraInfRXnrz1zHhY1tXJowg.jpg"",
            ""provider_name"": ""RTL Play"",
            ""provider_id"": 572
        },
        {
            ""display_priority"": 31,
            ""logo_path"": ""/9CHdbyMXYgFk9oM7H4t1FlrULHs.jpg"",
            ""provider_name"": ""Otta"",
            ""provider_id"": 626
        },
        {
            ""display_priority"": 32,
            ""logo_path"": ""/6IPjvnYl6WWkIwN158qBFXCr2Ne.jpg"",
            ""provider_name"": ""YouTube Premium"",
            ""provider_id"": 188
        },
        {
            ""display_priority"": 32,
            ""logo_path"": ""/z4vfN7KoOn6zruoCDRITnDZTdAx.jpg"",
            ""provider_name"": ""OzFlix"",
            ""provider_id"": 434
        },
        {
            ""display_priority"": 32,
            ""logo_path"": ""/kHx8k4ixfSZdj45FAYP2P9r4FUO.jpg"",
            ""provider_name"": ""Pickbox NOW"",
            ""provider_id"": 637
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/dH4BZucVyb5lW97TEbZ7RTAugjg.jpg"",
            ""provider_name"": ""MX Player"",
            ""provider_id"": 515
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/qJxuBkjkXWYmuTKk7hxvbmqvrNc.jpg"",
            ""provider_name"": ""Cin\u00E9polis KLIC"",
            ""provider_id"": 558
        },
        {
            ""display_priority"": 33,
            ""logo_path"": ""/ujkJsUqk59ZdN6fCslGV3dZDSHr.jpg"",
            ""provider_name"": ""Voyo"",
            ""provider_id"": 627
        },
        {
            ""display_priority"": 34,
            ""logo_path"": ""/aJ0b9BLU1Cvv5hIz9fEhKKc1x1D.jpg"",
            ""provider_name"": ""Hoopla"",
            ""provider_id"": 212
        },
        {
            ""display_priority"": 34,
            ""logo_path"": ""/7RJrotCrvD0oUjG0udv9on6CDKX.jpg"",
            ""provider_name"": ""Hayu Amazon Channel"",
            ""provider_id"": 296
        },
        {
            ""display_priority"": 34,
            ""logo_path"": ""/f8vwR4T8SO6cUouVQaPDY71TSgB.jpg"",
            ""provider_name"": ""South Park"",
            ""provider_id"": 498
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/6Y6w3F5mYoRHCcNAG0ZD2AndLJ2.jpg"",
            ""provider_name"": ""The CW"",
            ""provider_id"": 83
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/bVClgB5bpaTRM3sVPlboaxkFD0U.jpg"",
            ""provider_name"": ""KPN"",
            ""provider_id"": 563
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/gKno1uvHwHyhQTKMflDvEqj5oGJ.jpg"",
            ""provider_name"": ""Strim"",
            ""provider_id"": 578
        },
        {
            ""display_priority"": 35,
            ""logo_path"": ""/fDZtWPwSiKjVbbuZOVtlZAiH0rE.jpg"",
            ""provider_name"": ""WeTV"",
            ""provider_id"": 623
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/7cXdK4ExORmhkJl9wX1q3Yqs8lV.jpg"",
            ""provider_name"": ""ZDF Herzkino Amazon Channel"",
            ""provider_id"": 286
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/fWqVPYArdFwBc6vYqoyQB6XUl85.jpg"",
            ""provider_name"": ""HBO"",
            ""provider_id"": 118
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/xM2A6jTb4895MIuqPa6W6ooEcJS.jpg"",
            ""provider_name"": ""My5"",
            ""provider_id"": 333
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/7UpZTaQFcdISOzDOBMx6RavcaR.jpg"",
            ""provider_name"": ""CW Seed"",
            ""provider_id"": 206
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/xTVM8uXT9QocigQ07LE7Irc65W2.jpg"",
            ""provider_name"": ""Telia Play"",
            ""provider_id"": 553
        },
        {
            ""display_priority"": 36,
            ""logo_path"": ""/dpqap8iY6bsSqQf4xrkAG2j43gS.jpg"",
            ""provider_name"": ""DRTV"",
            ""provider_id"": 620
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/wUe8sI0PyRNNaWTSIDUoRADytvR.jpg"",
            ""provider_name"": ""BBC Player Amazon Channel"",
            ""provider_id"": 285
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/21dEscfO8n1tL35k4DANixhffsR.jpg"",
            ""provider_name"": ""Vudu"",
            ""provider_id"": 7
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/dtU2zKZvtdKgSKjyKekp8t0Ryd1.jpg"",
            ""provider_name"": ""BritBox"",
            ""provider_id"": 380
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/mAehaBHcatpbaYgZ0G6Z1czkXax.jpg"",
            ""provider_name"": ""Discovery Plus"",
            ""provider_id"": 524
        },
        {
            ""display_priority"": 37,
            ""logo_path"": ""/iGDZ6zPbVcngc0BQEsZX13Z7I07.jpg"",
            ""provider_name"": ""KKTV"",
            ""provider_id"": 624
        },
        {
            ""display_priority"": 38,
            ""logo_path"": ""/nVly1ywNU2hMYLaieL6ixhEFTWh.jpg"",
            ""provider_name"": ""CBC Gem"",
            ""provider_id"": 314
        },
        {
            ""display_priority"": 38,
            ""logo_path"": ""/xzfVRl1CgJPYa9dOoyVI3TDSQo2.jpg"",
            ""provider_name"": ""VUDU Free"",
            ""provider_id"": 332
        },
        {
            ""display_priority"": 38,
            ""logo_path"": ""/wLZCjEAlCKjEkQQM75bITfqL7D0.jpg"",
            ""provider_name"": ""LINE TV"",
            ""provider_id"": 625
        },
        {
            ""display_priority"": 39,
            ""logo_path"": ""/hNO6rEpZ9l2LQEkjacrpeoocKbX.jpg"",
            ""provider_name"": ""CTV"",
            ""provider_id"": 326
        },
        {
            ""display_priority"": 39,
            ""logo_path"": ""/zVJhpmIEgdDVbDt5TB72sZu3qdO.jpg"",
            ""provider_name"": ""Starz"",
            ""provider_id"": 43
        },
        {
            ""display_priority"": 40,
            ""logo_path"": ""/rA4QQGYokC8KZECGqqTC3a3QnMb.jpg"",
            ""provider_name"": ""FXNow Canada"",
            ""provider_id"": 348
        },
        {
            ""display_priority"": 40,
            ""logo_path"": ""/75mU4aWHPnMxSl95VT5O4lCR64U.jpg"",
            ""provider_name"": ""STUDIOCANAL PRESENTS Apple TV Channel"",
            ""provider_id"": 642
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/9TteoCidgcwT28Zs9yeAX7HdkwO.jpg"",
            ""provider_name"": ""Animax Plus Amazon Channel"",
            ""provider_id"": 195
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/4kL33LoKd99YFIaSOoOPMQOSw1A.jpg"",
            ""provider_name"": ""Showtime"",
            ""provider_id"": 37
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/bxdNcDbk1ohVeOMmM3eusAAiTLw.jpg"",
            ""provider_name"": ""HBO Go"",
            ""provider_id"": 425
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/gzHzhgt6cVSn4yy6UnJvLGbOSwL.jpg"",
            ""provider_name"": ""KinoPop"",
            ""provider_id"": 573
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/v3PhM1pr6omrcffUoBBZkiVeApH.jpg"",
            ""provider_name"": ""STV Player"",
            ""provider_id"": 593
        },
        {
            ""display_priority"": 41,
            ""logo_path"": ""/5OtaT8STJ8ZMkKt994C5XxrEAaP.jpg"",
            ""provider_name"": ""UPC TV"",
            ""provider_id"": 622
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/gmXeSpaYVJcb49SAzYcVHgQKQWM.jpg"",
            ""provider_name"": ""Filmtastic Amazon Channel"",
            ""provider_id"": 334
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/2BPU00vSfCZ4XI2CnQCBv8rZk2f.jpg"",
            ""provider_name"": ""CBS"",
            ""provider_id"": 78
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/bbxgdl6B5T75wJE713BiTCIBXyS.jpg"",
            ""provider_name"": ""PBS"",
            ""provider_id"": 209
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/vrFpju3t7kplDbFsN5GLJpG0obj.jpg"",
            ""provider_name"": ""Lionsgate Play"",
            ""provider_id"": 561
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/xbdgLcQ6kRrcVe1uJAG9lzlkSbY.jpg"",
            ""provider_name"": ""Oi Play"",
            ""provider_id"": 574
        },
        {
            ""display_priority"": 42,
            ""logo_path"": ""/bWczrFt9FD6DchYLQMMj2ga2iRA.jpg"",
            ""provider_name"": ""Serially"",
            ""provider_id"": 696
        },
        {
            ""display_priority"": 43,
            ""logo_path"": ""/2tAjxjo1n3H7fsXqMsxWFMeFUWp.jpg"",
            ""provider_name"": ""Pantaflix"",
            ""provider_id"": 177
        },
        {
            ""display_priority"": 43,
            ""logo_path"": ""/bCQVIO5iEjfstObco3fuhFB7sbs.jpg"",
            ""provider_name"": ""OUTtv Amazon Channel"",
            ""provider_id"": 607
        },
        {
            ""display_priority"": 44,
            ""logo_path"": ""/twV9iQPYeaoBzwsfRFGMGoMIUg8.jpg"",
            ""provider_name"": ""FXNow"",
            ""provider_id"": 123
        },
        {
            ""display_priority"": 44,
            ""logo_path"": ""/tQL30UKe7OykrtkYQCmYEFrdIMC.jpg"",
            ""provider_name"": ""Love Nature Amazon Channel"",
            ""provider_id"": 608
        },
        {
            ""display_priority"": 45,
            ""logo_path"": ""/w2TDH9TRI7pltf5LjN3vXzs7QbN.jpg"",
            ""provider_name"": ""Tubi TV"",
            ""provider_id"": 73
        },
        {
            ""display_priority"": 45,
            ""logo_path"": ""/mpdAy9QFQHA4OpRzykGUXsXqltN.jpg"",
            ""provider_name"": ""wedotv"",
            ""provider_id"": 392
        },
        {
            ""display_priority"": 46,
            ""logo_path"": ""/wbCleYwRFpUtWcNi7BLP3E1f6VI.jpg"",
            ""provider_name"": ""Kanopy"",
            ""provider_id"": 191
        },
        {
            ""display_priority"": 46,
            ""logo_path"": ""/awgDmkHSfGEcoIVpeQKwaE2OgLM.jpg"",
            ""provider_name"": ""Global TV"",
            ""provider_id"": 449
        },
        {
            ""display_priority"": 46,
            ""logo_path"": ""/fUUgfrOfvvPKx9vhFBd6IMdkfLy.jpg"",
            ""provider_name"": ""MGM Amazon Channel"",
            ""provider_id"": 588
        },
        {
            ""display_priority"": 47,
            ""logo_path"": ""/gmU9aPV3XUFusVs4kK1rcICUKqL.jpg"",
            ""provider_name"": ""Comedy Central"",
            ""provider_id"": 243
        },
        {
            ""display_priority"": 47,
            ""logo_path"": ""/h1PNHFp50cceDZ8jXUMnuVVMIw2.jpg"",
            ""provider_name"": ""VI movies and tv"",
            ""provider_id"": 614
        },
        {
            ""display_priority"": 47,
            ""logo_path"": ""/yJflQpWbgaiqYEVsFrE18lIEbaG.jpg"",
            ""provider_name"": ""FlixOl\u00E9 Amazon Channel"",
            ""provider_id"": 684
        },
        {
            ""display_priority"": 48,
            ""logo_path"": ""/shq88b09gTBYC4hA7K7MUL8Q4zP.jpg"",
            ""provider_name"": ""Microsoft Store"",
            ""provider_id"": 68
        },
        {
            ""display_priority"": 48,
            ""logo_path"": ""/3gTVbIj15Amgz5Qqg5dPDpgMW9V.jpg"",
            ""provider_name"": ""Looke Amazon Channel"",
            ""provider_id"": 683
        },
        {
            ""display_priority"": 49,
            ""logo_path"": ""/2joD3S2goOB6lmepX35A8dmaqgM.jpg"",
            ""provider_name"": ""Joyn Plus"",
            ""provider_id"": 421
        },
        {
            ""display_priority"": 49,
            ""logo_path"": ""/l6boVLijqAZLYXlZpkzzeNC4mvg.jpg"",
            ""provider_name"": ""Pongalo Amazon Channel  "",
            ""provider_id"": 690
        },
        {
            ""display_priority"": 50,
            ""logo_path"": ""/gbyLHzl4eYP0oP9oJZ2oKbpkhND.jpg"",
            ""provider_name"": ""Redbox"",
            ""provider_id"": 279
        },
        {
            ""display_priority"": 50,
            ""logo_path"": ""/42Cj5KNEteBRpfWnGWQbTJpJDGV.jpg"",
            ""provider_name"": ""OCS Amazon Channel "",
            ""provider_id"": 685
        },
        {
            ""display_priority"": 51,
            ""logo_path"": ""/iB415dMHBjdTZOOZA06FA1sxDWN.jpg"",
            ""provider_name"": ""Moviedome Plus Amazon Channel"",
            ""provider_id"": 706
        },
        {
            ""display_priority"": 52,
            ""logo_path"": ""/AhaVozbDe3SPHXTKyd6Crdt720S.jpg"",
            ""provider_name"": ""Max Go"",
            ""provider_id"": 139
        },
        {
            ""display_priority"": 52,
            ""logo_path"": ""/xB6NQNF0vlRlRh4KNPFE1Vlqn1Q.jpg"",
            ""provider_name"": ""Aniverse Amazon Channel"",
            ""provider_id"": 707
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/l9BRdAgQ3MkooOalsuu3yFQv2XP.jpg"",
            ""provider_name"": ""ABC"",
            ""provider_id"": 148
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/6FWwq6rayak6g6rvzVVP1NnX9gf.jpg"",
            ""provider_name"": ""Club Illico"",
            ""provider_id"": 469
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/3xIBSZdL2pZCJR2saHwDPhKW2aZ.jpg"",
            ""provider_name"": ""Home of Horror"",
            ""provider_id"": 479
        },
        {
            ""display_priority"": 53,
            ""logo_path"": ""/pHJhFw6XXRCTnIKK06MT0m1Vll4.jpg"",
            ""provider_name"": ""Superfresh Amazon Channel"",
            ""provider_id"": 708
        },
        {
            ""display_priority"": 54,
            ""logo_path"": ""/xL9SUR63qrEjFZAhtsipskeAMR7.jpg"",
            ""provider_name"": ""DIRECTV"",
            ""provider_id"": 358
        },
        {
            ""display_priority"": 54,
            ""logo_path"": ""/u04LR9vGEhc8B1ml4HSj1RCbqTG.jpg"",
            ""provider_name"": ""Filmtastic"",
            ""provider_id"": 480
        },
        {
            ""display_priority"": 55,
            ""logo_path"": ""/7P2JHkfv4AmU2MgSPGaJ0z6nNLG.jpg"",
            ""provider_name"": ""Crackle"",
            ""provider_id"": 12
        },
        {
            ""display_priority"": 55,
            ""logo_path"": ""/dCO5ge3nDm4LdnWSPe6jHPciE7U.jpg"",
            ""provider_name"": ""tvo"",
            ""provider_id"": 488
        },
        {
            ""display_priority"": 56,
            ""logo_path"": ""/iPK2kpaKnGYvSdEcRerIbkqWVPh.jpg"",
            ""provider_name"": ""Knowledge Network"",
            ""provider_id"": 525
        },
        {
            ""display_priority"": 57,
            ""logo_path"": ""/dTKs9JkJl06hnbnqUXHAxUwZrUS.jpg"",
            ""provider_name"": ""AMC"",
            ""provider_id"": 80
        },
        {
            ""display_priority"": 57,
            ""logo_path"": ""/pGk6V35szQnJVq2OoJLnRpjifb3.jpg"",
            ""provider_name"": ""ILLICO"",
            ""provider_id"": 492
        },
        {
            ""display_priority"": 60,
            ""logo_path"": ""/wSAxtofaArEuTOsqBmghVuJx7eP.jpg"",
            ""provider_name"": ""NBC"",
            ""provider_id"": 79
        },
        {
            ""display_priority"": 60,
            ""logo_path"": ""/3ISpW4LBSKAaCyIZI3cxHiox8dI.jpg"",
            ""provider_name"": ""Noovo"",
            ""provider_id"": 516
        },
        {
            ""display_priority"": 60,
            ""logo_path"": ""/1h8etYGesCuldkQGoUDyDJr92EB.jpg"",
            ""provider_name"": ""Amazon Arthaus Channel"",
            ""provider_id"": 533
        },
        {
            ""display_priority"": 61,
            ""logo_path"": ""/c7nw5oRfx5iZfyX0QmtOK0pbVaJ.jpg"",
            ""provider_name"": ""Epix"",
            ""provider_id"": 34
        },
        {
            ""display_priority"": 62,
            ""logo_path"": ""/rgpmwMkXqFYch9cway9qWMw0uXu.jpg"",
            ""provider_name"": ""Freeform"",
            ""provider_id"": 211
        },
        {
            ""display_priority"": 62,
            ""logo_path"": ""/dKH9TB94EIbnaWnjO6vX0snaNVP.jpg"",
            ""provider_name"": ""ZDF"",
            ""provider_id"": 537
        },
        {
            ""display_priority"": 63,
            ""logo_path"": ""/m6pLJ0l6MQJiKg1yxEs1holRSiq.jpg"",
            ""provider_name"": ""History"",
            ""provider_id"": 155
        },
        {
            ""display_priority"": 63,
            ""logo_path"": ""/o6li3XZrBKXSqyNRS39UQEfPTCH.jpg"",
            ""provider_name"": ""Virgin TV Go"",
            ""provider_id"": 594
        },
        {
            ""display_priority"": 64,
            ""logo_path"": ""/f7iqKjWYdVoYVIvKP3nboULcrM2.jpg"",
            ""provider_name"": ""Syfy"",
            ""provider_id"": 215
        },
        {
            ""display_priority"": 64,
            ""logo_path"": ""/aNvHP7E7X4hEGW7aT5tyo1xfnFN.jpg"",
            ""provider_name"": ""CuriosityStream Amazon Channel"",
            ""provider_id"": 603
        },
        {
            ""display_priority"": 65,
            ""logo_path"": ""/ujE7L9z0Ceu1T74RcahVn1FMbbK.jpg"",
            ""provider_name"": ""A\u0026E"",
            ""provider_id"": 156
        },
        {
            ""display_priority"": 65,
            ""logo_path"": ""/q6hCkmhpK5cDUURb4i6yWXNfpZz.jpg"",
            ""provider_name"": ""filmfriend"",
            ""provider_id"": 542
        },
        {
            ""display_priority"": 65,
            ""logo_path"": ""/jdQu3zBkbZCKnVUZwm63jBxAATk.jpg"",
            ""provider_name"": ""DocuBay Amazon Channel"",
            ""provider_id"": 604
        },
        {
            ""display_priority"": 66,
            ""logo_path"": ""/3wJNOOCbvqi7fJAdgf1QpL7Wwe2.jpg"",
            ""provider_name"": ""Lifetime"",
            ""provider_id"": 157
        },
        {
            ""display_priority"": 67,
            ""logo_path"": ""/pheENW1BxlexXX1CKJ4GyWudyMA.jpg"",
            ""provider_name"": ""Shudder"",
            ""provider_id"": 99
        },
        {
            ""display_priority"": 69,
            ""logo_path"": ""/pr2dlBnA2lp2WX6bI58VGO4Rz9n.jpg"",
            ""provider_name"": ""ITV Amazon Channel"",
            ""provider_id"": 598
        },
        {
            ""display_priority"": 70,
            ""logo_path"": ""/pZ9TSk3wlRYwiwwRxTsQJ7t2but.jpg"",
            ""provider_name"": ""Sundance Now"",
            ""provider_id"": 143
        },
        {
            ""display_priority"": 71,
            ""logo_path"": ""/olvOut34aWUFf1YoOqiqtjidiTK.jpg"",
            ""provider_name"": ""Popcornflix"",
            ""provider_id"": 241
        },
        {
            ""display_priority"": 71,
            ""logo_path"": ""/itokSbqrAKttZajeuUQlwDOy3zS.jpg"",
            ""provider_name"": ""Pok\u00E9mon Amazon Channel"",
            ""provider_id"": 599
        },
        {
            ""display_priority"": 72,
            ""logo_path"": ""/6qlN4ra7T69JynYVbs9SXVUlkLk.jpg"",
            ""provider_name"": ""Shout! Factory Amazon Channel"",
            ""provider_id"": 600
        },
        {
            ""display_priority"": 74,
            ""logo_path"": ""/1K9ZUrerCHALa0dyZ1OxidsJ28u.jpg"",
            ""provider_name"": ""Eros Now Amazon Channel"",
            ""provider_id"": 595
        },
        {
            ""display_priority"": 75,
            ""logo_path"": ""/kgyeCy524FHBpL0oWWKnjGArt9h.jpg"",
            ""provider_name"": ""FilmBox Live Amazon Channel"",
            ""provider_id"": 602
        },
        {
            ""display_priority"": 76,
            ""logo_path"": ""/ihCOKHIsNdqzhftgiJld6YVwr1G.jpg"",
            ""provider_name"": ""MotorTrend Amazon Channel"",
            ""provider_id"": 601
        },
        {
            ""display_priority"": 77,
            ""logo_path"": ""/94IdHexespnJs96kmGiJlflfiwU.jpg"",
            ""provider_name"": ""Pantaya"",
            ""provider_id"": 247
        },
        {
            ""display_priority"": 77,
            ""logo_path"": ""/hTXE4J8fB7KDYkhY6KbwJIfHMtM.jpg"",
            ""provider_name"": ""W4free"",
            ""provider_id"": 615
        },
        {
            ""display_priority"": 78,
            ""logo_path"": ""/oRXiHzPl2HJMXXFR4eebsb8F5Oc.jpg"",
            ""provider_name"": ""Boomerang"",
            ""provider_id"": 248
        },
        {
            ""display_priority"": 78,
            ""logo_path"": ""/xDGeUEkg6Vkud7lhZOyhsi3EIYj.jpg"",
            ""provider_name"": ""Nick\u002B Amazon Channel"",
            ""provider_id"": 589
        },
        {
            ""display_priority"": 79,
            ""logo_path"": ""/5uTsmZnDQmIOjZPEv8TNTy7GRJB.jpg"",
            ""provider_name"": ""Urban Movie Channel"",
            ""provider_id"": 251
        },
        {
            ""display_priority"": 79,
            ""logo_path"": ""/jTvkFUHvHsUoOzZRZ8WpmkiZD1v.jpg"",
            ""provider_name"": ""Home of Horror Amazon Channel"",
            ""provider_id"": 686
        },
        {
            ""display_priority"": 80,
            ""logo_path"": ""/97AOkkJyYajWLHgKGpK5NZOQ22O.jpg"",
            ""provider_name"": ""Bloody Movies Amazon Channel"",
            ""provider_id"": 680
        },
        {
            ""display_priority"": 81,
            ""logo_path"": ""/cBCzPOX6ir5L8hCoJlfIWycxauh.jpg"",
            ""provider_name"": ""Dove Channel"",
            ""provider_id"": 254
        },
        {
            ""display_priority"": 81,
            ""logo_path"": ""/jqByg3hw9LsuKTxgpAQPbO9b1ZQ.jpg"",
            ""provider_name"": ""Super Channel Amazon Channel"",
            ""provider_id"": 605
        },
        {
            ""display_priority"": 81,
            ""logo_path"": ""/4Q3nrYFVeOaSB76JlH2Ep08AQxA.jpg"",
            ""provider_name"": ""Film Total Amazon Channel"",
            ""provider_id"": 681
        },
        {
            ""display_priority"": 82,
            ""logo_path"": ""/8qNJcPBHZ4qewHrDJ7C7s2DBQ3V.jpg"",
            ""provider_name"": ""Yupp TV"",
            ""provider_id"": 255
        },
        {
            ""display_priority"": 82,
            ""logo_path"": ""/h8sud4kBfHnTni7G7pTnOGcArco.jpg"",
            ""provider_name"": ""StackTV Amazon Channel"",
            ""provider_id"": 606
        },
        {
            ""display_priority"": 82,
            ""logo_path"": ""/7BVWcGgkBBVpqg7RdoxlhIgb3s4.jpg"",
            ""provider_name"": ""Turk On Video Amazon Channel"",
            ""provider_id"": 693
        },
        {
            ""display_priority"": 83,
            ""logo_path"": ""/4XYI2rzRm34skcvamytegQx7Dmu.jpg"",
            ""provider_name"": ""Eros Now"",
            ""provider_id"": 218
        },
        {
            ""display_priority"": 84,
            ""logo_path"": ""/foT1TtL67MgEOWR6Cib8dKyCvJI.jpg"",
            ""provider_name"": ""Magnolia Selects"",
            ""provider_id"": 259
        },
        {
            ""display_priority"": 85,
            ""logo_path"": ""/rDYZ9v3Y09fuFyan51tHKE1mFId.jpg"",
            ""provider_name"": ""WWE Network"",
            ""provider_id"": 260
        },
        {
            ""display_priority"": 85,
            ""logo_path"": ""/rZALpU2NvloNDBuUWX7BVBPFLDG.jpg"",
            ""provider_name"": ""Smithsonian Channel Amazon Channel"",
            ""provider_id"": 609
        },
        {
            ""display_priority"": 86,
            ""logo_path"": ""/tTLB4xkjrKXxdtiWTeeS6qQB1v9.jpg"",
            ""provider_name"": ""MyOutdoorTV"",
            ""provider_id"": 264
        },
        {
            ""display_priority"": 86,
            ""logo_path"": ""/xGUwoyO5LlHKEQGGYSMoLxo7c6D.jpg"",
            ""provider_name"": ""BBC Earth Amazon Channel"",
            ""provider_id"": 610
        },
        {
            ""display_priority"": 87,
            ""logo_path"": ""/oMwjMgYiT2jcR7ELqCH3TPzpgTX.jpg"",
            ""provider_name"": ""Nickhits Amazon Channel"",
            ""provider_id"": 261
        },
        {
            ""display_priority"": 87,
            ""logo_path"": ""/4dFbTZSJqSi89LkNrFoP02jPMt7.jpg"",
            ""provider_name"": ""The Great Courses Signature Collection Amazon Channel"",
            ""provider_id"": 611
        },
        {
            ""display_priority"": 88,
            ""logo_path"": ""/yxBUPUBFzHE72uFXvFr1l0fnMJA.jpg"",
            ""provider_name"": ""Noggin Amazon Channel"",
            ""provider_id"": 262
        },
        {
            ""display_priority"": 88,
            ""logo_path"": ""/e07gcWq5OWhJ8MxZncJrDuoJAp2.jpg"",
            ""provider_name"": ""UMC Amazon Channel"",
            ""provider_id"": 612
        },
        {
            ""display_priority"": 89,
            ""logo_path"": ""/gYC72bT1nz4NvOFe7pPuCsNdKch.jpg"",
            ""provider_name"": ""Hopster TV"",
            ""provider_id"": 267
        },
        {
            ""display_priority"": 90,
            ""logo_path"": ""/w4GTJ1EDrgJku49XKSnRag9kKCT.jpg"",
            ""provider_name"": ""Laugh Out Loud"",
            ""provider_id"": 275
        },
        {
            ""display_priority"": 91,
            ""logo_path"": ""/UAZ2lJBWszijybQD4frqw2jxRO.jpg"",
            ""provider_name"": ""Smithsonian Channel"",
            ""provider_id"": 276
        },
        {
            ""display_priority"": 92,
            ""logo_path"": ""/orsVBNvPWxJNOVSEHMOk2h8R1wA.jpg"",
            ""provider_name"": ""Pure Flix"",
            ""provider_id"": 278
        },
        {
            ""display_priority"": 93,
            ""logo_path"": ""/llEJ6av9kAniTQUR9hF9mhVbzlB.jpg"",
            ""provider_name"": ""Hallmark Movies"",
            ""provider_id"": 281
        },
        {
            ""display_priority"": 95,
            ""logo_path"": ""/tU4tamrqRjbg3Lbmkryp3EiLPQJ.jpg"",
            ""provider_name"": ""PBS Kids Amazon Channel"",
            ""provider_id"": 293
        },
        {
            ""display_priority"": 96,
            ""logo_path"": ""/1zfRJQc14uEzZThdwNvxtxeWJw6.jpg"",
            ""provider_name"": ""Boomerang Amazon Channel"",
            ""provider_id"": 288
        },
        {
            ""display_priority"": 97,
            ""logo_path"": ""/kEnyHRflZPNWEOIXroZPhfdGi46.jpg"",
            ""provider_name"": ""Cinemax Amazon Channel"",
            ""provider_id"": 289
        },
        {
            ""display_priority"": 97,
            ""logo_path"": ""/fTc12wQdF3tOgKE16Eai4vjOFPg.jpg"",
            ""provider_name"": ""Hollywood Suite Amazon Channel"",
            ""provider_id"": 705
        },
        {
            ""display_priority"": 98,
            ""logo_path"": ""/fvSJ17mOt3MxKfnSgQVrtXTuepq.jpg"",
            ""provider_name"": ""Pantaya Amazon Channel"",
            ""provider_id"": 292
        },
        {
            ""display_priority"": 99,
            ""logo_path"": ""/6L2wLiZz3IG2X4MRbdRlGLgftMK.jpg"",
            ""provider_name"": ""Hallmark Movies Now Amazon Channel"",
            ""provider_id"": 290
        },
        {
            ""display_priority"": 100,
            ""logo_path"": ""/mMALQK52OFGoYUKOSCZILZkfGWs.jpg"",
            ""provider_name"": ""PBS Masterpiece Amazon Channel"",
            ""provider_id"": 294
        },
        {
            ""display_priority"": 101,
            ""logo_path"": ""/mlH42JbZMrapSF6zc8iTYURcZlH.jpg"",
            ""provider_name"": ""Viewster Amazon Channel"",
            ""provider_id"": 295
        },
        {
            ""display_priority"": 102,
            ""logo_path"": ""/72tiOIjZQPqm7MGhqoqyjyTJzSv.jpg"",
            ""provider_name"": ""MZ Choice Amazon Channel"",
            ""provider_id"": 291
        },
        {
            ""display_priority"": 104,
            ""logo_path"": ""/aLQ8rZhO7vgPFy1tae5vxvb81Wl.jpg"",
            ""provider_name"": ""Sling TV"",
            ""provider_id"": 299
        },
        {
            ""display_priority"": 106,
            ""logo_path"": ""/9baY98ZKyDaNArp1H9fAWqiR3Zi.jpg"",
            ""provider_name"": ""HiDive"",
            ""provider_id"": 430
        },
        {
            ""display_priority"": 109,
            ""logo_path"": ""/ubWucXFn34TrVlJBaJFgPaC4tOP.jpg"",
            ""provider_name"": ""Topic"",
            ""provider_id"": 454
        },
        {
            ""display_priority"": 110,
            ""logo_path"": ""/ttCYMg3dbKYeGCgCxzsNvT3L4qF.jpg"",
            ""provider_name"": ""MTV"",
            ""provider_id"": 453
        },
        {
            ""display_priority"": 111,
            ""logo_path"": ""/9ONs8SMAXtkiyaEIKATTpbwckx8.jpg"",
            ""provider_name"": ""Retrocrush"",
            ""provider_id"": 446
        },
        {
            ""display_priority"": 114,
            ""logo_path"": ""/ju3T8MFGNIoPiYpwHFpNlrYNyG7.jpg"",
            ""provider_name"": ""Shout! Factory TV"",
            ""provider_id"": 439
        },
        {
            ""display_priority"": 115,
            ""logo_path"": ""/3tCqvc5hPm5nl8Hm8o2koDRZlPo.jpg"",
            ""provider_name"": ""Chai Flicks"",
            ""provider_id"": 438
        },
        {
            ""display_priority"": 117,
            ""logo_path"": ""/vuS4VlY50SJVHbCU3vGxQehcsAg.jpg"",
            ""provider_name"": ""Mhz Choice"",
            ""provider_id"": 427
        },
        {
            ""display_priority"": 119,
            ""logo_path"": ""/oYpUb0xkRfEE5iccELlumPGubt4.jpg"",
            ""provider_name"": ""Vice TV "",
            ""provider_id"": 458
        },
        {
            ""display_priority"": 120,
            ""logo_path"": ""/sc5pTTCFbx7GQyOst5SG4U7nkPH.jpg"",
            ""provider_name"": ""Shudder Amazon Channel"",
            ""provider_id"": 204
        },
        {
            ""display_priority"": 122,
            ""logo_path"": ""/8WWD7t5Irwq9kAH4rufQ4Pe1Dog.jpg"",
            ""provider_name"": ""AcornTV Amazon Channel"",
            ""provider_id"": 196
        },
        {
            ""display_priority"": 123,
            ""logo_path"": ""/xTfyFZqWv8c8sxlFooUzemi6WRM.jpg"",
            ""provider_name"": ""BritBox Amazon Channel"",
            ""provider_id"": 197
        },
        {
            ""display_priority"": 124,
            ""logo_path"": ""/8vBJZkwkrUDYMSfmw5R0ZENd7yw.jpg"",
            ""provider_name"": ""Fandor Amazon Channel"",
            ""provider_id"": 199
        },
        {
            ""display_priority"": 125,
            ""logo_path"": ""/naqM14qSfg2q0S2zDylM5zQQ3jn.jpg"",
            ""provider_name"": ""Screambox Amazon Channel"",
            ""provider_id"": 202
        },
        {
            ""display_priority"": 126,
            ""logo_path"": ""/xImSZRKRYzIMPr4COgJNsEHdd2T.jpg"",
            ""provider_name"": ""Sundance Now Amazon Channel"",
            ""provider_id"": 205
        },
        {
            ""display_priority"": 127,
            ""logo_path"": ""/A5vrIl7YqlmNrOHZikrtO41V0sY.jpg"",
            ""provider_name"": ""Cartoon Network"",
            ""provider_id"": 317
        },
        {
            ""display_priority"": 128,
            ""logo_path"": ""/sPlIWhBAcoyw2IWuQ2PDdToNXld.jpg"",
            ""provider_name"": ""Adult Swim"",
            ""provider_id"": 318
        },
        {
            ""display_priority"": 129,
            ""logo_path"": ""/ldU2RCgdvkcSEBWWbttCpVO450z.jpg"",
            ""provider_name"": ""USA Network"",
            ""provider_id"": 322
        },
        {
            ""display_priority"": 130,
            ""logo_path"": ""/rbCRT408gY44bZH0KdtmKzoituI.jpg"",
            ""provider_name"": ""Fox"",
            ""provider_id"": 328
        },
        {
            ""display_priority"": 131,
            ""logo_path"": ""/4U02VrbgLfUKJAUCHKzxWFtnPx4.jpg"",
            ""provider_name"": ""FlixFling"",
            ""provider_id"": 331
        },
        {
            ""display_priority"": 132,
            ""logo_path"": ""/obBJU4ak4XvAOUM5iVmSUxDvqC3.jpg"",
            ""provider_name"": ""Bet\u002B Amazon Channel"",
            ""provider_id"": 343
        },
        {
            ""display_priority"": 135,
            ""logo_path"": ""/x4AFz5koB2R8BRn8WNh6EqXUGHc.jpg"",
            ""provider_name"": ""Darkmatter TV"",
            ""provider_id"": 355
        },
        {
            ""display_priority"": 137,
            ""logo_path"": ""/cezAIHmsUVvgAahfCR7J0z30y1N.jpg"",
            ""provider_name"": ""Bravo TV"",
            ""provider_id"": 365
        },
        {
            ""display_priority"": 138,
            ""logo_path"": ""/gJnQ40Z6T7HyY6fbmmI6qKE0zmK.jpg"",
            ""provider_name"": ""TNT"",
            ""provider_id"": 363
        },
        {
            ""display_priority"": 139,
            ""logo_path"": ""/auXCWejtQmZL7DplgokLXYq73Ed.jpg"",
            ""provider_name"": ""Food Network"",
            ""provider_id"": 366
        },
        {
            ""display_priority"": 140,
            ""logo_path"": ""/ukSXbR5qFjO2qCHpc6ZhcGPSjTJ.jpg"",
            ""provider_name"": ""BBC America"",
            ""provider_id"": 397
        },
        {
            ""display_priority"": 141,
            ""logo_path"": ""/2NRn6OApVKfDTKLuHDRN8UadLRw.jpg"",
            ""provider_name"": ""IndieFlix"",
            ""provider_id"": 368
        },
        {
            ""display_priority"": 142,
            ""logo_path"": ""/gxCvG3STez0PrDqi05LSYyWjLPk.jpg"",
            ""provider_name"": ""AHCTV"",
            ""provider_id"": 398
        },
        {
            ""display_priority"": 143,
            ""logo_path"": ""/eZK2W0v3yA2Dq7cFzifK0v9FN1b.jpg"",
            ""provider_name"": ""TLC"",
            ""provider_id"": 412
        },
        {
            ""display_priority"": 144,
            ""logo_path"": ""/bwTpY8DTKUjoi6YfuiMenahGrTj.jpg"",
            ""provider_name"": ""HGTV"",
            ""provider_id"": 406
        },
        {
            ""display_priority"": 145,
            ""logo_path"": ""/odh8CexN7yXa7IX4aIYtsUc0vHY.jpg"",
            ""provider_name"": ""DIY Network"",
            ""provider_id"": 405
        },
        {
            ""display_priority"": 146,
            ""logo_path"": ""/gMV6YwrWO9YpLiUQ5dAxnxJiWWj.jpg"",
            ""provider_name"": ""Investigation Discovery"",
            ""provider_id"": 408
        },
        {
            ""display_priority"": 147,
            ""logo_path"": ""/3bRK8VOvIfWIhOLGGwNA67kphXC.jpg"",
            ""provider_name"": ""Science Channel"",
            ""provider_id"": 411
        },
        {
            ""display_priority"": 148,
            ""logo_path"": ""/xZMxO6tGdeMmKxIvT4QjPz59ujm.jpg"",
            ""provider_name"": ""Destination America"",
            ""provider_id"": 402
        },
        {
            ""display_priority"": 149,
            ""logo_path"": ""/fXcLPLz67yG0JzLWXIsNJrdwRzr.jpg"",
            ""provider_name"": ""Animal Planet"",
            ""provider_id"": 399
        },
        {
            ""display_priority"": 150,
            ""logo_path"": ""/3LGhdwqMB0iuEwidFusc0I38Omm.jpg"",
            ""provider_name"": ""Discovery Life"",
            ""provider_id"": 404
        },
        {
            ""display_priority"": 151,
            ""logo_path"": ""/dfz7hQm0icTUdXJrScZXPMeO963.jpg"",
            ""provider_name"": ""Discovery"",
            ""provider_id"": 403
        },
        {
            ""display_priority"": 152,
            ""logo_path"": ""/st6VcNMu18MKbiTFhaWnxU9rBat.jpg"",
            ""provider_name"": ""Motor Trend"",
            ""provider_id"": 410
        },
        {
            ""display_priority"": 153,
            ""logo_path"": ""/aTiukuAuttjE2OdGv1eUhk3xsi0.jpg"",
            ""provider_name"": ""Cooking Channel"",
            ""provider_id"": 400
        },
        {
            ""display_priority"": 154,
            ""logo_path"": ""/7pkbHGkSYh6MKMTojJ80bT0KtPY.jpg"",
            ""provider_name"": ""Travel Channel"",
            ""provider_id"": 413
        },
        {
            ""display_priority"": 155,
            ""logo_path"": ""/hG3NOo8CJJTq7CQMj44kLFHoWOi.jpg"",
            ""provider_name"": ""Paramount Network"",
            ""provider_id"": 418
        },
        {
            ""display_priority"": 156,
            ""logo_path"": ""/sa10pK4Jwr5aA7rvafFP2zyLFjh.jpg"",
            ""provider_name"": ""Here TV"",
            ""provider_id"": 417
        },
        {
            ""display_priority"": 157,
            ""logo_path"": ""/zU4b7cGYV6kRDOI6s8dgZqUvwFI.jpg"",
            ""provider_name"": ""TV Land"",
            ""provider_id"": 419
        },
        {
            ""display_priority"": 158,
            ""logo_path"": ""/eWm07gxivsHwDx8CZRzVQIfVO4h.jpg"",
            ""provider_name"": ""Logo TV"",
            ""provider_id"": 420
        },
        {
            ""display_priority"": 159,
            ""logo_path"": ""/jJUUb3clz84u347JWx7RUFMdjwP.jpg"",
            ""provider_name"": ""VH1"",
            ""provider_id"": 422
        },
        {
            ""display_priority"": 161,
            ""logo_path"": ""/1Vzd0eRyJJ7djh0GuZczx4ap8PK.jpg"",
            ""provider_name"": ""DreamWorksTV Amazon Channel"",
            ""provider_id"": 263
        },
        {
            ""display_priority"": 162,
            ""logo_path"": ""/rcebVnRvZvPXauK4353Jgiu4DWI.jpg"",
            ""provider_name"": ""TBS"",
            ""provider_id"": 506
        },
        {
            ""display_priority"": 163,
            ""logo_path"": ""/3VxDqUk25KU5860XxHKwV9cy3L8.jpg"",
            ""provider_name"": ""AsianCrush"",
            ""provider_id"": 514
        },
        {
            ""display_priority"": 164,
            ""logo_path"": ""/mEiBVz62M9j3TCebmOspMfqkIn.jpg"",
            ""provider_name"": ""FILMRISE"",
            ""provider_id"": 471
        },
        {
            ""display_priority"": 165,
            ""logo_path"": ""/r1UgUKmt83FSDOIHBdRWKooZPNx.jpg"",
            ""provider_name"": ""Revry"",
            ""provider_id"": 473
        },
        {
            ""display_priority"": 168,
            ""logo_path"": ""/79mRAYq40lcYiXkQm6N7YErSSHd.jpg"",
            ""provider_name"": ""Spectrum On Demand"",
            ""provider_id"": 486
        },
        {
            ""display_priority"": 169,
            ""logo_path"": ""/lrZQdxtEHMbDZDnDo92KBkEHxSl.jpg"",
            ""provider_name"": ""OXYGEN"",
            ""provider_id"": 487
        },
        {
            ""display_priority"": 171,
            ""logo_path"": ""/rtTqPKRrVVXxvPV0T9OmSXhwXnY.jpg"",
            ""provider_name"": ""VRV"",
            ""provider_id"": 504
        },
        {
            ""display_priority"": 172,
            ""logo_path"": ""/pg4bIFyUsSIhFChqOz5Up1BxuIU.jpg"",
            ""provider_name"": ""tru TV"",
            ""provider_id"": 507
        },
        {
            ""display_priority"": 173,
            ""logo_path"": ""/pu5I5Fis0r7ReAOswcJzOKmdLrK.jpg"",
            ""provider_name"": ""DisneyNOW"",
            ""provider_id"": 508
        },
        {
            ""display_priority"": 174,
            ""logo_path"": ""/qiwHTuSh91SgVMtY9lP7y5tH6kN.jpg"",
            ""provider_name"": ""WeTV"",
            ""provider_id"": 509
        },
        {
            ""display_priority"": 175,
            ""logo_path"": ""/v8vA6WnPVTOE1o39waNFVmAqEJj.jpg"",
            ""provider_name"": ""Discovery Plus"",
            ""provider_id"": 520
        },
        {
            ""display_priority"": 176,
            ""logo_path"": ""/4UfmxLzph9Aso9pr9bXohp0V3sr.jpg"",
            ""provider_name"": ""ARROW"",
            ""provider_id"": 529
        },
        {
            ""display_priority"": 177,
            ""logo_path"": ""/wDWvnupneMbY6RhBTHQC9zU0SCX.jpg"",
            ""provider_name"": ""Plex"",
            ""provider_id"": 538
        },
        {
            ""display_priority"": 182,
            ""logo_path"": ""/jbcfM4YaulkzcPRIpiPZWIfcA67.jpg"",
            ""provider_name"": ""The Oprah Winfrey Network"",
            ""provider_id"": 555
        },
        {
            ""display_priority"": 189,
            ""logo_path"": ""/3VHvcxjJBMRdYZa76vsK8i46TOV.jpg"",
            ""provider_name"": ""British Path\u00E9 TV"",
            ""provider_id"": 571
        },
        {
            ""display_priority"": 195,
            ""logo_path"": ""/iFZdWKZr6kzkNRzZ3oRis0vfWob.jpg"",
            ""provider_name"": ""Freevee Amazon Channel"",
            ""provider_id"": 613
        },
        {
            ""display_priority"": 556,
            ""logo_path"": ""/lo403r2ha0brBHPynB28O84Qevz.jpg"",
            ""provider_name"": ""Netflix Free"",
            ""provider_id"": 459
        },
        {
            ""display_priority"": 638,
            ""logo_path"": ""/2qByOMV3SVPdADNXWtIGzA2GOkL.jpg"",
            ""provider_name"": ""CBS All Access Amazon Channel"",
            ""provider_id"": 198
        }
    ]
}";

        public static readonly string COUNTRIES_VALUES = @"[
    {
        ""iso_3166_1"": ""AD"",
        ""english_name"": ""Andorra"",
        ""native_name"": ""Andorra""
    },
    {
        ""iso_3166_1"": ""AE"",
        ""english_name"": ""United Arab Emirates"",
        ""native_name"": ""United Arab Emirates""
    },
    {
        ""iso_3166_1"": ""AF"",
        ""english_name"": ""Afghanistan"",
        ""native_name"": ""Afghanistan""
    },
    {
        ""iso_3166_1"": ""AG"",
        ""english_name"": ""Antigua and Barbuda"",
        ""native_name"": ""Antigua & Barbuda""
    },
    {
        ""iso_3166_1"": ""AI"",
        ""english_name"": ""Anguilla"",
        ""native_name"": ""Anguilla""
    },
    {
        ""iso_3166_1"": ""AL"",
        ""english_name"": ""Albania"",
        ""native_name"": ""Albania""
    },
    {
        ""iso_3166_1"": ""AM"",
        ""english_name"": ""Armenia"",
        ""native_name"": ""Armenia""
    },
    {
        ""iso_3166_1"": ""AN"",
        ""english_name"": ""Netherlands Antilles"",
        ""native_name"": ""Netherlands Antilles""
    },
    {
        ""iso_3166_1"": ""AO"",
        ""english_name"": ""Angola"",
        ""native_name"": ""Angola""
    },
    {
        ""iso_3166_1"": ""AQ"",
        ""english_name"": ""Antarctica"",
        ""native_name"": ""Antarctica""
    },
    {
        ""iso_3166_1"": ""AR"",
        ""english_name"": ""Argentina"",
        ""native_name"": ""Argentina""
    },
    {
        ""iso_3166_1"": ""AS"",
        ""english_name"": ""American Samoa"",
        ""native_name"": ""American Samoa""
    },
    {
        ""iso_3166_1"": ""AT"",
        ""english_name"": ""Austria"",
        ""native_name"": ""Austria""
    },
    {
        ""iso_3166_1"": ""AU"",
        ""english_name"": ""Australia"",
        ""native_name"": ""Australia""
    },
    {
        ""iso_3166_1"": ""AW"",
        ""english_name"": ""Aruba"",
        ""native_name"": ""Aruba""
    },
    {
        ""iso_3166_1"": ""AZ"",
        ""english_name"": ""Azerbaijan"",
        ""native_name"": ""Azerbaijan""
    },
    {
        ""iso_3166_1"": ""BA"",
        ""english_name"": ""Bosnia and Herzegovina"",
        ""native_name"": ""Bosnia & Herzegovina""
    },
    {
        ""iso_3166_1"": ""BB"",
        ""english_name"": ""Barbados"",
        ""native_name"": ""Barbados""
    },
    {
        ""iso_3166_1"": ""BD"",
        ""english_name"": ""Bangladesh"",
        ""native_name"": ""Bangladesh""
    },
    {
        ""iso_3166_1"": ""BE"",
        ""english_name"": ""Belgium"",
        ""native_name"": ""Belgium""
    },
    {
        ""iso_3166_1"": ""BF"",
        ""english_name"": ""Burkina Faso"",
        ""native_name"": ""Burkina Faso""
    },
    {
        ""iso_3166_1"": ""BG"",
        ""english_name"": ""Bulgaria"",
        ""native_name"": ""Bulgaria""
    },
    {
        ""iso_3166_1"": ""BH"",
        ""english_name"": ""Bahrain"",
        ""native_name"": ""Bahrain""
    },
    {
        ""iso_3166_1"": ""BI"",
        ""english_name"": ""Burundi"",
        ""native_name"": ""Burundi""
    },
    {
        ""iso_3166_1"": ""BJ"",
        ""english_name"": ""Benin"",
        ""native_name"": ""Benin""
    },
    {
        ""iso_3166_1"": ""BM"",
        ""english_name"": ""Bermuda"",
        ""native_name"": ""Bermuda""
    },
    {
        ""iso_3166_1"": ""BN"",
        ""english_name"": ""Brunei Darussalam"",
        ""native_name"": ""Brunei""
    },
    {
        ""iso_3166_1"": ""BO"",
        ""english_name"": ""Bolivia"",
        ""native_name"": ""Bolivia""
    },
    {
        ""iso_3166_1"": ""BR"",
        ""english_name"": ""Brazil"",
        ""native_name"": ""Brazil""
    },
    {
        ""iso_3166_1"": ""BS"",
        ""english_name"": ""Bahamas"",
        ""native_name"": ""Bahamas""
    },
    {
        ""iso_3166_1"": ""BT"",
        ""english_name"": ""Bhutan"",
        ""native_name"": ""Bhutan""
    },
    {
        ""iso_3166_1"": ""BV"",
        ""english_name"": ""Bouvet Island"",
        ""native_name"": ""Bouvet Island""
    },
    {
        ""iso_3166_1"": ""BW"",
        ""english_name"": ""Botswana"",
        ""native_name"": ""Botswana""
    },
    {
        ""iso_3166_1"": ""BY"",
        ""english_name"": ""Belarus"",
        ""native_name"": ""Belarus""
    },
    {
        ""iso_3166_1"": ""BZ"",
        ""english_name"": ""Belize"",
        ""native_name"": ""Belize""
    },
    {
        ""iso_3166_1"": ""CA"",
        ""english_name"": ""Canada"",
        ""native_name"": ""Canada""
    },
    {
        ""iso_3166_1"": ""CC"",
        ""english_name"": ""Cocos  Islands"",
        ""native_name"": ""Cocos (Keeling) Islands""
    },
    {
        ""iso_3166_1"": ""CD"",
        ""english_name"": ""Congo"",
        ""native_name"": ""Democratic Republic of the Congo (Kinshasa)""
    },
    {
        ""iso_3166_1"": ""CF"",
        ""english_name"": ""Central African Republic"",
        ""native_name"": ""Central African Republic""
    },
    {
        ""iso_3166_1"": ""CG"",
        ""english_name"": ""Congo"",
        ""native_name"": ""Republic of the Congo (Brazzaville)""
    },
    {
        ""iso_3166_1"": ""CH"",
        ""english_name"": ""Switzerland"",
        ""native_name"": ""Switzerland""
    },
    {
        ""iso_3166_1"": ""CI"",
        ""english_name"": ""Cote D'Ivoire"",
        ""native_name"": ""Côte d’Ivoire""
    },
    {
        ""iso_3166_1"": ""CK"",
        ""english_name"": ""Cook Islands"",
        ""native_name"": ""Cook Islands""
    },
    {
        ""iso_3166_1"": ""CL"",
        ""english_name"": ""Chile"",
        ""native_name"": ""Chile""
    },
    {
        ""iso_3166_1"": ""CM"",
        ""english_name"": ""Cameroon"",
        ""native_name"": ""Cameroon""
    },
    {
        ""iso_3166_1"": ""CN"",
        ""english_name"": ""China"",
        ""native_name"": ""China""
    },
    {
        ""iso_3166_1"": ""CO"",
        ""english_name"": ""Colombia"",
        ""native_name"": ""Colombia""
    },
    {
        ""iso_3166_1"": ""CR"",
        ""english_name"": ""Costa Rica"",
        ""native_name"": ""Costa Rica""
    },
    {
        ""iso_3166_1"": ""CS"",
        ""english_name"": ""Serbia and Montenegro"",
        ""native_name"": ""Serbia and Montenegro""
    },
    {
        ""iso_3166_1"": ""CU"",
        ""english_name"": ""Cuba"",
        ""native_name"": ""Cuba""
    },
    {
        ""iso_3166_1"": ""CV"",
        ""english_name"": ""Cape Verde"",
        ""native_name"": ""Cape Verde""
    },
    {
        ""iso_3166_1"": ""CX"",
        ""english_name"": ""Christmas Island"",
        ""native_name"": ""Christmas Island""
    },
    {
        ""iso_3166_1"": ""CY"",
        ""english_name"": ""Cyprus"",
        ""native_name"": ""Cyprus""
    },
    {
        ""iso_3166_1"": ""CZ"",
        ""english_name"": ""Czech Republic"",
        ""native_name"": ""Czech Republic""
    },
    {
        ""iso_3166_1"": ""DE"",
        ""english_name"": ""Germany"",
        ""native_name"": ""Germany""
    },
    {
        ""iso_3166_1"": ""DJ"",
        ""english_name"": ""Djibouti"",
        ""native_name"": ""Djibouti""
    },
    {
        ""iso_3166_1"": ""DK"",
        ""english_name"": ""Denmark"",
        ""native_name"": ""Denmark""
    },
    {
        ""iso_3166_1"": ""DM"",
        ""english_name"": ""Dominica"",
        ""native_name"": ""Dominica""
    },
    {
        ""iso_3166_1"": ""DO"",
        ""english_name"": ""Dominican Republic"",
        ""native_name"": ""Dominican Republic""
    },
    {
        ""iso_3166_1"": ""DZ"",
        ""english_name"": ""Algeria"",
        ""native_name"": ""Algeria""
    },
    {
        ""iso_3166_1"": ""EC"",
        ""english_name"": ""Ecuador"",
        ""native_name"": ""Ecuador""
    },
    {
        ""iso_3166_1"": ""EE"",
        ""english_name"": ""Estonia"",
        ""native_name"": ""Estonia""
    },
    {
        ""iso_3166_1"": ""EG"",
        ""english_name"": ""Egypt"",
        ""native_name"": ""Egypt""
    },
    {
        ""iso_3166_1"": ""EH"",
        ""english_name"": ""Western Sahara"",
        ""native_name"": ""Western Sahara""
    },
    {
        ""iso_3166_1"": ""ER"",
        ""english_name"": ""Eritrea"",
        ""native_name"": ""Eritrea""
    },
    {
        ""iso_3166_1"": ""ES"",
        ""english_name"": ""Spain"",
        ""native_name"": ""Spain""
    },
    {
        ""iso_3166_1"": ""ET"",
        ""english_name"": ""Ethiopia"",
        ""native_name"": ""Ethiopia""
    },
    {
        ""iso_3166_1"": ""FI"",
        ""english_name"": ""Finland"",
        ""native_name"": ""Finland""
    },
    {
        ""iso_3166_1"": ""FJ"",
        ""english_name"": ""Fiji"",
        ""native_name"": ""Fiji""
    },
    {
        ""iso_3166_1"": ""FK"",
        ""english_name"": ""Falkland Islands"",
        ""native_name"": ""Falkland Islands""
    },
    {
        ""iso_3166_1"": ""FM"",
        ""english_name"": ""Micronesia"",
        ""native_name"": ""Micronesia""
    },
    {
        ""iso_3166_1"": ""FO"",
        ""english_name"": ""Faeroe Islands"",
        ""native_name"": ""Faroe Islands""
    },
    {
        ""iso_3166_1"": ""FR"",
        ""english_name"": ""France"",
        ""native_name"": ""France""
    },
    {
        ""iso_3166_1"": ""GA"",
        ""english_name"": ""Gabon"",
        ""native_name"": ""Gabon""
    },
    {
        ""iso_3166_1"": ""GB"",
        ""english_name"": ""United Kingdom"",
        ""native_name"": ""United Kingdom""
    },
    {
        ""iso_3166_1"": ""GD"",
        ""english_name"": ""Grenada"",
        ""native_name"": ""Grenada""
    },
    {
        ""iso_3166_1"": ""GE"",
        ""english_name"": ""Georgia"",
        ""native_name"": ""Georgia""
    },
    {
        ""iso_3166_1"": ""GF"",
        ""english_name"": ""French Guiana"",
        ""native_name"": ""French Guiana""
    },
    {
        ""iso_3166_1"": ""GH"",
        ""english_name"": ""Ghana"",
        ""native_name"": ""Ghana""
    },
    {
        ""iso_3166_1"": ""GI"",
        ""english_name"": ""Gibraltar"",
        ""native_name"": ""Gibraltar""
    },
    {
        ""iso_3166_1"": ""GL"",
        ""english_name"": ""Greenland"",
        ""native_name"": ""Greenland""
    },
    {
        ""iso_3166_1"": ""GM"",
        ""english_name"": ""Gambia"",
        ""native_name"": ""Gambia""
    },
    {
        ""iso_3166_1"": ""GN"",
        ""english_name"": ""Guinea"",
        ""native_name"": ""Guinea""
    },
    {
        ""iso_3166_1"": ""GP"",
        ""english_name"": ""Guadaloupe"",
        ""native_name"": ""Guadeloupe""
    },
    {
        ""iso_3166_1"": ""GQ"",
        ""english_name"": ""Equatorial Guinea"",
        ""native_name"": ""Equatorial Guinea""
    },
    {
        ""iso_3166_1"": ""GR"",
        ""english_name"": ""Greece"",
        ""native_name"": ""Greece""
    },
    {
        ""iso_3166_1"": ""GS"",
        ""english_name"": ""South Georgia and the South Sandwich Islands"",
        ""native_name"": ""South Georgia & South Sandwich Islands""
    },
    {
        ""iso_3166_1"": ""GT"",
        ""english_name"": ""Guatemala"",
        ""native_name"": ""Guatemala""
    },
    {
        ""iso_3166_1"": ""GU"",
        ""english_name"": ""Guam"",
        ""native_name"": ""Guam""
    },
    {
        ""iso_3166_1"": ""GW"",
        ""english_name"": ""Guinea-Bissau"",
        ""native_name"": ""Guinea-Bissau""
    },
    {
        ""iso_3166_1"": ""GY"",
        ""english_name"": ""Guyana"",
        ""native_name"": ""Guyana""
    },
    {
        ""iso_3166_1"": ""HK"",
        ""english_name"": ""Hong Kong"",
        ""native_name"": ""Hong Kong SAR China""
    },
    {
        ""iso_3166_1"": ""HM"",
        ""english_name"": ""Heard and McDonald Islands"",
        ""native_name"": ""Heard & McDonald Islands""
    },
    {
        ""iso_3166_1"": ""HN"",
        ""english_name"": ""Honduras"",
        ""native_name"": ""Honduras""
    },
    {
        ""iso_3166_1"": ""HR"",
        ""english_name"": ""Croatia"",
        ""native_name"": ""Croatia""
    },
    {
        ""iso_3166_1"": ""HT"",
        ""english_name"": ""Haiti"",
        ""native_name"": ""Haiti""
    },
    {
        ""iso_3166_1"": ""HU"",
        ""english_name"": ""Hungary"",
        ""native_name"": ""Hungary""
    },
    {
        ""iso_3166_1"": ""ID"",
        ""english_name"": ""Indonesia"",
        ""native_name"": ""Indonesia""
    },
    {
        ""iso_3166_1"": ""IE"",
        ""english_name"": ""Ireland"",
        ""native_name"": ""Ireland""
    },
    {
        ""iso_3166_1"": ""IL"",
        ""english_name"": ""Israel"",
        ""native_name"": ""Israel""
    },
    {
        ""iso_3166_1"": ""IN"",
        ""english_name"": ""India"",
        ""native_name"": ""India""
    },
    {
        ""iso_3166_1"": ""IO"",
        ""english_name"": ""British Indian Ocean Territory"",
        ""native_name"": ""British Indian Ocean Territory""
    },
    {
        ""iso_3166_1"": ""IQ"",
        ""english_name"": ""Iraq"",
        ""native_name"": ""Iraq""
    },
    {
        ""iso_3166_1"": ""IR"",
        ""english_name"": ""Iran"",
        ""native_name"": ""Iran""
    },
    {
        ""iso_3166_1"": ""IS"",
        ""english_name"": ""Iceland"",
        ""native_name"": ""Iceland""
    },
    {
        ""iso_3166_1"": ""IT"",
        ""english_name"": ""Italy"",
        ""native_name"": ""Italy""
    },
    {
        ""iso_3166_1"": ""JM"",
        ""english_name"": ""Jamaica"",
        ""native_name"": ""Jamaica""
    },
    {
        ""iso_3166_1"": ""JO"",
        ""english_name"": ""Jordan"",
        ""native_name"": ""Jordan""
    },
    {
        ""iso_3166_1"": ""JP"",
        ""english_name"": ""Japan"",
        ""native_name"": ""Japan""
    },
    {
        ""iso_3166_1"": ""KE"",
        ""english_name"": ""Kenya"",
        ""native_name"": ""Kenya""
    },
    {
        ""iso_3166_1"": ""KG"",
        ""english_name"": ""Kyrgyz Republic"",
        ""native_name"": ""Kyrgyzstan""
    },
    {
        ""iso_3166_1"": ""KH"",
        ""english_name"": ""Cambodia"",
        ""native_name"": ""Cambodia""
    },
    {
        ""iso_3166_1"": ""KI"",
        ""english_name"": ""Kiribati"",
        ""native_name"": ""Kiribati""
    },
    {
        ""iso_3166_1"": ""KM"",
        ""english_name"": ""Comoros"",
        ""native_name"": ""Comoros""
    },
    {
        ""iso_3166_1"": ""KN"",
        ""english_name"": ""St. Kitts and Nevis"",
        ""native_name"": ""St. Kitts & Nevis""
    },
    {
        ""iso_3166_1"": ""KP"",
        ""english_name"": ""North Korea"",
        ""native_name"": ""North Korea""
    },
    {
        ""iso_3166_1"": ""KR"",
        ""english_name"": ""South Korea"",
        ""native_name"": ""South Korea""
    },
    {
        ""iso_3166_1"": ""KW"",
        ""english_name"": ""Kuwait"",
        ""native_name"": ""Kuwait""
    },
    {
        ""iso_3166_1"": ""KY"",
        ""english_name"": ""Cayman Islands"",
        ""native_name"": ""Cayman Islands""
    },
    {
        ""iso_3166_1"": ""KZ"",
        ""english_name"": ""Kazakhstan"",
        ""native_name"": ""Kazakhstan""
    },
    {
        ""iso_3166_1"": ""LA"",
        ""english_name"": ""Lao People's Democratic Republic"",
        ""native_name"": ""Laos""
    },
    {
        ""iso_3166_1"": ""LB"",
        ""english_name"": ""Lebanon"",
        ""native_name"": ""Lebanon""
    },
    {
        ""iso_3166_1"": ""LC"",
        ""english_name"": ""St. Lucia"",
        ""native_name"": ""St. Lucia""
    },
    {
        ""iso_3166_1"": ""LI"",
        ""english_name"": ""Liechtenstein"",
        ""native_name"": ""Liechtenstein""
    },
    {
        ""iso_3166_1"": ""LK"",
        ""english_name"": ""Sri Lanka"",
        ""native_name"": ""Sri Lanka""
    },
    {
        ""iso_3166_1"": ""LR"",
        ""english_name"": ""Liberia"",
        ""native_name"": ""Liberia""
    },
    {
        ""iso_3166_1"": ""LS"",
        ""english_name"": ""Lesotho"",
        ""native_name"": ""Lesotho""
    },
    {
        ""iso_3166_1"": ""LT"",
        ""english_name"": ""Lithuania"",
        ""native_name"": ""Lithuania""
    },
    {
        ""iso_3166_1"": ""LU"",
        ""english_name"": ""Luxembourg"",
        ""native_name"": ""Luxembourg""
    },
    {
        ""iso_3166_1"": ""LV"",
        ""english_name"": ""Latvia"",
        ""native_name"": ""Latvia""
    },
    {
        ""iso_3166_1"": ""LY"",
        ""english_name"": ""Libyan Arab Jamahiriya"",
        ""native_name"": ""Libya""
    },
    {
        ""iso_3166_1"": ""MA"",
        ""english_name"": ""Morocco"",
        ""native_name"": ""Morocco""
    },
    {
        ""iso_3166_1"": ""MC"",
        ""english_name"": ""Monaco"",
        ""native_name"": ""Monaco""
    },
    {
        ""iso_3166_1"": ""MD"",
        ""english_name"": ""Moldova"",
        ""native_name"": ""Moldova""
    },
    {
        ""iso_3166_1"": ""ME"",
        ""english_name"": ""Montenegro"",
        ""native_name"": ""Montenegro""
    },
    {
        ""iso_3166_1"": ""MG"",
        ""english_name"": ""Madagascar"",
        ""native_name"": ""Madagascar""
    },
    {
        ""iso_3166_1"": ""MH"",
        ""english_name"": ""Marshall Islands"",
        ""native_name"": ""Marshall Islands""
    },
    {
        ""iso_3166_1"": ""MK"",
        ""english_name"": ""Macedonia"",
        ""native_name"": ""Macedonia""
    },
    {
        ""iso_3166_1"": ""ML"",
        ""english_name"": ""Mali"",
        ""native_name"": ""Mali""
    },
    {
        ""iso_3166_1"": ""MM"",
        ""english_name"": ""Myanmar"",
        ""native_name"": ""Myanmar (Burma)""
    },
    {
        ""iso_3166_1"": ""MN"",
        ""english_name"": ""Mongolia"",
        ""native_name"": ""Mongolia""
    },
    {
        ""iso_3166_1"": ""MO"",
        ""english_name"": ""Macao"",
        ""native_name"": ""Macau SAR China""
    },
    {
        ""iso_3166_1"": ""MP"",
        ""english_name"": ""Northern Mariana Islands"",
        ""native_name"": ""Northern Mariana Islands""
    },
    {
        ""iso_3166_1"": ""MQ"",
        ""english_name"": ""Martinique"",
        ""native_name"": ""Martinique""
    },
    {
        ""iso_3166_1"": ""MR"",
        ""english_name"": ""Mauritania"",
        ""native_name"": ""Mauritania""
    },
    {
        ""iso_3166_1"": ""MS"",
        ""english_name"": ""Montserrat"",
        ""native_name"": ""Montserrat""
    },
    {
        ""iso_3166_1"": ""MT"",
        ""english_name"": ""Malta"",
        ""native_name"": ""Malta""
    },
    {
        ""iso_3166_1"": ""MU"",
        ""english_name"": ""Mauritius"",
        ""native_name"": ""Mauritius""
    },
    {
        ""iso_3166_1"": ""MV"",
        ""english_name"": ""Maldives"",
        ""native_name"": ""Maldives""
    },
    {
        ""iso_3166_1"": ""MW"",
        ""english_name"": ""Malawi"",
        ""native_name"": ""Malawi""
    },
    {
        ""iso_3166_1"": ""MX"",
        ""english_name"": ""Mexico"",
        ""native_name"": ""Mexico""
    },
    {
        ""iso_3166_1"": ""MY"",
        ""english_name"": ""Malaysia"",
        ""native_name"": ""Malaysia""
    },
    {
        ""iso_3166_1"": ""MZ"",
        ""english_name"": ""Mozambique"",
        ""native_name"": ""Mozambique""
    },
    {
        ""iso_3166_1"": ""NA"",
        ""english_name"": ""Namibia"",
        ""native_name"": ""Namibia""
    },
    {
        ""iso_3166_1"": ""NC"",
        ""english_name"": ""New Caledonia"",
        ""native_name"": ""New Caledonia""
    },
    {
        ""iso_3166_1"": ""NE"",
        ""english_name"": ""Niger"",
        ""native_name"": ""Niger""
    },
    {
        ""iso_3166_1"": ""NF"",
        ""english_name"": ""Norfolk Island"",
        ""native_name"": ""Norfolk Island""
    },
    {
        ""iso_3166_1"": ""NG"",
        ""english_name"": ""Nigeria"",
        ""native_name"": ""Nigeria""
    },
    {
        ""iso_3166_1"": ""NI"",
        ""english_name"": ""Nicaragua"",
        ""native_name"": ""Nicaragua""
    },
    {
        ""iso_3166_1"": ""NL"",
        ""english_name"": ""Netherlands"",
        ""native_name"": ""Netherlands""
    },
    {
        ""iso_3166_1"": ""NO"",
        ""english_name"": ""Norway"",
        ""native_name"": ""Norway""
    },
    {
        ""iso_3166_1"": ""NP"",
        ""english_name"": ""Nepal"",
        ""native_name"": ""Nepal""
    },
    {
        ""iso_3166_1"": ""NR"",
        ""english_name"": ""Nauru"",
        ""native_name"": ""Nauru""
    },
    {
        ""iso_3166_1"": ""NU"",
        ""english_name"": ""Niue"",
        ""native_name"": ""Niue""
    },
    {
        ""iso_3166_1"": ""NZ"",
        ""english_name"": ""New Zealand"",
        ""native_name"": ""New Zealand""
    },
    {
        ""iso_3166_1"": ""OM"",
        ""english_name"": ""Oman"",
        ""native_name"": ""Oman""
    },
    {
        ""iso_3166_1"": ""PA"",
        ""english_name"": ""Panama"",
        ""native_name"": ""Panama""
    },
    {
        ""iso_3166_1"": ""PE"",
        ""english_name"": ""Peru"",
        ""native_name"": ""Peru""
    },
    {
        ""iso_3166_1"": ""PF"",
        ""english_name"": ""French Polynesia"",
        ""native_name"": ""French Polynesia""
    },
    {
        ""iso_3166_1"": ""PG"",
        ""english_name"": ""Papua New Guinea"",
        ""native_name"": ""Papua New Guinea""
    },
    {
        ""iso_3166_1"": ""PH"",
        ""english_name"": ""Philippines"",
        ""native_name"": ""Philippines""
    },
    {
        ""iso_3166_1"": ""PK"",
        ""english_name"": ""Pakistan"",
        ""native_name"": ""Pakistan""
    },
    {
        ""iso_3166_1"": ""PL"",
        ""english_name"": ""Poland"",
        ""native_name"": ""Poland""
    },
    {
        ""iso_3166_1"": ""PM"",
        ""english_name"": ""St. Pierre and Miquelon"",
        ""native_name"": ""St. Pierre & Miquelon""
    },
    {
        ""iso_3166_1"": ""PN"",
        ""english_name"": ""Pitcairn Island"",
        ""native_name"": ""Pitcairn Islands""
    },
    {
        ""iso_3166_1"": ""PR"",
        ""english_name"": ""Puerto Rico"",
        ""native_name"": ""Puerto Rico""
    },
    {
        ""iso_3166_1"": ""PS"",
        ""english_name"": ""Palestinian Territory"",
        ""native_name"": ""Palestinian Territories""
    },
    {
        ""iso_3166_1"": ""PT"",
        ""english_name"": ""Portugal"",
        ""native_name"": ""Portugal""
    },
    {
        ""iso_3166_1"": ""PW"",
        ""english_name"": ""Palau"",
        ""native_name"": ""Palau""
    },
    {
        ""iso_3166_1"": ""PY"",
        ""english_name"": ""Paraguay"",
        ""native_name"": ""Paraguay""
    },
    {
        ""iso_3166_1"": ""QA"",
        ""english_name"": ""Qatar"",
        ""native_name"": ""Qatar""
    },
    {
        ""iso_3166_1"": ""RE"",
        ""english_name"": ""Reunion"",
        ""native_name"": ""Réunion""
    },
    {
        ""iso_3166_1"": ""RO"",
        ""english_name"": ""Romania"",
        ""native_name"": ""Romania""
    },
    {
        ""iso_3166_1"": ""RS"",
        ""english_name"": ""Serbia"",
        ""native_name"": ""Serbia""
    },
    {
        ""iso_3166_1"": ""RU"",
        ""english_name"": ""Russia"",
        ""native_name"": ""Russia""
    },
    {
        ""iso_3166_1"": ""RW"",
        ""english_name"": ""Rwanda"",
        ""native_name"": ""Rwanda""
    },
    {
        ""iso_3166_1"": ""SA"",
        ""english_name"": ""Saudi Arabia"",
        ""native_name"": ""Saudi Arabia""
    },
    {
        ""iso_3166_1"": ""SB"",
        ""english_name"": ""Solomon Islands"",
        ""native_name"": ""Solomon Islands""
    },
    {
        ""iso_3166_1"": ""SC"",
        ""english_name"": ""Seychelles"",
        ""native_name"": ""Seychelles""
    },
    {
        ""iso_3166_1"": ""SD"",
        ""english_name"": ""Sudan"",
        ""native_name"": ""Sudan""
    },
    {
        ""iso_3166_1"": ""SE"",
        ""english_name"": ""Sweden"",
        ""native_name"": ""Sweden""
    },
    {
        ""iso_3166_1"": ""SG"",
        ""english_name"": ""Singapore"",
        ""native_name"": ""Singapore""
    },
    {
        ""iso_3166_1"": ""SH"",
        ""english_name"": ""St. Helena"",
        ""native_name"": ""St. Helena""
    },
    {
        ""iso_3166_1"": ""SI"",
        ""english_name"": ""Slovenia"",
        ""native_name"": ""Slovenia""
    },
    {
        ""iso_3166_1"": ""SJ"",
        ""english_name"": ""Svalbard & Jan Mayen Islands"",
        ""native_name"": ""Svalbard & Jan Mayen""
    },
    {
        ""iso_3166_1"": ""SK"",
        ""english_name"": ""Slovakia"",
        ""native_name"": ""Slovakia""
    },
    {
        ""iso_3166_1"": ""SL"",
        ""english_name"": ""Sierra Leone"",
        ""native_name"": ""Sierra Leone""
    },
    {
        ""iso_3166_1"": ""SM"",
        ""english_name"": ""San Marino"",
        ""native_name"": ""San Marino""
    },
    {
        ""iso_3166_1"": ""SN"",
        ""english_name"": ""Senegal"",
        ""native_name"": ""Senegal""
    },
    {
        ""iso_3166_1"": ""SO"",
        ""english_name"": ""Somalia"",
        ""native_name"": ""Somalia""
    },
    {
        ""iso_3166_1"": ""SR"",
        ""english_name"": ""Suriname"",
        ""native_name"": ""Suriname""
    },
    {
        ""iso_3166_1"": ""SS"",
        ""english_name"": ""South Sudan"",
        ""native_name"": ""South Sudan""
    },
    {
        ""iso_3166_1"": ""ST"",
        ""english_name"": ""Sao Tome and Principe"",
        ""native_name"": ""São Tomé & Príncipe""
    },
    {
        ""iso_3166_1"": ""SU"",
        ""english_name"": ""Soviet Union"",
        ""native_name"": ""Soviet Union""
    },
    {
        ""iso_3166_1"": ""SV"",
        ""english_name"": ""El Salvador"",
        ""native_name"": ""El Salvador""
    },
    {
        ""iso_3166_1"": ""SY"",
        ""english_name"": ""Syrian Arab Republic"",
        ""native_name"": ""Syria""
    },
    {
        ""iso_3166_1"": ""SZ"",
        ""english_name"": ""Swaziland"",
        ""native_name"": ""Eswatini (Swaziland)""
    },
    {
        ""iso_3166_1"": ""TC"",
        ""english_name"": ""Turks and Caicos Islands"",
        ""native_name"": ""Turks & Caicos Islands""
    },
    {
        ""iso_3166_1"": ""TD"",
        ""english_name"": ""Chad"",
        ""native_name"": ""Chad""
    },
    {
        ""iso_3166_1"": ""TF"",
        ""english_name"": ""French Southern Territories"",
        ""native_name"": ""French Southern Territories""
    },
    {
        ""iso_3166_1"": ""TG"",
        ""english_name"": ""Togo"",
        ""native_name"": ""Togo""
    },
    {
        ""iso_3166_1"": ""TH"",
        ""english_name"": ""Thailand"",
        ""native_name"": ""Thailand""
    },
    {
        ""iso_3166_1"": ""TJ"",
        ""english_name"": ""Tajikistan"",
        ""native_name"": ""Tajikistan""
    },
    {
        ""iso_3166_1"": ""TK"",
        ""english_name"": ""Tokelau"",
        ""native_name"": ""Tokelau""
    },
    {
        ""iso_3166_1"": ""TL"",
        ""english_name"": ""Timor-Leste"",
        ""native_name"": ""Timor-Leste""
    },
    {
        ""iso_3166_1"": ""TM"",
        ""english_name"": ""Turkmenistan"",
        ""native_name"": ""Turkmenistan""
    },
    {
        ""iso_3166_1"": ""TN"",
        ""english_name"": ""Tunisia"",
        ""native_name"": ""Tunisia""
    },
    {
        ""iso_3166_1"": ""TO"",
        ""english_name"": ""Tonga"",
        ""native_name"": ""Tonga""
    },
    {
        ""iso_3166_1"": ""TR"",
        ""english_name"": ""Turkey"",
        ""native_name"": ""Turkey""
    },
    {
        ""iso_3166_1"": ""TT"",
        ""english_name"": ""Trinidad and Tobago"",
        ""native_name"": ""Trinidad & Tobago""
    },
    {
        ""iso_3166_1"": ""TV"",
        ""english_name"": ""Tuvalu"",
        ""native_name"": ""Tuvalu""
    },
    {
        ""iso_3166_1"": ""TW"",
        ""english_name"": ""Taiwan"",
        ""native_name"": ""Taiwan""
    },
    {
        ""iso_3166_1"": ""TZ"",
        ""english_name"": ""Tanzania"",
        ""native_name"": ""Tanzania""
    },
    {
        ""iso_3166_1"": ""UA"",
        ""english_name"": ""Ukraine"",
        ""native_name"": ""Ukraine""
    },
    {
        ""iso_3166_1"": ""UG"",
        ""english_name"": ""Uganda"",
        ""native_name"": ""Uganda""
    },
    {
        ""iso_3166_1"": ""UM"",
        ""english_name"": ""United States Minor Outlying Islands"",
        ""native_name"": ""U.S. Outlying Islands""
    },
    {
        ""iso_3166_1"": ""US"",
        ""english_name"": ""United States of America"",
        ""native_name"": ""United States""
    },
    {
        ""iso_3166_1"": ""UY"",
        ""english_name"": ""Uruguay"",
        ""native_name"": ""Uruguay""
    },
    {
        ""iso_3166_1"": ""UZ"",
        ""english_name"": ""Uzbekistan"",
        ""native_name"": ""Uzbekistan""
    },
    {
        ""iso_3166_1"": ""VA"",
        ""english_name"": ""Holy See"",
        ""native_name"": ""Vatican City""
    },
    {
        ""iso_3166_1"": ""VC"",
        ""english_name"": ""St. Vincent and the Grenadines"",
        ""native_name"": ""St. Vincent & Grenadines""
    },
    {
        ""iso_3166_1"": ""VE"",
        ""english_name"": ""Venezuela"",
        ""native_name"": ""Venezuela""
    },
    {
        ""iso_3166_1"": ""VG"",
        ""english_name"": ""British Virgin Islands"",
        ""native_name"": ""British Virgin Islands""
    },
    {
        ""iso_3166_1"": ""VI"",
        ""english_name"": ""US Virgin Islands"",
        ""native_name"": ""U.S. Virgin Islands""
    },
    {
        ""iso_3166_1"": ""VN"",
        ""english_name"": ""Vietnam"",
        ""native_name"": ""Vietnam""
    },
    {
        ""iso_3166_1"": ""VU"",
        ""english_name"": ""Vanuatu"",
        ""native_name"": ""Vanuatu""
    },
    {
        ""iso_3166_1"": ""WF"",
        ""english_name"": ""Wallis and Futuna Islands"",
        ""native_name"": ""Wallis & Futuna""
    },
    {
        ""iso_3166_1"": ""WS"",
        ""english_name"": ""Samoa"",
        ""native_name"": ""Samoa""
    },
    {
        ""iso_3166_1"": ""XC"",
        ""english_name"": ""Czechoslovakia"",
        ""native_name"": ""Czechoslovakia""
    },
    {
        ""iso_3166_1"": ""XG"",
        ""english_name"": ""East Germany"",
        ""native_name"": ""East Germany""
    },
    {
        ""iso_3166_1"": ""XK"",
        ""english_name"": ""Kosovo"",
        ""native_name"": ""Kosovo""
    },
    {
        ""iso_3166_1"": ""YE"",
        ""english_name"": ""Yemen"",
        ""native_name"": ""Yemen""
    },
    {
        ""iso_3166_1"": ""YT"",
        ""english_name"": ""Mayotte"",
        ""native_name"": ""Mayotte""
    },
    {
        ""iso_3166_1"": ""YU"",
        ""english_name"": ""Yugoslavia"",
        ""native_name"": ""Yugoslavia""
    },
    {
        ""iso_3166_1"": ""ZA"",
        ""english_name"": ""South Africa"",
        ""native_name"": ""South Africa""
    },
    {
        ""iso_3166_1"": ""ZM"",
        ""english_name"": ""Zambia"",
        ""native_name"": ""Zambia""
    },
    {
        ""iso_3166_1"": ""ZW"",
        ""english_name"": ""Zimbabwe"",
        ""native_name"": ""Zimbabwe""
    }
]";

        public static readonly string CONFIGURATION = @"{
    ""images"": {
        ""base_url"": ""http://image.tmdb.org/t/p/"",
        ""secure_base_url"": ""https://image.tmdb.org/t/p/"",
        ""backdrop_sizes"": [
            ""w300"",
            ""w780"",
            ""w1280"",
            ""original""
        ],
        ""logo_sizes"": [
            ""w45"",
            ""w92"",
            ""w154"",
            ""w185"",
            ""w300"",
            ""w500"",
            ""original""
        ],
        ""poster_sizes"": [
            ""w92"",
            ""w154"",
            ""w185"",
            ""w342"",
            ""w500"",
            ""w780"",
            ""original""
        ],
        ""profile_sizes"": [
            ""w45"",
            ""w185"",
            ""h632"",
            ""original""
        ],
        ""still_sizes"": [
            ""w92"",
            ""w185"",
            ""w300"",
            ""original""
        ]
    },
    ""change_keys"": [
        ""adult"",
        ""air_date"",
        ""also_known_as"",
        ""alternative_titles"",
        ""biography"",
        ""birthday"",
        ""budget"",
        ""cast"",
        ""certifications"",
        ""character_names"",
        ""created_by"",
        ""crew"",
        ""deathday"",
        ""episode"",
        ""episode_number"",
        ""episode_run_time"",
        ""freebase_id"",
        ""freebase_mid"",
        ""general"",
        ""genres"",
        ""guest_stars"",
        ""homepage"",
        ""images"",
        ""imdb_id"",
        ""languages"",
        ""name"",
        ""network"",
        ""origin_country"",
        ""original_name"",
        ""original_title"",
        ""overview"",
        ""parts"",
        ""place_of_birth"",
        ""plot_keywords"",
        ""production_code"",
        ""production_companies"",
        ""production_countries"",
        ""releases"",
        ""revenue"",
        ""runtime"",
        ""season"",
        ""season_number"",
        ""season_regular"",
        ""spoken_languages"",
        ""status"",
        ""tagline"",
        ""title"",
        ""translations"",
        ""tvdb_id"",
        ""tvrage_id"",
        ""type"",
        ""video"",
        ""videos""
    ]
}";
    }
}
#endif
