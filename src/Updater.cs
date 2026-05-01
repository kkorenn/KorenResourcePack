using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using UnityModManagerNet;

namespace KorenResourcePack
{
    public static partial class Main
    {
        private const string UpdateApiUrl = "https://api.github.com/repos/kkorenn/KorenResourcePack/releases/latest";

        private static void CheckForUpdates(UnityModManager.ModEntry modEntry)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(UpdateApiUrl);
                req.UserAgent = "KorenResourcePack-Updater";
                req.Accept = "application/vnd.github+json";
                req.Timeout = 8000;

                string json;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader r = new StreamReader(resp.GetResponseStream()))
                {
                    json = r.ReadToEnd();
                }

                string latestTag = ExtractJsonString(json, "tag_name");
                if (string.IsNullOrEmpty(latestTag))
                {
                    modEntry.Logger.Log("[Update] No tag_name in release JSON.");
                    return;
                }

                string current = modEntry.Info.Version;
                if (!IsNewerVersion(current, latestTag))
                {
                    modEntry.Logger.Log("[Update] Up to date (" + current + ").");
                    return;
                }

                string zipUrl = ExtractAssetZipUrl(json);
                if (string.IsNullOrEmpty(zipUrl))
                {
                    modEntry.Logger.Log("[Update] " + latestTag + " available but no .zip asset found.");
                    return;
                }

                modEntry.Logger.Log("[Update] New version " + latestTag + " found. Downloading...");
                string tmpZip = Path.Combine(Path.GetTempPath(), "KorenResourcePack_update_" + Guid.NewGuid().ToString("N") + ".zip");
                HttpWebRequest dl = (HttpWebRequest)WebRequest.Create(zipUrl);
                dl.UserAgent = "KorenResourcePack-Updater";
                dl.Timeout = 30000;
                using (HttpWebResponse dlResp = (HttpWebResponse)dl.GetResponse())
                using (Stream s = dlResp.GetResponseStream())
                using (FileStream fs = File.Create(tmpZip))
                {
                    s.CopyTo(fs);
                }

                string tmpExtract = Path.Combine(Path.GetTempPath(), "KorenResourcePack_extract_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmpExtract);
                ZipFile.ExtractToDirectory(tmpZip, tmpExtract);

                string srcDll = FindFile(tmpExtract, "KorenResourcePack.dll");
                string srcInfo = FindFile(tmpExtract, "Info.json");
                if (srcDll == null || srcInfo == null)
                {
                    modEntry.Logger.Log("[Update] Zip missing dll or Info.json.");
                    return;
                }

                string srcRoot = Path.GetDirectoryName(srcDll);
                string modDir = modEntry.Path;
                CopyDirectoryOverwrite(srcRoot, modDir);

                try { File.Delete(tmpZip); } catch { }
                try { Directory.Delete(tmpExtract, true); } catch { }

                modEntry.Logger.Log("[Update] Installed " + latestTag + ". Restart game to apply.");
            }
            catch (Exception ex)
            {
                modEntry.Logger.Log("[Update] Failed: " + ex.Message);
            }
        }

        private static string ExtractJsonString(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string ExtractAssetZipUrl(string json)
        {
            foreach (Match m in Regex.Matches(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\""))
            {
                string url = m.Groups[1].Value;
                if (url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return url;
                }
            }
            return null;
        }

        private static bool IsNewerVersion(string current, string latestTag)
        {
            try
            {
                Version cur = ParseVersion(current);
                Version lat = ParseVersion(latestTag);
                return lat > cur;
            }
            catch
            {
                return !string.Equals(current, latestTag.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static Version ParseVersion(string v)
        {
            string s = (v ?? "").TrimStart('v', 'V');
            int dash = s.IndexOf('-');
            if (dash >= 0) s = s.Substring(0, dash);
            string[] parts = s.Split('.');
            int[] nums = { 0, 0, 0, 0 };
            for (int i = 0; i < parts.Length && i < 4; i++)
            {
                int.TryParse(parts[i], out nums[i]);
            }
            return new Version(nums[0], nums[1], nums[2], nums[3]);
        }

        private static string FindFile(string root, string name)
        {
            foreach (string path in Directory.GetFiles(root, name, SearchOption.AllDirectories))
            {
                return path;
            }
            return null;
        }

        private static void CopyDirectoryOverwrite(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (string file in Directory.GetFiles(src))
            {
                string name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(dst, name), true);
            }
            foreach (string dir in Directory.GetDirectories(src))
            {
                string name = Path.GetFileName(dir);
                CopyDirectoryOverwrite(dir, Path.Combine(dst, name));
            }
        }
    }
}
