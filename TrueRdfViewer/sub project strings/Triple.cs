using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    public abstract class Triple { public string subject, predicate; }
    public class OTriple : Triple { public string obj; }
    public class DTriple : Triple { public Literal data; }
    // Следующие определения содержатся в модуле TripleInt
    //public enum LiteralVidEnumeration { unknown, integer, text, date }
    //public class Literal 
    //{ 
    //    public LiteralVidEnumeration vid; 
    //    public object value;
    //    public override string ToString()
    //    {
    //        switch (vid)
    //        {
    //            case LiteralVidEnumeration.text:
    //                {
    //                    Text txt = (Text)value;
    //                    return "\"" + txt.s + "\"@" + txt.l;
    //                }
    //            default: return value.ToString();
    //        }
    //    }
    //} 
    //public class Text { public string s, l; }

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
    public class SubjPredObj
    {
        public string subj, pred, obj;
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
    //public class SubjPredComparer : IComparer<SubjPred>
    //{
    //    public int Compare(SubjPred x, SubjPred y)
    //    {
    //        return x.CompareTo(y);
    //    }
    //}
}
