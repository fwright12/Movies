using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Movies
{
    public class LazyJson
    {
        public IReadOnlyDictionary<string, ArraySegment<byte>> Children => _Children;
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

        private byte[] Bytes;
        private HashSet<string> Properties;
        private Dictionary<string, ArraySegment<byte>> _Children = new Dictionary<string, ArraySegment<byte>>();

        private (JsonReaderState ReaderState, long Offset) State;
        private List<ArraySegment<byte>> Parts { get; }

        public LazyJson(byte[] bytes, IEnumerable<string> properties)
        {
            Bytes = bytes;
            Properties = properties.ToHashSet();
            Parts = new List<ArraySegment<byte>> { Bytes };
            State = default;
        }

        public bool TryGetValue(string propertyName, out ArraySegment<byte> value)
        {
            if (Children.TryGetValue(propertyName, out value))
            {
                return true;
            }
            else if (State.Offset >= Bytes.Length)
            {
                value = default;
                return false;
            }

            var reader = new Utf8JsonReader(new ArraySegment<byte>(Bytes).Slice((int)State.Offset), true, State.ReaderState);
            value = default;
            long lastBytesConsumed = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var property = reader.GetString();
                    var propertyNameLength = (int)(reader.BytesConsumed - lastBytesConsumed);
                    lastBytesConsumed = reader.BytesConsumed;

                    reader.Skip();

                    if (Properties.Contains(property))
                    {
                        var last = Parts[Parts.Count - 1];

                        var objectLength = (int)(reader.BytesConsumed - lastBytesConsumed);
                        var offset = (int)(reader.BytesConsumed + State.Offset - objectLength - propertyNameLength - last.Offset);

                        if (offset > 0)
                        {
                            Parts.Insert(Parts.Count - 1, last.Slice(0, offset));
                        }

                        value = last.Slice(offset += propertyNameLength, objectLength);

                        Parts[Parts.Count - 1] = last.Slice(offset += objectLength);

                        _Children.Add(property, value);

                        if (property == propertyName)
                        {
                            State = (reader.CurrentState, reader.BytesConsumed);
                            return true;
                        }
                    }
                }

                lastBytesConsumed = reader.BytesConsumed;
            }

            if (Parts.Count == 1)
            {
                value = Parts[0];
            }
            else
            {
                value = Encoding.UTF8.GetBytes(string.Join("", Parts.Select(part => Encoding.UTF8.GetString(part))));
            }

            //var temp = Parts.Select(part => System.Text.Encoding.UTF8.GetString(part)).ToList();
            //var sdfasfdasdfsa = Encoding.UTF8.GetString(value);

            _Children.Add("", value);
            State = (reader.CurrentState, reader.BytesConsumed);
            return propertyName == "";
        }
    }
}
