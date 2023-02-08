using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace AoP.Editor
{
    public sealed class FileAni
    {

        public readonly IDictionary<string, IDictionary<string, List<string>>> dataBySections;

        public FileAni(string path)
        {
            dataBySections = new Dictionary<string, IDictionary<string, List<string>>>();
            string currentSection = string.Empty;
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8).Select(x => CleanLine(x)))
            {
                if (string.IsNullOrEmpty(line)) continue;

                if (TryParseSectionName(line, out string section))
                {
                    currentSection = section;
                }
                else if (TryParseParamValue(line, out (string param, string value) pair))
                {
                    if (!dataBySections.ContainsKey(currentSection))
                    {
                        dataBySections[currentSection] = new Dictionary<string, List<string>>();
                    }
                    var currentSectionData = dataBySections[currentSection];
                    if (!currentSectionData.ContainsKey(pair.param))
                    {
                        currentSectionData[pair.param] = new List<string>();
                    }
                    currentSectionData[pair.param].Add(pair.value);
                }
            }
        }

        private string CleanLine(string line)
        {
            string result = line.Trim();
            int commentIndex = result.IndexOf(';');
            if (commentIndex == 0)
            {
                result = string.Empty;
            }
            else if (commentIndex > 0)
            {
                result = result.Substring(0, commentIndex);
            }
            return result.Trim();
        }

        private bool TryParseSectionName(string line, out string section)
        {
            int openIndex = line.IndexOf('[');
            if (openIndex > -1)
            {
                int closeIndex = line.IndexOf(']', openIndex + 1);
                if (closeIndex > openIndex)
                {
                    int delta = closeIndex - openIndex;
                    if (delta > 1)
                    {
                        section = line.Substring(openIndex + 1, delta - 1).Trim();
                    }
                    else
                    {
                        section = string.Empty;
                    }
                    return true;
                }
            }
            section = null;
            return false;
        }

        private bool TryParseParamValue(string line, out (string param, string value) pair)
        {
            int equalsSignIndex = line.IndexOf('=');
            if (equalsSignIndex > -1)
            {
                string[] splitted = line.Split('=', System.StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length > 1)
                {
                    pair = (splitted[0].Trim(), splitted[1].Trim());
                    return true;
                }
            }
            pair = ("", "");
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var entry in dataBySections)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    sb.AppendLine(string.Format("[{0}]", entry.Key));
                }
                foreach (var paramValue in entry.Value)
                {
                    foreach (string value in paramValue.Value)
                    {
                        sb.AppendLine(string.Format("{0} = {1}", paramValue.Key, value));
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
