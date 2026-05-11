using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace KorenResourcePack
{
    internal static class Updater
    {
        private const string UpdateApiUrl =
            "https://api.github.com/repos/kkorenn/KorenResourcePack/releases/latest";

        private static bool updateAvailable;
        private static string latestVersion;
        private static string currentVersion;
        private static string downloadUrl;
        private static bool showUpdatePopup;

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

                currentVersion = modEntry.Info.Version;

                if (!IsNewerVersion(currentVersion, tag))
                    return;

                foreach (var a in (JArray)obj["assets"])
                {
                    string url = a["browser_download_url"]?.ToString();

                    if (!string.IsNullOrEmpty(url) &&
                        url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        latestVersion = tag;
                        downloadUrl = url;
                        updateAvailable = true;
                        showUpdatePopup = true;

                        modEntry.Logger.Log("[Update] New version found: " + tag);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Update] Check failed: " + ex.Message);
            }
        }

        // ---------------- UI ----------------

        internal static void DrawUpdatePopup(UnityModManager.ModEntry modEntry)
        {
            if (!showUpdatePopup || !updateAvailable) return;

            GUILayout.BeginArea(
                new Rect(Screen.width / 2 - 200, Screen.height / 2 - 150, 400, 200),
                GUI.skin.box
            );

            GUILayout.Label($"Update Available!\n{currentVersion} → {latestVersion}");

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Install & Restart", GUILayout.Height(40)))
                InstallUpdate(modEntry, true);

            if (GUILayout.Button("Install", GUILayout.Height(40)))
                InstallUpdate(modEntry, false);

            if (GUILayout.Button("Ignore", GUILayout.Height(40)))
                showUpdatePopup = false;

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // ---------------- INSTALL ----------------

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

                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, true);

                ZipFile.ExtractToDirectory(tmpZip, extractDir);

                string root = extractDir;

                // GitHub zips often extract into a single subfolder
                string[] subdirs = Directory.GetDirectories(root);  
                if (subdirs.Length == 1 && Directory.GetFiles(root).Length == 0)
                    root = subdirs[0];

                DirectoryCopy(root, modEntry.Path);

                File.Delete(tmpZip);
                Directory.Delete(extractDir, true);

                modEntry.Logger.Log("[Update] Installed " + latestVersion);

                if (restart)
                    RestartGame();
                else
                    showUpdatePopup = false;
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Update] Install failed: " + ex.Message);
            }
        }
        // ---------------- RESTART ----------------

        private static void RestartGame()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });

                Process.GetCurrentProcess().Kill();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("[Update] Restart failed: " + ex.Message);
            }
        }

        // ---------------- HELPERS ----------------

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