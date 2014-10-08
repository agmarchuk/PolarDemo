using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace RdfInMemory
{
    public class RdfGraph
    {
        // Типы
        private PType tp_entity;
        private PType tp_rliteral;
        private PType tp_rliteral_seq;
        private PType tp_otriples; // s p o 
        private PType tp_dtriples; // s p off (указатель на literals)
        //private PType tp_dtriples_spf; // s p off (для данных)
        private PType tp_entitiesTree;
        private void LoadTypes()
        {
            this.tp_entity = new PType(PTypeEnumeration.integer);
            this.tp_rliteral = new PTypeUnion(
                new NamedType("void", new PType(PTypeEnumeration.none)),
                new NamedType("integer", new PType(PTypeEnumeration.integer)),
                new NamedType("string", new PTypeRecord(
                    new NamedType("s", new PType(PTypeEnumeration.sstring)),
                    new NamedType("l", new PType(PTypeEnumeration.sstring)))),
                new NamedType("date", new PType(PTypeEnumeration.longinteger)));
            this.tp_rliteral_seq = new PTypeSequence(tp_rliteral);
            this.tp_otriples = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("obj", tp_entity)));
            this.tp_dtriples = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
            this.tp_entitiesTree = new PTypeSequence(new PTypeRecord(
                new NamedType("id", tp_entity),
                new NamedType("fields", new PTypeSequence(new PTypeRecord(
                    new NamedType("prop", tp_entity),
                    new NamedType("literal", new PType(PTypeEnumeration.longinteger))))),
                new NamedType("direct", new PTypeSequence(new PTypeRecord(
                    new NamedType("prop", tp_entity),
                    new NamedType("ref", tp_entity)))),
                new NamedType("inverse", new PTypeSequence(new PTypeRecord(
                    new NamedType("prop", tp_entity),
                    new NamedType("sources", new PTypeSequence(tp_entity)))))));
        }

        // Путь к базе данных
        private string path;
        // Ячейки
        private PaCell otriples;
        private PaCell literals;
        private PaCell dtriples;
        private PxCell tree_fix;
        // Словари
        //private Dictionary<int, int> codeNumb = new Dictionary<int,int>();
        private struct record 
        { 
            public KeyValuePair<int, long>[] fields;
            public KeyValuePair<int, int>[] direct;
            public KeyValuePair<int, int[]>[] inverse;
        };
        private Dictionary<int, record> codeRec = new Dictionary<int, record>();
        // Конструктор
        public RdfGraph(string path)
        {
            LoadTypes();
            this.path = path;
            otriples = new PaCell(tp_otriples, path + "otriples.pac", false);
            literals = new PaCell(tp_rliteral_seq, path + "literals.pac", false);
            dtriples = new PaCell(tp_dtriples, path + "dtriples.pac", false);
            tree_fix = new PxCell(tp_entitiesTree, path + "tree_fix.pxc", false);
            if (!tree_fix.IsEmpty) // Возможно, не требуется проверять
            {
                for (int i=0; i<tree_fix.Root.Count(); i++) 
                {
                    object[] rec = (object[])tree_fix.Root.Element(i).Get();
                    int key = (int)rec[0];
                    //codeNumb.Add(key, i);
                    var f = ((object[])rec[1])
                        .Cast<object[]>()
                        .Select(ob2 => new KeyValuePair<int, long>((int)ob2[0], (long)ob2[1]))
                        .ToArray();
                    var d = ((object[])rec[2])
                        .Cast<object[]>()
                        .Select(ob2 => new KeyValuePair<int, int>((int)ob2[0], (int)ob2[1]))
                        .ToArray();
                    var inverse = ((object[])rec[3])
                        .Cast<object[]>()
                        .Select(ob2 => new KeyValuePair<int, int[]>((int)ob2[0], ((object[])ob2[1]).Cast<int>().ToArray()))
                        .ToArray();
                    codeRec.Add(key,
                        new record() 
                        {
                            fields = f,
                            direct = d,
                            inverse = inverse
                        });
                }
                //codeNumb
            }
        }

        public void LoadTurtle(string tfile)
        {
            DateTime tt0 = DateTime.Now;

            // ======== Загрузка первичных ячеек данными ========
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            literals.Clear();
            literals.Fill(new object[0]);
            int i = 0;
            foreach (var triple in TripleInt.LoadGraph(tfile))
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
                    if (lit.Vid == LiteralVidEnumeration.integer)
                        da = new object[] { 1, lit.Value };
                    else if (lit.Vid == LiteralVidEnumeration.date)
                        da = new object[] { 3, lit.Value };
                    else if (lit.Vid == LiteralVidEnumeration.text)
                    {
                        Text t = (Text)lit.Value;
                        da = new object[] { 2, new object[] { t.Value, t.Lang } };
                    }
                    else
                        da = new object[] { 0, null };
                    var off = literals.Root.AppendElement(da);
                    dtriples.Root.AppendElement(new object[] 
                    { 
                        tr.subject, 
                        tr.predicate, 
                        off 
                    });
                }
            }
            otriples.Flush();
            dtriples.Flush();
            literals.Flush();

            // ======== Формирование вторичных ячеек ========
            PaCell otriples_op = new PaCell(tp_otriples, path + "otriples_op.pac", false);
            otriples_op.Clear(); otriples_op.Fill(new object[0]); // Другой вариант - копирование файла
            otriples.Root.Scan((off, pobj) =>
            {
                otriples_op.Root.AppendElement(pobj);
                return true;
            });
            otriples_op.Flush();

            Console.WriteLine("Additional files ok. duration={0}", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // ======= Сортировки =======
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
            Console.WriteLine("dtriples.Root.Sort ok. Duration={0} msec.", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            // ==== Определение количества сущностей ====
            // Делаю три упрощенных сканера
            DiapasonScanner<int> i_fields = new DiapasonScanner<int>(dtriples, ent => (int)((object[])ent.Get())[0]);
            DiapasonScanner<int> i_direct = new DiapasonScanner<int>(otriples, ent => (int)((object[])ent.Get())[0]);
            DiapasonScanner<int> i_inverse = new DiapasonScanner<int>(otriples_op, ent => (int)((object[])ent.Get())[2]);

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

            // ==== Построение дерева слиянием трех ячеек ====
            // Делаю три сканера из трех ячеек
            DiapasonElementsScanner<SubjPredInt> fields = new DiapasonElementsScanner<SubjPredInt>(dtriples, ob =>
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
            Console.WriteLine("tree_fix ok. Duration={0} msec.", (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            
            otriples.Close();
            otriples_op.Close();
            dtriples.Close();
            literals.Close();
        }


        internal void WarmUp()
        {
            //throw new NotImplementedException();
            DateTime tt0 = DateTime.Now;
            Console.WriteLine("Warming-up...");
            foreach (var v in literals.Root.ElementValues()) ;
            Console.WriteLine("ok. duration={0}", (DateTime.Now - tt0).Ticks / 10000L);
        }


        internal bool ChkSubjPredObj(int subj, int pred, int obj)
        {
            record rec;
            if (!codeRec.TryGetValue(subj, out rec)) return false;
            var direct = rec.direct;
            return direct.Any(pair => pair.Key == pred && pair.Value == obj);
        }
        internal bool ChkSubjPredObj0(int subj, int pred, int obj)
        {
            var rec_ent = tree_fix.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return false;
            var direct = (object[])rec_ent.Field(2).Get();
            return direct.Any(pair => (int)((object[])pair)[0] == pred && (int)((object[])pair)[1] == obj);
        }
        internal IEnumerable<long> GetDataCodeBySubjPred(int subj, int pred)
        {
            record rec;
            if (!codeRec.TryGetValue(subj, out rec)) return Enumerable.Empty<long>();
            return rec.fields.Where(pair => pair.Key == pred)
                .Select(pair => pair.Value);
        }
        internal Literal DecodeDataCode(long lcode)
        {
            PaEntry lit_ent = literals.Root.Element(0);
            lit_ent.offset = lcode;
            return new Literal((object[])lit_ent.Get());
        }
        internal IEnumerable<Literal> GetDataBySubjPred1(int subj, int pred)
        {
            record rec;
            if (!codeRec.TryGetValue(subj, out rec)) return Enumerable.Empty<Literal>();
            var offsets = rec.fields
                .Where(pair => pair.Key == pred)
                .Select(pair => pair.Value);
            PaEntry lit_ent = literals.Root.Element(0);
            var lits = offsets.Select(off =>
            {
                lit_ent.offset = off;
                object[] pair = (object[])lit_ent.Get(); 
                return new Literal(pair);
            });
            return lits;
        }
        internal IEnumerable<Literal> GetDataBySubjPred0(int subj, int pred)
        {
            var rec_ent = tree_fix.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<Literal>();
            var fields = (object[])rec_ent.Field(1).Get();
            PaEntry lit_ent = literals.Root.Element(0);
            var offsets = fields.Where(pair => (int)((object[])pair)[0] == pred)
                .Select(pair => (long)((object[])pair)[1]);
            var lits = offsets.Select(off =>
            {
                lit_ent.offset = off;
                object[] pair = (object[])lit_ent.Get();
                return new Literal(pair);
            });
            return lits;
        }
        public IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            record rec;
            if (!codeRec.TryGetValue(subj, out rec)) return Enumerable.Empty<int>();
            var direct = rec.direct;
            return direct
                .Where(pair => pair.Key == pred)
                .Select(pair => pair.Value);
        }
        public IEnumerable<int> GetObjBySubjPred0(int subj, int pred)
        {
            var rec_ent = tree_fix.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<int>();
            var direct = (object[])rec_ent.Field(2).Get();
            return direct
                .Where(pair => (int)((object[])pair)[0] == pred)
                .Select(pair => (int)((object[])pair)[1]);
        }
        public IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            record rec;
            if (!codeRec.TryGetValue(obj, out rec)) return Enumerable.Empty<int>();
            //TODO: Хорошо бы сделать сначала выделение первого, удовлетворяющего условию по предикату 
            return rec.inverse.Where(pair => pair.Key == pred).SelectMany(pair => pair.Value);
        }
        public IEnumerable<int> GetSubjectByObjPred0(int obj, int pred)
        {
            var rec_ent = tree_fix.Root.BinarySearchFirst(ent => ((int)ent.Field(0).Get()).CompareTo(obj));
            if (rec_ent.IsEmpty) return Enumerable.Empty<int>();
            var inverse = (object[])rec_ent.Field(3).Get();
            var pred_set = inverse
                .FirstOrDefault(pair => (int)((object[])pair)[0] == pred);
            if (pred_set == null) return Enumerable.Empty<int>();
            return ((object[])((object[])pred_set)[1]).Cast<int>();
        }
    }
}
