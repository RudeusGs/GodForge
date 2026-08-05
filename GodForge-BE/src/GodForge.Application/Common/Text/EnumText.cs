namespace GodForge.Application.Common.Text;

public static class EnumText
{
    public static string ToCamelCase<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var text = value.ToString();
        return text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];
    }
}
