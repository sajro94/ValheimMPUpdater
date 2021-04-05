using System;
using System.Net;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using System.Text.Json;
using System.Reflection;

namespace ValheimMPUpdater
{
    class MpUpdater
    {
        public MpUpdater()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
        }

        public void Run()
        {
            const string GITHUB_API = "https://api.github.com/repos/{0}/{1}/releases/latest";
            WebClient webClient = new WebClient();
            // Added user agent
            webClient.Headers.Add("User-Agent", "Sajro94's Valheim ModPack Updater");
            Uri uri = new Uri(string.Format(GITHUB_API, "sajro94", "ValheimModPack"));
            string releases = webClient.DownloadString(uri);
            JsonDocument json = JsonDocument.Parse(releases);
            var test = json.RootElement;
            Console.WriteLine("Finding newest release");
            string downloadUrl = handleRelease(test);

            if(downloadUrl != "")
            {
                Console.WriteLine("Newest Release Found");
                var downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ValheimModHandler");
                var extractedPath = Path.Combine(downloadDir, "Extracted");
                var downloadPath = Path.Combine(downloadDir, "DownloadedModpack.zip");
                Console.WriteLine("Downloading Newest Version");
                webClient.DownloadFile(downloadUrl, downloadPath);
                Console.WriteLine("Newest Version Downloaded");
                Directory.Delete(extractedPath, true);
                Console.WriteLine("Extracting Contents");
                ZipFile.ExtractToDirectory(downloadPath, extractedPath);
                Console.WriteLine("Contents Extracted");
                Console.WriteLine("Moving Contents to Valheim game folder");
                var clientPath = Path.Combine(extractedPath, "ClientSideOnly");
                var universalPath = Path.Combine(extractedPath, "Universal");
                const string foldersPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 892970";
                var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var subKey = baseKey.OpenSubKey(foldersPath);
                var valpath = Path.Combine((string)subKey.GetValue("InstallLocation"));
                DirectoryCopy(clientPath, valpath, true);
                DirectoryCopy(universalPath, valpath, true);
                Console.WriteLine("All files and folders have been moved.");
                Console.WriteLine("Finished Updating Mods");
            }
            else
            {
                Console.WriteLine("Could not find newest release");
                Console.WriteLine("Update Aborted");
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private string handleRelease(JsonElement release)
        {
            foreach (var item in release.EnumerateObject())
            {
                if (item.Name == "assets")
                {
                    return handleAssets(item.Value);
                }
            }
            return "";
        }

        private string handleAssets(JsonElement assets)
        {
            foreach (var element in assets.EnumerateArray())
            {
                foreach (var item in element.EnumerateObject())
                {
                    if (item.Name == "browser_download_url")
                    {
                        return item.Value.GetString();
                    }

                }
            }
            return "";
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}
