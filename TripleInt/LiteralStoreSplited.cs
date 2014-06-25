using System;
using System.Collections.Generic;
using System.IO;
using PolarDB;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class LiteralStoreSplited
    {                   
        public PaCell stringsCell;   
        public PaCell doublesCell;
        public PaCell boolsCell;
        public PaCell typedObjectsCell;
        private List<Literal> writeBuffer;
        private static string dataCellPath;
        private static LiteralStoreSplited literals;

        public LiteralStoreSplited()
        {
                    
        }


        public static LiteralStoreSplited Literals
        {
            get
            {
                if (literals == null)
                {
                    literals=new LiteralStoreSplited();
                    literals.Open(false);
                }
                return literals;
            }
        }

        public static string DataCellPath
        {
            set
            {
               dataCellPath = value + "/literals";
                if (!Directory.Exists(dataCellPath)) Directory.CreateDirectory(dataCellPath);
            }
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

            
        }

        public void Clear()
        {
             doublesCell.Clear();
                doublesCell.Fill(new object[0]);
            stringsCell.Clear();
                stringsCell.Fill(new object[0]);
            typedObjectsCell.Clear();
                typedObjectsCell.Fill(new object[0]);
            boolsCell.Clear();
                boolsCell.Fill(new object[0]);
        }

        public void WarmUp()
        {
            foreach (var t in stringsCell.Root.ElementValues()) ;
            foreach (var t in boolsCell.Root.ElementValues()) ;
            foreach (var t in typedObjectsCell.Root.ElementValues()) ;
            foreach (var t in doublesCell.Root.ElementValues()) ;
        }

        public Literal Read(long offset, int predicateCode)
        {
            LiteralVidEnumeration? literalVidEnumeration = TripleInt.PredicatesCoding.LiteralVid[predicateCode];
            if (literalVidEnumeration == null) throw new Exception("object predicate call literal");
            var literal = new Literal(literalVidEnumeration.Value);
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
                    object[] stringLiteralObj = (object[]) Read(stringsCell, offset);
                    literal.Value = new Text()
                    {
                        Value = (string) stringLiteralObj[0],
                        Lang = (string) stringLiteralObj[1]
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

        object Read(PaCell fromCell, long offset)
        {
            var paEntry = fromCell.Root.Element(0);
            paEntry.offset = offset;
            return paEntry.Get();
        }

        public Literal Write(Literal literal)
        {
            if (writeBuffer == null) writeBuffer = new List<Literal>();
            writeBuffer.Add(literal);
            if (writeBuffer.Count >= 1000)
                WriteBufferForce();
            return literal;
        }

        public void WriteBufferForce()
        {         
            foreach (var lit in writeBuffer)
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
             
            }
            writeBuffer.Clear();
           Flush();
        }

        private void Flush()
        {
            typedObjectsCell.Flush();
            stringsCell.Flush();
            boolsCell.Flush();
             doublesCell.Flush();
        }
    }
}