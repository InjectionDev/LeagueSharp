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
            var webResponse = this.webRequest.EndGetResponse(result);
            var body = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();

            if (onGetVersionCompleted != null && !string.IsNullOrEmpty(body))
            {
                versionCompletedArgs.LastAssemblyVersion = this.GetVersionFromAssemblyInfo(body);

                if (!string.IsNullOrEmpty(versionCompletedArgs.LastAssemblyVersion) && !string.IsNullOrEmpty(versionCompletedArgs.LastCommomVersion))
                    onGetVersionCompleted(versionCompletedArgs);
            }
        }

        void FinishWebRequestCommom(IAsyncResult result)
        {
            var webResponse = this.webRequest.EndGetResponse(result);
            var body = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();

            if (onGetVersionCompleted != null && !string.IsNullOrEmpty(body))
            {
                versionCompletedArgs.LastCommomVersion = this.GetVersionFromAssemblyInfo(body);
                versionCompletedArgs.CurrentCommomVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

                if (!string.IsNullOrEmpty(versionCompletedArgs.LastAssemblyVersion) && !string.IsNullOrEmpty(versionCompletedArgs.LastCommomVersion))
                    onGetVersionCompleted(versionCompletedArgs);
            }
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
