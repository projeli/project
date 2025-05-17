using System.Text.Json;
using System.Text.Json.Serialization;

namespace Projeli.ProjectService.Domain.Converters;

public class JsonStringFlagsEnumConverter<T> : JsonConverter<T> where T : Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var enumString = reader.GetString();
        if (enumString is null) return default!;

        var enumValues = enumString
            .Split(',')
            .Select(s => Enum.Parse(typeToConvert, s.Trim(), true))
            .Cast<T>();

        var combinedValue = enumValues.Cast<int>().Aggregate(0, (acc, val) => acc | val);
        return (T)Enum.ToObject(typeToConvert, combinedValue);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var enumType = typeof(T);
        var flagValues = Enum.GetValues(enumType)
            .Cast<T>()
            .Where(flag => value.HasFlag(flag) && Convert.ToInt32(flag) != 0)
            .Select(flag => flag.ToString())
            .ToArray();

        writer.WriteStartArray();
        foreach (var flag in flagValues)
        {
            writer.WriteStringValue(flag);
        }
        writer.WriteEndArray();
    }
}