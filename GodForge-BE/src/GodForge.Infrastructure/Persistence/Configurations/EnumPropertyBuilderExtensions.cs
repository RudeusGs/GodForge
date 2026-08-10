using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GodForge.Infrastructure.Persistence.Configurations;

internal static class EnumPropertyBuilderExtensions
{
    public static PropertyBuilder<TEnum> HasCamelCaseEnumConversion<TEnum>(
        this PropertyBuilder<TEnum> propertyBuilder)
        where TEnum : struct, Enum
        => propertyBuilder.HasConversion(new CamelCaseEnumConverter<TEnum>());

    private sealed class CamelCaseEnumConverter<TEnum> : ValueConverter<TEnum, string>
        where TEnum : struct, Enum
    {
        public CamelCaseEnumConverter()
            : base(
                value => ToCamelCase(value),
                value => Enum.Parse<TEnum>(value, true))
        {
        }

        private static string ToCamelCase(TEnum value)
        {
            var text = value.ToString();
            return text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];
        }
    }
}
