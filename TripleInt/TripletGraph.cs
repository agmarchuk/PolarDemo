using System.Collections.Generic;

namespace TripleIntClasses
{
    public class TripletGraph
    {
        public string subject;
        public List<KeyValuePair<int, string>> PredicateObjValuePairs=new List<KeyValuePair<int, string>>(); 
        public List<KeyValuePair<int, Literal>> PredicateDataValuePairs = new List<KeyValuePair<int, Literal>>();
    }
}