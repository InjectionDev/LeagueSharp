using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DevCommom
{
    public class AssemblyUtil
    {

        public delegate void OnGetVersionCompleted(OnGetVersionCompletedArgs args);
        public event OnGetVersionCompleted onGetVersionCompleted;

        WebRequest webRequest;

        string AssemblyName;

        public AssemblyUtil(string pAssemblyName)
        {
            this.AssemblyName = pAssemblyName;
        }

        public void GetLastVersionAsync()
        {
            var urlBase = string.Format(@"https://raw.githubusercontent.com/InjectionDev/LeagueSharp/master/{0}/Properties/AssemblyInfo.cs", this.AssemblyName);

            this.webRequest = WebRequest.Create(urlBase);
            this.webRequest.BeginGetResponse(new AsyncCallback(FinishWebRequest), null);
        }

        void FinishWebRequest(IAsyncResult result)
        {
            var webResponse = webRequest.EndGetResponse(result);
            var body = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();

            var response = body;
            var currentVersion = response.Remove(0, response.LastIndexOf("AssemblyVersion"));
            currentVersion = currentVersion.Substring(currentVersion.IndexOf("\"") + 1);
            currentVersion = currentVersion.Substring(0, currentVersion.IndexOf("\""));

            if (onGetVersionCompleted != null)
            {
                OnGetVersionCompletedArgs versionCompletedArgs = new OnGetVersionCompletedArgs();
                versionCompletedArgs.CurrentVersion = currentVersion;
                versionCompletedArgs.IsSuccess = true;
                versionCompletedArgs.AssemblyName = this.AssemblyName;

                onGetVersionCompleted(versionCompletedArgs);
            }
        }



    }

    public class OnGetVersionCompletedArgs : EventArgs
    {
        public bool IsSuccess;
        public string CurrentVersion;
        public string AssemblyName;
    }
}
