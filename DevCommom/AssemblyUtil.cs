using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Net;
using System.IO;
using System.Reflection;

namespace DevCommom
{
    public class AssemblyUtil
    {
        private OnGetVersionCompletedArgs versionCompletedArgs;

        public delegate void OnGetVersionCompleted(OnGetVersionCompletedArgs args);
        public event OnGetVersionCompleted onGetVersionCompleted;

        private WebRequest webRequest;
        private WebRequest webRequestCommom;

        private string AssemblyName;


        public AssemblyUtil(string pAssemblyName)
        {
            this.AssemblyName = pAssemblyName;

            versionCompletedArgs = new OnGetVersionCompletedArgs();
        }
        
        public void GetLastVersionAsync()
        {
            var urlBase = string.Format(@"https://raw.githubusercontent.com/InjectionDev/LeagueSharp/master/{0}/Properties/AssemblyInfo.cs", this.AssemblyName);
            this.webRequest = WebRequest.Create(urlBase);
            this.webRequest.BeginGetResponse(new AsyncCallback(FinishWebRequest), null);

            var urlBaseCommom = string.Format(@"https://raw.githubusercontent.com/InjectionDev/LeagueSharp/master/DevCommom/Properties/AssemblyInfo.cs");
            this.webRequestCommom = WebRequest.Create(urlBase);
            this.webRequestCommom.BeginGetResponse(new AsyncCallback(FinishWebRequestCommom), null);
        }

        void FinishWebRequest(IAsyncResult result)
        {
            try
            {
                var webResponse = this.webRequest.EndGetResponse(result);
                var body = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();

                if (onGetVersionCompleted != null)
                {
                    versionCompletedArgs.LastAssemblyVersion = this.GetVersionFromAssemblyInfo(body);
                    onGetVersionCompleted(versionCompletedArgs);
                }
            }
            catch { }
        }

        void FinishWebRequestCommom(IAsyncResult result)
        {
            try
            {
                var webResponse = this.webRequest.EndGetResponse(result);
                var body = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();

                versionCompletedArgs.LastCommomVersion = this.GetVersionFromAssemblyInfo(body);
                versionCompletedArgs.CurrentCommomVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (Game.Mode == GameMode.Running && versionCompletedArgs.CurrentCommomVersion != versionCompletedArgs.LastCommomVersion)
                    Game.PrintChat(string.Format("<font color='#fb762d'>DevCommom Library NEW VERSION available! Please Update while NOT INGAME! {0}</font>", versionCompletedArgs.LastCommomVersion));
            }
            catch { }
        }

        private string GetVersionFromAssemblyInfo(string body)
        {
            var currentVersion = body.Remove(0, body.LastIndexOf("AssemblyVersion"));
            currentVersion = currentVersion.Substring(currentVersion.IndexOf("\"") + 1);
            currentVersion = currentVersion.Substring(0, currentVersion.IndexOf("\""));

            return currentVersion;
        }

    }

    public class OnGetVersionCompletedArgs : EventArgs
    {
        public string LastAssemblyVersion;
        public string LastCommomVersion;

        public string CurrentCommomVersion;
    }
}
