using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projeli.ProjectService.Api.Converters;

public class JsonNumberEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(T));
        
        if (underlyingType == typeof(ulong))
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetUInt64(out var ulongValue))
                {
                    return (T)(object)ulongValue;
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (ulong.TryParse(reader.GetString(), out var ulongValue))
                {
                    return (T)(object)ulongValue;
                }
            }
        }
        else
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt64(out var longValue))
                {
                    return (T)Enum.ToObject(typeof(T), longValue);
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (long.TryParse(stringValue, out var longValue))
                {
                    return (T)Enum.ToObject(typeof(T), longValue);
                }
            }
        }
        
        throw new JsonException($"Unable to convert {reader.TokenType} to enum of type {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var underlyingType = Enum.GetUnderlyingType(typeof(T));
        if (underlyingType == typeof(ulong))
        {
            var ulongValue = (ulong)(object)value;
            writer.WriteStringValue(ulongValue.ToString());
        }
        else
        {
            writer.WriteNumberValue(Convert.ToInt64(value));
        }
    }
}