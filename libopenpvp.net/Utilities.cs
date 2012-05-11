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
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - unixEpoch).TotalMilliseconds;
        }
    }
}
