using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace BinaryTree
{
    public class DexTree : PxCell
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="readOnly"></param>
        public DexTree(
            string filePath, bool readOnly = true)
            : base(PTypeTree(), filePath, readOnly)
        {
        }
        /// <summary>
        /// дерево с 10 ветвями и long значением.
        /// </summary>
        /// <returns></returns>
        private static PType PTypeTree()
        {
            var tpBtree = new PTypeRecord();
            tpBtree = new PTypeRecord(
                new NamedType("zero", new PTypeUnion()),
                new NamedType("one", new PTypeUnion()),
                new NamedType("two", new PTypeUnion()),
                new NamedType("three", new PTypeUnion()),
                new NamedType("four", new PTypeUnion()),
                new NamedType("five", new PTypeUnion()),
                new NamedType("six", new PTypeUnion()),
                new NamedType("seven", new PTypeUnion()),
                new NamedType("eight", new PTypeUnion()),
                new NamedType("nine", new PTypeUnion()),
                new NamedType("value", new PType(PTypeEnumeration.longinteger)));
            for (int i = 0; i < 10; i++)
                ((PTypeUnion) tpBtree.Fields[i].Type).Variants = new[]
                {
                    new NamedType("empty", new PType(PTypeEnumeration.none)),
                    new NamedType("next", tpBtree)
                };

            return tpBtree;
        }

        public static int counter = 0;


        public void Fill(KeyValuePair<int, long>[] elements)
        {
            Clear();
            Fill2(ToTreeObject(
                elements.Select(num => new KeyValuePair<char[], long>(num.Key.ToString().ToCharArray(), num.Value)), 0));
        }
        
        private object[] ToTreeObject(IEnumerable<KeyValuePair<char[], long>> seq, int h)
        {
             long element = long.MinValue;
            var filter = seq
                .Where(num
                    =>
                {
                    if (num.Key.Length != h) return true;
                    element = num.Value;
                    return false;
                }).ToArray();
            int hp = h + 1;
            object[] node = new object[11];
            node[10] = element;
            for (int i = 0; i < 10; i++)
                node[i] = new object[] {0, null};
            //   if (!filter.Any()) return node;
            foreach (var group in filter.GroupBy(num => (int)num.Key[h]-48))
                node[group.Key] = new object[] {1, ToTreeObject(group, hp)};
            return node;
        }


        public long Search(int key)
        {
            var entry = Root;
            char[] keyChars = key.ToString().ToCharArray();
            for (int i = 0; i < keyChars.Length; i++)
            {
                var union = entry.Field(keyChars[i] - 48);
                if (union.Tag() == 0) return long.MinValue;
                entry = union.UElement();
            }
            return (long)(entry.Field(10).Get());
        }
    }

}
