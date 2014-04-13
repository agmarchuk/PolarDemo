using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using PolarDB;

namespace NameTable
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";

            StringIntCoding sic = new StringIntCoding(path);

            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;
            DateTime tt00 = tt0;

            int portion_size = 5000000;
            int n_portions = 20;

            sic.Clear();
            HashSet<string> hs = new HashSet<string>();
            //SortedSet<string> hs = new SortedSet<string>();
            for (int j = 0; j < n_portions; j++)
            {
                tt0 = DateTime.Now;
                hs.Clear();
                for (int i = 0; i < portion_size; i++)
                {
                    string id = (Guid.NewGuid()).ToString();
                    hs.Add(id);
                }
                Console.WriteLine("Set ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                string[] arr = new string[hs.Count];
                hs.CopyTo(arr);
                Array.Sort<string>(arr);
                Console.WriteLine("Sorting ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                var dic = sic.InsertPortion(arr);
                Console.WriteLine("InsertPortion ok. portion=" + j + " HashSet.Size=" + hs.Count + " duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            Console.WriteLine("Total: {0}", (DateTime.Now - tt00).Ticks / 10000L);
        }
        public static void Main2(string[] args)
        {
            string path = @"..\..\..\Databases\";
            System.IO.StreamReader sr = new System.IO.StreamReader(@"F:\FactographData\freebase-rdf-2013-02-10-00-00.nt2");

            StringIntCoding sic = new StringIntCoding(path);

            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;

            string line = "";
            int linecnt = 0;
            int nportion = 20000000;

            List<string> ids = null;
            HashSet<string> hs = new HashSet<string>();
            for (int j = 0; j < 10; j++)
            {
                tt0 = DateTime.Now;
                ids = new List<string>(nportion * 2);
                hs.Clear();
                for (int i = 0; i < nportion; i++)
                {
                    line = sr.ReadLine(); linecnt++;
                    //if (linecnt % 1000 == 0) Console.Write(" " + linecnt);
                    if (line == null) break;
                    if (line.Length == 0) continue;
                    if (line[0] == '@') continue;
                    string[] parts = line.Split('\t');
                    if (parts.Length != 3) continue;
                    //ids.Add(parts[0]);
                    //ids.Add(parts[1]);
                    hs.Add(parts[0]);
                    char fc = parts[2][0];
                    if (fc != '\"' && fc != '<' && fc != '-' && !char.IsDigit(fc))
                    {
                        string ss = parts[2].Substring(0, parts[2].Length - 1);
                        //if (ss != "true" && ss != "false") ids.Add(ss);
                        if (ss != "true" && ss != "false") hs.Add(ss);
                    }
                    
                    //ids.Add(Guid.NewGuid().ToString());
                }
                //Console.WriteLine("idlist ok. line="+ (linecnt / 1000000) +" HashSet.Size=" + hs.Count + " duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                string[] arr = new string[hs.Count];
                hs.CopyTo(arr);
                Array.Sort<string>(arr);
                //Console.WriteLine("Sorting ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                var dic = sic.InsertPortion(arr);
                Console.WriteLine("InsertPortion ok. line=" + (linecnt / 1000000) + " HashSet.Size=" + hs.Count + " duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
                if (line == null) break;
                tt0 = DateTime.Now;
                // Сборка мусора
                GC.Collect();
                //Console.WriteLine("GC ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            }
            tt0 = DateTime.Now;
            sic.MakeIndexed();
            Console.WriteLine("Indexes ok. Count=" + sic.Count() + " duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            //Console.WriteLine("dic count=" + dic.Count());

            //int code = sic.GetCode("zzz");
            //Console.WriteLine(code);
            //string name = sic.GetName(5);
            //Console.WriteLine(name);
            //Console.WriteLine("count=" + sic.Count());
            //Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L);
        }
        public static void Main1(string[] args)
        {
            string path = @"..\..\..\Databases\";
            PType seq = new PTypeSequence(new PType(PTypeEnumeration.sstring));
            string originalCell = path + "original_nt.pac";
            string tmpCell = path + "tmp_nt.pac";
            string sourceCell = path + "source_nt.pac";
            Console.WriteLine("Start");
            PaCell cell = new PaCell(seq, originalCell, false);
            DateTime tt0 = DateTime.Now;

            List<string> namePool = new List<string>(100000);
            for (int i = 0; i < 100000; i++)
            {
                namePool.Add(Guid.NewGuid().ToString());
            }
            SortedSet<string> ss = new SortedSet<string>(namePool); //var q = ss.ElementAt<string>(10);
            string[] ssa = ss.ToArray();
            Console.WriteLine("ssa.Count=" + ssa.Count());
            // Буду проводить слияние

            cell.Close();
            System.IO.File.Move(originalCell, sourceCell);
            if (!System.IO.File.Exists(tmpCell))
            {
                PaCell tmp = new PaCell(seq, tmpCell, false);
                tmp.Fill(new object[0]);
                tmp.Close();
            }
            System.IO.File.Move(tmpCell, originalCell);
            PaCell source = new PaCell(seq, sourceCell);
            PaCell target = new PaCell(seq, originalCell, false);
            if (!target.IsEmpty) target.Clear();
            target.Fill(new object[0]);

            int ssa_ind = 0;
            bool ssa_notempty = ssa_ind < ssa.Count() ? true : false; //ssa_notempty = false;
            string ssa_current = ssa_notempty ? ssa[ssa_ind] : null;
            ssa_ind++;

            long nn = source.Root.Count();
            Console.WriteLine("длина последовательности: " + nn);
            foreach (var val in source.Root.ElementValues())
            {
                // Пропускаю элементы из нового потока, которые меньше текущего сканированного элемента 
                string s = (string)val;
                int cmp = 0;
                while (ssa_notempty && (cmp = ssa_current.CompareTo(s)) <= 0)
                {
                    if (cmp < 0) target.Root.AppendElement(ssa_current); // При равенстве, новый элемент игнорируется
                    if (ssa_ind < ssa.Count())
                    {
                        ssa_current = ssa[ssa_ind]; //ssa.ElementAt<string>(ssa_ind);
                        ssa_ind++;
                    }
                    else
                    {
                        ssa_notempty = false;
                    }
                }
                target.Root.AppendElement(s);
            }
            target.Close();
            source.Close();
            System.IO.File.Move(sourceCell, tmpCell);



            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L);
        }
        public static void Main0(string[] args)
        {
            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;

            NameTableInt ntab = new NameTableInt(path);
            Console.WriteLine(ntab.Count());

            //ntab.SetNameGetCode("aaaaaaaaaaaaaaaaa");
            //ntab.SetNameGetCode("bbb");
            //ntab.SetNameGetCode("aa2");
            //ntab.SetNameGetCode("aa1");
            //var query = ntab.GetKeys().ToArray();
            //foreach (var k in query) Console.WriteLine(k);
            //Console.WriteLine();
            //ntab.SetNameGetCode("aaa_");
            //ntab.SetNameGetCode("bbb_");
            //ntab.SetNameGetCode("aa2_");
            //ntab.SetNameGetCode("aa1");
            //foreach (var k in ntab.GetKeys()) Console.WriteLine(" " + ntab.GetName(k));
            //ntab.Close();
            //return;

            ntab.PushName("aaa");
            ntab.PushName("bbb");
            ntab.PushName("aaa");
            // Генератор идентификаторов
            for (int i = 0; i < 100000; i++)
            {
                Guid guid = Guid.NewGuid();
                string id_str = guid.ToString();
                //ntab.SetNameGetCode(id_str);
                ntab.PushName(id_str);
                if ((i + 1) % 10000 == 0) Console.Write(" " + (i+1)); 
            }
            ntab.Close();
            Console.WriteLine();
            Console.WriteLine("duration=" + (DateTime.Now - tt0).Ticks / 10000L);
        }
    }
}
