using AutoPatchPluginCL.Models;
using MetroFramework;
using Newtonsoft.Json;
using System;
using System.IO;
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

        private void Main_Load(object sender, EventArgs e)
        {
            // TEST ONLY
            PatchList pl = new PatchList
            {
                CurrentVersion = 1
            };
            pl.Paths.Add(new Patch() { RelativeURL = "patch1.rar", Version = 1 });
            File.WriteAllText("PatchList.json", JsonConvert.SerializeObject(pl, Formatting.Indented));
            //

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
                PatchList = JsonConvert.DeserializeObject<PatchList>(File.ReadAllText("PatchList.json"));
                //foreach(string str in PatchList.Paths)
                //{

                //}
                //int CurrentVersionClient = client.DownloadFile();
            } else
            {
                MetroMessageBox.Show(this, "Invalid URL of Path List", "Auto Patch for ConquerLoader", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
            }
        }

        private bool ValidURL(string URI)
        {
            return Uri.TryCreate(URI, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
