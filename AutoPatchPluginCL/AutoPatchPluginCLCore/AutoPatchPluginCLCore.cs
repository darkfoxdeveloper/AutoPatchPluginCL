using CLCore;
using ConquerLoader.CLCore;
using System.Diagnostics;
using System.Windows.Forms;

namespace AutoPatchPluginCLCore
{
    public class AutoPatchPluginCLCore : IPlugin
	{
		public string Explanation
		{
			get
			{
				return "This plugin is a core for ConquerLoader AutoPatch";
			}
		}

		public string Name
		{
			get
			{
				return "AutoPatchPluginCLCore";
			}
		}
		public PluginType PluginType { get; } = PluginType.FREE;

		public void Init()
		{
            CLCore.LoaderEvents.LauncherLoaded += LoaderEvents_LauncherLoaded;
		}

        private void LoaderEvents_LauncherLoaded()
		{
			Process.Start("AutoPatchPluginCL.exe").WaitForExit();
		}

        public void Configure()
		{
			MessageBox.Show($"Not required configuration yet!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
