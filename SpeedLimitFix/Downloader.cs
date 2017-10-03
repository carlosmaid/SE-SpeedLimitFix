using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SpeedLimitFix
{
    public class Downloader
    {
        const string updateStatusUrl = @"https://raw.githubusercontent.com/krypt-lynx/SE-SpeedLimitFix/master/update.xml?r={0}"; // random value to punch IE caching

        public void StartUpdateCheckAsync()
        {
            Thread thread = new Thread(StartUpdateCheck);
            thread.IsBackground = true;
            thread.Start();
        }

        public void StartUpdateCheck()
        {
            WebRequest request = WebRequest.Create(new Uri(string.Format(updateStatusUrl, new Random().Next())));

            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
            try
            {
                var response = request.GetResponse();
                XmlDocument doc = new XmlDocument();
                doc.Load(response.GetResponseStream());
                DoUpdateCheckDone(doc);
            }
            catch
            {
                DoUpdateCheckDone(null);
            }
        }

        private void DoUpdateCheckDone(XmlDocument doc)
        {
            if (UpdateCheckDone != null)
            {
                Sandbox.MySandboxGame.Static.Invoke(() => {
                    UpdateCheckDone(doc);
                });
            }
        }

        public Action<XmlDocument> UpdateCheckDone = null;
    }

}
