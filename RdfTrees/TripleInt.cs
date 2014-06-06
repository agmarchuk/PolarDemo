using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfTrees
{
    public abstract class TripleInt
    {
        public int subject, predicate;
        public static int Code(string s) { return s.GetHashCode(); }
        public static string Decode(int e) { return "noname" + e; }
    }
    public class OTripleInt : TripleInt { public int obj; }
    public class DTripleInt : TripleInt { public Literal data; }
    public enum LiteralVidEnumeration { unknown, integer, text, date }
    public class Literal
    {
        public LiteralVidEnumeration vid;
        public object value;
        public override string ToString()
        {
            switch (vid)
            {
                case LiteralVidEnumeration.text:
                    {
                        Text txt = (Text)value;
                        return "\"" + txt.s + "\"@" + txt.l;
                    }
                default: return value.ToString();
            }
        }
        public override bool Equals(object obj)
        {
            //TODO: Надо еще язык приделать
            if (vid == LiteralVidEnumeration.text) return ((Text)value).s.CompareTo(obj) == 0;
            return value.Equals(obj);
        }
    }
    public class Text { public string s, l; }

    public class SubjPredInt : IComparable
    {
        public int subj, pred;
        public int CompareTo(object sp)
        {
            int cmp = subj.CompareTo(((SubjPredInt)sp).subj);
            if (cmp != 0) return cmp;
            return pred.CompareTo(((SubjPredInt)sp).pred);
        }
        public static SubjPredInt MinValue
        {
            get { return new SubjPredInt() { subj = Int32.MinValue, pred = Int32.MinValue }; }
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
