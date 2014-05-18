using System;
using System.Collections.Generic;
using System.Globalization;

namespace  TruRDFViewer
{
    public abstract class Triple 
    { 
        public string subject, predicate;
       
        
   
    }
    public class OTriple : Triple { public string obj; }
    public class DTriple : Triple { public Literal data; }
    public enum LiteralVidEnumeration { typedObject, integer, text, date, boolean, nil }
    public class Literal
    {
        protected bool Equals(Literal other)
        {
            return vid == other.vid && Equals(Value, other.Value);
        }



        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)vid * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            }
        }

        public readonly LiteralVidEnumeration vid;

        public string GetString()
        {
            switch (vid)
            {
                case LiteralVidEnumeration.typedObject:
                    return ((TypedObject)Value).Value;
                case LiteralVidEnumeration.text:
                    return ((Text)Value).Value;
                case LiteralVidEnumeration.date:
                    return ((DateTime)Value).ToString(CultureInfo.InvariantCulture);
                case LiteralVidEnumeration.integer:
                case LiteralVidEnumeration.boolean:
                    return Value.ToString();
                case LiteralVidEnumeration.nil:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public Literal(LiteralVidEnumeration vid)
        {
            this.vid = vid;
        }

        public object Value { get; set; }

        public bool HasValue
        {
            get
            {
                return Value is Double && Value == (object)double.MinValue
                       || Value is long && (long)Value == DateTime.MinValue.ToBinary()
                       || Value is Text && !string.IsNullOrEmpty(((Text)Value).Value);
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Literal)obj);
        }
    }
                                                       
    public class TypedObject : ICloneable
    {
        protected bool Equals(TypedObject other)
        {
            return string.Equals(Value, other.Value) && string.Equals(Type, other.Type);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }

        public string Value { get; set; }
        public string Type { get; set; }
        public override string ToString()
        {
            string result = "\"" + Value + "\"";
            if (!string.IsNullOrWhiteSpace(Type))
                result += "^^" + Type;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TypedObject)obj);
        }

        public object Clone()
        {
            return new TypedObject() { Type = Type, Value = Value };
        }
    }
    public class Text : ICloneable
    {
        protected bool Equals(Text other)
        {
            return string.Equals(Value, other.Value) && string.Equals(Lang, other.Lang);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (Lang != null ? Lang.GetHashCode() : 0);
            }
        }

        public string Value { get; set; }
        public string Lang { get; set; }
        public override string ToString()
        {
            string result = "\"" + Value + "\"";
            if (!string.IsNullOrWhiteSpace(Lang))
                result += "@" + Lang;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Text)obj);
        }

        public object Clone()
        {
            return new Text() { Lang = Lang, Value = Value };

        }
    }
             
    public class SubjPred
    {
        public string subj, pred;
        public int CompareTo(object sp)
        {
            int cmp = subj.CompareTo(((SubjPred)sp).subj);
            if (cmp != 0) return cmp;
            return pred.CompareTo(((SubjPred)sp).pred);
        }
    }
    public class SubjPredObj : IComparable
    {
        public string subj, pred, obj;
        public SubjPredObj() { }
        public SubjPredObj(object pobj)
        {
            object[] rec = (object[])pobj;
            subj = (string) rec[0];
            pred = (string) rec[1];
            obj = (string) rec[2];
        }
        public int CompareTo(object sp)
        {
            SubjPredObj target = (SubjPredObj)sp;
            int cmp = subj.CompareTo(target.subj);
            if (cmp != 0) return cmp;
            cmp = pred.CompareTo(target.pred);
            if (cmp != 0) return cmp;
            return obj.CompareTo(target.obj); 
        }
    }
    public class SPComparer : IComparer<SubjPred>
    {
        public int Compare(SubjPred x, SubjPred y)
        {
            return x.CompareTo(y);
        }
    }
    public class SPOComparer : IComparer<SubjPredObj>
    {
        public int Compare(SubjPredObj x, SubjPredObj y)
        {
            return x.CompareTo(y);
        }
    }
}
