using System.IO;
using System.Linq;
using System.Xml.Linq;
using PolarBasedEngine;
using PolarDB;

namespace PolarBasedRDF
{
    internal class RDFTripletsByPolarEngine
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
        private FixedIndex<SubjPred> spDirectIndex, opIndex, spDataIndex;

        public RDFTripletsByPolarEngine(DirectoryInfo path)
        {
            if (!path.Exists) path.Create();
            directCell = new PaCell(ptDirects, directCellPath = Path.Combine(path.FullName, "rdf.direct.pac"),
                File.Exists(directCellPath));
            dataCell = new PaCell(ptData, dataCellPath = Path.Combine(path.FullName, "rdf.data.pac"),
                File.Exists(dataCellPath));
            if (dataCell.IsEmpty || directCell.IsEmpty) return;
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            sDataIndex = new FixedIndex<string>("s of data", dataCell.Root, entry => (string) entry.Field(0).Get());
            sDirectIndex = new FixedIndex<string>("s of direct", directCell.Root, entry => (string) entry.Field(0).Get());
            oIndex = new FixedIndex<string>("o of direct", directCell.Root, entry => (string) entry.Field(2).Get());
            spDataIndex = new FixedIndex<SubjPred>("s and p of data", dataCell.Root,
                entry =>
                 new SubjPred
                    {
                        subj = (string) entry.Field(0).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
            spDirectIndex = new FixedIndex<SubjPred>("s and p of direct", directCell.Root,
                entry =>
                   new SubjPred
                    {
                        subj = (string) entry.Field(0).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
            opIndex = new FixedIndex<SubjPred>("o and p of direct", directCell.Root,
                entry =>
                    new SubjPred
                    {
                        subj = (string) entry.Field(2).Get(),
                        pred = (string) entry.Field(1).Get()
                    });
        }

        #region Load

        public void Load(int tripletsCountLimit, params string[] filesPaths)
        {
            directCell.Close();
            dataCell.Close();
        
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
            ReaderRDF.ReaderRDF.ReadFiles(tripletsCountLimit, filesPaths, (id, property, value, isObj, lang) =>
            {
                if (isObj)
                    directSerialFlow.V(new object[] {id, property, value});
                else dataSerialFlow.V(new object[] {id, property, value, lang ?? ""});
            });
            directSerialFlow.Se();
            dataSerialFlow.Se();
            directSerialFlow.EndSerialFlow();
            dataSerialFlow.EndSerialFlow();
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
                CreateIndexes();
            }
            sDataIndex.Load(null);
            sDirectIndex.Load(null);
            oIndex.Load(null);
            var subjPredComparer = new SubjPredComparer();
            spDataIndex.Load(subjPredComparer);
            spDirectIndex.Load(subjPredComparer);
            opIndex.Load(subjPredComparer);
        }

        #endregion

        public string GetItem(string id)
        {
            return
                sDirectIndex.GetAllByKey(id)
                    .Select(spo => spo.Type.Interpret(spo.Get()))
                    .Concat(
                        oIndex.GetAllByKey(id)
                            .Select(spo => spo.Type.Interpret(spo.Get()))
                            .Concat(
                                sDataIndex.GetAllByKey(id)
                                    .Select(spo => spo.Type.Interpret(spo.Get()))))
                    .Aggregate((all, one) => all + one);
        }

        public XElement GetItemByIdBasic(string id, bool addinverse)
        {
            var type = spDirectIndex.GetFirstByKey(new SubjPred { pred = ONames.rdftypestring, subj = id });
            XElement res = new XElement("record", new XAttribute("id", id), type.offset==long.MinValue ? null : new XAttribute("type", ((object[])type.Get())[2]),
                sDataIndex.GetAllByKey(id).Select(entry => entry.Get()).Cast<object[]>().Select(v3 =>
                    new XElement("field", new XAttribute("prop", v3[1]),
                        string.IsNullOrEmpty((string) v3[3]) ? null : new XAttribute(ONames.xmllang, v3[3]),
                        v3[2])),
                 sDirectIndex.GetAllByKey(id).Select(entry => entry.Get()).Cast<object[]>().Select(v2 =>
                    new XElement("direct", new XAttribute("prop", v2[1]),
                        new XElement("record", new XAttribute("id", v2[2])))),
                null);
            // Обратные ссылки
            if (addinverse)
            {
                var query = oIndex.GetAllByKey(id);
                string predicate = null;
                XElement inverse = null;
                foreach (PaEntry en in query)
                {
                    var rec = (object[]) en.Get();
                    string pred = (string) rec[1];
                    if (pred != predicate)
                    {
                        res.Add(inverse);
                        inverse = new XElement("inverse", new XAttribute("prop", pred));
                        predicate = pred;
                    }
                    string idd = (string) rec[0];
                    inverse.Add(new XElement("record", new XAttribute("id", idd)));
                }
                res.Add(inverse);
            }
            return res;
        }

    }
}
