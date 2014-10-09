using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DevCommom
{
    public class AssemblyUtil
    {

        public delegate void OnGetVersionCompleted(OnGetVersionCompletedArgs args);
        public static event OnGetVersionCompleted onGetVersionCompleted;

        public AssemblyUtil()
        {

        }

        public void GetLastVersionAsync()
        {
            using (WebClient webClient = new WebClient())
            {
                var urlBase = string.Format(@"https://raw.githubusercontent.com/InjectionDev/LeagueSharp/master/{0}/Properties/AssemblyInfo.cs", Assembly.GetExecutingAssembly().GetName().Name);

                webClient.DownloadStringCompleted += webClient_DownloadStringCompleted;
                webClient.DownloadStringAsync(new Uri(urlBase));
            }
        }

        void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            var response = e.Result;
            var currentVersion = response.Remove(0, response.LastIndexOf("AssemblyVersion"));
            currentVersion = currentVersion.Substring(currentVersion.IndexOf("\"") + 1);
            currentVersion = currentVersion.Substring(0, currentVersion.IndexOf("\""));

            if (onGetVersionCompleted != null)
            {
                OnGetVersionCompletedArgs versionCompletedArgs = new OnGetVersionCompletedArgs();
                versionCompletedArgs.CurrentVersion = currentVersion;
                versionCompletedArgs.IsSuccess = e.Error == null;

                onGetVersionCompleted(versionCompletedArgs);
            }
        }

    }

    public class OnGetVersionCompletedArgs : EventArgs
    {
        public bool IsSuccess;
        public string CurrentVersion;
    }
}
