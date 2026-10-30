using System;

public class DateTimeUtils
{
    public static DateTime GetStartOfNextDayOfWeek(DayOfWeek dayOfWeek, DateTime dateTime)
    {
        DayOfWeek currentDay = dateTime.DayOfWeek;
        int daysUntilDayOfWeek = ((int)dayOfWeek - (int)currentDay + 7) % 7;
        daysUntilDayOfWeek = daysUntilDayOfWeek == 0 ? 7 : daysUntilDayOfWeek;
        DateTime nextDayOfWeek = dateTime.AddDays(daysUntilDayOfWeek);
        DateTime startOfNextDayOfWeek = new DateTime(nextDayOfWeek.Year, nextDayOfWeek.Month, nextDayOfWeek.Day, 0, 0, 0);
        return startOfNextDayOfWeek;
    }
}