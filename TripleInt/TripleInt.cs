using System;
using System.Collections.Generic;
using NameTable;

namespace TripleIntClasses
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
        public static int Code(string s) { return s.GetHashCode(); }
        public static string Decode(int e) { return "noname" + e; }

    }
}