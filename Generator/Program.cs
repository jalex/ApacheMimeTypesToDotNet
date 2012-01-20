using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace Generator {

    class Program {
        const string ApacheMimeTypesURL = "http://svn.apache.org/repos/asf/httpd/httpd/trunk/docs/conf/mime.types";
        const string FileName = "../../ApacheMimeTypes/Apache.cs";
        const string NameSpace = "ApacheMimeTypes";
        const string ClassName = "Apache";
        const string DictionaryName = "MimeTypes";
        const string Indent = "    ";

        static void Main(string[] args) {
            string mimeTypesText = GetApacheMimeTypes(ApacheMimeTypesURL);
            if(string.IsNullOrEmpty(mimeTypesText)) {
                Console.Write("Apache mime.types file empty!");
                Environment.Exit(0);
            }

            var list = ParseMimeTypes(mimeTypesText);
            if(list.Count == 0) {
                Console.WriteLine("Attempted to convert mime.types file to dictionary but no entries!");
                Environment.Exit(0);
            }

            WriteDictionaryToCSharpMimeTypesClass(list);

            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }

        static string GetApacheMimeTypes(string url) {
            WebClient webClient = new WebClient();
            return webClient.DownloadString(url);
        }

        static SortedList<string, string> ParseMimeTypes(string mimeTypesText) {
            var list = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);

            using(StringReader reader = new StringReader(mimeTypesText)) {
                string line;
                while((line = reader.ReadLine()) != null) {
                    // skip commments
                    if(line.Trim().StartsWith("#")) continue;

                    string[] parts = Regex.Split(line, "\\s+");
                    if(parts.Length < 2) {
                        Console.WriteLine("Skipping line: " + line);
                        continue;
                    }

                    string mimeType = parts[0];
                    for(var n = 1; n < parts.Length; n++) {
                        var ext = parts[n];
                        if(list.ContainsKey(ext)) {
                            Console.WriteLine("Potential duplicate for extension: " + ext);
                            continue;
                        }
                        list.Add(ext, mimeType);
                    }
                }
            }

            return list;
        }

        static void WriteDictionaryToCSharpMimeTypesClass(SortedList<string, string> list) {
            StringBuilder sb = new StringBuilder().AppendFormat(@"
using System;
using System.Collections.Generic;

namespace ApacheMimeTypes {{

{0}public class Apache {{

{0}{0}/// <summary>
{0}{0}/// Apache mime types dictionary (ex: {{""pdf"", ""application/pdf""}})
{0}{0}/// </summary>
{0}{0}public static IDictionary<string, string> MimeTypes {{ get {{ return Nested.mimeTypes; }} }}

{0}{0}class Nested {{
{0}{0}{0}static Nested() {{}}

{0}{0}{0}internal static readonly Dictionary<string, string> mimeTypes = new Dictionary<string, string>() {{", Indent);
            var sep = string.Empty;
            foreach(var e in list) {
                sb.Append(sep).AppendLine();
                sb.AppendFormat(@"{0}{0}{0}{0}{{""{1}"", ""{2}""}}", Indent, e.Key, e.Value);
                sep = ",";
            }
            sb.AppendFormat(@"
{0}{0}{0}}};
{0}{0}}}
{0}}}
}}
", Indent);
            
            using(TextWriter writer = File.CreateText(FileName)) {
                writer.Write(sb.ToString().Trim());
                writer.WriteLine();
            }
        }

        
    }
}
