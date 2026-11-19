using System.Collections.Generic;

namespace MiniAppsLauncher
{
    public class AppConfig
    {
        public AppConfig()
        {
            Version = 1;
            Apps = new List<AppEntry>();
        }

        public int Version { get; set; }

        public List<AppEntry> Apps { get; set; }
    }
}
