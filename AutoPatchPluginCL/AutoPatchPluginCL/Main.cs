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
using System.Windows.Forms;

namespace AutoPatch
{
    public partial class Main : MetroFramework.Forms.MetroForm
    {
        private AutoPatchConfig _AutoPatchConfig = null;
        private PatchList _PatchList = null;
        private bool AnyPatchApplied = false;
        private LayoutEditor _Editor;
        private readonly string _LayoutPath = Path.Combine("layout.json");
        private bool _EditorEnabled;
        private bool _PatchListGetOK = false;
        private DateTime _LastSpeedUpdate = DateTime.Now;
        private long _LastBytes = 0;
        private string _LastSpeedText = "";
        private long _TotalBytes = -1;
        private long _TotalBytesDone = 0; 

        public Main(string[] args)
        {
            InitializeComponent();
            _Editor = new LayoutEditor(this);
            _Editor.Enable(false);
            _Editor.SnapToGrid = true;
            _Editor.GridSize = 5;
            LoadLayout();
            if ((args.Length > 0 && args[0] == "--edit-layout"))// || Debugger.IsAttached)
            {
                var enable = !_EditorEnabled;
                _EditorEnabled = enable;
                _Editor.Enable(enable);
            }
        }
        private static string FormatSize(long bytes)
        {
            if (bytes < 0) return "???";
            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:F2} {units[unit]}";
        }

        private void LoadLayout()
        {
            var cfg = LayoutSerializer.Load(_LayoutPath);
            LayoutSerializer.Apply(this, cfg);
        }

        private void Main_Load(object sender, EventArgs e)
        {
            if (_EditorEnabled)
            {
                lblStatus.Text = "Edit mode ready. Right click to see options (Save, etc).";
                btnPlay.Enabled = true;
            } else
            {
                // Start Autopatcher
                if (File.Exists("AutoPatchPluginCLConfig.json"))
                {
                    _AutoPatchConfig = JsonConvert.DeserializeObject<AutoPatchConfig>(File.ReadAllText("AutoPatchPluginCLConfig.json"));
                }
                else
                {
                    AutoPatchConfig config = new AutoPatchConfig
                    {
                        CurrentVersion = 0,
                        PatchListUrl = "http://localhost/PatchList.json"
                    };
                    File.WriteAllText("AutoPatchPluginCLConfig.json", JsonConvert.SerializeObject(config, Formatting.Indented));
                    _AutoPatchConfig = config;
                }
                lblCurrentVer.Text = "Current version: " + _AutoPatchConfig.CurrentVersion.ToString();
                if (ValidURL(_AutoPatchConfig.PatchListUrl))
                {
                    bgWorkerAutoPatch.DoWork += BgWorkerAutoPatch_DoWork;
                    bgWorkerAutoPatch.ProgressChanged += BgWorkerAutoPatch_ProgressChanged;
                    bgWorkerAutoPatch.RunWorkerCompleted += BgWorkerAutoPatch_RunWorkerCompleted;
                    bgWorkerAutoPatch.RunWorkerAsync();
                }
                else
                {
                    MetroMessageBox.Show(this, "Invalid URL of Path List", "Auto Patch for ConquerLoader", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                }
            }
        }

        private void BgWorkerAutoPatch_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (AnyPatchApplied)
            {
                lblStatus.Text = "Patched client. You can launch now.";
            } else
            {
                if (_PatchListGetOK)
                {
                    lblStatus.Text = "All ready for launch. Client updated.";
                } else
                {
                    lblStatus.Text = $"Cannot get patch list or not existing patch files. [{_AutoPatchConfig.PatchListUrl}]";
                }
            }
            btnPlay.Enabled = true;
        }

