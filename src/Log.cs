using System;
using System.IO;
using System.Text;

namespace MiniAppsLauncher
{
    internal static class Log
    {
        private const long MaxBytes = 1024 * 1024;
        private static readonly object Gate = new object();
        private static readonly string DirectoryPathValue = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniAppsLauncher");
        private static readonly string FilePathValue = Path.Combine(DirectoryPathValue, "launcher.log");

        public static string DirectoryPath
        {
            get { return DirectoryPathValue; }
        }

        public static string FileName
        {
            get { return FilePathValue; }
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Error(string message)
        {
            Write("ERROR", message, null);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(DirectoryPathValue);
                    RotateIfNeeded();

                    StringBuilder line = new StringBuilder();
                    line.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    line.Append(" [").Append(level).Append("] ").Append(message);
                    if (ex != null)
                    {
                        line.AppendLine();
                        line.Append(ex);
                    }
                    line.AppendLine();

                    File.AppendAllText(FilePathValue, line.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never take the launcher down.
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                FileInfo info = new FileInfo(FilePathValue);
                if (!info.Exists || info.Length <= MaxBytes)
                {
                    return;
                }

                string previous = FilePathValue + ".1";
                if (File.Exists(previous))
                {
                    File.Delete(previous);
                }
                File.Move(FilePathValue, previous);
            }
            catch
            {
            }
        }
    }
}
