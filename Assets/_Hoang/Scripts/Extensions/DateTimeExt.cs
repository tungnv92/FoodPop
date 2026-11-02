using System;

public static class DateTimeExt
{
    public static DateTime ToDateTime(this string str)
    {
        if (string.IsNullOrEmpty(str) || !DateTime.TryParse(str, out DateTime dateTime))
        {
            dateTime = DateTime.MinValue;
        }

        return dateTime;
    }
}
