using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libopenpvp.net
{
    public static class Utilities
    {

        public static long CurrentTimeMillis()
        {
            return GetTimeMillis(DateTime.UtcNow);
        }

        public static long GetTimeMillis(DateTime time)
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(time - unixEpoch).TotalMilliseconds;
        }
    }
}
