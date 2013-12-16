using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PolarBasedEngine;
using PolarBasedRDFHashes;
using PolarDB;

namespace PolarBasedRDF
{
    internal class RDFTripletsByPolarEngine
    {
        private readonly PType ptDirects = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.integer)),
            new NamedType("p", new PType(PTypeEnumeration.integer)),
            new NamedType("o", new PType(PTypeEnumeration.integer))
            ));

        private readonly PType ptData = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.integer)),
            new NamedType("p", new PType(PTypeEnumeration.integer)),
            new NamedType("d", new PType(PTypeEnumeration.integer)),
            new NamedType("l", new PType(PTypeEnumeration.sstring))
            ));
        private  readonly  PType ptHashValue=new PTypeSequence(new PTypeRecord(
            new NamedType("hash of id", new PType(PTypeEnumeration.integer)),
            new NamedType("id", new PType(PTypeEnumeration.sstring))));

        private PaCell directCell, dataCell, predDataCell, predObjell, idCell, textCell;
        private readonly string directCellPath, dataCellPath, predDataCellPath, predObjellPath, idCellPath, textCellPath;
        private FixedIndex<int> sDirectIndex, oIndex, sDataIndex, predicateDataIndex, predicateObjIndex, idIndex, textIndex;
        private  FixedIndex<SubjPred<int>> spDirectIndex, opIndex, spDataIndex;

        public RDFTripletsByPolarEngine(DirectoryInfo path)
        {
            if (!path.Exists) path.Create();
            directCell = new PaCell(ptDirects, directCellPath = Path.Combine(path.FullName, "rdf.hashed.direct.pac"),
                File.Exists(directCellPath));
            dataCell = new PaCell(ptData, dataCellPath = Path.Combine(path.FullName, "rdf.hashed.data.pac"),
                File.Exists(dataCellPath));
            predDataCell = new PaCell(ptHashValue, predDataCellPath = Path.Combine(path.FullName, "data predicates.pac"),
                File.Exists(predDataCellPath));
            predObjell = new PaCell(ptHashValue, predObjellPath = Path.Combine(path.FullName, "object  predicates.pac"),
               File.Exists(predObjellPath));
            idCell = new PaCell(ptHashValue, idCellPath = Path.Combine(path.FullName, "ids.pac"),
               File.Exists(idCellPath));
            textCell = new PaCell(ptHashValue, textCellPath = Path.Combine(path.FullName, "texts.pac"), File.Exists(textCellPath));
            if (dataCell.IsEmpty || directCell.IsEmpty) return;
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            sDataIndex = new FixedIndex<int>("s of data", dataCell.Root, entry => (int) entry.Field(0).Get());
            sDirectIndex = new FixedIndex<int>("s of direct", directCell.Root, entry => (int)entry.Field(0).Get());
            oIndex = new FixedIndex<int>("o of direct", directCell.Root, entry => (int)entry.Field(2).Get());

            predicateObjIndex = new FixedIndex<int>("predicate of object", predObjell.Root, entry => (int)entry.Field(0).Get());
            predicateDataIndex = new FixedIndex<int>("predicate of data", predDataCell.Root, entry => (int)entry.Field(0).Get());
            idIndex = new FixedIndex<int>("id", idCell.Root, entry => (int)entry.Field(0).Get());
            textIndex = new FixedIndex<int>("text", textCell.Root, entry => (int)entry.Field(0).Get());

            spDataIndex = new FixedIndex<SubjPred<int>> ("s and p of data", dataCell.Root,
                entry =>
                    new SubjPred<int>((int) entry.Field(0).Get(), (int) entry.Field(1).Get()));
            spDirectIndex =new FixedIndex<SubjPred<int>>("s and p of direct", directCell.Root,
                entry =>
                    new SubjPred<int>((int)entry.Field(0).Get(), (int)entry.Field(1).Get()));
            opIndex = new FixedIndex<SubjPred<int>>("o and p of direct", directCell.Root,
                entry =>
                    new SubjPred<int>((int)entry.Field(0).Get(), (int)entry.Field(1).Get()));
        }

        #region Load

        public void Load(int tripletsCountLimit, params string[] filesPaths)
        {
            directCell.Close();
            dataCell.Close();
            predDataCell.Close();
            predObjell.Close();
            idCell.Close();
            textCell.Close();

            File.Delete(dataCellPath);
            File.Delete(directCellPath);
            File.Delete(predDataCellPath);
            File.Delete(predObjellPath);
            File.Delete(idCellPath);
            File.Delete(textCellPath);

            directCell = new PaCell(ptDirects, directCellPath, false);
            dataCell = new PaCell(ptData, dataCellPath, false);
            predDataCell = new PaCell(ptHashValue, predDataCellPath, false);
            predObjell = new PaCell(ptHashValue, predObjellPath, false);
            idCell = new PaCell(ptHashValue, idCellPath, false);
            textCell = new PaCell(ptHashValue, textCellPath, false);

            var directSerialFlow = (ISerialFlow) directCell;
            var dataSerialFlow = (ISerialFlow) dataCell;
            var idSerialFlow = (ISerialFlow)idCell;
            var predObjSerialFlow = (ISerialFlow)predObjell;
            var predDataSerialFlow = (ISerialFlow)predDataCell;
            var textSerialFlow = (ISerialFlow)textCell;
            directSerialFlow.StartSerialFlow();
            dataSerialFlow.StartSerialFlow();
            idSerialFlow.StartSerialFlow();
            predObjSerialFlow.StartSerialFlow();
            predDataSerialFlow.StartSerialFlow();
            textSerialFlow.StartSerialFlow();
            directSerialFlow.S();
            dataSerialFlow.S();
            idSerialFlow.S();
            textSerialFlow.S();
            predObjSerialFlow.S();
            predDataSerialFlow.S();
            var existsId = new ArrayIntMax<bool>();
            var existsText = new ArrayIntMax<bool>();
            var existsPredicate = new ArrayIntMax<bool>();
            int i = 0;
            for (int j = 0; j < filesPaths.Length && i < tripletsCountLimit; j++)
                i += ReadFile(filesPaths[j], (id, property, value, isObj, lang) =>
                {
                    int hashId = id.GetHashCode();
                    if (!existsId[hashId])
                    {
                        idSerialFlow.V(new object[] {hashId, id});
                        existsId[hashId] = true;
                    }
                    int hProperty = property.GetHashCode();
                    if (isObj)
                    {
                        if (!existsPredicate[hProperty])
                        {
                            predObjSerialFlow.V(new object[] {hProperty, property});
                            existsPredicate[hProperty] = true;
                        }
                        var objHash = value.GetHashCode();
                        directSerialFlow.V(new object[] { hashId, hProperty, objHash });
                        if (!existsId[objHash])
                        {
                            existsId[objHash] = true;
                            idSerialFlow.V(new object[] { objHash, value });
                        }
                    }
                    else
                    {
                        if (!existsPredicate[hProperty])
                        {
                            predDataSerialFlow.V(new object[] {hProperty, property});
                            existsPredicate[hProperty] = true;
                        }
                        int hashText = value.GetHashCode();
                        if (!existsText[hashText])
                        {
                            textSerialFlow.V(new object[] { hashText, value });
                            existsText[hashText] = true;
                        }
                        dataSerialFlow.V(new object[] { hashId, hProperty, hashText, lang ?? "" });
                    }
                },
                    tripletsCountLimit);
            directSerialFlow.Se();
            dataSerialFlow.Se();
            idSerialFlow.Se();
            textSerialFlow.Se();
            predObjSerialFlow.Se();
            predDataSerialFlow.Se();
            directSerialFlow.EndSerialFlow();
            dataSerialFlow.EndSerialFlow();
            idSerialFlow.EndSerialFlow();
            textSerialFlow.EndSerialFlow();
            predObjSerialFlow.EndSerialFlow();
            predDataSerialFlow.EndSerialFlow();
        }

        internal void LoadIndexes()
        {
            if (sDataIndex != null)
            {
                sDataIndex.Close();
                sDirectIndex.Close();
                oIndex.Close();
                spDataIndex.Close();
                spDirectIndex.Close();
                opIndex.Close();
                predicateDataIndex.Close();
                predicateObjIndex.Close();
                idIndex.Close();
                textIndex.Close();
            }
            CreateIndexes();
            sDataIndex.Load(null);
            sDirectIndex.Load(null);
            oIndex.Load(null);
            var subjPredComparer = new SubjPredCoparer<int>();
            spDataIndex.Load(subjPredComparer);
            spDirectIndex.Load(subjPredComparer);
            opIndex.Load(subjPredComparer);

            predicateDataIndex.Load(null);
            predicateObjIndex.Load(null);
            idIndex.Load(null);
            textIndex.Load(null);
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

        public string GetItemHashesInterpret(string id)
        {
            return
                sDirectIndex.GetAllByKey(id.GetHashCode())
                    .Select(spo => spo.Type.Interpret(spo.Get()))
                    .Concat(
                        oIndex.GetAllByKey(id.GetHashCode())
                            .Select(spo => spo.Type.Interpret(spo.Get()))
                            .Concat(
                                sDataIndex.GetAllByKey(id.GetHashCode())
                                    .Select(spo => spo.Type.Interpret(spo.Get()))))
                    .Aggregate((all, one) => all + one);
        }

        public XElement GetItemByIdBasic(string id, bool addinverse)
        {
            int hid = id.GetHashCode();
            var type = spDirectIndex.GetFirstByKey(new SubjPred<int>(hid, ONames.rdftypestring.GetHashCode()));
            XElement res = new XElement("record", new XAttribute("id", id), type.offset == long.MinValue ? null : new XAttribute("type", idIndex.GetFirstByKey(((object[])type.Get())[2].GetHashCode()).Field(1).Get()),
                sDataIndex.GetAllByKey(hid).Select(entry => entry.Get()).Cast<object[]>().Select(v3 =>
                    new XElement("field", new XAttribute("prop", predicateDataIndex.GetFirstByKey((int)v3[1]).Field(1).Get()),
                        string.IsNullOrEmpty((string) v3[3]) ? null : new XAttribute(ONames.xmllang, v3[3]),
                       textIndex.GetFirstByKey((int)v3[2]).Field(1).Get())),
                 sDirectIndex.GetAllByKey(hid).Select(entry => entry.Get()).Cast<object[]>().Select(v2 =>
                    new XElement("direct", new XAttribute("prop", predicateObjIndex.GetFirstByKey((int)v2[1]).Field(1).Get()),
                        new XElement("record", new XAttribute("id", idIndex.GetFirstByKey((int)v2[2]).Field(1).Get())))),
                null);
            // Обратные ссылки
            if (addinverse)
            {
                var query = oIndex.GetAllByKey(hid);
                string predicate = null;
                XElement inverse = null;
                foreach (PaEntry en in query)
                {
                    var rec = (object[]) en.Get();
                    string pred = (string) predicateObjIndex.GetFirstByKey((int)rec[1]).Field(1).Get();
                    if (pred != predicate)
                    {
                        res.Add(inverse);
                        inverse = new XElement("inverse", new XAttribute("prop", pred));
                        predicate = pred;
                    }
                    string idd = (string) idIndex.GetFirstByKey((int)rec[0]).Field(1).Get();
                    inverse.Add(new XElement("record", new XAttribute("id", idd)));
                }
                res.Add(inverse);
            }
            return res;
        }
       
    }
}
