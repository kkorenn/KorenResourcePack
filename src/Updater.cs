using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private const string UpdateApiUrl = "https://api.github.com/repos/kkorenn/KorenResourcePack/releases/latest";

        private static bool updateAvailable;
        private static string latestVersion;
        private static string currentVersion;
        private static string downloadUrl;
        private static bool showUpdatePopup;

        private static void CheckForUpdates(UnityModManager.ModEntry modEntry)
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
                {
                    json = r.ReadToEnd();
                }

                JObject obj = JObject.Parse(json);
                string tag = obj["tag_name"]?.ToString();
                if (string.IsNullOrEmpty(tag)) return;

                currentVersion = modEntry.Info.Version;
                if (!IsNewerVersion(currentVersion, tag)) return;

                JArray assets = (JArray)obj["assets"];
                foreach (var a in assets)
                {
                    string url = a["browser_download_url"]?.ToString();
                    if (url != null && url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        latestVersion = tag;
                        downloadUrl = url;
                        updateAvailable = true;
                        showUpdatePopup = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                mod?.Logger?.Log("[Update] Check failed: " + ex.Message);
            }
        }

        private static void DrawUpdatePopup(UnityModManager.ModEntry modEntry)
        {
            if (!showUpdatePopup || !updateAvailable) return;

            GUILayout.BeginArea(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 100, 400, 200), GUI.skin.box);

            GUILayout.Label($"Update Available!\n{currentVersion} → {latestVersion}");

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Install & Restart", GUILayout.Height(40)))
            {
                InstallUpdate(modEntry, true);
            }

            if (GUILayout.Button("Install", GUILayout.Height(40)))
            {
                InstallUpdate(modEntry, false);
            }

            if (GUILayout.Button("Don't Update", GUILayout.Height(40)))
            {
                showUpdatePopup = false;
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private static void InstallUpdate(UnityModManager.ModEntry modEntry, bool restart)
        {
            try
            {
                modEntry.Logger.Log("[Update] Downloading " + latestVersion);

                string tmpZip = Path.Combine(Path.GetTempPath(), "krp_update.zip");

                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "KorenResourcePack-Updater");
                    wc.DownloadFile(downloadUrl, tmpZip);
                }

                string extractDir = Path.Combine(Path.GetTempPath(), "krp_extract");
                if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                ZipFile.ExtractToDirectory(tmpZip, extractDir);

                string srcDll = FindFile(extractDir, "KorenResourcePack.dll");
                if (srcDll == null) throw new Exception("DLL not found in update.");

                string srcRoot = Path.GetDirectoryName(srcDll);

                string backupDir = Path.Combine(modEntry.Path, "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                DirectoryCopy(modEntry.Path, backupDir);

                DirectoryCopy(srcRoot, modEntry.Path);

                File.Delete(tmpZip);
                Directory.Delete(extractDir, true);

                modEntry.Logger.Log("[Update] Installed " + latestVersion);

                if (restart)
                {
                    Application.Quit();
                }
                else
                {
                    showUpdatePopup = false;
                }
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Update] Install failed: " + ex.Message);
            }
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

        private static string FindFile(string root, string name)
        {
            foreach (string f in Directory.GetFiles(root, name, SearchOption.AllDirectories))
                return f;
            return null;
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
