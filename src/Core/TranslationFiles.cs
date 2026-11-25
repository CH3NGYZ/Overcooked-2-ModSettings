using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using OC2ModSettings.SDK;

namespace OC2ModSettings
{
    internal sealed class TranslationFiles
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(true, true);
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> languages =
            new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        internal static TranslationFiles Load(string directory, Action<string> warn)
        {
            TranslationFiles result = new TranslationFiles();
            if (string.IsNullOrEmpty(directory)) return result;
            foreach (string folder in List(directory, true, warn))
            {
                string locale = ModSettingsTranslations.NormalizeLocale(Path.GetFileName(folder));
                if (locale == null) continue;
                foreach (string path in List(folder, false, warn))
                {
                    if (!string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase)) continue;
                    string owner = Path.GetFileNameWithoutExtension(path);
                    if (string.IsNullOrEmpty(owner)) continue;
                    Dictionary<string, string> labels = Read(path, warn);
                    if (labels == null || labels.Count == 0) continue;
                    Dictionary<string, Dictionary<string, string>> owners;
                    if (!result.languages.TryGetValue(locale, out owners))
                    {
                        owners = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                        result.languages.Add(locale, owners);
                    }
                    Dictionary<string, string> existing;
                    if (owners.TryGetValue(owner, out existing))
                        foreach (KeyValuePair<string, string> pair in labels) existing[pair.Key] = pair.Value;
                    else owners.Add(owner, labels);
                }
            }
            return result;
        }

        internal string Get(string ownerGuid, string key, string locale)
        {
            Dictionary<string, Dictionary<string, string>> owners;
            Dictionary<string, string> labels;
            string value;
            return locale != null && ownerGuid != null && key != null && languages.TryGetValue(locale, out owners)
                && owners.TryGetValue(ownerGuid, out labels) && labels.TryGetValue(key, out value) ? value : null;
        }

        private static string[] List(string path, bool folders, Action<string> warn)
        {
            try
            {
                string[] entries = folders ? Directory.GetDirectories(path) : Directory.GetFiles(path);
                Array.Sort(entries, StringComparer.Ordinal);
                return entries;
            }
            catch (DirectoryNotFoundException) { }
            catch (IOException error) { Warn(warn, path, error.Message); }
            catch (UnauthorizedAccessException error) { Warn(warn, path, error.Message); }
            catch (SecurityException error) { Warn(warn, path, error.Message); }
            return new string[0];
        }

        private static Dictionary<string, string> Read(string path, Action<string> warn)
        {
            try
            {
                Dictionary<string, string> labels = new Dictionary<string, string>(StringComparer.Ordinal);
                using (StreamReader reader = new StreamReader(path, Utf8, false))
                {
                    int lineNumber = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        line = line.Trim();
                        if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)
                            || line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("r:", StringComparison.Ordinal)
                            || line.StartsWith("sr:", StringComparison.Ordinal)) continue;
                        string key, value;
                        if (Parse(line, out key, out value)) labels[key] = value;
                        else Warn(warn, path + ":" + lineNumber, "Invalid translation line.");
                    }
                }
                return labels;
            }
            catch (IOException error) { Warn(warn, path, error.Message); }
            catch (UnauthorizedAccessException error) { Warn(warn, path, error.Message); }
            catch (SecurityException error) { Warn(warn, path, error.Message); }
            catch (DecoderFallbackException error) { Warn(warn, path, error.Message); }
            return null;
        }

        private static bool Parse(string line, out string key, out string value)
        {
            key = value = null;
            StringBuilder text = new StringBuilder(line.Length);
            for (int index = 0; index < line.Length; index++)
            {
                char current = line[index];
                if (current == '=' && key == null)
                {
                    key = text.ToString().Trim();
                    if (key.Length == 0) return false;
                    text.Length = 0;
                }
                else if (current != '\\' || index + 1 == line.Length) text.Append(current);
                else
                {
                    char escape = line[++index];
                    switch (escape)
                    {
                        case '\\': case '=': text.Append(escape); break;
                        case 'n': text.Append('\n'); break;
                        case 'r': text.Append('\r'); break;
                        case 'u':
                            ushort code;
                            if (index + 4 >= line.Length || !ushort.TryParse(line.Substring(index + 1, 4),
                                NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out code)) return false;
                            text.Append((char)code); index += 4; break;
                        default: text.Append('\\').Append(escape); break;
                    }
                }
            }
            if (key == null) return false;
            value = text.ToString().Trim(' ', '\t');
            return true;
        }

        private static void Warn(Action<string> warn, string path, string message)
        { if (warn != null) warn(path + ": " + message); }
    }
}
