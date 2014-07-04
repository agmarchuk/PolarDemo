using System;
using PolarDB;
using TripleIntClasses;

namespace LiteralStores
{
    public class LiteralStoreSplited : LiteralStoreAbstract
    {
        public PaCell stringsCell;
        public PaCell typedObjectsCell;


        public LiteralStoreSplited(string path, NameSpaceStore nameSpaceStore) : base(path, nameSpaceStore)
        {
            stringsCell = new PaCell(new PTypeSequence(new PTypeRecord(new NamedType("string value", new PType(PTypeEnumeration.sstring)), new NamedType("lang", new PType(PTypeEnumeration.sstring)))), path + "strings.pac", false);
            typedObjectsCell = new PaCell(new PTypeSequence(new PTypeRecord(new NamedType("string value", new PType(PTypeEnumeration.sstring)), new NamedType("type", new PType(PTypeEnumeration.sstring)))), path + "typedObject.pac", false);
        }

        public override void Clear()
        {
            stringsCell.Clear();
            typedObjectsCell.Clear();
            stringsCell.Fill(new object[0]);
            typedObjectsCell.Fill(new object[0]);
        }

        public override void WarmUp()
        {
            foreach (var elementValue in stringsCell.Root.ElementValues()) ;
            foreach (var elementValue in typedObjectsCell.Root.ElementValues()) ;
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
                    literal.Value = BitConverter.Int64BitsToDouble(offset);
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
                    literal.Value = offset > 0;
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
                    lit.Offset = typedObjectsCell.Root.AppendElement(new object[] {valu.Value, valu.Type});
                    break;
                case LiteralVidEnumeration.integer:
                    lit.Offset = BitConverter.DoubleToInt64Bits(System.Convert.ToDouble(lit.Value));
                    break;
                case LiteralVidEnumeration.text:
                    Text value = (Text) lit.Value;
                    lit.Offset = stringsCell.Root.AppendElement(new object[] {value.Value, value.Lang});
                    break;
                case LiteralVidEnumeration.date:
                    lit.Offset = (long) lit.Value;
                    break;
                case LiteralVidEnumeration.boolean:
                    lit.Offset = (bool) lit.Value ? 1 : 0;
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
            return lit;
        }

        public override void Flush()
        {
            typedObjectsCell.Flush();
            stringsCell.Flush();
        }
    }
}