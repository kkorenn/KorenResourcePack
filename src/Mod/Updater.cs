using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityModManagerNet;

namespace KorenResourcePack
{
    internal static class Updater
    {
        private const string UpdateApiUrl =
            "https://api.github.com/repos/kkorenn/KorenResourcePack/releases/latest";

        private static string latestVersion;
        private static string downloadUrl;
        private static string baseDisplayName;
        private static int installStarted;

        // ---------------- CHECK UPDATE ----------------

        internal static void CheckForUpdates(UnityModManager.ModEntry modEntry)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(UpdateApiUrl);
                req.UserAgent = "KorenResourcePack-Updater";
                req.Timeout = 8000;

                string json;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                    json = r.ReadToEnd();

                JObject obj = JObject.Parse(json);

                string tag = obj["tag_name"]?.ToString();
                if (string.IsNullOrEmpty(tag)) return;

                string currentVersion = modEntry.Info.Version;

                if (!IsNewerVersion(currentVersion, tag))
                    return;

                JArray assets = obj["assets"] as JArray;
                if (assets == null) return;

                foreach (var a in assets)
                {
                    string url = a["browser_download_url"]?.ToString();

                    if (!string.IsNullOrEmpty(url) &&
                        url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        latestVersion = tag;
                        downloadUrl = url;

                        modEntry.Logger.Log("[Update] New version found: " + tag);
                        InstallUpdate(modEntry);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Update] Check failed: " + ex.Message);
            }
        }

        // ---------------- INSTALL ----------------

        private static void InstallUpdate(UnityModManager.ModEntry modEntry)
        {
            if (Interlocked.Exchange(ref installStarted, 1) != 0) return;

            string tmpRoot = null;
            try
            {
                SetTitleStatus(modEntry, "<color=gray> [Downloading Update...]</color>");
                modEntry.Logger.Log("[Update] Downloading " + latestVersion);

                tmpRoot = Path.Combine(Path.GetTempPath(), "krp_update_" + Guid.NewGuid().ToString("N"));
                string tmpZip = Path.Combine(tmpRoot, "KorenResourcePack.zip");
                string extractDir = Path.Combine(tmpRoot, "extract");

                Directory.CreateDirectory(tmpRoot);

                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "KorenResourcePack-Updater");
                    wc.DownloadFile(downloadUrl, tmpZip);
                }

                SetTitleStatus(modEntry, "<color=gray> [Applying Update...]</color>");
                ZipFile.ExtractToDirectory(tmpZip, extractDir);

                string root = extractDir;

                // GitHub zips often extract into a single subfolder
                string[] subdirs = Directory.GetDirectories(root);  
                if (subdirs.Length == 1 && Directory.GetFiles(root).Length == 0)
                    root = subdirs[0];

                DirectoryCopy(root, modEntry.Path);

                Directory.Delete(tmpRoot, true);

                modEntry.Logger.Log("[Update] Installed " + latestVersion);
                SetTitleStatus(modEntry, "<color=green> [Update Downloaded. Restart]</color>");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Update] Install failed: " + ex.Message);
                SetTitleStatus(modEntry, "<color=red> [Update Failed]</color>");
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tmpRoot) && Directory.Exists(tmpRoot))
                        Directory.Delete(tmpRoot, true);
                }
                catch
                {
                }
            }
        }

        // ---------------- HELPERS ----------------

        private static void SetTitleStatus(UnityModManager.ModEntry modEntry, string postfix)
        {
            try
            {
                if (string.IsNullOrEmpty(baseDisplayName))
                    baseDisplayName = string.IsNullOrEmpty(modEntry.Info.DisplayName)
                        ? modEntry.Info.Id
                        : StripStatusPostfix(modEntry.Info.DisplayName);

                modEntry.Info.DisplayName = baseDisplayName + postfix;
            }
            catch
            {
            }
        }

        private static string StripStatusPostfix(string displayName)
        {
            int idx = displayName.IndexOf("<color=", StringComparison.Ordinal);
            if (idx >= 0) return displayName.Substring(0, idx).TrimEnd();
            return displayName;
        }

        private static void DirectoryCopy(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string dest = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dest = Path.Combine(destDir, Path.GetFileName(dir));
                DirectoryCopy(dir, dest);
            }
        }

        private static bool IsNewerVersion(string current, string latest)
        {
            try
            {
                Version c = new Version(current.TrimStart('v'));
                Version l = new Version(latest.TrimStart('v'));
                return l > c;
            }
            catch
            {
                return current != latest;
            }
        }
    }
}
