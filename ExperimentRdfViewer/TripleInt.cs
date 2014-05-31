using System;
using System.Collections.Generic;
using NameTable;

namespace  TrueRdfViewer
{
    public abstract class TripleInt 
    { 
        public int subject, predicate;
        public static IStringIntCoding SiCodingEntities;
        public static IStringIntCoding SiCodingPredicates;
        public static Dictionary<string, int> EntitiesCodeCache = new Dictionary<string, int>();
        public static Dictionary<string, int> PredicatesCodeCache = new Dictionary<string, int>();
     
        public static long totalMilisecondsCodingUsages = 0;
        
        public static int CodeEntities(string s)
        {   
            int c;
            if(!EntitiesCodeCache.TryGetValue(s, out c))
            {
                DateTime st = DateTime.Now;
                   c = SiCodingEntities.GetCode(s);
           //  c = s.GetHashCode();
                totalMilisecondsCodingUsages += (DateTime.Now - st).Ticks/10000;
                EntitiesCodeCache.Add(s, c); //s.GetHashCode() 
            }
            return c;
        }              
        public static string DecodeEntities(int e)
        {
          //   return e.ToString();
            return SiCodingEntities.GetName(e);
        }

        public static int CodePredicates(string s)
        {
            int c;
           // if (!PredicatesCodeCache.TryGetValue(s, out c))
            {
                DateTime st = DateTime.Now;
                c = SiCodingPredicates.GetCode(s);
                //  c = s.GetHashCode();
                totalMilisecondsCodingUsages += (DateTime.Now - st).Ticks / 10000;
            //    PredicatesCodeCache.Add(s, c); //s.GetHashCode() 

            }
            return c;
        }
        public static string DecodePredicates(int e)
        {
            //   return e.ToString();
            return SiCodingPredicates.GetName(e);
        } 


    }
    public class OTripleInt : TripleInt { public int obj; }
    public class DTripleInt : TripleInt { public Literal data;

    public DTripleInt(int subject, int predicate, Literal data)
        {
            this.subject = subject;
            this.predicate = predicate;
            this.data = data;
        }

        public DTripleInt()
        {
        
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
             
    public class SubjPredInt
    {
        public int subj, pred;
        public int CompareTo(object sp)
        {
            int cmp = subj.CompareTo(((SubjPredInt)sp).subj);
            if (cmp != 0) return cmp;
            return pred.CompareTo(((SubjPredInt)sp).pred);
        }
    }
    public class SubjPredObjInt : IComparable
    {
        public int subj, pred, obj;
        public SubjPredObjInt() { }
        public SubjPredObjInt(object pobj)
        {
            object[] rec = (object[])pobj;
            subj = (int)rec[0];
            pred = (int)rec[1];
            obj = (int)rec[2];
        }
        public int CompareTo(object sp)
        {
            SubjPredObjInt target = (SubjPredObjInt)sp;
            int cmp = subj.CompareTo(target.subj);
            if (cmp != 0) return cmp;
            cmp = pred.CompareTo(target.pred);
            if (cmp != 0) return cmp;
            return obj.CompareTo(target.obj); 
        }
    }
    public class SPComparer : IComparer<SubjPredInt>
    {
        public int Compare(SubjPredInt x, SubjPredInt y)
        {
            return x.CompareTo(y);
        }
    }
    public class SPOComparer : IComparer<SubjPredObjInt>
    {
        public int Compare(SubjPredObjInt x, SubjPredObjInt y)
        {
            return x.CompareTo(y);
        }
    }
}
