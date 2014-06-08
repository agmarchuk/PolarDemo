using System.Collections.Generic;

namespace TripleIntClasses
{
    public class SPOComparer : IComparer<SubjPredObjInt>
    {
        public int Compare(SubjPredObjInt x, SubjPredObjInt y)
        {
            return x.CompareTo(y);
        }
    }
}
