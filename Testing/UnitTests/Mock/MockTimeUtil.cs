using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace UnitTests.Mock;

[Injectable(TypeOverride = typeof(TimeUtil))]
public class MockTimeUtil : TimeUtil
{
    public const int OneHourAsSeconds = TimeUtil.OneHourAsSeconds;

    // No-deps constructor
    public MockTimeUtil() { }

    // Allow tests to control "now"; defaults to system UTC when not set
    public DateTime? UtcNowOverride { get; set; }
    public DateTimeOffset? UtcNowOffsetOverride { get; set; }

    private DateTime UtcNow => UtcNowOverride ?? DateTime.UtcNow;
    private DateTimeOffset UtcNowOffset => UtcNowOffsetOverride ?? DateTimeOffset.UtcNow;

    public string GetDate()
    {
        // Keep behavior consistent with server: YYYY-MM-DD
        return UtcNowOffset.ToString("yyyy-MM-dd");
    }

    public DateTime GetDateTimeNow()
    {
        return UtcNow;
    }

    public string GetTime()
    {
        // Keep behavior consistent with server: HH-MM-SS
        return UtcNowOffset.ToString("HH-mm-ss");
    }

    public long GetTimeStamp()
    {
        return UtcNowOffset.ToUnixTimeSeconds();
    }

    public long GetStartOfDayTimeStamp(long? timestamp)
    {
        var now = timestamp.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).DateTime : UtcNow;
        var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        return ((DateTimeOffset)startOfDay).ToUnixTimeMilliseconds();
    }

    public long GetTimeStampFromNowDays(int daysFromNow)
    {
        return UtcNowOffset.AddDays(daysFromNow).ToUnixTimeSeconds();
    }

    public long GetTimeStampFromNowHours(int hoursFromNow)
    {
        return UtcNowOffset.AddHours(hoursFromNow).ToUnixTimeSeconds();
    }

    public string GetBsgTimeMailFormat()
    {
        return UtcNowOffset.ToString("HH:mm");
    }

    public string GetBsgDateMailFormat()
    {
        return UtcNowOffset.ToString("dd.MM.yyyy");
    }

    public int GetHoursAsSeconds(int hours)
    {
        return OneHourAsSeconds * hours;
    }

    public long GetTimeStampOfNextHour()
    {
        var now = UtcNow;
        var timeUntilNextHour = TimeSpan
            .FromMinutes(60 - now.Minute)
            .Subtract(TimeSpan.FromSeconds(now.Second))
            .Subtract(TimeSpan.FromMilliseconds(now.Millisecond));
        return ((DateTimeOffset)now.Add(timeUntilNextHour)).ToUnixTimeSeconds();
    }

    public long GetTodayMidnightTimeStamp()
    {
        var now = UtcNow;
        var hours = now.Hour;
        var minutes = now.Minute;
        if (hours > 0 && minutes > 0)
        {
            hours--;
        }
        var lastFullHour = new DateTime(now.Year, now.Month, now.Day, hours, 0, 0, DateTimeKind.Utc);
        return ((DateTimeOffset)lastFullHour).ToUnixTimeSeconds();
    }

    public DateTime GetDateTimeFromTimeStamp(long timeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timeStamp).DateTime;
    }

    public DateTime GetUtcDateTimeFromTimeStamp(long timeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timeStamp).UtcDateTime;
    }

    public double GetMinutesAsSeconds(int minutes)
    {
        return minutes * 60;
    }
}
