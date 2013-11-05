using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using PolarDB;

namespace BinaryTree
{
        public class BTree : PxCell
        {

            internal static readonly object[] Empty;
          
            /// <summary>
            /// 
            /// </summary>
            /// <param name="ptElement">polar type of element of tree</param>
            /// <param name="elementDepth">функция сравнения элемента дерева и добавляемого объекта</param>
            /// <param name="filePath"></param>
            /// <param name="readOnly"></param>
            public BTree(PType ptElement, Func<object, PxEntry, int> elementDepth, string filePath, bool readOnly = true)
                : base(PTypeTree(ptElement), filePath, readOnly)
            {
                this.elementDepth = elementDepth;
            }

            static BTree()
            {
                Empty = new object[] {0, null};
            }

            /// <summary>
            ///  Тип
            /// BTree<T> = empty^none,
            /// pair^{element: T, less: BTree<T>, more: BTree<T>};
            /// </summary>
            /// <param name="tpElement"></param>
            /// <returns></returns>
            private static PTypeUnion PTypeTree(PType tpElement)
            {
                var tpBtree = new PTypeUnion();
                tpBtree.Variants = new[]
                {
                    new NamedType("empty", new PType(PTypeEnumeration.none)),
                    new NamedType("pair", new PTypeRecord(
                        new NamedType("element", tpElement),
                        new NamedType("less", tpBtree),
                        new NamedType("more", tpBtree),
                        //1 - слева больше, -1 - справа больше.
                        new NamedType("balance", new PType(PTypeEnumeration.integer))))
                };
                return tpBtree;
            }

            public static int counter = 0;
            private readonly Func<object, PxEntry, int> elementDepth;
            /// <summary>
            /// путь от последнего узла с ненулевым балансом до добавленой вершины (не включая её) составляет пары: <PxEntry /> ,баланса и значение баланса.
            /// </summary>
            private readonly List<KeyValuePair<PxEntry, int>> listEntries4Balance = new List<KeyValuePair<PxEntry, int>>();

