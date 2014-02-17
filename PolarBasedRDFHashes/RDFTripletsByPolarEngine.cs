using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PolarBasedEngine;
using PolarBasedRDF;
using PolarDB;

namespace PolarBasedRDFHashes
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
            new NamedType("l", new PTypeFString(2))
            ));
        private  readonly  PType ptHashValue=new PTypeSequence(new PTypeRecord(
            new NamedType("hash of id", new PType(PTypeEnumeration.integer)),
            new NamedType("id", new PType(PTypeEnumeration.sstring))));

        private PaCell directCell, dataCell, predDataCell, predObjell, idCell, textCell;
        private readonly string directCellPath, dataCellPath, predDataCellPath, predObjellPath, idCellPath, textCellPath;
        private FixedIndex<int> oIndex, predicateDataIndex, predicateObjIndex, idIndex, textIndex;
        private  FixedIndex<SubjPred<int>> opIndex;//spDataIndex, spDirectIndexs, DirectIndexs, DataIndex

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
            idCell = new PaCell(new PTypeSequence(new PTypeRecord(new NamedType("hash",new PType(PTypeEnumeration.integer)), new NamedType("id",new PTypeFString(20)))), idCellPath = Path.Combine(path.FullName, "ids.pac"),
               File.Exists(idCellPath));
            textCell = new PaCell(ptHashValue, textCellPath = Path.Combine(path.FullName, "texts.pac"), File.Exists(textCellPath));
            if (dataCell.IsEmpty || directCell.IsEmpty) return;
            CreateIndexes();
        }

        private void CreateIndexes()
        {
           // sDataIndex = new FixedIndex<SortItem<int>>("s of data", dataCell.Root, entry => new SortItem<int>((int) entry.Field(0).Get()));
            //sDirectIndex = new FixedIndex<SortItem<int>>("s of direct", directCell.Root, entry =>new SortItem<int>((int)entry.Field(0).Get()));
            oIndex = new FixedIndex<int>("o of direct", directCell.Root, entry => (int)entry.Field(2).Get());

            predicateObjIndex = new FixedIndex<int>("predicate of object", predObjell.Root, entry => (int)entry.Field(0).Get());
            predicateDataIndex = new FixedIndex<int>("predicate of data", predDataCell.Root, entry =>(int)entry.Field(0).Get());
            idIndex = new FixedIndex<int>("id", idCell.Root, entry =>(int)entry.Field(0).Get());
            textIndex = new FixedIndex<int>("text", textCell.Root, entry => (int)entry.Field(0).Get());

            //spDataIndex = new FixedIndex<SubjPred<int>> ("s and p of data", dataCell.Root,
            //    entry =>
            //        new SubjPred<int>((int) entry.Field(0).Get(), (int) entry.Field(1).Get()));
            //spDirectIndex =new FixedIndex<SubjPred<int>>("s and p of direct", directCell.Root,
            //    entry =>
            //        new SubjPred<int>((int)entry.Field(0).Get(), (int)entry.Field(1).Get()));
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
            idCell = new PaCell(new PTypeSequence(new PTypeRecord(new NamedType("hash", new PType(PTypeEnumeration.integer)), new NamedType("id", new PTypeFString(20)))), idCellPath, false);
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
            var existsId = new HashSet<int>();// ArrayIntMax<bool>();
            var existsText  = new HashSet<int>();// = new ArrayIntMax<bool>();
            var existsPredicate = new HashSet<int>();// = new ArrayIntMax<bool>();
           ReaderRDF.ReaderRDF.ReadFiles(tripletsCountLimit, filesPaths,
            (id, property, value, isObj, lang) =>
                {
                    int hashId = id.GetHashCode();
                    if (!existsId.Contains(hashId))
                    {
                        idSerialFlow.V(new object[] {hashId, id});
                        existsId.Add(hashId);
                    }
                    int hProperty = property.GetHashCode();
                    if (isObj)
                    {
                        if (!existsPredicate.Contains(hProperty))
                        {
                            predObjSerialFlow.V(new object[] {hProperty, property});
                            existsPredicate.Add(hProperty);
                        }
                        var objHash = value.GetHashCode();
                        directSerialFlow.V(new object[] { hashId, hProperty, objHash });
                        if (!existsId.Contains(objHash))
                        {
                            existsId.Add(objHash);
                            idSerialFlow.V(new object[] { objHash, value });
                        }
                    }
                    else
                    {
                        if (!existsPredicate.Contains(hProperty))
                        {
                            predDataSerialFlow.V(new object[] {hProperty, property});
                            existsPredicate.Add(hProperty);
                        }
                        int hashText = value.GetHashCode();
                        if (!existsText.Contains(hashText))
                        {
                            textSerialFlow.V(new object[] { hashText, value });
                            existsText.Add(hashText);
                        }
                        dataSerialFlow.V(new object[] { hashId, hProperty, hashText, lang ?? "" });
                    }
                });
               
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
            if (oIndex != null)
            {
              //  sDataIndex.Close();
              //  sDirectIndex.Close();
                oIndex.Close();
               //spDataIndex.Close();
                //spDirectIndex.Close();
                opIndex.Close();
                predicateDataIndex.Close();
                predicateObjIndex.Close();
               // idIndex.Close();
                textIndex.Close();
            }
            CreateIndexes();
           // var itemCOmparer = new SortItemComparer<int>();
         //   sDataIndex.Load(itemCOmparer);
          //  sDirectIndex.Load(itemCOmparer);
            oIndex.Load(null);
            var subjPredComparer = new SubjPredCoparer<int>();
            //spDataIndex.Load(subjPredComparer);
            //spDirectIndex.Load(subjPredComparer);
            dataCell.Root.SortByKey(o =>
            {
                var tri = ((object[])o);
                return new SubjPred<int>((int)tri[0], (int)tri[1]);
            }, subjPredComparer);
            directCell.Root.SortByKey(o =>
            {
                var tri = ((object[])o);
                return new SubjPred<int>((int)tri[0], (int)tri[1]);
            }, subjPredComparer);
         
            idCell.Root.SortByKey(o => (int)((object[])o)[0]);
            opIndex.Load(subjPredComparer);

            predicateDataIndex.Load(null);
            predicateObjIndex.Load(null);
           // idIndex.Load(null);
            textIndex.Load(null);
        }

        //class SortItemComparer<T> : IComparer<SortItem<T>> where T : IComparable
        //{
        //    public int Compare(SortItem<T> x, SortItem<T> y)
        //    {
        //        return x.CompareTo(y);
        //    }
        //}
        //class SortItem<T> : IComparable where T : IComparable
        //{
        //    private readonly T item;

        //    public SortItem(T item)
        //    {
        //        this.item = item;
        //    }
        //    public int CompareTo(object obj)
        //    {
        //        return item.CompareTo(((SortItem<T>)obj).item);
        //    }
        //}
     
        #endregion

        //public string GetItemHashesInterpret(string id)
        //{
        //    return
        //        sDirectIndex.GetAllByKey(new SortItem<int>(id.GetHashCode()))
        //            .Select(spo => spo.Type.Interpret(spo.Get()))
        //            .Concat(
        //                oIndex.GetAllByKey(new SortItem<int>(id.GetHashCode()))
        //                    .Select(spo => spo.Type.Interpret(spo.Get()))
        //                    .Concat(
        //                        sDataIndex.GetAllByKey(new SortItem<int>(id.GetHashCode()))
        //                            .Select(spo => spo.Type.Interpret(spo.Get()))))
        //            .Aggregate((all, one) => all + one);
        //}

        public XElement GetItemByIdBasic(string id, bool addinverse)
        {
            int hid = id.GetHashCode();
            var rdfTypeCode = ONames.rdftypestring.GetHashCode();
            var type = directCell.Root.BinarySearchFirst((entry =>
            {
                int compareTo = hid.CompareTo((entry.Field(0).Get()));
                return compareTo !=0 ? compareTo : ((int)entry.Field(1).Get()).CompareTo(rdfTypeCode);
            }));
            XElement res = new XElement("record", new XAttribute("id", id), type.offset == long.MinValue ? null : new XAttribute("type", idIndex.GetFirstByKey(((object[])type.Get())[2].GetHashCode()).Field(1).Get()),
                dataCell.Root.BinarySearchAll(entry => hid.CompareTo(entry.Field(0).Get())).Select(entry => entry.Get()).Cast<object[]>().Select(v3 =>
                    new XElement("field", new XAttribute("prop", predicateDataIndex.GetFirstByKey((int)v3[1]).Field(1).Get()),
                        string.IsNullOrEmpty((string) v3[3]) ? null : new XAttribute(ONames.xmllang, v3[3]),
                       textIndex.GetFirstByKey((int)v3[2]).Field(1).Get())),
                directCell.Root.BinarySearchAll(entry => hid.CompareTo(entry.Field(0).Get())).Select(entry => entry.Get()).Cast<object[]>().Select(v2 =>
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
