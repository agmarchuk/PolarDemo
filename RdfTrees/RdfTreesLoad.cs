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
            DiapasonScanner<SubjPredInt> fields = new DiapasonScanner<SubjPredInt>(dtriples_sp, ent =>
            {
                object[] v = (object[])ent.Get();
                return new SubjPredInt() { subj = (int)v[0], pred = (int)v[1] };
            });
            // Стартуем сканеры
            direct.Start(); inverse.Start(); fields.Start();
            // Заведем ячейку для результата сканирования
            PaCell tree_free = new PaCell(tp_entitiesTree, path + "tree_free.pac", false);
            tree_free.Clear();

            int cnt_e = 0, cnt_ep = 0;
            long c1 = 0, c2 = 0, c3 = 0;
            // Начинаем тройное сканирование
            int id = Int32.MinValue;
            int prop;
            tree_free.StartSerialFlow();
            tree_free.S();
            while (direct.HasValue || inverse.HasValue || fields.HasValue)
            {
                SubjPredInt key = SubjPredInt.MinValue;
                int num = -1; // 0 - direct, 1 - inverse, 2 - fields
                if (direct.HasValue && direct.KeyCurrent.CompareTo(key) >= 0) { num = 0; key = direct.KeyCurrent; }
                if (inverse.HasValue && inverse.KeyCurrent.CompareTo(key) >= 0) { num = 1; key = inverse.KeyCurrent; }
                if (fields.HasValue && fields.KeyCurrent.CompareTo(key) >= 0) { num = 2; key = fields.KeyCurrent; }
                // Здесь у нас НОВОЕ значение ключа, но возможно старое значение идентификатора
                cnt_ep++;
                if (cnt_ep % 100000 == 0) Console.Write("{0} ", cnt_ep);
                if (id < key.subj)
                { // Новое значение id - новая сущность
                    cnt_e++;
                    id = key.subj;
                }
                else
                { // Старое значение
                }
                if (direct.HasValue && direct.KeyCurrent.CompareTo(key) == 0) 
                {
                    var diap = direct.Scan();
                    c1 += diap.numb;
                }
                if (inverse.HasValue && inverse.KeyCurrent.CompareTo(key) == 0)
                {
                    var diap = inverse.Scan();
                    c2 += diap.numb;
                }
                if (fields.HasValue && fields.KeyCurrent.CompareTo(key) == 0)
                {
                    var diap = fields.Scan();
                    c3 += diap.numb;
                }
            }
            tree_free.Se();
            tree_free.EndSerialFlow();
            Console.WriteLine("Scan3 ok. Duration={0} msec. cnt_e={1} cnt_ep={2}", (DateTime.Now - tt0).Ticks / 10000L, cnt_e, cnt_ep); tt0 = DateTime.Now;
            Console.WriteLine("otriples={0} otriples_op={1} dtriples_sp={2}", otriples.Root.Count(), otriples_op.Root.Count(), dtriples_sp.Root.Count());
            Console.WriteLine("otriples={0} otriples_op={1} dtriples_sp={2}", c1, c2, c3);  

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
