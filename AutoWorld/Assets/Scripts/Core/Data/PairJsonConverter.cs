using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AutoWorld.Core.Data
{
    public sealed class PairArrayJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            objectType.IsGenericType &&
            objectType.GetGenericTypeDefinition() == typeof(Pair<,>);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException("Pair must be a JSON array: [key, value]");

            var arr = JArray.Load(reader);
            if (arr.Count != 2)
                throw new JsonSerializationException("Pair array must have exactly 2 elements: [key, value]");

            var tArgs = objectType.GetGenericArguments();
            var tKey = tArgs[0];
            var tVal = tArgs[1];

            var k = arr[0].ToObject(tKey, serializer);
            var v = arr[1].ToObject(tVal, serializer);

            // 생성자 사용 (필드 세팅보다 안전)
            return Activator.CreateInstance(objectType, k, v);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = value.GetType();
            var key = type.GetField("key").GetValue(value);
            var val = type.GetField("value").GetValue(value);

            writer.WriteStartArray();
            serializer.Serialize(writer, key);
            serializer.Serialize(writer, val);
            writer.WriteEndArray();
        }
    }
}