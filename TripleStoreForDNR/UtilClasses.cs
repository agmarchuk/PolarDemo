using System;
using System.Collections.Generic;
using PolarDB;

namespace TripleStoreForDNR
{
    public class DiapasonScanner<Key> where Key : IComparable
    {
        private PaCell cell;
        private long count = Int64.MinValue;
        private long i_current; // i_current == count - конец файла
        private Key key_current;
        public bool HasValue { get { return i_current < count; } }
        public Key KeyCurrent { get { return key_current; } }
        private long number;
        private Func<PaEntry, Key> keyFunction;
        public DiapasonScanner(PaCell sequ, Func<PaEntry, Key> keyFunction)
        {
            this.cell = sequ;
            this.keyFunction = keyFunction;
            if (!this.cell.IsEmpty) Start();
        }
        public void Start()
        {
            this.count = cell.Root.Count();
            i_current = 0;
            if (count > 0)
            {
                key_current = keyFunction(cell.Root.Element(i_current));
            }
        }
        public Diapason Next()
        {
            //if (count == Int64.MinValue) Start();
            long i_start = i_current;
            //long numb = 1;
            Key key_scanned = key_current;
            for (; ; )
            {
                i_current++;
                if (i_current >= count)
                {
                    i_current = count;
                    break;
                }
                key_current = keyFunction(cell.Root.Element(i_current));
                if (key_scanned.CompareTo(key_current) < 0)
                {
                    break;
                }
                // Ключ тот же - продолжение цикла
            }
            // Вышли либо по концу файла, либо по увеличенному значению ключа
            return new Diapason() { start = i_start, numb = i_current - i_start };
        }
    }
    public class DiapasonElementsScanner<Key> where Key : IComparable
    {
        private PaCell cell;
        private long count = Int64.MinValue;
        private long i_current; // i_current == count - конец файла
        private object element_current;
        private Key key_current;
        public bool HasValue { get { return i_current < count; } }
        public Key KeyCurrent { get { return key_current; } }
        private Func<object, Key> keyFunction; // превращает объектное представление элемента в ключ
        public DiapasonElementsScanner(PaCell sequ, Func<object, Key> keyFunction)
        {
            this.cell = sequ;
            this.keyFunction = keyFunction;
            if (!this.cell.IsEmpty) Start();
        }
        public void Start()
        {
            this.count = cell.Root.Count();
            i_current = 0;
            if (count > 0)
            {
                element_current = cell.Root.Element(i_current).Get();
                key_current = keyFunction(element_current);
            }
        }
        public Diapason Next(out object[] elements)
        {
            //if (count == Int64.MinValue) Start();
            long i_start = i_current;
            //long numb = 1;
            Key key_scanned = key_current;
            List<object> elem_list = new List<object>();
            for (; ; )
            {
                elem_list.Add(element_current);
                i_current++;
                if (i_current >= count)
                {
                    i_current = count;
                    break;
                }
                element_current = cell.Root.Element(i_current).Get();
                key_current = keyFunction(element_current);
                if (key_scanned.CompareTo(key_current) < 0)
                {
                    break;
                }
                // Ключ тот же - продолжение цикла
            }
            // Вышли либо по концу файла, либо по увеличенному значению ключа
            elements = elem_list.ToArray();
            return new Diapason() { start = i_start, numb = i_current - i_start };
        }
    }
        public class SubjPredObjInt : IComparable
        {
            public int subj, pred, obj;
            public SubjPredObjInt() { }
            public SubjPredObjInt(object pobj)
            {
                object[] rec = (object[])pobj;
                subj = (int)rec[0];
                pred = (int)rec[1];
                obj = (int)rec[2];
            }
            public int CompareTo(object sp)
            {
                SubjPredObjInt target = (SubjPredObjInt)sp;
                int cmp = subj.CompareTo(target.subj);
                if (cmp != 0) return cmp;
                cmp = pred.CompareTo(target.pred);
                if (cmp != 0) return cmp;
                return obj.CompareTo(target.obj);
            }
        }
        public class SPOComparer : IComparer<SubjPredObjInt>
        {
            public int Compare(SubjPredObjInt x, SubjPredObjInt y)
            {
                return x.CompareTo(y);
            }
        }
        public class SubjPredInt : IComparable
        {
            public int subj, pred;
            public int CompareTo(object sp)
            {
                int cmp = subj.CompareTo(((SubjPredInt)sp).subj);
                if (cmp != 0) return cmp;
                return pred.CompareTo(((SubjPredInt)sp).pred);
            }
        }
        public class SPComparer : IComparer<SubjPredInt>
        {
            public int Compare(SubjPredInt x, SubjPredInt y)
            {
                return x.CompareTo(y);
            }
        }
}
