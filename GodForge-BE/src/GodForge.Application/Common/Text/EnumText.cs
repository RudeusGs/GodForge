namespace GodForge.Application.Common.Text;

public static class EnumText
{
    public static bool TryParseDefined<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);
    }

    public static string ToCamelCase<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var text = value.ToString();
        return text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];
    }

    public static string ToSnakeCase<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var text = value.ToString();
        if (text.Length == 0)
            return text;

        var builder = new System.Text.StringBuilder(text.Length + 4);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (index > 0 && char.IsUpper(character))
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
