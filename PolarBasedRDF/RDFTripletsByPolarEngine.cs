﻿using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using PolarBasedEngine;
using PolarDB;

namespace PolarBasedRDF
{
    class RDFTripletsByPolarEngine
    {
        private readonly PType ptDirects = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("o", new PType(PTypeEnumeration.sstring))
            ));

        private readonly PType ptData = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("d", new PType(PTypeEnumeration.sstring)),
            new NamedType("l", new PType(PTypeEnumeration.sstring))
            ));

        private PaCell directCell, dataCell;
        private readonly string directCellPath;
        private readonly string dataCellPath;
        private FixedIndex<string> sDirectIndex, oIndex, sDataIndex;
        private FixedIndex<FixedIndex<string>.SubjPred> spDirectIndex, opIndex, spDataIndex;

        public RDFTripletsByPolarEngine(DirectoryInfo path)
        {
            if (!path.Exists) path.Create();
            directCell = new PaCell(ptDirects, directCellPath = Path.Combine(path.FullName, "rdf.direct.pac"), File.Exists(directCellPath));
            dataCell = new PaCell(ptData, dataCellPath = Path.Combine(path.FullName, "rdf.data.pac"), File.Exists(dataCellPath));
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            sDataIndex = new FixedIndex<string>("s of data", dataCell.Root, entry => (string) entry.Field(0).Get());
            sDirectIndex = new FixedIndex<string>("s of direct", directCell.Root, entry => (string) entry.Field(0).Get());
            oIndex = new FixedIndex<string>("o of direct", directCell.Root, entry => (string) entry.Field(2).Get());
            spDataIndex = new FixedIndex<FixedIndex<string>.SubjPred>("s and p of data", dataCell.Root,
                entry =>
                    new FixedIndex<string>.SubjPred {subj = (string) entry.Field(0).Get(), pred = (string) entry.Field(1).Get()});
            spDirectIndex = new FixedIndex<FixedIndex<string>.SubjPred>("s and p of direct", directCell.Root,
                entry =>
                    new FixedIndex<string>.SubjPred {subj = (string) entry.Field(0).Get(), pred = (string) entry.Field(1).Get()});
            opIndex = new FixedIndex<FixedIndex<string>.SubjPred>("o and p of direct", directCell.Root,
                entry =>
                    new FixedIndex<string>.SubjPred {subj = (string) entry.Field(2).Get(), pred = (string) entry.Field(1).Get()});
        }

        #region Load

        public void Load(int tripletsCountLimit, params string[] filesPaths)
        {
            directCell.Close();
            dataCell.Close();
            sDataIndex.Close();
            sDirectIndex.Close();
            oIndex.Close();
            spDataIndex.Close();
            spDirectIndex.Close();
            opIndex.Close();
            File.Delete(dataCellPath);
            File.Delete(directCellPath);
            directCell = new PaCell(ptDirects, directCellPath, false);
            dataCell = new PaCell(ptData, dataCellPath, false);

            var directSerialFlow = (ISerialFlow) directCell;
            var dataSerialFlow = (ISerialFlow) dataCell;
            directSerialFlow.StartSerialFlow();
            dataSerialFlow.StartSerialFlow();
            directSerialFlow.S();
            dataSerialFlow.S();
            int i = 0;
            for (int j = 0; j < filesPaths.Length && i < tripletsCountLimit; j++)
                i += ReadFile(filesPaths[j], (id, property, value, isObj, lang) =>
                {
                    if (isObj)
                        directSerialFlow.V(new object[] {id, property, value});
                    else dataSerialFlow.V(new object[] {id, property, value, lang ?? ""});
                },
                    tripletsCountLimit);
            directSerialFlow.Se();
            dataSerialFlow.Se();
            directSerialFlow.EndSerialFlow();
            dataSerialFlow.EndSerialFlow();
            CreateIndexes();
            LoadIndexes();
        }

        private void LoadIndexes()
        {
            sDataIndex.Load(null);
            sDirectIndex.Load(null);
            oIndex.Load(null);
            var subjPredComparer = new FixedIndex<string>.SubjPredComparer();
            spDataIndex.Load(subjPredComparer);
            spDirectIndex.Load(subjPredComparer);
            opIndex.Load(subjPredComparer);
        }

        internal delegate void QuadAction(string id, string property,
            string value, bool isObj = true, string lang = null);

        internal static int ReadFile(string filePath, QuadAction quadAction, int tripletsCountLimit)
        {
            var extension = Path.GetExtension(filePath);
            if (extension == null || !File.Exists(filePath)) return 0;
            extension = extension.ToLower();
            if (extension == ".xml")
                return ReadXML2Quad(filePath, quadAction, tripletsCountLimit);
            else if (extension == ".nt2")
                return ReadTSV(filePath, quadAction, tripletsCountLimit);
            return 0;
        }

        private static readonly Regex NsRegex = new Regex(@"^@prefix\s+(\w+):\s+<(.+)>\.$",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex TripletsRegex = new Regex("^(\\S+)\\s+(\\S+)\\s+(\"(.+)\"(@(\\S*))?|(.+))\\.$",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// TODO ns
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="quadAction"></param>
        /// <param name="tripletsCountLimit"></param>
        private static int ReadTSV(string filePath, QuadAction quadAction, int tripletsCountLimit)
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
                for (i = 0; i < tripletsCountLimit && !reader.EndOfStream; i++, readLine = string.Empty, tryCount = 0)
                    while (tryCount < tryLinesCountMax && !String2Quard(quadAction, readLine += reader.ReadLine()))
                        tryCount++;
                return i + 1;
            }
        }

        private static bool String2Quard(QuadAction quadAction, string readLine)
        {
            Match lineMatch;
            if (!(lineMatch = TripletsRegex.Match(readLine)).Success) return false;
            var dMatch = lineMatch.Groups[4];
            if (dMatch.Success)
                quadAction(lineMatch.Groups[1].Value, lineMatch.Groups[2].Value, dMatch.Value, false,
                    lineMatch.Groups[6].Value);
            else
                quadAction(lineMatch.Groups[1].Value, lineMatch.Groups[2].Value, lineMatch.Groups[7].Value);
            return true;
        }

        #region Read XML

        public static string LangAttributeName = "xml:lang",
            RDFAbout = "rdf:about",
            RDFResource = "rdf:resource",
            NS = "http://fogid.net/o/";


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
                            quadAction(id, ONames.rdftypestring, NS + xml.Name);
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

        #endregion

        #endregion

        public string GetItem(string id)
        {
            return
                sDirectIndex.GetAllByKey(id)
                    .Select(spo => spo.Type.Interpret(spo.Get()))
                    .Aggregate((all, one) => all + one) +
                oIndex.GetAllByKey(id)
                    .Select(spo => spo.Type.Interpret(spo.Get()))
                    .Aggregate((all, one) => all + one) +
                sDataIndex.GetAllByKey(id)
                    .Select(spo => spo.Type.Interpret(spo.Get()))
                    .Aggregate((all, one) => all + one);
        }
    }
}
