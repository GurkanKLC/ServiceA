namespace ServiceB.Application.Services;
public static class ValueParser
{
    public static object ParseString(string value)
    {
        if (int.TryParse(value, out int intValue))
        {
            return intValue;
        }
        if (double.TryParse(value, out double doubleValue))
        {
            return doubleValue;
        }
        if (bool.TryParse(value, out bool boolValue))
        {
            return boolValue;
        }
        if (DateTime.TryParse(value, out DateTime dateTimeValue))
        {
            return dateTimeValue;
        }

        // Başka türler için ek kontroller eklenebilir
        return value; // Dönüştürülemiyorsa string olarak döndür
    }
}
