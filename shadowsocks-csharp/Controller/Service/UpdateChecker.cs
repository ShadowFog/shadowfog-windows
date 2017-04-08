using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

/***********************************/
using System.Windows.Forms;
using System.IO.Compression;
/***********************************/

using Newtonsoft.Json.Linq;

using Shadowsocks.Model;
using Shadowsocks.Util;

namespace Shadowsocks.Controller
{
    /*************************************************  This UpdateChecker is for shadowfog  ******************************************************/
    /**********************************  The update checker for shadowsocks is rewritten for lastest release  *************************************/
    /************************************************************ Oct. 20th ***********************************************************************/

    public class UpdateChecker
    {
        //Use "releases/latest" instead of "releases";
        //private const string UpdateURL = "https://api.github.com/repos/shadowsocks/shadowsocks-windows/releases";
        //private const string UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36";
        private const string UpdateURLShadowFog = "https://api.github.com/repos/ShadowFog/shadowfog-windows/releases/latest";
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.101 Safari/537.36";

        private Configuration config;
        public bool NewVersionFound;
        public string LatestVersionNumber;
        public string LatestVersionSuffix;
        public string LatestVersionName;
        public string LatestVersionURL;
        public string LatestVersionLocalName;
        public event EventHandler CheckUpdateCompleted;

        public const string Version = "3.4.3";
        public const string ShadowFogVersion = "0.5.0";
        public const string ShadowFogSubVersion = ".7";

        private class CheckUpdateTimer : System.Timers.Timer
        {
            public Configuration config;

            public CheckUpdateTimer(int p) : base(p)
            {
            }
        }

        public void CheckUpdate(Configuration config, int delay)
        {
            CheckUpdateTimer timer = new CheckUpdateTimer(delay);
            timer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;
            timer.config = config;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckUpdateTimer timer = (CheckUpdateTimer)sender;
            Configuration config = timer.config;
            timer.Elapsed -= Timer_Elapsed;
            timer.Enabled = false;
            timer.Dispose();
            CheckUpdate(config);
        }

