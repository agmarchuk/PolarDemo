using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace BinaryTree
{
    //public struct TreeRecInfo
    //{
    //    public PxEntry reclocation; // место расположения записи узла дерева
    //    public long treevolume;
    //    public TreeRecInfo(PxEntry reclocation, long treevolume)
    //    {
    //        this.reclocation = reclocation;
    //        this.treevolume = treevolume;
    //    }
    //}
    public static class ExtensionMethods
    {
        public static int counter = 0;
        /// <summary>
        /// Поместить элемент в дерево в соответствии со значением функции сравнения, вернуть ссылку на голову нового дерева
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="element"></param>
        /// <param name="elementDepth"></param>
        public static void Add(this PxEntry entry, object element, Func<object, PxEntry, int> elementDepth)
        {
            if (entry.Tag() == 0)
            {// Если дерево пустое, то организовать одиночное значение
                entry.Set(new object[] { 1, new object[] {
                    element,
                    new object[] { 0, null },
                    new object[] { 0, null }
                }});
                counter++;
            }
            else
            {// Если не пустое
                // Сравним пришедший элемент с имеющимся в корне
                var rec = entry.UElement();
                PxEntry el_ent = rec.Field(0);
                counter++;
                int cmp = elementDepth(element, el_ent);
                // Теперь два или три варианта. Пока сделаю два
                int direction = cmp < 0 ? 1 : 2;
                //Console.Write(cmp < 0 ? "L" : "R");

                var f = rec.Field(direction); // Вход для поддерева
                // Поддерево может быть пустое или непустое
                if (f.Tag() == 0) // пустое
                { // Просто запишем новое значение
                    f.Set(new object[] { 1, new object[] { element, new object[] { 0, null }, new object[] { 0, null } } });
                }
                else // непустое поддерево
                {
                    f.Add(element, elementDepth);
                }
            }
        }
        public static PxEntry BinarySearchInBT(this PxEntry entry, Func<PxEntry, int> elementDepth)
        {
            if (entry.Tag() == 0) return new PxEntry(entry.Typ, long.MinValue, entry.fis);
            PxEntry elementEntry = entry.UElementUnchecked(1).Field(0); // Можно сэкономить на запоминании входа для uelement'а
            int level = elementDepth(elementEntry);
            if (level == 0) return elementEntry;
            if (level < 0) return BinarySearchInBT(entry.UElementUnchecked(1).Field(1), elementDepth);
            else return BinarySearchInBT(entry.UElementUnchecked(1).Field(2), elementDepth);
        }

    }
}
