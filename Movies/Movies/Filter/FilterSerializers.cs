using Movies.Models;
using Movies.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Movies
{
    public class AppPropertiesCache : IJsonCache, IEnumerable<KeyValuePair<string, JsonResponse>>
    {
        public Xamarin.Forms.Application Application { get; }

        public AppPropertiesCache(Xamarin.Forms.Application application)
        {
            Application = application;
        }

        public Task AddAsync(string url, JsonResponse response)
        {
            Application.Properties[url] = JsonSerializer.Serialize(response);
            return Application.SavePropertiesAsync();
        }

        public async Task Clear()
        {
            foreach (var kvp in System.Linq.Enumerable.ToList(this))
            {
                await Expire(kvp.Key);
            }
        }

        public Task<bool> IsCached(string url) => Task.FromResult(Application.Properties.ContainsKey(url));

        public Task<JsonResponse> TryGetValueAsync(string url) => Task.FromResult(TryGetValue(url, out var response) ? response : null);

        public bool TryGetValue(string url, out JsonResponse response)
        {
            if (Application.Properties.TryGetValue(url, out var value) && value is string cached)
            {
                try
                {
                    var root = JsonDocument.Parse(cached).RootElement;

                    if (root.TryGetValue(out string json, nameof(JsonResponse.Json)) && root.TryGetValue(out DateTime timeStamp, nameof(JsonResponse.Timestamp)))
                    {
                        response = new JsonResponse(json, timeStamp);
                        return true;
                    }
                }
                catch { }
            }

            response = null;
            return false;
        }

        public async Task<bool> Expire(string url)
        {
            bool success = Application.Properties.Remove(url);
            await Application.SavePropertiesAsync();
            return success;
        }

        public async IAsyncEnumerator<KeyValuePair<string, JsonResponse>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var itr = GetEnumerator();

            while (itr.MoveNext())
            {
                yield return itr.Current;
            }

            await Task.CompletedTask;
        }

        public IEnumerator<KeyValuePair<string, JsonResponse>> GetEnumerator()
        {
            foreach (var kvp in Application.Properties)
            {
                if (kvp.Key is string url && Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out _) && TryGetValue(url, out var response))
                {
                    yield return new KeyValuePair<string, JsonResponse>(url, response);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public abstract class ReadOnlyJsonConverter<T> : JsonConverter<T>
    {
        public JsonSerializerOptions GetOptions(JsonSerializerOptions original, Func<JsonConverter, bool> predicate)
        {
            var options = new JsonSerializerOptions(original);

            for (int i = 0; i < options.Converters.Count; i++)
            {
                if (!predicate(options.Converters[i]))
                {
                    options.Converters.RemoveAt(i--);
                }
            }
            
            return options;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteRawValue(JsonSerializer.Serialize(value, GetOptions(options, converter => converter != this)));
    }

    public class ItemTypeConverter : ReadOnlyJsonConverter<Type>
    {
        public static readonly Type[] Types = new Type[]
        {
            typeof(Movie),
            typeof(TVShow),
            typeof(TVSeason),
            typeof(TVEpisode),
            typeof(Person),
            typeof(Collection),
            typeof(List),
        };

        public override bool CanConvert(Type typeToConvert) => typeof(Type).IsAssignableFrom(typeToConvert);

        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return Types.FirstOrDefault(type => type.ToString() == value) ?? throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }

    public class PredicateConverter : ReadOnlyJsonConverter<OperatorPredicate>
    {
        public override OperatorPredicate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var json = JsonDocument.ParseValue(ref reader);
            var root = json.RootElement;

            if (root.TryGetValue(out string name, nameof(OperatorPredicate.LHS)) &&
                root.TryGetValue(out int op, nameof(OperatorPredicate.Operator)) &&
                //root.TryGetProperty(nameof(OperatorPredicate.Operator), out var opElement) &&
                //opElement.TryGetInt32(out var op) &&
                root.TryGetProperty(nameof(OperatorPredicate.RHS), out var rhsNode))
            {
                if (name == CollectionViewModel.ITEM_TYPE)
                {
                    return new OperatorPredicate
                    {
                        LHS = name,
                        Operator = (Operators)op,
                        RHS = rhsNode.Deserialize<Type>(options)
                    };
                }

                var propertyConverter = options.Converters.OfType<PropertyConverter>().FirstOrDefault();

                if (propertyConverter == null)
                {
                    return null;
                }

                var properties = propertyConverter.Properties.Where(property => property.Name == name).ToArray();

                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];

                    try
                    {
                        var rhs = rhsNode.Deserialize(property.Type, options);

                        if (i == properties.Length - 1 || property?.Values.OfType<object>().Contains(rhs) == true)
                        {
                            return new OperatorPredicate
                            {
                                LHS = property,
                                Operator = (Operators)op,
                                RHS = rhs
                            };
                        }
                    }
                    catch { }
                }
            }

            throw new JsonException();
        }

        /*public bool TryDeserialize(JsonElement rhsNode, Type type, out object result)
        {
            if (PrimitiveJsonTypes.Contains(type))
            {
                var method = typeof(JsonElement).GetMethod(nameof(JsonElement.des));
                method = method.MakeGenericMethod(type);

                try
                {
                    result = method.Invoke(rhsNode, null);
                    return true;
                }
                catch { }
            }
            else if (property.Values != null)
            {
                var valueStr = rhsNode.GetString();
                rhs = property.Values.OfType<object>().FirstOrDefault(value => value.ToString() == valueStr);
            }

            if (rhs != null)
            {

            }
        }*/
    }

    public class PropertyConverter : JsonConverter<Property>
    {
        public IEnumerable<Property> Properties { get; }

        public PropertyConverter()
        {
            var types = new Type[]
            {
                typeof(Media),
                typeof(Movie),
                typeof(TVShow),
            };

            var properties = new List<Property>
            {
                CollectionViewModel.People,
                CollectionViewModel.MonetizationType,
                TMDB.SCORE
            };

            foreach (var type in types)
            {
                foreach (var field in type.GetFields())//System.Reflection.BindingFlags.Static))
                {
                    var value = field.GetValue(null);

                    if (value is Property temp)
                    {
                        properties.Add(temp);
                    }
                }

                /*foreach (var property in type.GetProperties())
                {
                    Print.Log(property);
                    list.Add(new ReflectedProperty(property));
                }*/
            }

            Properties = properties;
        }

        public override bool CanConvert(Type typeToConvert) => typeof(Property).IsAssignableFrom(typeToConvert);

        public override Property Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return Properties.FirstOrDefault(property => property.Name == value);
        }

        public override void Write(Utf8JsonWriter writer, Property value, JsonSerializerOptions options) => writer.WriteStringValue(value.Name);
    }

    public class PersonConverter : JsonConverter<PersonViewModel>
    {
        public override PersonViewModel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var root = JsonDocument.ParseValue(ref reader).RootElement;

            if (root.TryGetValue(out int id, "id") && root.TryGetValue(out string name, "name"))
            {
                var year = root.TryGetValue(out int temp, "year") ? temp : (int?)null;
                return new PersonViewModel(new Person(name, year).WithID(TMDB.IDKey, id));
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, PersonViewModel value, JsonSerializerOptions options) => writer.WriteRawValue(JsonSerializer.Serialize(new
        {
            id = (value.Item as Person).TryGetID(TMDB.ID, out var id) ? id : -1,
            name = (value.Item as Person).Name,
            year = (value.Item as Person).BirthYear,
        }, options));
    }
}