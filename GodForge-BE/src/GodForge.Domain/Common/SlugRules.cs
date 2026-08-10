namespace GodForge.Domain.Common;

internal static class SlugRules
{
    public static bool IsValid(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength)
            return false;

        var previousWasHyphen = true;

        foreach (var c in value)
        {
            if (c == '-')
            {
                if (previousWasHyphen)
                    return false;

                previousWasHyphen = true;
                continue;
            }

            if ((c < 'a' || c > 'z') &&
                (c < '0' || c > '9'))
            {
                return false;
            }

            previousWasHyphen = false;
        }

        return !previousWasHyphen;
    }
}
