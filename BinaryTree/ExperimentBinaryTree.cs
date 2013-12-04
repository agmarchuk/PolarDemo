using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PolarDB;

namespace BinaryTree
{
    public class BTreeInt : PxCell
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptElement">polar type of element of tree</param>
        /// <param name="keyComparer">функция сравнения элемента дерева и добавляемого объекта</param>
        /// <param name="getKey"></param>
        /// <param name="filePath"></param>
        /// <param name="emptyElement"></param>
        /// <param name="readOnly"></param>
        public BTreeInt(PType ptElement, 
            string filePath, bool readOnly = true)
            : base(PTypeTree(ptElement), filePath, readOnly)
        {
        }

        /// <summary>
        ///  Тип
        /// BTree<T> = empty^none,
        /// pair^{element: T, less: BTree<T>, more: BTree<T>};
        /// </summary>
        /// <param name="tpElement"></param>
        /// <returns></returns>
        private static PType PTypeTree(PType tpElement)
        {
            var tpBtree = new PTypeRecord();
            tpBtree = new PTypeRecord(
                new NamedType("element", tpElement),
                new NamedType("next", new PTypeUnion()));

            ((PTypeUnion) tpBtree.Fields[1].Type).Variants = new[]
            {
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("pair", new PTypeRecord(
                    new NamedType("less", tpBtree),
                    new NamedType("more", tpBtree),
                    //1 - слева больше, -1 - справа больше.
                    new NamedType("balance", new PType(PTypeEnumeration.integer))))
            };

            return tpBtree;
        }

        public static int counter = 0;

        /// <summary>
        /// путь от последнего узла с ненулевым балансом до добавленой вершины (не включая её) составляет пары: <PxEntry /> ,баланса и значение баланса.
        /// </summary>
        private readonly List<KeyValuePair<PxEntry, int>> listEntries4Balance = new List<KeyValuePair<PxEntry, int>>();

        /// <summary>
        /// Поместить элемент в дерево в соответствии со значением функции сравнения,
        ///  вернуть ссылку на голову нового дерева
        /// </summary>
        /// <param name="element"></param>
        /// <returns>была ли изменена высота дерева</returns>
        public void Add(object element)
        {
            var node = Root;
            var lastUnBalanceNode = node;
            listEntries4Balance.Clear();
            //int h = 0;
            //Tkey key,keyAdd = getKey(element);
            object value;
            while (true)
            {
              //  h++;
                counter++;
                PxEntry balanceEntry = node.Field(3);
                var balance = (int) balanceEntry.Get();
                int cmp = 0;//keyComparer(key, keyAdd);
                if (cmp == 0)
                {
                    var left = node.Field(1).GetHead();
                    var right = node.Field(2).GetHead();
                    node.Set(new []
                    {
                            element,
                            //TODO
                            balance
                     
                    });
                    node.Field(1).SetHead(left);
                    node.Field(2).SetHead(right);
                    return;
                }
                if (balance != 0)
                {
                    lastUnBalanceNode = node;
                    listEntries4Balance.Clear();
                }
                var goLeft = cmp < 0;
                //TODO catch overflow memory
                listEntries4Balance.Add(new KeyValuePair<PxEntry, int>(balanceEntry, goLeft ? balance + 1 : balance - 1));
                node = node.Field(goLeft ? 1 : 2);
            }
            // когда дерево пустое, организовать одиночное значение
            node.Set( new[]
                {
                    element, new object[] {0, null}, new object[] {0, null}, 0
                });
            if (listEntries4Balance.Count == 0) return;
            for (int i = 0; i < listEntries4Balance.Count; i++)
                listEntries4Balance[i].Key.Set(listEntries4Balance[i].Value);
            //  ChangeBalanceSlowlyLongSequence(element, lastUnBalanceNode);
            int b = listEntries4Balance[0].Value;
            if (b == 2)
                FixWithRotateRight(lastUnBalanceNode, listEntries4Balance);
            else if (b == -2)
                FixWithRotateLeft(lastUnBalanceNode, listEntries4Balance);
            //  return true;
        }

/*
 * пригодится, когда дерево оооочень большое будет, так ое, что  список переполнит оперативную память
            private void ChangeBalanceSlowlyLongSequence(object element, PxEntry lastUnBalanceNode)
            {
                var nodeBalance = lastUnBalanceNode;
                //   foreach (bool isLeft in listEntries4Balance)
                int com = 0;
                while (nodeBalance.Tag() != 0 && (com=elementDepth(element, nodeBalance.UElementUnchecked(1).Field(0))) != 0)
                {
                    var nodeEntry = nodeBalance.UElementUnchecked(1);
                    var balanceEntry = nodeEntry.Field(3);
                    if (com < 0)
                    {
                        balanceEntry.Set((int) balanceEntry.Get().Value + 1);
                        nodeBalance = nodeEntry.Field(1);
                    }
                    else
                    {
                        balanceEntry.Set((int) balanceEntry.Get().Value - 1);
                        nodeBalance = nodeEntry.Field(2);
                    }
                }
            }

          */