            public static int H(PxEntry tree)
            {
                return tree.Tag() == 0 
                    ? 0 
                    : 1 + Math.Max(H(tree.UElementUnchecked(1).Field(1)), 
                                   H(tree.UElementUnchecked(1).Field(2)));
            }

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
                while (node.Tag() != 0)
                {
                    var nodeEntry = node.UElementUnchecked(1);
                    counter++;
                    int cmp = elementDepth(element, nodeEntry.Field(0));
                    PxEntry balanceEntry = nodeEntry.Field(3);
                    var balance = (int)balanceEntry.Get().Value;
                    if (cmp == 0)
                    {
                        var left = nodeEntry.Field(1).GetHead();
                        var right = nodeEntry.Field(2).GetHead();
                        node.Set(new object[]
                        {
                            1, new[]
                            {
                                element,
                                Empty,
                                Empty,
                              balance
                            }
                        });
                        node.UElementUnchecked(1).Field(1).SetHead(left);
                        node.UElementUnchecked(1).Field(2).SetHead(right);
                        return;
                    }
                    if (balance != 0)
                    {
                        lastUnBalanceNode = node;
                        listEntries4Balance.Clear();
                    }
                    var goLeft = cmp < 0;
                    //TODO catch overflow memory
                    listEntries4Balance.Add(new KeyValuePair<PxEntry, int>(balanceEntry,
                        goLeft ? balance + 1 : balance - 1));
                    node = nodeEntry.Field(goLeft ? 1 : 2);
                }
                // когда дерево пустое, организовать одиночное значение
                node.Set(new object[]
                {
                    1, new[]
                    {
                        element, new object[] {0, null}, new object[] {0, null}, 0
                    }
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
                var rootEntry = root.UElementUnchecked(1);
                var r = rootEntry.Field(2); //Right;
                var rEntry = r.UElementUnchecked(1);
                var rl = rEntry.Field(1); //right of Left;
                var rBalance = entries[1].Value;
                if (rBalance == 1) 
                {

                    var rlEntry = rl.UElementUnchecked(1);
                    rlEntry.Field(3).Set(0);
                    //запоминаем RL
                    var rlold = rl.GetHead();
                    int rlBalance = (entries.Count == 2 ? 0 : entries[2].Value); 
                    //Изменяем правую
                    rl.SetHead(rlEntry.Field(2).GetHead());
                    entries[1].Key.Set(Math.Min(0, -rlBalance));
                    //запоминаем правую
                    var oldR = r.GetHead();
                    //изменяем корневую
                    r.SetHead(rlEntry.Field(1).GetHead());
                    entries[0].Key.Set(Math.Max(0, -rlBalance));
                    //запоминаем корневую
                    var rootOld = root.GetHead();
                    //RL теперь корень
                    root.SetHead(rlold);
                    rootEntry = root.UElementUnchecked(1);
                    //подставляем запомненые корень и правую.
                    rootEntry.Field(1).SetHead(rootOld);
                    rootEntry.Field(2).SetHead(oldR);
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
                var rootEntry = root.UElementUnchecked(1);
                var l = rootEntry.Field(1); //Left;
                var lEntry = l.UElementUnchecked(1);
                var lr = lEntry.Field(2); //right of Left;
                var leftBalance = entries[1].Value; 
                if (leftBalance == -1)
                {
                    var lrEntry = lr.UElementUnchecked(1);
                    var lrold = lr.GetHead();
                    int lrBalance = (entries.Count == 2 ? 0 : entries[2].Value); 
                    lr.SetHead(lrEntry.Field(1).GetHead());
                    entries[1].Key.Set(Math.Max(0, -lrBalance));
                    var oldR = l.GetHead();
                    l.SetHead(lrEntry.Field(2).GetHead());
                    entries[0].Key.Set(Math.Min(0, -lrBalance));
                    var rootOld = root.GetHead();
                    root.SetHead(lrold);
                    rootEntry = root.UElementUnchecked(1);
                    rootEntry.Field(2).SetHead(rootOld);
                    rootEntry.Field(1).SetHead(oldR);
                    rootEntry.Field(3).Set(0);
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

            public void Fill(PxEntry elementsEntry, Func<object, object> orderKeySelector)
            {
                Fill(elementsEntry.Elements()
                                    .Select(oe => oe.Get().Value), orderKeySelector);
            }

            public void Fill(IEnumerable<object> elements, Func<object, object> orderKeySelector)
            {
                object[] elementsSorted = elements
                    .OrderBy(orderKeySelector)
                    .Cast<object>()
                    .ToArray();
                Clear();
                Fill2(ToTreeObject(elementsSorted, 0, elementsSorted.Length));
            }

            private static object[] ToTreeObject(object[] elements, int beg, int len)
            {
                if (len == 0) return Empty;
                if (len == 1)
                    return new object[]
                    {
                        1, new[]
                        {
                            // запись
                            elements[beg], // значение
                            Empty,
                            Empty,
                            0
                        }
                    };
                int half = len / 2;
                return new object[]
                {
                    1, new[]
                    {
                        // запись
                        elements[beg + half], // значение
                        ToTreeObject(elements, beg, half),
                        ToTreeObject(elements, beg + half + 1, len - half - 1),
                        0
                    }
                };
            }

            public PxEntry BinarySearch(Func<PxEntry, int> eDepth)
            {
                var entry = Root;
                while (true)
                {
                    if (entry.Tag() == 0) return new PxEntry(entry.Typ, Int64.MinValue, entry.fis);
                    PxEntry elementEntry = entry.UElementUnchecked(1).Field(0);
                        // Можно сэкономить на запоминании входа для uelement'а
                    int level = eDepth(elementEntry);
                    if (level == 0) return elementEntry;
                    entry = entry.UElementUnchecked(1).Field(level < 0 ? 1 : 2);
                }
            }

            public bool Equals(object obj, Func<object, object, bool> elementsComparer)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals(((BTree) obj).Root, Root, elementsComparer);
            }

            private static bool Equals(PxEntry left, PxEntry right, Func<object, object, bool> elementsComparer)
            {
                int tag = 0;
                if((tag=left.Tag())!=right.Tag()) return false;
                if(tag==0) return true;
               var r = right.UElement();
                var l =right.UElement();
                bool @equals = elementsComparer(r.Field(0).Get().Value, l.Field(0).Get().Value);
                bool b = (int)r.Field(3).Get().Value == (int)l.Field(3).Get().Value;
                if(!b) return false;
                if(!@equals) return false;
                return @equals
                       && b
                       && Equals(r.Field(1), l.Field(1))
                       && Equals(r.Field(2), l.Field(2));
            }
        }
    }
