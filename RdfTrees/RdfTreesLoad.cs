using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace RdfTrees
{
    public partial class RdfTrees
    {
        public void LoadTurtle(string filename)
        {
            // Дополнительные типы
            PType tp_entity = new PType(PTypeEnumeration.integer);
            PType tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("object", tp_entity)));
            PType tp_dtriple_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("data", tp_literal)));
            PType tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
            // Дополнительные ячейки и индексы
            PaCell otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            PaCell dtriples = new PaCell(tp_dtriple_seq, path + "dtriples.pac", false);

            DateTime tt0 = DateTime.Now;

            // Загрузка otriples, dtriples
            Load(filename, otriples, dtriples);
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

            PaCell dtriples_sp = new PaCell(tp_dtriple_spf, path + "dtriples_spf.pac", false);
            dtriples_sp.Clear(); dtriples_sp.Fill(new object[0]);
            dtriples.Root.Scan((off, pobj) =>
            {
                object[] tri = (object[])pobj;
                int s = (int)tri[0];
                int p = (int)tri[1];
                dtriples_sp.Root.AppendElement(new object[] { s, p, off });
                return true;
            });
            dtriples_sp.Flush();
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
            dtriples_sp.Root.SortByKey(rec =>
            {
                object[] r = (object[])rec;
                return new SubjPredInt() { pred = (int)r[1], subj = (int)r[0] };
            }, sp_compare);
            Console.WriteLine("dtriples_sp.Root.Sort ok. Duration={0} msec.", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

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
                if (cnt_e % 10000 == 0) Console.Write("{0} ", cnt_e / 10000);
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
                    var diap = fields.Scan();
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
                    var diap = direct.Scan();
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
                    var diap = inverse.Scan();
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
            Console.WriteLine("Scan3 ok. Duration={0} msec. cnt_e={1} ", (DateTime.Now - tt0).Ticks / 10000L, cnt_e); tt0 = DateTime.Now;
            //Console.WriteLine("otriples={0} otriples_op={1} dtriples_sp={2}", otriples.Root.Count(), otriples_op.Root.Count(), dtriples_sp.Root.Count());

        }
        private static void Load(string filepath, PaCell otriples, PaCell dtriples)
        {
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            foreach (var triple in TurtleInt.LoadGraph(filepath))
            {
                if (i % 100000 == 0) Console.Write("{0} ", i / 100000);
                i++;
                if (triple is OTripleInt)
                {
                    var tr = (OTripleInt)triple;
                    otriples.Root.AppendElement(new object[] 
                    { 
                        tr.subject, 
                        tr.predicate, 
                        tr.obj 
                    });
                }
                else
                {
                    var tr = (DTripleInt)triple;
                    Literal lit = tr.data;
                    object[] da;
                    if (lit.vid == LiteralVidEnumeration.integer)
                        da = new object[] { 1, lit.value };
                    else if (lit.vid == LiteralVidEnumeration.date)
                        da = new object[] { 3, lit.value };
                    else if (lit.vid == LiteralVidEnumeration.text)
                    {
                        Text t = (Text)lit.value;
                        da = new object[] { 2, new object[] { t.s, t.l } };
                    }
                    else
                        da = new object[] { 0, null };
                    dtriples.Root.AppendElement(new object[] 
                    { 
                        tr.subject, 
                        tr.predicate, 
                        da 
                    });
                }
            }
            otriples.Flush();
            dtriples.Flush();
        }
    }
}
