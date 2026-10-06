namespace Modules.System.Crm.Application.Normalization;

public static class CustomerMobileNormalizer
{
    public static string Normalize(string mobile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mobile);

        var value = mobile.Trim();
        var result = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (character == '+' && result.Length == 0)
            {
                result.Append(character);
                continue;
            }

            if (TryNormalizeDigit(character, out var digit))
                result.Append(digit);
        }

        var normalized = result.ToString();

        if (normalized.StartsWith("00", StringComparison.Ordinal))
            normalized = "+" + normalized[2..];

        return normalized;
    }

    private static bool TryNormalizeDigit(
        char character,
        out char digit)
    {
        if (character is >= '0' and <= '9')
        {
            digit = character;
            return true;
        }

        digit = character switch
        {
            '۰' => '0',
            '۱' => '1',
            '۲' => '2',
            '۳' => '3',
            '۴' => '4',
            '۵' => '5',
            '۶' => '6',
            '۷' => '7',
            '۸' => '8',
            '۹' => '9',
            '٠' => '0',
            '١' => '1',
            '٢' => '2',
            '٣' => '3',
            '٤' => '4',
            '٥' => '5',
            '٦' => '6',
            '٧' => '7',
            '٨' => '8',
            '٩' => '9',
            _ => default
        };

        return digit != default;
    }
}
