using System.Collections.Generic;
using System.Linq;
using PolarDB;

namespace TripleIntClasses
{
    public class DiapLinks
    {
        public Diapason all = new Diapason();
        public KeyValuePair<int, Diapason>[] predArr = new KeyValuePair<int,Diapason>[0];
        private static object DiapasonToPObject(Diapason di) { return new object[] {di.start, di.numb}; }
        public object ToPObject()
        {
            return new object[] { DiapasonToPObject(all),
                predArr.Select(kvp => new object[] {kvp.Key, new object[] {kvp.Value.start, kvp.Value.numb}}).ToArray()};
        }
    }
    public class DiapLinksScanner
    {
        public bool HasValue { get { return count > 0 && i_current < count; } }
        public int KeyCurrent { get { return key_current; } }
        private PaCell cell; // ячейка, носитель последовательности
        private int key_field; // номер ключевого поля (предикат всегда второе поле (1))
        private long count = 0; // количество элементов в последовательности
        private long i_current = 0; // прочитанная позиция последовательности
        private int key_current; // значение ключа в прочитанной позиции последовательности
        private int pred_current; // значение предиката в прочитанной позиции последовательности
        public DiapLinksScanner(PaCell cell, int key_field)
        {
            if (cell == null || cell.IsEmpty) return;
            this.cell = cell;
            this.key_field = key_field;
            this.count = cell.Root.Count();
            if (count == 0) return;
            this.i_current = 0;
            NextCurrent();
        }
        private void NextCurrent()
        {
            object[] element = (object[])cell.Root.Element(i_current).Get();
            key_current = (int)element[key_field];
            pred_current = (int)element[1];
        }
        public DiapLinks Scan()
        {
            long i_start = i_current;
            long i_pred_start = i_current;
            List<KeyValuePair<int, Diapason>> predList = new List<KeyValuePair<int, Diapason>>();
            int key_scanned = key_current;
            int pred_scanned = pred_current;

            for ( ; ; )
            {
                i_current++;
                if (i_current >= count)
                { // завершение по концу последовательности
                    i_current = count;
                    break;
                }
                NextCurrent();
                if (pred_current != pred_scanned)
                { // смена предиката
                    predList.Add(new KeyValuePair<int,Diapason>(pred_scanned, 
                        new Diapason() { start = i_pred_start, numb = i_current - i_pred_start }));
                    i_pred_start = i_current;
                    pred_scanned = pred_current;
                }
                if (key_current != key_scanned)
                { // завершение по изменению ключа 
                    break;
                }
                // Ключ тот же - продолжение цикла
            }
            // Вышли либо по концу файла, либо по увеличенному значению ключа
            if (i_current > i_pred_start)
            { // добавление последнего диапазона
                predList.Add(new KeyValuePair<int, Diapason>(pred_scanned,
                    new Diapason() { start = i_pred_start, numb = i_current - i_pred_start }));
                i_pred_start = i_current;
            }
            return new DiapLinks()
            {
                all = new Diapason() { start = i_start, numb = i_current - i_start },
                predArr = predList.ToArray()
            };
        }
    }
}
