using net.jancerveny.sofaking.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace net.jancerveny.sofaking.BusinessLogic
{
    public static class TPBProxies
    {
        private static Proxy[] Proxies = new Proxy[] {
        };

        private static int LastProxyIndex { get; set; }

        public static void SetProxies(string[] proxies) {
            Proxies = proxies.Select(x => new Proxy(x)).ToArray();
        }

        public static string GetProxy()
        {
            // Set the oldest invalid proxies as valid, to make them avvailable for a retry
            for (var i = 0; i < Proxies.Length; i++)
            {
                if (!Proxies[i].Valid && Proxies[i].LastAttempt < DateTime.Now.AddMinutes(-15))
                {
                    Proxies[i].Valid = true;
                }
            }

            // Find a valid proxy to try
            int? lastProxyIndex = null;
            for(var i = 0; i < Proxies.Length; i++)
            {
                if(Proxies[i].Valid)
                {
                    lastProxyIndex = i;
                    break;
                }
            }

            if(lastProxyIndex == null)
            {
                throw new Exception("Valid proxies exhausted. Retry later.");
            }

            var proxy = Proxies[lastProxyIndex.Value];
            proxy.LastAttempt = DateTime.Now;
            proxy.NumberOfAttempts++;
            LastProxyIndex = lastProxyIndex.Value;
            return proxy.Host;
        }

        public static void ProxyInvalid()
        {
            Proxies[LastProxyIndex].Valid = false;
        }
    }
}