        /// <summary>
        /// балансирует дерево поворотом влево
        /// </summary>           
        /// <param name="root">PxEntry балансируемой вершины с балансом=-2</param>
        /// <param name="entries">balance entries of path from prime node to added(excluded from entries), and them balaces</param>
        private static void FixWithRotateLeft(PxEntry root, List<KeyValuePair<PxEntry, int>> entries)
        {
            var r = root.Field(2); //Right;
            var rl = r.Field(1); //right of Left;
            var rBalance = entries[1].Value;
            if (rBalance == 1)
            {

                rl.Field(3).Set(0);
                //запоминаем RL
                var rlold = rl.GetHead();
                int rlBalance = (entries.Count == 2 ? 0 : entries[2].Value);
                //Изменяем правую
                rl.SetHead(rl.Field(2).GetHead());
                entries[1].Key.Set(Math.Min(0, -rlBalance));
                //запоминаем правую
                var oldR = r.GetHead();
                //изменяем корневую
                r.SetHead(rl.Field(1).GetHead());
                entries[0].Key.Set(Math.Max(0, -rlBalance));
                //запоминаем корневую
                var rootOld = root.GetHead();
                //RL теперь корень
                root.SetHead(rlold);
                
                //подставляем запомненые корень и правую.
                root.Field(1).SetHead(rootOld);
                root.Field(2).SetHead(oldR);
                return;
            }
            if (rBalance == -1)
            {
                entries[0].Key.Set(0);
                entries[1].Key.Set(0);
            }
            else //0
            {
                entries[0].Key.Set(-1);
                entries[1].Key.Set(1);
            }

            var rOld = r.GetHead();
            r.SetHead(rl.GetHead());
            rl.SetHead(root.GetHead());
            root.SetHead(rOld);
        }

        /// <summary>
        /// балансирует дерево поворотом вправо
        /// </summary>
        /// <param name="root">PxEntry балансируемой вершины с балансом=2</param>
        /// <param name="entries"> пары: PxEntry содержащая баланс и баланс, соответсвующие пути от балансируемой вершины (включительно) до добавленой не включительно</param>
        private static void FixWithRotateRight(PxEntry root, List<KeyValuePair<PxEntry, int>> entries)
        {
            var l = root.Field(1); //Left;
            var lr = l.Field(2); //right of Left;
            var leftBalance = entries[1].Value;
            if (leftBalance == -1)
            {
                var lrold = lr.GetHead();
                int lrBalance = (entries.Count == 2 ? 0 : entries[2].Value);
                lr.SetHead(lr.Field(1).GetHead());
                entries[1].Key.Set(Math.Max(0, -lrBalance));
                var oldR = l.GetHead();
                l.SetHead(lr.Field(2).GetHead());
                entries[0].Key.Set(Math.Min(0, -lrBalance));
                var rootOld = root.GetHead();
                root.SetHead(lrold);
                root.Field(2).SetHead(rootOld);
                root.Field(1).SetHead(oldR);
                root.Field(3).Set(0);
                return;
            }
            if (leftBalance == 1) // 1
            {
                entries[0].Key.Set(0);
                entries[1].Key.Set(0);
            }
            else // 0
            {
                entries[0].Key.Set(1);
                entries[1].Key.Set(-1);
            }
            var lOld = l.GetHead();
            l.SetHead(lr.GetHead());
            lr.SetHead(root.GetHead());
            root.SetHead(lOld);
        }

