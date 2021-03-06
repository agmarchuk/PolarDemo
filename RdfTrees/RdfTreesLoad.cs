﻿using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;
using TripleIntClasses;


namespace RdfTreesNamespace
{
    public partial class RdfTrees
    {
        public override void LoadTurtle(string filepath, bool useBuffer)
        {
            // Дополнительные ячейки и индексы
            otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            PaCell dtriples = new PaCell(tp_dtriple_spf, path + "dtriples.pac", false); // Временно выведена в переменные класса, открывается при инициализации

            DateTime tt0 = DateTime.Now;

            // Загрузка otriples, dtriples
            if (useBuffer)
                TurtleInt.LoadByGraphsBuffer(filepath, otriples, dtriples, this);
            else TurtleInt.LoadTriplets(filepath, otriples, dtriples, this);
            Console.WriteLine("Load ok. duration={0}", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            
            // Формирование дополнительных файлов
            
            otriples.Close(); // Копирование файла
            if (System.IO.File.Exists(path + "otriples_op.pac")) System.IO.File.Delete(path + "otriples_op.pac");
            System.IO.File.Copy(path + "otriples.pac", path + "otriples_op.pac");
            otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            PaCell otriples_op = new PaCell(tp_otriple_seq, path + "otriples_op.pac", false);
            //otriples_op.Clear(); otriples_op.Fill(new object[0]); // Другой вариант - покомпонентная перепись
            //otriples.Root.Scan((off, pobj) =>
            //{
            //    otriples_op.Root.AppendElement(pobj);
            //    return true;
            //});
            //otriples_op.Flush();

            //PaCell dtriples_sp = new PaCell(tp_dtriple_spf, path + "dtriples_spf.pac", false);
            //dtriples_sp.Clear(); dtriples_sp.Fill(new object[0]);
            //dtriples.Root.Scan((off, pobj) =>
            //{
            //    object[] tri = (object[])pobj;
            //    int s = (int)tri[0];
            //    int p = (int)tri[1];
            //    dtriples_sp.Root.AppendElement(new object[] { s, p, off });
            //    return true;
            //});
            //dtriples_sp.Flush();
            Console.WriteLine("Additional files ok. duration={0}", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Сортировки
            // Упорядочивание otriples по s-p-o
            SPOComparer spo_compare = new SPOComparer();

            otriples.Root.SortByKey<SubjPredObjInt>(rec => new SubjPredObjInt(rec), spo_compare);
            Console.WriteLine("otriples.Root.Sort ok. Duration={0} msec.", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            SPComparer sp_compare = new SPComparer();
            // Упорядочивание otriples_op по o-p
            otriples_op.Root.SortByKey<SubjPredInt>(rec =>
            {
                object[] r = (object[])rec;
                return new SubjPredInt() { pred = (int)r[1], subj = (int)r[2] };
            }, sp_compare);
            Console.WriteLine("otriples_op Sort ok. Duration={0} msec.", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // Упорядочивание dtriples_sp по s-p
            dtriples.Root.SortByKey(rec =>
            {
                object[] r = (object[])rec;
                return new SubjPredInt() { pred = (int)r[1], subj = (int)r[0] };
            }, sp_compare);
            Console.WriteLine("dtriples_sp.Root.Sort ok. Duration={0} msec.", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
            Scale.WriteScale(otriples);
            Console.WriteLine("CreateScale ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;
            //int cnt_e = MakeTreeFree(otriples, otriples_op, dtriples_sp);
            //Console.WriteLine("Scan3 ok. Duration={0} msec. cnt_e={1} ", (DateTime.Now - tt0).Ticks / 10000L, cnt_e); tt0 = DateTime.Now;
            //Console.WriteLine("otriples={0} otriples_op={1} dtriples_sp={2}", otriples.Root.Count(), otriples_op.Root.Count(), dtriples_sp.Root.Count());

            otriples.Close();
            otriples_op.Close();
            dtriples.Close();
            // Создает ячейку фиксированного формата tree_fix.pxc
            MakeTreeFix();
            
        }

        private int MakeTreeFree(PaCell otriples, PaCell otriples_op, PaCell dtriples_sp)
        {
            // Делаю три сканера из трех ячеек
            DiapasonScanner<SubjPredInt> fields = new DiapasonScanner<SubjPredInt>(dtriples_sp, ent =>
            {
                object[] v = (object[])ent.Get();
                return new SubjPredInt() { subj = (int)v[0], pred = (int)v[1] };
            });
            DiapasonScanner<SubjPredInt> direct = new DiapasonScanner<SubjPredInt>(otriples, ent =>
            {
                object[] v = (object[])ent.Get();
                return new SubjPredInt() { subj = (int)v[0], pred = (int)v[1] };
            });
            DiapasonScanner<SubjPredInt> inverse = new DiapasonScanner<SubjPredInt>(otriples_op, ent =>
            {
                object[] v = (object[])ent.Get();
                return new SubjPredInt() { subj = (int)v[2], pred = (int)v[1] };
            });
            // Стартуем сканеры
            fields.Start(); direct.Start(); inverse.Start();
            // Заведем ячейку для результата сканирования
            PaCell tree_free = new PaCell(tp_entitiesTree, path + "tree_free.pac", false);
            tree_free.Clear();

            int cnt_e = 0, cnt_ep = 0; // для отладки
            long c1 = 0, c2 = 0, c3 = 0; // для отладки
            //PaEntry ent_dtriples = dtriples.Root.Element(0); // вход для доступа к литералам
            // Начинаем тройное сканирование
            tree_free.StartSerialFlow();
            tree_free.S();
            while (fields.HasValue || direct.HasValue || inverse.HasValue)
            {
                // Здесь у нас НОВОЕ значение идентификатора
                cnt_e++;
                if (cnt_e % 10000000 == 0) Console.Write("{0} ", cnt_e / 10000000);
                int id0 = fields.HasValue ? fields.KeyCurrent.subj : Int32.MaxValue;
                int id1 = direct.HasValue ? direct.KeyCurrent.subj : Int32.MaxValue;
                int id2 = inverse.HasValue ? inverse.KeyCurrent.subj : Int32.MaxValue;
                // Минимальное значение кода идентификатора
                int id = Math.Min(id0, Math.Min(id1, id2));

                // Начало записи
                tree_free.R();
                // Запись идентификатора
                tree_free.V(id);

                tree_free.S();
                while (fields.HasValue && fields.KeyCurrent.subj == id)
                {
                    int su = fields.KeyCurrent.subj;
                    int pr = fields.KeyCurrent.pred;
                    var diap = fields.Next();
                    c3 += diap.numb;

                    for (long ind = diap.start; ind < diap.start + diap.numb; ind++)
                    {
                        object[] row = (object[])dtriples_sp.Root.Element(ind).Get();
                        int subj = (int)row[0];
                        int prop = (int)row[1];
                        long off = (long)row[2];
                        if (subj != su || prop != pr) throw new Exception("Assert err: 287282");
                        tree_free.V(new object[] { prop, off });
                    }
                }
                tree_free.Se();
                tree_free.S();
                while (direct.HasValue && direct.KeyCurrent.subj == id)
                {
                    int su = direct.KeyCurrent.subj;
                    int pr = direct.KeyCurrent.pred;
                    var diap = direct.Next();
                    c1 += diap.numb;
                    for (long ind = diap.start; ind < diap.start + diap.numb; ind++)
                    {
                        object[] row = (object[])otriples.Root.Element(ind).Get();
                        int subj = (int)row[0];
                        int prop = (int)row[1];
                        int obj = (int)row[2];
                        if (subj != su || prop != pr) throw new Exception("Assert err: 287283");
                        tree_free.V(new object[] { prop, obj });
                    }
                }
                tree_free.Se();

                tree_free.S();
                while (inverse.HasValue && inverse.KeyCurrent.subj == id)
                {
                    int su = inverse.KeyCurrent.subj;
                    int pr = inverse.KeyCurrent.pred;
                    var diap = inverse.Next();
                    c2 += diap.numb;

                    tree_free.R();
                    tree_free.V(pr);
                    tree_free.S();
                    for (long ind = diap.start; ind < diap.start + diap.numb; ind++)
                    {
                        object[] row = (object[])otriples_op.Root.Element(ind).Get();
                        int subj = (int)row[0];
                        int prop = (int)row[1];
                        int obj = (int)row[2];
                        if (obj != su || prop != pr) throw new Exception("Assert err: 287284");
                        tree_free.V(subj);
                    }
                    tree_free.Se();
                    tree_free.Re();
                }
                tree_free.Se();
                // Конец записи
                tree_free.Re();
            }
            tree_free.Se();
            tree_free.EndSerialFlow();
            return cnt_e;
        }
        // Была Площадка для тестирования, а теперь это главный метод
        public int MakeTreeFix()
        {
            DateTime tt0 = DateTime.Now;
            // Служебная часть. Она не нужна, если параметрами будут PaCell otriples, PaCell otriples_op, PaCell dtriples_sp
            PType tp_entity = new PType(PTypeEnumeration.integer);
            PType tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("object", tp_entity)));
            //PType tp_dtriple_seq = new PTypeSequence(new PTypeRecord(
            //    new NamedType("subject", tp_entity),
            //    new NamedType("predicate", tp_entity),
            //    new NamedType("data", tp_literal)));
            PType tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
             otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", true);
            PaCell otriples_op = new PaCell(tp_otriple_seq, path + "otriples_op.pac", true);
            PaCell dtriples_sp = new PaCell(tp_dtriple_spf, path + "dtriples.pac", true);

            // ==== Определение количества сущностей ====
            // Делаю три упрощенных сканера
            DiapasonScanner<int> i_fields = new DiapasonScanner<int>(dtriples_sp, ent =>
                {
                    object[] v = (object[])ent.Get();
                    return (int)v[0];
                });
            DiapasonScanner<int> i_direct = new DiapasonScanner<int>(otriples, ent =>
            {
                object[] v = (object[])ent.Get();
                return (int)v[0];
            });
            DiapasonScanner<int> i_inverse = new DiapasonScanner<int>(otriples_op, ent =>
            {
                object[] v = (object[])ent.Get();
                return (int)v[2];
            });
            int n_entities = 0;
            i_fields.Start();
            i_direct.Start();
            i_inverse.Start();
            while (i_fields.HasValue || i_direct.HasValue || i_inverse.HasValue)
            {
                n_entities++;
                int id0 = i_fields.HasValue ? i_fields.KeyCurrent : Int32.MaxValue;
                int id1 = i_direct.HasValue ? i_direct.KeyCurrent : Int32.MaxValue;
                int id2 = i_inverse.HasValue ? i_inverse.KeyCurrent : Int32.MaxValue;
                // Минимальное значение кода идентификатора
                int id = Math.Min(id0, Math.Min(id1, id2));

                if (id0 == id) i_fields.Next();
                if (id1 == id) i_direct.Next();
                if (id2 == id) i_inverse.Next();
            }
            Console.WriteLine("Scan3count ok. Duration={0} msec. cnt_e={1} ", (DateTime.Now - tt0).Ticks / 10000L, n_entities); tt0 = DateTime.Now;

            // ==== Построение дерева слиянием отрех ячеек ====
            // Делаю три сканера из трех ячеек
            DiapasonElementsScanner<SubjPredInt> fields = new DiapasonElementsScanner<SubjPredInt>(dtriples_sp, ob =>
            {
                object[] v = (object[])ob;
                return new SubjPredInt() { subj = (int)v[0], pred = (int)v[1] };
            });
            DiapasonElementsScanner<SubjPredInt> direct = new DiapasonElementsScanner<SubjPredInt>(otriples, ob =>
            {
                object[] v = (object[])ob;
                return new SubjPredInt() { subj = (int)v[0], pred = (int)v[1] };
            });
            DiapasonElementsScanner<SubjPredInt> inverse = new DiapasonElementsScanner<SubjPredInt>(otriples_op, ob =>
            {
                object[] v = (object[])ob;
                return new SubjPredInt() { subj = (int)v[2], pred = (int)v[1] };
            });
            // Стартуем сканеры
            fields.Start(); direct.Start(); inverse.Start();

            // Заведем ячейку для результата сканирования
            PxCell tree_fix = this.entitiesTree; //new PxCell(tp_entitiesTree, path + "tree_fix.pxc", false);
            tree_fix.Clear();
            tree_fix.Root.SetRepeat(n_entities);
            Console.WriteLine("tree_fix length={0}", tree_fix.Root.Count());
            long longindex = 0;
            
            int cnt_e = 0; // для отладки
            long c1 = 0, c2 = 0, c3 = 0; // для отладки
            //PaEntry ent_dtriples = dtriples.Root.Element(0); // вход для доступа к литералам
            // Начинаем тройное сканирование
            while (fields.HasValue || direct.HasValue || inverse.HasValue)
            {
                // Здесь у нас НОВОЕ значение идентификатора
                cnt_e++;
                if (cnt_e % 10000000 == 0) Console.Write("{0} ", cnt_e / 10000000);
                int id0 = fields.HasValue ? fields.KeyCurrent.subj : Int32.MaxValue;
                int id1 = direct.HasValue ? direct.KeyCurrent.subj : Int32.MaxValue;
                int id2 = inverse.HasValue ? inverse.KeyCurrent.subj : Int32.MaxValue;
                // Минимальное значение кода идентификатора
                int id = Math.Min(id0, Math.Min(id1, id2));
                // массив для получения "однородных" элементов из сканнеров
                object[] elements;

                List<object[]> list_fields = new List<object[]>();
                while (fields.HasValue && fields.KeyCurrent.subj == id)
                {
                    int su = fields.KeyCurrent.subj;
                    int pr = fields.KeyCurrent.pred;
                    var diap = fields.Next(out elements);
                    
                    c3 += diap.numb;
                    list_fields.AddRange(elements.Cast<object[]>().Select(e3 => new object[] { e3[1], e3[2] }));
                }
                List<object[]> list_direct = new List<object[]>();
                while (direct.HasValue && direct.KeyCurrent.subj == id)
                {
                    int su = direct.KeyCurrent.subj;
                    int pr = direct.KeyCurrent.pred;
                    var diap = direct.Next(out elements);
                    
                    c1 += diap.numb;
                    list_direct.AddRange(elements.Cast<object[]>().Select(e3 => new object[] { e3[1], e3[2] }));
                }
                List<object[]> list_inverse = new List<object[]>();
                while (inverse.HasValue && inverse.KeyCurrent.subj == id)
                {
                    int su = inverse.KeyCurrent.subj;
                    int pr = inverse.KeyCurrent.pred;
                    var diap = inverse.Next(out elements);
                    
                    c2 += diap.numb;
                    object[] pr_sources_pair = new object[2];
                    pr_sources_pair[0] = pr;
                    pr_sources_pair[1] = elements.Cast<object[]>().Select(e3 => e3[0]).ToArray();
                    list_inverse.Add(pr_sources_pair);
                }
                //Собираем полную запись
                object[] record = new object[] { id, list_fields.ToArray(), list_direct.ToArray(), list_inverse.ToArray() };
                // Записываем в качестве элемента последовательности
                tree_fix.Root.Element(longindex).Set(record); longindex++;
            }
            tree_fix.Close();
            this.entitiesTree = new PxCell(tp_entitiesTree, path + "entitiesTree.pxc", false);
            Console.WriteLine("Scan3fix ok. Duration={0} msec. cnt_e={1} ", (DateTime.Now - tt0).Ticks / 10000L, cnt_e); tt0 = DateTime.Now;
            return cnt_e;
        }          
    }   


}
