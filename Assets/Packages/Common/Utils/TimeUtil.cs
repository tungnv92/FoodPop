using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class TimeUtil
{
    const string sFormatDate = "yyyy-MM-dd HH:mm:ss";
    public static string TimeToString(double seconds)
    {
        seconds = Math.Max(0, seconds);
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalDays >= 1)
            return $"{t.Days:D1}d {t.Hours:D2}h";
        if (t.TotalHours >= 1)
            return $"{t.Hours:D2}h {t.Minutes:D2}m";
        if (t.TotalMinutes >= 1)
            return $"{t.Minutes:D2}:{t.Seconds:D2}";
        return $"00:{t.Seconds:D2}";
    }


    public static string TimeToNewDay(DateTime currentTime)
    {
        DateTime newDayTime = currentTime.Date.AddDays(1);
        // Tính khoảng thời gian còn lại
        TimeSpan timeLeft = newDayTime - currentTime;
        // Trả về chuỗi định dạng
        return $"{(int)timeLeft.TotalHours:D2}h {timeLeft.Minutes:D2}m {timeLeft.Seconds:D2}s";
    }
    public static string TimeToNewDayShort(DateTime currentTime)
    {
        DateTime newDayTime = currentTime.Date.AddDays(1);
        // Tính khoảng thời gian còn lại
        TimeSpan timeLeft = newDayTime - currentTime;
        // Trả về chuỗi định dạng
        if (timeLeft.TotalHours > 0)
            return $"{(int)timeLeft.TotalHours:D2}h {timeLeft.Minutes:D2}m";
        return $"{(int)timeLeft.Minutes:D2}m {timeLeft.Seconds:D2}s";
    }
    public static DateTime NormalizeDateTime(DateTime dateTime)
    {
        string formatted = dateTime.ToString(sFormatDate, CultureInfo.InvariantCulture);
        if (DateTime.TryParseExact(formatted, sFormatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return result;
        else
            return DateTime.Now; // fallback hợp lý
    }

    public static string DateTimeToString(DateTime dateTime)
    {
        return dateTime.ToString(sFormatDate, CultureInfo.InvariantCulture);
    }
    public static DateTime StringToDateTime(string sDateTime)
    {
        if (DateTime.TryParseExact(sDateTime, sFormatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return result;
        return DateTime.Now;
    }


    public static string GetTimeStringFromSecond(double time)
    {
        TimeSpan t = TimeSpan.FromSeconds(time);
        return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }
}