        public void Fill(PxEntry elementsEntry, bool editable)
        {
            Fill(elementsEntry.Elements()
                .Select(oe => (int)oe.Get()), editable);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="editable"></param>
        public void Fill(IEnumerable<int> elements, bool  editable)
        {
            int[] elementsSorted = elements
                .OrderBy(e=>e)
                .ToArray();
            Clear();
            int h = 0;
            //editable ? ToTreeObjectWithBalance(new ToTreeObjectParams(elementsSorted, 0, elementsSorted.Length), ref h) : 
            Fill2(ToTreeObject(new ToTreeObjectParams(elementsSorted, 0, elementsSorted.Length)));
        }

        private object[] ToTreeObject(ToTreeObjectParams @params)
        {
            if (@params.Len == 0) return new object[] {Int32.MinValue, new object[]{0, null}};
            //if (len == 1)
            //    return new[]
            //    {
            //        // запись
            //        elements[beg], // значение
            //        new object[]{0, null}
            //    };
            int len = @params.Len;
            @params.Len = (int) (@params.Len*0.5);
            int value = @params.Elements[@params.Len == 0 ? @params.Beg : @params.Beg + @params.Len];
            object[] left = ToTreeObject(new ToTreeObjectParams(@params.Elements, @params.Beg, @params.Len));
            @params.Beg += @params.Len + 1;
            @params.Len = len - @params.Len - 1;
            return new object[]
            {
                // запись
               value, // значение
                new object[]
                {
                    1,
                    new object[]
                    {
                        left,
                        ToTreeObject(@params),
                        0
                    }
                }

            };
        }

        public readonly object EmptyElement;

        public class ToTreeObjectParams
        {
            public ToTreeObjectParams(int[] elements, int beg, int len)
            {
                this.Elements = elements;
                this.Beg = beg;
                this.Len = len;
            }

            public int[] Elements;

            public int Beg;

            public int Len;
        }

        //private static object[] ToTreeObjectWithBalance(ToTreeObjectParams @params, ref int h)
        //{
        //    if (@params.Len == 0) return Empty;
        //    h++;
        //    if (@params.Len == 1)
        //        return new object[]
        //        {
        //            1, new[]
        //            {
        //                // запись
        //                @params.Elements[@params.Beg], // значение
        //                Empty,
        //                Empty,
        //                0
        //            }
        //        };
        //    int leftH = 0, rightH = 0, l = @params.Len;
        //    @params.Len /= 2;
        //    var left = ToTreeObjectWithBalance(@params, ref leftH);
        //    @params.Beg += @params.Len + 1;
        //    @params.Len = l - @params.Len - 1;
        //    return new object[]
        //    {
        //        1, new[]
        //        {
        //            // запись
        //            @params.Elements[@params.Beg + @params.Len], // значение
        //            left,
        //            ToTreeObjectWithBalance(@params, ref rightH),
        //            leftH - rightH
        //        }
        //    };
        //}

        public PxEntry BinarySearch(int key)
        {
            var entry = Root;
            while (true)
            {
                //if (entry.Tag() == 0) return new PxEntry(entry.Typ, Int64.MinValue, entry.fis);
                PxEntry elementEntry = entry.Field(0);
                // Можно сэкономить на запоминании входа для uelement'а
                var o = (int)elementEntry.Get();
                if(o == key)
                 return elementEntry;
                if (o==Int32.MinValue) return new PxEntry(entry.Typ, Int64.MinValue, entry.fis);
                entry = entry.Field(1).UElementUnchecked(1).Field(o < key ? 0 : 1);
            }
        }

        public bool Equals(object obj, Func<object, object, bool> elementsComparer)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(((BTree) obj).Root, Root, elementsComparer);
        }

        private static bool Equals(PxEntry left, PxEntry right, Func<object, object, bool> elementsComparer)
        {
            int tag;
            if ((tag = left.Tag()) != right.Tag()) return false;
            if (tag == 0) return true;
            var r = right.UElement();
            var l = right.UElement();
            bool @equals = elementsComparer(r.Field(0).Get(), l.Field(0).Get());
           // bool b = (int) r.Field(3).Get().Value == (int) l.Field(3).Get().Value;
            return @equals
                //   && b
                   && Equals(r.Field(1), l.Field(1))
                   && Equals(r.Field(2), l.Field(2));
        }
        public int H(PxEntry tree)
        {
            return (int)tree.Field(0).Get() ==Int32.MinValue
                ? 0
                : 1 + Math.Max(H(tree.Field(1)),
                    H(tree.Field(2)));
        }
    }

}