        private void BgWorkerAutoPatch_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            lblCurrentVer.Text = "Current version: " + _AutoPatchConfig.CurrentVersion.ToString();
            pBarProgress.Value = Math.Max(0, Math.Min(100, e.ProgressPercentage));
            var info = e.UserState as PatchProgressInfo;
            if (info != null)
            {
                lblStatus.Text =
                    $"Patching... {e.ProgressPercentage}%  " +
                    $"{info.CurrentText}  " +
                    $"{info.TotalText}  " +
                    $"{info.SpeedText}";
            }
            else
            {
                lblStatus.Text = $"Patching... {e.ProgressPercentage}%";
            }
        }
        private void BgWorkerAutoPatch_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(_AutoPatchConfig.PatchListUrl, "PatchList.json");
                    var patchListUri = new Uri(_AutoPatchConfig.PatchListUrl);
                    _PatchList = JsonConvert.DeserializeObject<PatchList>(File.ReadAllText("PatchList.json"));
                    if (_PatchList.CurrentVersion <= _AutoPatchConfig.CurrentVersion)
                    {
                        _PatchListGetOK = true;
                        return;
                    }
                    var patchs = _PatchList.Paths
                        .Where(x => x.Version > _AutoPatchConfig.CurrentVersion)
                        .OrderBy(x => x.Version)
                        .ToList();

                    _TotalBytes = 0;
                    foreach (var p in patchs)
                    {
                        var patchUrl = $"{patchListUri.Scheme}://{patchListUri.Host}/{p.RelativeURL}";
                        try
                        {
                            var req = WebRequest.Create(patchUrl);
                            req.Method = "HEAD";
                            using (var resp = req.GetResponse())
                            {
                                if (resp.ContentLength > 0)
                                    _TotalBytes += resp.ContentLength;
                                else
                                    _TotalBytes = -1;
                            }
                        }
                        catch
                        {
                            _TotalBytes = -1;
                        }
                        if (_TotalBytes < 0) break;
                    }
                    _TotalBytesDone = 0;
                    var filesToClear = new List<string>();
                    for (int i = 0; i < patchs.Count; i++)
                    {
                        var p = patchs[i];
                        string fileNameRar = $"Patch{p.Version}.rar";
                        var url = $"{patchListUri.Scheme}://{patchListUri.Host}/{p.RelativeURL}";
                        _LastBytes = 0;
                        _LastSpeedUpdate = DateTime.Now;
                        _LastSpeedText = "";
                        long lastTotalBytesToReceive = -1;
                        using (var done = new System.Threading.ManualResetEvent(false))
                        {
                            DownloadProgressChangedEventHandler progressHandler = (s, ev) =>
                            {
                                lastTotalBytesToReceive = ev.TotalBytesToReceive;
                                double globalProgress = ((i * 100.0) + ev.ProgressPercentage) / patchs.Count;
                                var now = DateTime.Now;
                                var timeDiff = (now - _LastSpeedUpdate).TotalSeconds;
                                if (timeDiff >= 0.5)
                                {
                                    long bytesDiff = ev.BytesReceived - _LastBytes;
                                    double speed = (timeDiff > 0) ? (bytesDiff / timeDiff) : 0;
                                    _LastBytes = ev.BytesReceived;
                                    _LastSpeedUpdate = now;
                                    _LastSpeedText = (speed > 1024 * 1024)
                                        ? $"{speed / 1024 / 1024:F2} MB/s"
                                        : $"{speed / 1024:F1} KB/s";
                                }
                                string currentText = $"File: {FormatSize(ev.BytesReceived)} / {FormatSize(ev.TotalBytesToReceive)}";
                                long totalSoFar = (_TotalBytesDone >= 0 && ev.BytesReceived >= 0) ? (_TotalBytesDone + ev.BytesReceived) : -1;
                                string totalText = (_TotalBytes >= 0)
                                    ? $"Total: {FormatSize(totalSoFar)} / {FormatSize(_TotalBytes)}"
                                    : $"Total: {FormatSize(totalSoFar)} / ???";
                                var info = new PatchProgressInfo
                                {
                                    SpeedText = string.IsNullOrWhiteSpace(_LastSpeedText) ? "" : $"Speed: {_LastSpeedText}",
                                    CurrentText = currentText,
                                    TotalText = totalText
                                };
                                bgWorkerAutoPatch.ReportProgress((int)globalProgress, info);
                            };
                            System.ComponentModel.AsyncCompletedEventHandler completedHandler = (s, ev) => done.Set();
                            client.DownloadProgressChanged += progressHandler;
                            client.DownloadFileCompleted += completedHandler;
                            client.DownloadFileAsync(new Uri(url), fileNameRar);
                            done.WaitOne();
                            client.DownloadProgressChanged -= progressHandler;
                            client.DownloadFileCompleted -= completedHandler;
                        }
                        if (lastTotalBytesToReceive > 0 && _TotalBytesDone >= 0)
                            _TotalBytesDone += lastTotalBytesToReceive;
                        else
                            _TotalBytesDone = (_TotalBytesDone >= 0) ? _TotalBytesDone : -1;
                        if (File.Exists(fileNameRar))
                        {
                            using (var archive = ArchiveFactory.Open(fileNameRar))
                            {
                                foreach (var entry in archive.Entries.Where(en => !en.IsDirectory))
                                {
                                    entry.WriteToDirectory(
                                        Environment.CurrentDirectory,
                                        new ExtractionOptions
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                                }
                            }

                            ApplyNewVersionAndReloadVersion();
                            filesToClear.Add(fileNameRar);
                        }
                        AnyPatchApplied = true;
                        var endInfo = new PatchProgressInfo
                        {
                            SpeedText = "",
                            CurrentText = "File: done",
                            TotalText = (_TotalBytes >= 0)
                                ? $"Total: {FormatSize(_TotalBytesDone)} / {FormatSize(_TotalBytes)}"
                                : $"Total: {FormatSize(_TotalBytesDone)} / ???"
                        };
                        bgWorkerAutoPatch.ReportProgress((int)(((i + 1) * 100.0) / patchs.Count), endInfo);
                    }
                    foreach (var f in filesToClear)
                        File.Delete(f);
                    _PatchListGetOK = true;
                }
            }
            catch
            {
                _PatchListGetOK = false;
            }
        }

        private void ApplyNewVersionAndReloadVersion()
        {
            _AutoPatchConfig.CurrentVersion++;
            File.WriteAllText("AutoPatchPluginCLConfig.json", JsonConvert.SerializeObject(_AutoPatchConfig, Formatting.Indented));
        }

        private bool ValidURL(string URI)
        {
            return Uri.TryCreate(URI, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void LblStatus_Click(object sender, EventArgs e)
        {
        }

        private void SaveLayoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cfg = LayoutSerializer.Capture(this);
            LayoutSerializer.Save(_LayoutPath, cfg);
            MessageBox.Show("Saved successfully.", "AutoPatchPluginCL", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowHideTitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cfg = LayoutSerializer.Capture(this);
            cfg.ShowFormTitle = !cfg.ShowFormTitle;
            LayoutSerializer.Save(_LayoutPath, cfg);
            MessageBox.Show(cfg.ShowFormTitle ? "Form Title [Visible]" : "Form Title [Hide]", "AutoPatchPluginCL", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
    public class PatchProgressInfo
    {
        public string SpeedText { get; set; } = "";
        public string CurrentText { get; set; } = "";
        public string TotalText { get; set; } = "";
    }
}
