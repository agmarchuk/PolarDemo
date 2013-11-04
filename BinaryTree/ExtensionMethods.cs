using System;
using System.Collections.Generic;
using PolarDB;

namespace BinaryTree
{
    public static class ExtensionMethods
    {
        public static BTree ToBTree(this PxEntry elementsEntry, string path, Func<object, PxEntry, int> elementsComparer, Func<object, object> keySelector)
        {
            var newTree = new BTree(((PTypeSequence)elementsEntry.Typ).ElementType, elementsComparer, path, false);
            newTree.Fill(elementsEntry, keySelector);
            return newTree;
        }

        public static BTree ToBTree(this IEnumerable<object> elementsEntry, PType pTypeOfElement, string path,
            Func<object, PxEntry, int> elementsComparer, Func<object, object> keySelector)
        {
            var newTree = new BTree(pTypeOfElement, elementsComparer, path, false);
            newTree.Fill(elementsEntry, keySelector);
            return newTree;
        }
    }
}
