using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrueRdfViewer
{
    // Шкала двухбитных величин
    public class Scale2
    {
        // ранг шкалы, т.е. длина индекса в битах, напр. для шкалы, содержашей 1024 битовых пары, ранг будет 10  
        private int range;
        public int[] arr;
        public Scale2(int range)
        {
            this.range = range;
            arr = new int[1 << (range + 1 - 5)];
        }
        public int this[int ind]
        {
            get
            {
                return (arr[ind >> 4] >> ((ind & 15) << 1)) & 3;
            }
            set
            {
                //int v1 = arr[ind >> 4];
                //int v2 = v1 & (~(3 << ((ind & 15) << 1)));
                //int v3 = ((value & 3) << ((ind & 15) << 1));
                //Console.WriteLine("{0:x} {1:x} ", v2, v3);
                arr[ind >> 4] = arr[ind >> 4] & (~(3 << ((ind & 15) << 1))) | ((value & 3) << ((ind & 15) << 1));
            }
        }
        public int Code(string subj, string pred, string obj)
        {
            return (subj.GetHashCode() ^ pred.GetHashCode() ^ obj.GetHashCode()) & ((1 << range) - 1);
        }
        public int Count() { return 1 << range; }
    }
}
