using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace MiniAppsLauncher
{
    internal static class ConfigStore
    {
        private const string ConfigFileName = "apps.json";
        private const string EmbeddedExampleName = "apps.example.json";

        public static string ConfigDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MiniAppsLauncher");
            }
        }

        public static string ConfigPath
        {
            get
            {
                string portable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
                if (File.Exists(portable))
                {
                    return portable;
                }
                return Path.Combine(ConfigDirectory, ConfigFileName);
            }
        }

        public static AppConfig Load()
        {
            string path = ConfigPath;
            if (!File.Exists(path))
            {
                AppConfig seeded = ReadEmbeddedExample();
                Save(seeded, path);
                Log.Info("Created default config at " + path);
                return seeded;
            }

            return Load(path);
        }

        public static AppConfig Load(string path)
        {
            string json = File.ReadAllText(path);
            AppConfig config = Deserialize(json);
            Log.Info("Loaded config " + path + " (" + config.Apps.Count + " apps)");
            return config;
        }

        public static void Save(AppConfig config)
        {
            Save(config, ConfigPath);
        }

        public static void Save(AppConfig config, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = Serialize(config);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, json, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                string backup = path + ".bak";
                try
                {
                    if (File.Exists(backup))
                    {
                        File.Delete(backup);
                    }
                    File.Replace(temporary, path, backup);
                }
                catch (Exception)
                {
                    File.Copy(temporary, path, true);
                    File.Delete(temporary);
                }
            }
            else
            {
                File.Move(temporary, path);
            }

            Log.Info("Saved config " + path);
        }

        public static string Serialize(AppConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  \"version\": ").Append(config.Version).AppendLine(",");
            sb.AppendLine("  \"apps\": [");

            for (int i = 0; i < config.Apps.Count; i++)
            {
                AppEntry app = config.Apps[i];
                sb.AppendLine("    {");
                AppendString(sb, "id", app.Id, true);
                AppendString(sb, "name", app.Name, true);
                AppendString(sb, "path", app.Path, true);
                AppendString(sb, "arguments", app.Arguments, true);
                AppendString(sb, "workingDirectory", app.WorkingDirectory, true);
                AppendString(sb, "icon", app.Icon, true);
                AppendString(sb, "category", app.Category, true);
                sb.Append("      \"runAsAdmin\": ").Append(app.RunAsAdmin ? "true" : "false").AppendLine(",");
                sb.Append("      \"enabled\": ").Append(app.Enabled ? "true" : "false").AppendLine(",");
                sb.Append("      \"singleInstance\": ").Append(app.SingleInstance ? "true" : "false").AppendLine();
                sb.Append("    }");
                if (i < config.Apps.Count - 1)
                {
                    sb.Append(",");
                }
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void AppendString(StringBuilder sb, string key, string value, bool comma)
        {
            sb.Append("      \"").Append(key).Append("\": \"").Append(Escape(value)).Append("\"");
            if (comma)
            {
                sb.Append(",");
            }
            sb.AppendLine();
        }

        private static AppConfig Deserialize(string json)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            AppConfig config = serializer.Deserialize<AppConfig>(json);
            if (config == null)
            {
                throw new InvalidDataException("Config file is empty.");
            }

            if (config.Apps == null)
            {
                config.Apps = new List<AppEntry>();
            }

            List<AppEntry> apps = new List<AppEntry>();
            foreach (AppEntry app in config.Apps)
            {
                if (app == null)
                {
                    continue;
                }

                if (app.Name == null)
                {
                    app.Name = "";
                }
                if (app.Id == null)
                {
                    app.Id = "";
                }
                if (string.IsNullOrWhiteSpace(app.Id))
                {
                    app.Id = MakeId(app.DisplayName);
                }
                if (string.IsNullOrWhiteSpace(app.Name))
                {
                    app.Name = app.DisplayName;
                }
                apps.Add(app);
            }

            config.Apps = apps;
            return config;
        }

        private static AppConfig ReadEmbeddedExample()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(EmbeddedExampleName))
                {
                    if (stream == null)
                    {
                        Log.Error("Embedded " + EmbeddedExampleName + " was not found.");
                        return new AppConfig();
                    }

                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return Deserialize(reader.ReadToEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Could not read the embedded example config", ex);
                return new AppConfig();
            }
        }

        private static string MakeId(string value)
        {
            StringBuilder sb = new StringBuilder();
            string source = value == null ? "" : value.ToLowerInvariant();
            foreach (char c in source)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-')
                {
                    sb.Append('-');
                }
            }

            string id = sb.ToString().Trim('-');
            return id.Length == 0 ? "app" : id;
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            StringBuilder sb = new StringBuilder(value.Length + 8);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
