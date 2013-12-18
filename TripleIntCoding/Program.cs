using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PolarDB;

namespace TripleIntCoding
{
    public class PairInt : IComparable
    { 
        public int first, second;
        public int CompareTo(object v2)
        {
            PairInt p2 = (PairInt)v2;
            int cmp = first.CompareTo(p2.first);
            if (cmp != 0) return cmp;
            return second.CompareTo(p2.second);
        }
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            Random rnd = new Random();
            string path = @"..\..\..\Databases\";
            PType tp_oprops = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", new PType(PTypeEnumeration.integer)),
                new NamedType("predicate", new PType(PTypeEnumeration.integer)),
                new NamedType("object", new PType(PTypeEnumeration.integer))));
            PType tp_subject_index = new PTypeSequence(new PType(PTypeEnumeration.longinteger));
            PaCell oprops = new PaCell(tp_oprops, path + "opreps.pac", false);
            PaCell subject_index = new PaCell(tp_subject_index, path + "subj_ind.pac", false);

            Console.WriteLine("Start");

            DateTime tt0 = DateTime.Now;

            bool toload = true;
            if (toload)
            {
                oprops.Clear();
                subject_index.Clear();
                oprops.Fill(new object[0]);
                subject_index.Fill(new object[0]);
                for (int i = 0; i < 100000000; i++)
                {
                    object[] valu = new object[] { rnd.Next(), rnd.Next(), 999 };
                    long offset = oprops.Root.AppendElement(valu);
                    subject_index.Root.AppendElement(offset);
                    if (i % 1000000 == 0) Console.Write(" " + i);
                }
                Console.WriteLine();
                int[][] samples = 
{ new int[] { 777, 21 }, new int[] { 777, 19 }, new int[] { 777, 22 }, new int[] { 7777777, 21 }, new int[] { 777, 18 } };
                foreach (int[] sa in samples)
                {
                    object[] valu = new object[] { sa[0], sa[1], 999 };
                    long offset = oprops.Root.AppendElement(valu);
                    subject_index.Root.AppendElement(offset);
                }
                oprops.Flush();
                subject_index.Flush();
                Console.WriteLine("Loading ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

                PaEntry.bufferBytes = 400000000;
                PaEntry oentry = oprops.Root.Element(0);

                //subject_index.Root.SortByKey<int>(off =>
                //{
                //    oentry.offset = (long)off;
                //    return (int)oentry.Field(0).Get();
                //});
                subject_index.Root.SortByKey<PairInt>(off =>
                {
                    oentry.offset = (long)off;
                    object[] triple = (object[])oentry.Get();
                    return new PairInt() { first = (int)triple[0], second = (int)triple[1] };
                });
                Console.WriteLine("Sort ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            PaEntry oentry2 = oprops.Root.Element(0);
            var query = subject_index.Root.BinarySearchAll(ent =>
            {
                long off = (long)ent.Get();
                oentry2.offset = off;
                int sub = (int)oentry2.Field(0).Get();
                return sub.CompareTo(777);
            });
            Console.WriteLine("BinarySearch ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            foreach (var ent in query)
            {
                oentry2.offset = (long)ent.Get();
                object[] valu = (object[])oentry2.Get();
                Console.WriteLine("{0} {1} {2}", valu[0], valu[1], valu[2]);
            }
            
            Console.WriteLine("Finish. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }
    }
}
