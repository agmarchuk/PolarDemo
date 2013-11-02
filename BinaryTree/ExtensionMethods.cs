using System;
using System.Collections.Generic;
using PolarDB;

namespace BinaryTree
{
    public static class ExtensionMethods
    {
        public static int counter = 0;

        /// <summary>
        /// Поместить элемент в дерево в соответствии со значением функции сравнения,
        ///  вернуть ссылку на голову нового дерева
        /// </summary>
        /// <param name="root"></param>
        /// <param name="element"></param>
        /// <param name="elementDepth"></param>
        /// <returns>была ли изменена высота дерева</returns>
        public static bool Add(this PxEntry root, object element, Func<object, PxEntry, int> elementDepth)
        {
            var lastUnBalanceNode = root;
            bool any = false;
            var node = root;
            var goLeft=new List<bool>();
            while (node.Tag() != 0)
            {
                var nodeEntry = node.UElement();
                any = true;
                // Если не пустое
                // Сравним пришедший элемент с имеющимся в корне

                counter++;
                int cmp = elementDepth(element, nodeEntry.Field(0));
                if (cmp == 0)
                {
                    node.Set(new object[]
                    {
                        1, new[]
                        {
                            element,
                            nodeEntry.Field(1).Get().Value,
                            nodeEntry.Field(2).Get().Value,
                            nodeEntry.Field(3).Get().Value
                        }
                    });
                    return false;
                }
                if ((int)nodeEntry.Field(3).Get().Value != 0)
                {
                    lastUnBalanceNode = node;
                    goLeft.Clear();
                }
                    goLeft.Add(cmp < 0);
                    node = nodeEntry.Field(cmp < 0 ? 1 : 2);
            }
            // когда дерево пустое, организовать одиночное значение
            node.Set(new object[]
            {
                1, new[]
                {
                    element, new object[] {0, null}, new object[] {0, null}, 0
                }
            });
            if (!any) return true;
            node = lastUnBalanceNode;
            for (int i = 0; i < goLeft.Count; i++)
            {
                var nodeEntry = node.UElement();
                var balanceEntry = nodeEntry.Field(3);
                if (goLeft[i])
                {
                    balanceEntry.Set((int)balanceEntry.Get().Value + 1);
                    node = nodeEntry.Field(1);
                }
                else
                {
                    balanceEntry.Set((int) balanceEntry.Get().Value - 1);
                    node = nodeEntry.Field(2);
                }
            }
            var b = (int) lastUnBalanceNode.UElement().Field(3).Get().Value;
            if (b == 2)
                FixWithRotateRight(lastUnBalanceNode);
            else if (b == -2)
                FixWithRotateLeft(lastUnBalanceNode);
            return true;
        }

        public static bool AddRecursive(this PxEntry root, object element, Func<object, PxEntry, int> elementDepth)
        {
                if (root.Tag() == 0)
                {// Если дерево пустое, то организовать одиночное значение
                    root.Set(new object[]
                    {
                        1, new[]
                        {
                           element, new object[] {0, null}, new object[] {0, null}, 0
                        }
                    });
                    counter++;
                    return true;
                }
            // Если не пустое
            // Сравним пришедший элемент с имеющимся в корне
            var rootEntry = root.UElement();
            counter++;
            int cmp = elementDepth(element, rootEntry.Field(0));
            if (cmp == 0)
            {
                root.Set(new object[]
                    {
                        1, new[]
                        {
                            element,
                            rootEntry.Field(1).Get().Value,
                            rootEntry.Field(2).Get().Value,
                            rootEntry.Field(3).Get().Value
                        }
                    });
                return false;
            }
            //если при добавлении не изменилась высота(возможно поддерево сбалансировалось), балансировать не надо
            if (!Add(rootEntry.Field(cmp < 0 ? 1 : 2), element, elementDepth)) return false;
            var balanceEntity = rootEntry.Field(3);
            //добавили влево, увеличили баланс. вправо уменьшили
            var balance = (int) balanceEntity.Get().Value;
            if (balance == 2) { }
            balance += (cmp > 0 ? -1 : 1);
            if (balance == 2)
                return FixWithRotateRight(root);
            if (balance == -2)
                return FixWithRotateLeft(root);
            balanceEntity.Set(balance);
            return balance != 0; 
        }

