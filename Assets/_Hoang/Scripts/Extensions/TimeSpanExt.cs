using System;
using System.Text;

public static class TimeSpanExt
{
    public static string SecondsToString_MS(this int seconds)
    {
        StringBuilder builder = new StringBuilder();

        int minutes = seconds / 60;
        seconds -= minutes * 60;

        if (minutes > 0)
        {
            if (minutes < 10) builder.Append("0");
            builder.Append($"{minutes}:");
        }

        if (seconds < 10) builder.Append("0");       
        builder.Append($"{seconds}");
        return builder.ToString();
    }
    public static string SecondsToString_HMS(this int seconds)
    {
        int hours = seconds / 3600;
        seconds -= hours * 3600;
        int minutes = seconds / 60;
        seconds -= minutes * 60;
        StringBuilder builder = new StringBuilder();
        if (hours > 0) builder.Append($"{hours}h");
        if (minutes > 0) builder.Append($"{minutes}m");
        if (seconds > 0) builder.Append($"{seconds}s");
        return builder.ToString();
    }

    public static string SecondsToString_DH_HMS(this int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);

        if (seconds < 60)
        {
            return "0:" + seconds.ToString("00");
        }
        else if (seconds < 3600)
        {
            if (ts.Minutes < 10)
            {
                return string.Format("{0:D1}:{1:D2}", ts.Minutes, ts.Seconds);
            }
            return string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
        }
        else if (seconds < 3600 * 24)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", ts.Hours, ts.Minutes, ts.Seconds);
        }
        else
        {
            return string.Format("{0}d {1}h", ts.Days, ts.Hours);
        }
    }

    public static string ToString_DH_HMS(this TimeSpan t)
    {
        if (t.TotalSeconds < 60)
        {
            return t.Seconds.ToString("00") + "s";
        }
        else if (t.TotalSeconds < 3600)
        {
            return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }
        else if (t.TotalSeconds < 3600 * 24)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
        }
        else
        {
            return string.Format("{0}d {1}h", t.Days, t.Hours);
        }
    }

    public static bool TryConvertToSeconds(this TimeSpan timeSpan, out int seconds)
    {
        double totalSeconds = timeSpan.TotalSeconds;
        if (totalSeconds > int.MaxValue || totalSeconds < int.MinValue)
        {
            seconds = 0;
            return false;
        }
        seconds = (int)totalSeconds;
        return true;
    }
}
