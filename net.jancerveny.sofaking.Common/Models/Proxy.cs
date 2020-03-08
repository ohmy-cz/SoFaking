using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.Common.Models
{
    public class Proxy
    {
        public string Host { get; set; }
        public DateTime? LastAttempt { get; set; }
        public int NumberOfAttempts { get; set; }
        public bool Valid { get; set; }
        public Proxy(string host)
        {
            Host = host;
            LastAttempt = null;
            NumberOfAttempts = 0;
            Valid = true;
        }
    }
}
