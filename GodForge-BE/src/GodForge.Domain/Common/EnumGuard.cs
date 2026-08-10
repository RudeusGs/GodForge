namespace GodForge.Domain.Common;

internal static class EnumGuard
{
    public static void ThrowIfUndefined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"The value is not defined for {typeof(TEnum).Name}.");
        }
    }
}
