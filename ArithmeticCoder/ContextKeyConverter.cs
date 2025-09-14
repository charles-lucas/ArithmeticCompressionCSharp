using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace ArithmeticCoder
{
    internal class ContextKeyConverter : JsonConverter<ContextKey>
    {
        public override ContextKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ContextKey.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, ContextKey contextKey, JsonSerializerOptions options)
        {
            //writer.WriteStringValue(contextKey.ToString());
            writer.WriteNumber("MaxLength", contextKey.MaxLength);
            writer.WriteStartArray();
            foreach(byte bite in contextKey.Key)
            {
                writer.WriteNumber("keypart", bite);
            }
            writer.WriteEndArray();
        }

        public override ContextKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ContextKey.Parse(reader.GetString());
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] ContextKey contextKey, JsonSerializerOptions options)
        {
            writer.WritePropertyName(contextKey.ToString());
        }
    }
}
