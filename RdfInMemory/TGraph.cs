using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace RdfInMemory
{
    public class TGraph : IGraph
    {
        #region Типы
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
        #endregion

        // Путь к базе данных
        private string path;
        // 
        private TNamespaceMapper mapper = new TNamespaceMapper();
        public INamespaceMapper NamespaceMap { get { return mapper; } }

        #region Ячейки и словари
        // Ячейки
        private PaCell otriples;
        private PaCell literals;
        private PaCell dtriples;
        private PxCell tree_fix;
        // Словари
        private struct record
        {
            public KeyValuePair<int, long>[] fields;
            public KeyValuePair<int, int>[] direct;
            public KeyValuePair<int, int[]>[] inverse;
        };
        private Dictionary<int, record> codeRec = new Dictionary<int, record>();
        #endregion

        // Конструктор
        public TGraph(string path)
        {
            LoadTypes();
            this.path = path;
            otriples = new PaCell(tp_otriples, path + "otriples.pac", false);
            literals = new PaCell(tp_rliteral_seq, path + "literals.pac", false);
            dtriples = new PaCell(tp_dtriples, path + "dtriples.pac", false);
            tree_fix = new PxCell(tp_entitiesTree, path + "tree_fix.pxc", false);
            FillDictionary();
        }

        public bool IsEmpty { get { return tree_fix.Root.Count() == 0; } }

        public void Clear()
        {
            mapper.Clear();
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            literals.Clear();
            literals.Fill(new object[0]);
        }
        public bool Assert(Triple t)
        {
            if (t.Object.NodeType == NodeType.Uri)
            {
                otriples.Root.AppendElement(new object[] { ((TUriNode)t.Subject).Code, ((TUriNode)t.Predicate).Code, ((TUriNode)t.Object).Code });
            }
            else if (t.Object.NodeType == NodeType.Literal)
            {
                dtriples.Root.AppendElement(new object[] { ((TUriNode)t.Subject).Code, ((TUriNode)t.Predicate).Code, ((TLiteralNode)t.Object).Code });
            }
            else return false;
            return true;
        }
        public void Build()
        {
            DateTime tt0 = DateTime.Now;
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
        // Создатели
        public IUriNode CreateUriNode(string urivalue)
        {
            IUriNode node = new TUriNode(urivalue, this);
            return node;
        }
        public ILiteralNode CreateLiteralNode(string value)
        {
            ILiteralNode node = new TLiteralNode(value, this);
            return node;
        }
        public ILiteralNode CreateLiteralNode(string value, Uri datatype)
        {
            throw new Exception("CreateLiteralNode does not implemented");
            return null;
        }
        public ILiteralNode CreateLiteralNode(string value, string lang)
        {
            throw new Exception("CreateLiteralNode does not implemented");
            return null;
        }

        private void FillDictionary()
        {
            for (int i = 0; i < tree_fix.Root.Count(); i++)
            {
                object[] rec = (object[])tree_fix.Root.Element(i).Get();
                int key = (int)rec[0];
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
        }

        // Доступ к литералу по коду: читается все структурные значение литерала
        internal object GetLiteralPValue(long ocode)
        {
            PaEntry entry = literals.Root.Element(0);
            entry.offset = ocode;
            return entry.Get();
        }
        // Доступ к Uri по коду: (из таблицы имен) читается строка хранимого uri - pref:local-name (QName) или абсолютный вариант
        internal object GetUriPValue(int code)
        {
            return "http://test/" + code; // временное (отладочное) решение
        }
        // Добавление литерала
        internal long AddLiteral(Literal lit)
        {
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
            return off;
        }

        public bool ContainsTriple(Triple t)
        {
            if (t.Object.NodeType == NodeType.Uri)
            {
                record rec;
                if (!codeRec.TryGetValue(((TUriNode)t.Subject).Code, out rec)) return false;
                var direct = rec.direct;
                return direct.Any(pair => pair.Key == ((TUriNode)t.Predicate).Code && pair.Value == ((TUriNode)t.Object).Code);

            }
            throw new Exception("Unimplemented variant in ContainsTriple()");
        }
        // ПОка только объектные!
        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred)
        {
            record rec;
            if (!codeRec.TryGetValue(((TUriNode)subj).Code, out rec)) return Enumerable.Empty<Triple>();
            return rec.direct.Where(pair => pair.Key == ((TUriNode)pred).Code)
                .Select(pair => new Triple(
                    new TUriNode(((TUriNode)subj).Code, this),
                    new TUriNode(((TUriNode)pred).Code, this),
                    new TUriNode(pair.Value, this))); 
        }
        // Пока только объектные, хотя это легко можно будет изменить(?)
        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj)
        {
            record rec;
            if (!codeRec.TryGetValue(((TUriNode)obj).Code, out rec)) return Enumerable.Empty<Triple>();
            //TODO: Хорошо бы сделать сначала выделение первого, удовлетворяющего условию по предикату 
            var arr = rec.inverse.Where(pair => pair.Key == ((TUriNode)pred).Code).ToArray(); // Может можно экономнее...
            if (arr.Length == 0) return Enumerable.Empty<Triple>();
            var query = arr[0].Value.Select(scode => new Triple(
                    new TUriNode(scode, this),
                    new TUriNode(((TUriNode)pred).Code, this),
                    new TUriNode(((TUriNode)obj).Code, this)));
            return query;
        }
    }
}
