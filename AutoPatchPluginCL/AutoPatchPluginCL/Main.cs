using AutoPatchPluginCL.Models;
using MetroFramework;
using Newtonsoft.Json;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace AutoPatch
{
    public partial class Main : MetroFramework.Forms.MetroForm
    {
        public Main()
        {
            InitializeComponent();
        }

        private AutoPatchConfig AutoPatchConfig = null;
        private PatchList PatchList = null;

        private void Main_Load(object sender, EventArgs e)
        {
            if (File.Exists("AutoPatchPluginCLConfig.json"))
            {
                AutoPatchConfig = JsonConvert.DeserializeObject<AutoPatchConfig>(File.ReadAllText("AutoPatchPluginCLConfig.json"));
            } else
            {
                AutoPatchConfig config = new AutoPatchConfig
                {
                    CurrentVersion = 0,
                    PatchListUrl = "http://localhost/PatchList.json"
                };
                File.WriteAllText("AutoPatchPluginCLConfig.json", JsonConvert.SerializeObject(config, Formatting.Indented));
                AutoPatchConfig = config;
            }
            lblCurrentVer.Text = "Current version: " + AutoPatchConfig.CurrentVersion.ToString();
            WebClient client = new WebClient();
            if (ValidURL(AutoPatchConfig.PatchListUrl))
            {
                // Download the patchlist from specified url
                client.DownloadFile(AutoPatchConfig.PatchListUrl, "PatchList.json");
                Uri PatchListURI = new Uri(AutoPatchConfig.PatchListUrl);
                PatchList = JsonConvert.DeserializeObject<PatchList>(File.ReadAllText("PatchList.json"));
                if (PatchList.CurrentVersion > AutoPatchConfig.CurrentVersion)
                {
                    List<Patch> patchs = PatchList.Paths.Where(x => x.Version > AutoPatchConfig.CurrentVersion).OrderBy(x => x.Version).ToList();
                    List<string> FilesToClear = new List<string>();
                    foreach (Patch p in patchs)
                    {
                        string fileNameRar = "Patch" + p.Version + ".rar";
                        client.DownloadFile(PatchListURI.Scheme + "://" + PatchListURI.Host + "/" + p.RelativeURL, fileNameRar);
                        if (File.Exists(fileNameRar))
                        {
                            var archive = ArchiveFactory.Open(fileNameRar);
                            foreach (var entry in archive.Entries)
                            {
                                if (!entry.IsDirectory)
                                {
                                    entry.WriteToDirectory(Environment.CurrentDirectory, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
                                }
                            }
                            ApplyNewVersionAndReloadVersion();
                            FilesToClear.Add(fileNameRar);
                            archive.Dispose();
                        }
                    }
                    foreach (string filename in FilesToClear)
                    {
                        File.Delete(filename);
                    }
                    Environment.Exit(0);
                }
            } else
            {
                MetroMessageBox.Show(this, "Invalid URL of Path List", "Auto Patch for ConquerLoader", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        private void ApplyNewVersionAndReloadVersion()
        {
            AutoPatchConfig.CurrentVersion++;
            File.WriteAllText("AutoPatchPluginCLConfig.json", JsonConvert.SerializeObject(AutoPatchConfig, Formatting.Indented));
            lblCurrentVer.Text = "Current version: " + AutoPatchConfig.CurrentVersion.ToString();
        }

        private bool ValidURL(string URI)
        {
            return Uri.TryCreate(URI, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
