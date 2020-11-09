using System.Collections.Generic;

namespace AutoPatchPluginCL.Models
{
    public class PatchList
    {
        public int CurrentVersion { get; set; }
        public List<Patch> Paths { get; set; }
        public PatchList()
        {
            if (Paths == null)
            {
                Paths = new List<Patch>();
            }
        }
    }
}
