using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SortNumbersByDiapasons
{
    internal class Program
    {
        private class Diapason
        {
            public int start = 0;
            public int count = 0;
            public int position=0;

            public Diapason(int start, int count)
            {
                this.start = start;
                this.count = count;
            }
        }

        private static void Main(string[] args)
        {
            Random r = new Random();
            Func<int> GetNum = r.Next;
            int[] source = Enumerable.Range(0, 500*1000*1000).Select(i => GetNum()).ToArray();
            for (int k =10; k < 32; k++)
            {
                var start = DateTime.Now;

                Console.WriteLine("K "+k);

                SortedList<int, Diapason> diapasons = new SortedList<int, Diapason>();
             int[] copy=new int[source.Length];
                Array.Copy(source, copy,source.Length);
                SortWithArraysRecursive(k, copy,10, source.Length-10);
                Console.WriteLine(DateTime.Now - start);
                for (int i = 11; i < copy.Length; i++)
                {
                    if(copy[i]<copy[i-1]) throw new Exception();
                }
            }

        }

        private static int[] SortWithSortedListAndArraySort(int k, int[] source)
        {
            SortedList<int, Diapason> diapasons=new SortedList<int, Diapason>();
            Func<int, int> GetDiapason = x => x >> k;
            foreach (int num in source)
            {
                Diapason diapason;
                if (!diapasons.TryGetValue(GetDiapason(num), out diapason))
                    diapasons.Add(GetDiapason(num), new Diapason(0, 1));
                else diapason.count++;
            }
            int sum = 0;
            foreach (var diapason in diapasons.Values)
            {
                diapason.position = diapason.start = sum;
                sum += diapason.count;
            }
            int[] dest = new int[source.Length];
            foreach (int num in source)
            {
                var diapason = diapasons[GetDiapason(num)];

                dest[diapason.position] = num;
                diapason.position++;
            }
            foreach (var diapason in diapasons)
                Array.Sort(dest, diapason.Value.start, diapason.Value.count);
            Console.WriteLine("diapasons.Count " + diapasons.Count);
            return dest;
        }
        private static int[] SortWithSortedListRecursive(int k, int[] source, int start=0, int length=-1)
        {
            if(k==0)
                Console.WriteLine();
            if (length == -1) length = source.Length;
            if (length == 0 || length == 1) return source;
            SortedList<int, Diapason> diapasons = new SortedList<int, Diapason>();
            Func<int, int> GetDiapason = x => x >> k;
            for (int index = start; index < start + length; index++)
            {
                int num = source[index];
                Diapason diapason;
                if (!diapasons.TryGetValue(GetDiapason(num), out diapason))
                    diapasons.Add(GetDiapason(num), new Diapason(0, 1));
                else diapason.count++;
            }
            int sum = 0;
            foreach (var diapason in diapasons.Values)
            {
                diapason.position = diapason.start = sum;
                sum += diapason.count;
            }
            int[] dest = new int[length];
            
            for (int index = 0; index < length; index++)
            {
                int num = source[index+start];
                var diapason = diapasons[GetDiapason(num)];

                dest[diapason.position] = num;
                diapason.position++;
            }    
            for (int i = 0; i < length; i++)
                source[i + start] = dest[i];
            for (int i = 0; i < diapasons.Count; i++)
            {
                var d = diapasons.ElementAt(i);
                for (int j = 0; j < d.Value.start; j++)
                    if (source[start + j] > source[start+ d.Value.start]) throw new Exception();
                Array.Sort(source, d.Value.start, d.Value.count);
            }
           
            
          //  source = diapasons.Values.Aggregate(source, (current, diapason) => SortWithSortedListRecursive(k/2, current, diapason.start, diapason.count));
           // if(diapasons.Count>2)
           // Console.WriteLine("diapasons.Count " + diapasons.Count);
            return source;
        }
        private static void SortWithArraysRecursive(int k, int[] source, int start = 0, int length = -1, bool recursice=true)
        {
            if (length == -1) length = source.Length;
            if (k == 0)
            {
                Array.Sort(source, start, length);
                return;
            }
            if (length == 0 || length == 1) return ;
            Dictionary<int, Diapason> diapasons=new Dictionary<int, Diapason>();
            Func<int, int> GetDiapason = x => x >> k;
            for (int index = start; index < start + length; index++)
            {
                int num = source[index];
                Diapason diapason;
                if (!diapasons.TryGetValue(GetDiapason(num), out diapason))
                    diapasons.Add(GetDiapason(num), new Diapason(0, 1));
                else diapason.count++;
            }
            int sum = 0;
            //order diapasons
            int[] keys=new int[diapasons.Count];
            diapasons.Keys.CopyTo(keys, 0);
            Diapason[] ds=new Diapason[diapasons.Count];
            diapasons.Values.CopyTo(ds, 0);
            Array.Sort(keys, ds);

            for (int i = 0; i < ds.Length; i++)
            {
                ds[i].position = ds[i].start = sum;
                sum += ds[i].count;
                diapasons[keys[i]] = ds[i];
            }
            int[] dest = new int[length];
            for (int index = 0; index < length; index++)
            {
                int num = source[index + start];
                var diapason = diapasons[GetDiapason(num)];

                dest[diapason.position] = num;
                diapason.position++;
            }
            for (int i = 0; i < length; i++)
                source[i + start] = dest[i];
            foreach (Diapason value in diapasons.Values)
            {
                if (recursice)
                    SortWithArraysRecursive(k/2, source, start+value.start, value.count, recursice);
                else
                Array.Sort(source, start+ value.start ,value.count);
            }
        
            // if(diapasons.Count>2)
            // Console.WriteLine("diapasons.Count " + diapasons.Count);
            
        }
    }
}
