using System;

namespace TripleIntClasses
{
    public class SubjPredInt :IComparable
    {
        public int subj, pred;
        public int CompareTo(object sp)
        {
            int cmp = subj.CompareTo(((SubjPredInt)sp).subj);
            if (cmp != 0) return cmp;
            return pred.CompareTo(((SubjPredInt)sp).pred);
        }
    }
}