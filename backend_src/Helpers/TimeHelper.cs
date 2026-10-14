using System;

namespace EnterprisePOS
{
    public static class TimeHelper
    {
        public static DateTime PKT 
        {
            get 
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
        }
    }
}
