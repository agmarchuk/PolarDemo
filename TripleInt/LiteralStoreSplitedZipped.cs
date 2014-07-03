using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Huffman;
using NameTable;
using PolarDB;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class LiteralStoreSplitedZipped : LiteralStoreAbstract
    {                   
        public PaCell stringsCell;   
        public PaCell doublesCell;
        public PaCell boolsCell;
        public PaCell typedObjectsCell;
        private List<Literal> writeBuffer;
      
     
        private Archive stringsArhive;
        private PaCell StringsArchedCell;

        public LiteralStoreSplitedZipped(string path, NameSpaceStore nameSpaceStore) : base(path, nameSpaceStore)
        {
            
        }

     
     

        public void Open(bool readOnlyMode)
        {
            doublesCell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.real)), dataCellPath + "/doublesLiterals.pac", readOnlyMode);
            var pTypeString = new PType(PTypeEnumeration.sstring);
            var pTypeStringsPair = new PTypeRecord(new NamedType("value", pTypeString),
                new NamedType("add info", pTypeString));
            stringsCell = new PaCell(new PTypeSequence(pTypeStringsPair), dataCellPath + "/stringsLiterals.pac", readOnlyMode);
            boolsCell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.boolean)), dataCellPath + "/booleansLiterals.pac", readOnlyMode);
            typedObjectsCell = new PaCell(new PTypeSequence(pTypeStringsPair), dataCellPath + "/typedObjectsLiterals.pac", readOnlyMode);
            stringsArhive=new Archive(dataCellPath+"/strings archive");
            var ptypeCode = new PTypeSequence(new PType(PTypeEnumeration.@byte));
            StringsArchedCell = new PaCell(new PTypeSequence(new PTypeRecord(new NamedType("string code", ptypeCode), new NamedType("lang code", ptypeCode))), dataCellPath + "/strings archive/binary data", false);
        }

        public override void Clear()
        {
             doublesCell.Clear();
                doublesCell.Fill(new object[0]);
            stringsCell.Clear();
                stringsCell.Fill(new object[0]);
            typedObjectsCell.Clear();
                typedObjectsCell.Fill(new object[0]);
            boolsCell.Clear();
                boolsCell.Fill(new object[0]);
            stringsArhive.Clear();     
            StringsArchedCell.Clear();
            StringsArchedCell.Fill(new object[0]);
        }

        public override void WarmUp()
        {
          //  foreach (var t in stringsCell.Root.ElementValues()) ;
            foreach (var t in StringsArchedCell.Root.ElementValues()) ;
            foreach (var t in boolsCell.Root.ElementValues()) ;
            foreach (var t in typedObjectsCell.Root.ElementValues()) ;
            foreach (var t in doublesCell.Root.ElementValues()) ;
        }

        public override Literal Read(long offset, LiteralVidEnumeration? vid)
        {
       
          
            if (vid == null) throw new Exception("object predicate call literal");
            var literal = new Literal(vid.Value);
            switch (literal.vid)
            {
                case LiteralVidEnumeration.typedObject:
                    object[] typedLiteralObj = (object[]) Read(typedObjectsCell, offset);
                    literal.Value = new TypedObject()
                    {
                        Value = (string) typedLiteralObj[0],
                        Type = (string) typedLiteralObj[1]
                    };
                    break;
                case LiteralVidEnumeration.integer:
                    literal.Value = Read(doublesCell, offset);
                    break;
                case LiteralVidEnumeration.text:
                    object[] stringLiteralObj = (object[]) Read(StringsArchedCell, offset);
                    literal.Value = new Text()
                    {
                        Value = stringsArhive.Decompress(((object[]) stringLiteralObj[0]).Cast<byte>().ToArray()),
                        Lang = stringsArhive.Decompress(((object[])stringLiteralObj[1]).Cast<byte>().ToArray())
                    };
                    break;
                case LiteralVidEnumeration.date:
                    literal.Value = offset;
                    break;
                case LiteralVidEnumeration.boolean:
                    literal.Value = Read(boolsCell, offset);
                    break;
                case LiteralVidEnumeration.nil: 
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return literal;
        }

     

        public override Literal Write(Literal lit)
        {
           
                switch (lit.vid)
                {
                    case LiteralVidEnumeration.typedObject:
                        TypedObject valu = (TypedObject) lit.Value;
                        lit.Offset = typedObjectsCell.Root.AppendElement(new object[] { valu.Value, valu.Type});
                        break;
                    case LiteralVidEnumeration.integer:
                        lit.Offset = doublesCell.Root.AppendElement(lit.Value);
                        break;
                    case LiteralVidEnumeration.text:
                        Text value = (Text) lit.Value;
                        stringsArhive.AddFrequency(value.Value);
                        stringsArhive.AddFrequency(value.Lang);
                        lit.Offset = stringsCell.Root.AppendElement(new object[]{ value.Value, value.Lang});
                        break;
                    case LiteralVidEnumeration.date:
                        lit.Offset = (long) lit.Value;
                        break;
                    case LiteralVidEnumeration.boolean:
                        lit.Offset = boolsCell.Root.AppendElement(lit.Value);
                        break;
                    case LiteralVidEnumeration.nil:
                        lit.Offset = long.MaxValue;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var da = Literal.ToObjects(lit);
                //в памяти сам литерал уже не хранится
                lit.Value = null;       
          
           Flush();
            return lit;
        }

        public override void Flush()
        {
            typedObjectsCell.Flush();
            stringsCell.Flush();
            boolsCell.Flush();
             doublesCell.Flush();
        }

        public void Compress(PaCell dtriplets, PredicatesCoding predicatesCoding)
        {
            stringsArhive.WriteCell();
            PaEntry paEntry = stringsCell.Root.Element(0);
            foreach (var dtripletElement in dtriplets.Root.Elements())
            {
                int predicateCode = (int) dtripletElement.Field(1).Get();
                if (predicatesCoding.LiteralVid[predicateCode] == LiteralVidEnumeration.text)
                {
                    PaEntry offsetElement = dtripletElement.Field(2);
                    long offset = (long) offsetElement.Get();
                    paEntry.offset = offset;
                    object[] stri_lang = (object[]) paEntry.Get();
                 
                offsetElement.Set(
                    StringsArchedCell.Root.AppendElement(new object[]{ 
                        stringsArhive.Compress((string)stri_lang[0]).Cast<object>().ToArray(),
                        stringsArhive.Compress((string)stri_lang[01]).Cast<object>().ToArray()}));
                
                }
            }
            StringsArchedCell.Flush();
        }
    }
}