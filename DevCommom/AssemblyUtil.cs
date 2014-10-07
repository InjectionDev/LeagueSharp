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
        public static bool IsLastVersion()
        {
            using (WebClient webClient = new WebClient())
            {
                var url = string.Format(@"https://raw.githubusercontent.com/InjectionDev/LeagueSharp/master/{0}/Properties/AssemblyInfo.cs", Assembly.GetExecutingAssembly().GetName());
                var response = webClient.DownloadString(url);

                if (response.Contains("AssemblyVersion"))
                {
                    var currentVersion = response.Remove(0, response.LastIndexOf("AssemblyVersion"));
                    currentVersion = currentVersion.Substring(currentVersion.IndexOf("\"") + 1);
                    currentVersion = currentVersion.Substring(0, currentVersion.IndexOf("\""));

                    return (Assembly.GetExecutingAssembly().GetName().Version.ToString() == currentVersion);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
