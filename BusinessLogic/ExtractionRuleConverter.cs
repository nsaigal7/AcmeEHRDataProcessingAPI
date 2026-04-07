using System.Text.Json;
using System.Text.Json.Serialization;
using AcmeEHRDataProcessingAPI.Models;

namespace AcmeEHRDataProcessingAPI.Converters;
#region Partially AI-generated
public class ExtractionRuleConverter : JsonConverter<ExtractionRule>
{
    public override ExtractionRule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
                return ExtractionRule.All();

            // Single resource type as a bare string
            return ExtractionRule.Some(value);
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var types = new List<string>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                    types.Add(reader.GetString()!);
            }
            return ExtractionRule.Some(types);
        }

        throw new JsonException($"Cannot deserialize ExtractionRule from token type {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, ExtractionRule value, JsonSerializerOptions options)
    {
        if (value.IsAll)
        {
            writer.WriteStringValue("all");
        }
        else
        {
            writer.WriteStartArray();
            foreach (var t in value.ResourceTypes ?? new())
                writer.WriteStringValue(t);
            writer.WriteEndArray();
        }
    }
#endregion
}