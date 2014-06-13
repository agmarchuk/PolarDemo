using System;
using System.Collections.Generic;

namespace TripleIntClasses
{
    public class OTripleInt : TripleInt
    {
        public int obj;
        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            var oTripleInt = (obj as OTripleInt);
            return oTripleInt != null && (oTripleInt.obj==this.obj && oTripleInt.predicate==predicate && oTripleInt.subject==subject);
        }

// override object.GetHashCode
        public override int GetHashCode()
        {   

            return
                new KeyValuePair<KeyValuePair<int, int>, int>(new KeyValuePair<int, int>(subject, predicate), obj)
                    .GetHashCode();
        }
    }
}