        public void CheckUpdate(Configuration config)
        {
            this.config = config;

            try
            {
                Logging.Debug("Checking updates...");
                WebClient http = CreateWebClient();
                http.DownloadStringCompleted += http_DownloadStringCompleted;
                //http.DownloadStringAsync(new Uri(UpdateURL));
                http.DownloadStringAsync(new Uri(UpdateURLShadowFog));
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        // modified from array processing to single Json obj;
        //private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        //{
        //    try
        //    {
        //        string response = e.Result;

        //        JArray result = JArray.Parse(response);

        //        List<Asset> asserts = new List<Asset>();
        //        if (result != null)
        //        {
        //            foreach (JObject release in result)
        //            {
        //                var isPreRelease = (bool) release["prerelease"];
        //                if (isPreRelease && !config.checkPreRelease)
        //                {
        //                    continue;
        //                }
        //                foreach (JObject asset in (JArray)release["assets"])
        //                {
        //                    Asset ass = Asset.ParseAsset(asset);
        //                    if (ass != null)
        //                    {
        //                        ass.prerelease = isPreRelease;
        //                        if (ass.IsNewVersion(Version, config.checkPreRelease))
        //                        {
        //                            asserts.Add(ass);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        if (asserts.Count != 0)
        //        {
        //            SortByVersions(asserts);
        //            Asset asset = asserts[asserts.Count - 1];
        //            NewVersionFound = true;
        //            LatestVersionURL = asset.browser_download_url;
        //            LatestVersionNumber = asset.version;
        //            LatestVersionName = asset.name;
        //            LatestVersionSuffix = asset.suffix == null ? "" : $"-{asset.suffix}";

        //            startDownload();
        //        }
        //        else
        //        {
        //            Logging.Debug("No update is available");
        //            if (CheckUpdateCompleted != null)
        //            {
        //                CheckUpdateCompleted(this, new EventArgs());
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logging.LogUsefulException(ex);
        //    }
        //}
        private void http_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                string response = e.Result;

                JObject release = JObject.Parse(response);
                Asset asset = new Asset();

                if (release != null)
                {
                    if ((bool)release["prerelease"])
                    {
                        return;
                    }
                    foreach (JObject assetObj in (JArray)release["assets"]) //release["assets"] is a JSON array with only one object...
                    {
                        asset.Parse(assetObj);
                    }
                }
                if (asset.IsNewVersion(ShadowFogVersion))
                {
                    NewVersionFound = true;
                    LatestVersionURL = asset.browser_download_url;
                    LatestVersionNumber = asset.version;
                    LatestVersionName = asset.name;

                    startDownload();
                }
                else
                {
                    Logging.Debug("No update is available");
                    if (CheckUpdateCompleted != null)
                    {
                        CheckUpdateCompleted(this, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        private void startDownload()
        {
            try
            {
                LatestVersionLocalName = Utils.GetTempPath(LatestVersionName);
                WebClient http = CreateWebClient();
                http.DownloadFileCompleted += Http_DownloadFileCompleted;
                http.DownloadFileAsync(new Uri(LatestVersionURL), LatestVersionLocalName);
            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        /**************************************************************************************************************/
        // this callback function add auto program replacing procedure
        /**************************************************************************************************************/
        private void Http_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                {
                    Logging.LogUsefulException(e.Error);
                    return;
                }
                Logging.Debug($"New version {LatestVersionNumber}{LatestVersionSuffix} found: {LatestVersionLocalName}");
                if (CheckUpdateCompleted != null)
                {
                    CheckUpdateCompleted(this, new EventArgs());
                }

                /**************************************************************************************************************/
                // dirty auto updater put here...
                string ShadowFogFullPath = Application.StartupPath + @"\ShadowFog.exe";
                string ShadowFogOld = Application.StartupPath + @"\ShadowFog.exe.old";
                System.IO.File.Move(ShadowFogFullPath, ShadowFogOld);
                ZipFile.ExtractToDirectory(LatestVersionLocalName, Application.StartupPath);
                /**************************************************************************************************************/

            }
            catch (Exception ex)
            {
                Logging.LogUsefulException(ex);
            }
        }

        private WebClient CreateWebClient()
        {
            WebClient http = new WebClient();
            http.Headers.Add("User-Agent", UserAgent);
            // The line below is not sure to enable: whether use proxy to update
            http.Proxy = new WebProxy(IPAddress.Loopback.ToString(), config.localPort);
            return http;
        }

        //private void SortByVersions(List<Asset> asserts)
        //{
        //    asserts.Sort();
        //}

        public class Asset : IComparable<Asset>
        {
            public bool prerelease;
            public string name;
            public string version;
            public string browser_download_url;
            public string suffix;

            public static Asset ParseAsset(JObject assertJObject)
            {
                var name = (string) assertJObject["name"];
                Match match = Regex.Match(name, @"^Shadowsocks-(?<version>\d+(?:\.\d+)*)(?:|-(?<suffix>.+))\.\w+$",
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string version = match.Groups["version"].Value;

                    var asset = new Asset
                    {
                        browser_download_url = (string) assertJObject["browser_download_url"],
                        name = name,
                        version = version
                    };

                    if (match.Groups["suffix"].Success)
                    {
                        asset.suffix = match.Groups["suffix"].Value;
                    }

                    return asset;
                }

                return null;
            }

            public bool IsNewVersion(string currentVersion, bool checkPreRelease)
            {
                if (prerelease && !checkPreRelease)
                {
                    return false;
                }
                if (version == null)
                {
                    return false;
                }
                var cmp = CompareVersion(version, currentVersion);
                return cmp > 0;
            }

            /**************************************************************************************************************/
            // Begins: some useful methods
            /**************************************************************************************************************/
            public bool IsNewVersion(string currentVersion)
            {
                if (prerelease)
                {
                    return false;
                }
                if (version == null)
                {
                    return false;
                }
                return CompareVersion(version, currentVersion) > 0;
            }

            public void Parse(JObject asset)
            {
                name = (string)asset["name"];
                browser_download_url = (string)asset["browser_download_url"];
                version = ParseVersionFromURL(browser_download_url);
                prerelease = browser_download_url.IndexOf("prerelease", StringComparison.Ordinal) >= 0;
            }

            private static string ParseVersionFromURL(string url)
            {
                Match match = Regex.Match(url, @".*Shadowfog-win.*?-([\d\.]+)\.\w+", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (match.Groups.Count == 2)
                    {
                        return match.Groups[1].Value;
                    }
                }
                return null;
            }
            /**************************************************************************************************************/
            // Ends: some useful methods
            /**************************************************************************************************************/

            public static int CompareVersion(string l, string r)
            {
                var ls = l.Split('.');
                var rs = r.Split('.');
                for (int i = 0; i < Math.Max(ls.Length, rs.Length); i++)
                {
                    int lp = (i < ls.Length) ? int.Parse(ls[i]) : 0;
                    int rp = (i < rs.Length) ? int.Parse(rs[i]) : 0;
                    if (lp != rp)
                    {
                        return lp - rp;
                    }
                }
                return 0;
            }

            public int CompareTo(Asset other)
            {
                return CompareVersion(version, other.version);
            }
        }
    }
}
