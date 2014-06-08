using System;

namespace TripleIntClasses
{
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
}