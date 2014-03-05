﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TrueRdfViewer
{
    public class DiapasonScanner<Key> where Key: IComparable
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
        public Diapason Scan()
        {
            //if (count == Int64.MinValue) Start();
            long i_start = i_current;
            //long numb = 1;
            Key key_scanned = key_current;
            for (;;)
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
}
