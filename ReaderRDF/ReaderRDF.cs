using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace ReaderRDF
{
    public class ReaderRDF
    {
        private static readonly Regex NsRegex = new Regex(@"^@prefix\s+(\w+):\s+<(.+)>\.$", RegexOptions.Singleline);
        private static readonly Regex TripletsRegex = new Regex(@"^([^\t]+)\t([^\t]+)\t(""(.+)""(@(\.*))?|(.+))\.$", RegexOptions.Singleline);
        private const string LangAttributeName = "xml:lang";
        private const string RDFAbout = "rdf:about";
        private const string RDFResource = "rdf:resource";
        private const string NS = "http://fogid.net/o/";

        public delegate void QuadAction(string id, string property,
            string value, bool isObj = true, string lang = null);

        public static int ReadFiles(int tripletsCountLimit, string[] filesPaths, QuadAction quadAction)
        {
            int i = 0;
            for (int j = 0; j < filesPaths.Length && i < tripletsCountLimit; j++)
                i += ReadFile(filesPaths[j], quadAction, tripletsCountLimit);
            return i;
        }

        public static int ReadFile(string filePath, QuadAction quadAction, int tripletsCountLimit)
        {
            var extension = Path.GetExtension(filePath);
            if (extension == null || !File.Exists(filePath)) return 0;
            extension = extension.ToLower();
            if (extension == ".xml")
                return ReadXML2Quad(filePath, quadAction, tripletsCountLimit);
            else if (extension == ".nt2")
                return ReadTSV(filePath, quadAction, tripletsCountLimit);
            else if (extension == ".ttl")
                return ReadTurtle(filePath, quadAction, tripletsCountLimit);
            return 0;
        }

        /// <summary>
        /// TODO ns
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="quadAction"></param>
        /// <param name="tripletsCountLimit"></param>
        public static int ReadTSV(string filePath, QuadAction quadAction, long tripletsCountLimit)
        {
            using (var reader = new StreamReader(filePath))
            {
                Match nsReg;
                while ((nsReg = NsRegex.Match(reader.ReadLine())).Success)
                {
                    // nsReg.Groups[1]
                    //nsReg.Groups[2]
                }
                int tryCount = 0;
                const int tryLinesCountMax = 10;
                string readLine = string.Empty;
                int i;
                Match lineMatch;
                Group dMatch;
                for (i = 0; i < tripletsCountLimit && !reader.EndOfStream; i++, readLine = string.Empty, tryCount = 0)
                    while ((readLine += reader.ReadLine()).Length > 0 && readLine[0] != '@' &&
                           tryCount++ < tryLinesCountMax)
                    {
                        if (!(lineMatch = TripletsRegex.Match(readLine)).Success) continue;
                        dMatch = lineMatch.Groups[4];
                        if (dMatch.Success && (dMatch.Value != "true" && dMatch.Value != "false"))
                            quadAction(lineMatch.Groups[1].Value, lineMatch.Groups[2].Value, dMatch.Value, false,
                                lineMatch.Groups[6].Value);
                        else
                            quadAction(lineMatch.Groups[1].Value, lineMatch.Groups[2].Value, lineMatch.Groups[7].Value);
                        break;
                    }
                return i + 1;
            }
        }

        /// <summary>
        /// Turtle format in *.ttl file:
        /// s
        ///     p d@l ;
        ///     ...
        ///     p o ;
        ///     ...
        /// ...
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="quadAction"></param>
        /// <param name="tripletsCountLimit"></param>
        /// <returns></returns>
        private static int ReadTurtle(string filePath, QuadAction quadAction, int tripletsCountLimit)
        {
            using (StreamReader file = new StreamReader(filePath))
            {
                var prefixNamespace = new Dictionary<string, string>();
                string line = file.ReadLine();
                while (line != null && line.StartsWith("@prefix "))
                {
                    var parts = line.Split(' ');
                    prefixNamespace.Add(parts[1].TrimEnd(':'), parts[2].TrimStart('<').TrimEnd('>'));
                    line = file.ReadLine();
                }
                string id = "";
                int count = 0;
                while (!file.EndOfStream && count++ < tripletsCountLimit)
                {
                    line = file.ReadLine();
                    if (line.StartsWith("    "))
                    {
                        var parts = line.Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                        var fullPredicate = Local2FullNamespace(parts[0], prefixNamespace);
                        if (!parts[1].StartsWith("\""))
                            quadAction(id, fullPredicate, Local2FullNamespace(parts[1], prefixNamespace));
                        else
                        {
                            //поиск языка( @en )
                            if (parts.Length > 3)
                                parts[1] = line.TrimStart().Remove(0, parts[0].Length).TrimEnd(';',' ').TrimStart();
                            var dataLnagType = parts[1].Split('@');
                            if (dataLnagType.Length == 1) //если нет языка поиск xsd типа (^^xsd:integer)
                            {
                                dataLnagType = parts[1].Split(new[] {"^^"}, StringSplitOptions.RemoveEmptyEntries);
                                if (dataLnagType.Length == 1)
                                    quadAction(id, fullPredicate.Trim('"'), parts[1].Trim('"').Trim('\"'), false);
                                else quadAction(id, fullPredicate, dataLnagType[0].Trim('"'), false);
                            }
                            else
                                quadAction(id, fullPredicate, dataLnagType[0].Trim('"'), false, dataLnagType[1]);
                        }
                    }
                    else id = Local2FullNamespace(line, prefixNamespace);
                }
                return count;
            }
        }

        private static string Local2FullNamespace(string name, Dictionary<string, string> prefixNamespace)
        {
            if (name.StartsWith("<")) return name.TrimStart('<').TrimEnd('>');
            string[] prefixWithValue = name.Split(':');
            string fullNamespace;
            if (prefixWithValue.Length == 2 &&
                prefixNamespace.TryGetValue(prefixWithValue[0], out fullNamespace))
                name = fullNamespace + prefixWithValue[1];
            return name;
        }

        /// <summary>
        /// TODO tripletsCountLimit
        /// </summary>
        /// <param name="url"></param>
        /// <param name="quadAction"></param>
        /// <param name="tripletsCountLimit"></param>
        private static int ReadXML2Quad(string url, QuadAction quadAction, int tripletsCountLimit)
        {
            string id = string.Empty;
            int i = 0;
            using (var xml = new XmlTextReader(url))
                while (i < tripletsCountLimit && xml.Read())
                    if (xml.IsStartElement())
                        if (xml.Depth == 1 && (id = xml[RDFAbout]) != null)
                        {
                            quadAction(id, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", NS + xml.Name);
                            i++;
                        }
                        else if (xml.Depth == 2 && id != null)
                        {
                            string resource = xml[RDFResource];
                            bool isObj = (resource) != null;
                            quadAction(id, NS + xml.Name,
                                isObj: isObj,
                                lang: isObj ? null : xml[LangAttributeName],
                                value: isObj ? resource : xml.ReadString());
                            i++;
                        }
            return i + 1;
        }
    }
}