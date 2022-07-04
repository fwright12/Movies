using Movies.Models;
using Movies.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Movies
{
    public class PredicateConverter : JsonConverter<OperatorPredicate>
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
                var propertyConverter = options.Converters.OfType<PropertyConverter>().FirstOrDefault();

                if (propertyConverter == null)
                {
                    return null;
                }
                
                foreach (var property in propertyConverter.Properties.Where(property => property.Name == name))
                {
                    try
                    {
                        return new OperatorPredicate
                        {
                            LHS = property,
                            Operator = (Operators)op,
                            RHS = rhsNode.Deserialize(property.Type, options)
                        };
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

        public JsonSerializerOptions GetOptions(JsonSerializerOptions original, Func<JsonConverter, bool> predicate)
        {
            var options = new JsonSerializerOptions(original);

            for (int i = 0; i < options.Converters.Count; i++)
            {
                if (predicate(options.Converters[i]))
                {
                    options.Converters.RemoveAt(i--);
                }
            }

            return options;
        }

        public override void Write(Utf8JsonWriter writer, OperatorPredicate value, JsonSerializerOptions options) => writer.WriteRawValue(JsonSerializer.Serialize(value, GetOptions(options, option => !(option is PredicateConverter))));
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
                CollectionViewModel.MonetizationType
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
                return new PersonViewModel(App.DataManager, new Person(name, year).WithID(TMDB.IDKey, id));
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