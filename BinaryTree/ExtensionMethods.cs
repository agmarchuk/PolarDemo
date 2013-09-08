using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace BinaryTree
{
    public struct TreeRecInfo
    {
        public PxEntry reclocation; // место расположения записи узла дерева
        public long treevolume;
        public TreeRecInfo(PxEntry reclocation, long treevolume)
        {
            this.reclocation = reclocation;
            this.treevolume = treevolume;
        }
    }
    public static class ExtensionMethods
    {
        private static Random rnd = new Random();
        public static int counter = 0;
        /// <summary>
        /// Поместить элемент в дерево в соответствии со значением функции сравнения, вернуть ссылку на голову нового дерева
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="element"></param>
        /// <param name="compare"></param>
        public static TreeRecInfo Add(this PxEntry entry, object element, Comparison<object> compare)
        {
            if (entry.Tag() == 0)
            {// Если дерево пустое, то организовать одиночное значение
                entry.Set(new object[] { 1, new object[] {
                    element,
                    1L,
                    new object[] { 0, null },
                    new object[] { 0, null }
                }});
                counter++;
                return new TreeRecInfo(entry.UElementUnchecked(1), 1L); //TODO: здесь можно сэкономить на обращении к диску -- Сэкономил
            }
            else
            {// Если не пустое
                // Сравним пришедший элемент с имеющимся в корне
                var rec = entry.UElement();
                object el = rec.Field(0).Get().Value;
                long currentvolume = (long)rec.Field(1).Get().Value;
                counter++;
                //int cmp =  rnd.Next(2) * 2 - 1; //compare(element, el);
                int cmp = compare(element, el);
                // Теперь два или три варианта. Пока сделаю два
                int direction = cmp < 0 ? 2 : 3;
                //Console.Write(cmp < 0 ? "L" : "R");

                var f = rec.Field(direction); // Вход для поддерева
                // Поддерево может быть пустое или непустое
                if (f.Tag() == 0) // пустое
                { // Просто запишем новое значение
                    f.Set(new object[] { 1, new object[] { element, 1L, new object[] { 0, null }, new object[] { 0, null } } });
                }
                else // непустое поддерево
                {
                    // вход в запись корня поддерева
                    var subrec = f.UElementUnchecked(1); //TODO: Здесь можно сэкономить на обращении к диску? -- Сэкономил 
                    var res = f.Add(element, compare);
                    // Если указатель изменился, надо зафиксировать новое значение
                    if (res.reclocation.offset != subrec.offset) rec.Field(direction).SetElement(res.reclocation);
                }
                // Попробую завершить обработку здесь. Для отладки...
                return new TreeRecInfo(rec, 0L);
                // Далее нужно бы сбалансировать дерево, но я понял, что не очень представляю как
            }
        }
        public static PxEntry BinarySearchInBT(this PxEntry entry, Func<PxEntry, int> elementDepth)
        {
            if (entry.Tag() == 0) return new PxEntry(entry.Typ, long.MinValue, entry.fis);
            PxEntry elementEntry = entry.UElementUnchecked(1).Field(0);
            int level = elementDepth(elementEntry);
            if (level == 0) return elementEntry;
            if (level < 0) return BinarySearchInBT(entry.UElementUnchecked(1).Field(2), elementDepth);
            else return BinarySearchInBT(entry.UElementUnchecked(1).Field(3), elementDepth);
        }

    }
}
