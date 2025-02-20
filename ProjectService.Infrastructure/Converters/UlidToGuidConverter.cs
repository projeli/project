using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ProjectService.Infrastructure.Converters;

public class UlidToGuidConverter : ValueConverter<Ulid, Guid>
{
    private static readonly ConverterMappingHints DefaultHints = new(size: 16);

    public UlidToGuidConverter() : this(null)
    {
    }
    
    public UlidToGuidConverter(ConverterMappingHints? mappingHints)
        : base(
            convertToProviderExpression: x => x.ToGuid(),
            convertFromProviderExpression: x => new Ulid(x),
            mappingHints: DefaultHints.With(mappingHints))
    {
    }
}