        /// <summary>
        /// балансирует дерево поворотом влево
        /// </summary>
        /// <param name="root"></param>
        /// <returns> возвращает истину, если высота не изменилась</returns>
        static bool FixWithRotateLeft(this PxEntry root)
        {
            var rootEntry = root.UElement();
            var r = rootEntry.Field(2); //Right;
            var rEntry = r.UElement();
            var rl = rEntry.Field(1); //right of Left;
            var rBalanseEntry = rEntry.Field(3);
            var rBalance = (int)rBalanseEntry.Get().Value;
            switch (rBalance)
            {
                case 0:
                case -1:
                    {
                        root.Set(new object[]
                        {1, new []
                        {
                            rEntry.Field(0).Get().Value,
                            new object[]
                            {
                                1,
                                new[]
                                {
                                    rootEntry.Field(0).Get().Value,
                                    rootEntry.Field(1).Get().Value,
                                    rl.Get().Value,
                                    rBalance==-1 ? 0 : -1
                                }
                            },
                            rEntry.Field(2).Get().Value,
                            rBalance==-1 ? 0 : 1
                        }});
                        return rBalance == 0;
                    }
                case 1:
                    {
                        var rlEntry = rl.UElement();
                        bool rlEmpty = rl.Tag() == 0;
                        var rlBalance =rlEmpty ? 0 : (int)rlEntry.Field(3).Get().Value;
                        root.Set(new object[]{1,
                            new[]
                        {
                            rlEntry.Field(0).Get().Value,
                            new object[]
                            {
                                1,
                                new []
                                {
                                    rootEntry.Field(0).Get().Value,
                                    rootEntry.Field(1).Get().Value,
                                     rlEntry.Field(1).Get().Value,
                                    Math.Max(0, -rlBalance)
                                }
                            },
                            new object[]
                            {
                                1,
                                
                                new []
                                {
                                    rEntry.Field(0).Get().Value,
                                    rlEntry.Field(2).Get().Value,
                                    rEntry.Field(2).Get().Value,
                                     Math.Min(0, -rlBalance)
                                }
                            },
                            0
                        }});
                        return false;
                    }
                default: return true;
            }
        }

        /// <summary>
        /// балансирует дерево поворотом вправо
        /// </summary>
        /// <param name="root"></param>
        /// <returns> возвращает истину, если высота не изменилась</returns>
        static bool FixWithRotateRight(this PxEntry root)
        {
            var rootEntry = root.UElement();
            var l = rootEntry.Field(1); //Left;
          
            var lEntry = l.UElement();
            var lr = lEntry.Field(2); //right of Left;
            var leftBalanseEntry = lEntry.Field(3);
            var leftBalance = (int)leftBalanseEntry.Get().Value;
            switch (leftBalance)
            {
                case 0:
                case 1:
                {
                    root.Set(new object[]
                    {
                        1, new[]
                        {
                            lEntry.Field(0).Get().Value,
                            lEntry.Field(1).Get().Value,
                            new object[]
                            {
                                1,
                                new[]
                                {
                                    rootEntry.Field(0).Get().Value,
                                    lr.Get().Value,
                                    rootEntry.Field(2).Get().Value,
                                    leftBalance == 1 ? 0 : 1
                                }
                            },
                            leftBalance == 1 ? 0 : -1
                        }
                    });
                    return leftBalance == 0;
                    }
                case -1:
                    {
                        var lrEntry = lr.UElement();
                        bool lrEmpty = lr.Tag() == 0;
                        var lrBalance = lrEmpty ? 0 : (int)lrEntry.Field(3).Get().Value;
                        root.Set(new object[]
                        {
                            1, new[]
                            {
                                lrEntry.Field(0).Get().Value,
                                new object[]
                                {
                                    1,
                                    new[]
                                    {
                                        lEntry.Field(0).Get().Value,
                                        lEntry.Field(1).Get().Value,
                                        lrEntry.Field(1).Get().Value,
                                        lrEmpty ? 0 : Math.Max(0, -lrBalance)
                                    }
                                },
                                new object[]
                                {
                                    1,
                                    new[]
                                    {
                                        rootEntry.Field(0).Get().Value,
                                        lrEntry.Field(2).Get().Value,
                                        rootEntry.Field(2).Get().Value,
                                        lrEmpty ? 0 : Math.Min(0, -lrBalance)
                                    }
                                },
                                0
                            }
                        });
                        return false;
                    }
                default: return true;
            }
        }

        private static PxEntry CopyFrom(this PxEntry output, PxEntry input)
        {
            if (input.Tag() == 0) return output;
            output.Set(new object[]
            {
                1, new[]
                {
                    input.UElement().Field(0).Get().Value,
                    BTree.Empty,
                    BTree.Empty,
                    input.UElement().Field(0).Get().Value,
                }
            });
            output.UElement().Field(1).CopyFrom(input.UElement().Field(1));
            output.UElement().Field(2).CopyFrom(input.UElement().Field(2));
            return output;
        }
        private static PxEntry MoveDownRight(this PxEntry output, PxEntry input, object newRoot, object newBalance)
        {
            if (input.Tag() == 0) //last
            {
                output.Set(new object[] {1, new[] {newRoot, BTree.Empty, BTree.Empty, newBalance}});
                return output;
            }
            var oldElement = input.UElement().Field(0).Get().Value;
            var oldBalance = input.UElement().Field(3).Get().Value;
            output.Set(new object[]
            {
                1, new[]
                {
                    input.UElement().Field(0).Get().Value,
                    BTree.Empty,
                    BTree.Empty,
                    input.UElement().Field(0).Get().Value,
                }
            });
            output.UElement().Field(1).CopyFrom(input.UElement().Field(1));
            output.UElement().Field(2).CopyFrom(input.UElement().Field(2));
            return output;
        }
        public static PxEntry BinarySearchInBT(this PxEntry entry, Func<PxEntry, int> elementDepth)
        {
            while (true)
            {
                if (entry.Tag() == 0) return new PxEntry(entry.Typ, long.MinValue, entry.fis);
                PxEntry elementEntry = entry.UElement().Field(0); // Можно сэкономить на запоминании входа для uelement'а
                int level = elementDepth(elementEntry);
                if (level == 0) return elementEntry;
                entry = entry.UElement().Field(level < 0 ? 1 : 2);
            }
        }
    }
}
