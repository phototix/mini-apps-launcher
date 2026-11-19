using System;
using System.IO;

namespace MiniAppsLauncher
{
    public class AppEntry
    {
        public AppEntry()
        {
            Id = "";
            Name = "";
            Path = "";
            Arguments = "";
            WorkingDirectory = "";
            Icon = "";
            Category = "";
            Enabled = true;
            RunAsAdmin = false;
            SingleInstance = true;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public string Icon { get; set; }
        public string Category { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool Enabled { get; set; }
        public bool SingleInstance { get; set; }

        public string ExpandedPath
        {
            get { return Expand(Path); }
        }

        public string ExpandedWorkingDirectory
        {
            get
            {
                string workingDirectory = Expand(WorkingDirectory);
                if (!string.IsNullOrWhiteSpace(workingDirectory))
                {
                    return workingDirectory;
                }

                string executable = ExpandedPath;
                if (string.IsNullOrWhiteSpace(executable))
                {
                    return "";
                }
                return System.IO.Path.GetDirectoryName(executable);
            }
        }

        public string ExpandedIconPath
        {
            get { return Expand(Icon); }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    return Name;
                }
                string executable = ExpandedPath;
                return string.IsNullOrWhiteSpace(executable)
                    ? "(unnamed app)"
                    : System.IO.Path.GetFileNameWithoutExtension(executable);
            }
        }

        public bool Exists
        {
            get
            {
                string executable = ExpandedPath;
                return !string.IsNullOrWhiteSpace(executable) && File.Exists(executable);
            }
        }

        public static string Expand(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return Environment.ExpandEnvironmentVariables(value.Trim());
        }
    }
}
