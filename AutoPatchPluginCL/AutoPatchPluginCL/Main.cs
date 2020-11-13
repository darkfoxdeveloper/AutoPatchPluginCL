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
        private readonly WebClient client = new WebClient();
        private bool AnyPatchApplied = false;

        private void Main_Load(object sender, EventArgs e)
        {
            // Start Autopatcher

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
            if (ValidURL(AutoPatchConfig.PatchListUrl))
            {
                bgWorkerAutoPatch.DoWork += BgWorkerAutoPatch_DoWork;
                bgWorkerAutoPatch.ProgressChanged += BgWorkerAutoPatch_ProgressChanged;
                bgWorkerAutoPatch.RunWorkerCompleted += BgWorkerAutoPatch_RunWorkerCompleted;
                bgWorkerAutoPatch.RunWorkerAsync();
            } else
            {
                MetroMessageBox.Show(this, "Invalid URL of Path List", "Auto Patch for ConquerLoader", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        private void BgWorkerAutoPatch_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (AnyPatchApplied)
            {
                lblStatus.Text = "Patched client. You can launch now.";
            } else
            {
                lblStatus.Text = "All ready for launch.";
            }
            btnPlay.Enabled = true;
        }

        private void BgWorkerAutoPatch_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            lblCurrentVer.Text = "Current version: " + AutoPatchConfig.CurrentVersion.ToString();
            pBarProgress.Value = e.ProgressPercentage;
            lblStatus.Text = "Patching... " + e.ProgressPercentage + "%";
        }

        private void BgWorkerAutoPatch_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // Download the patchlist from specified url
            client.DownloadFile(AutoPatchConfig.PatchListUrl, "PatchList.json");
            Uri PatchListURI = new Uri(AutoPatchConfig.PatchListUrl);
            PatchList = JsonConvert.DeserializeObject<PatchList>(File.ReadAllText("PatchList.json"));
            if (PatchList.CurrentVersion > AutoPatchConfig.CurrentVersion)
            {
                List<Patch> patchs = PatchList.Paths.Where(x => x.Version > AutoPatchConfig.CurrentVersion).OrderBy(x => x.Version).ToList();
                List<string> FilesToClear = new List<string>();
                int xCount = 1;
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
                    decimal val = Decimal.Divide(xCount, patchs.Count) * 100;
                    if (val > 100)
                    {
                        val = 100;
                    }
                    xCount++;
                    AnyPatchApplied = xCount>1;
                    bgWorkerAutoPatch.ReportProgress((int)val);
                }
                foreach (string filename in FilesToClear)
                {
                    File.Delete(filename);
                }
            }
            client.Dispose();
        }

        private void ApplyNewVersionAndReloadVersion()
        {
            AutoPatchConfig.CurrentVersion++;
            File.WriteAllText("AutoPatchPluginCLConfig.json", JsonConvert.SerializeObject(AutoPatchConfig, Formatting.Indented));
        }

        private bool ValidURL(string URI)
        {
            return Uri.TryCreate(URI, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
