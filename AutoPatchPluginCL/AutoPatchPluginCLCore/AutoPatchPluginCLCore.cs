using ConquerLoader.CLCore;
using System.Collections.Generic;
using System.Diagnostics;

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

		public LoadType LoadType
		{
			get
			{
				return LoadType.ON_FORM_LOAD;
			}
		}
		public List<Parameter> Parameters { get; set; }

		public void Run()
		{
			Process.Start("AutoPatchPluginCL.exe").WaitForExit();
		}
	}
}
