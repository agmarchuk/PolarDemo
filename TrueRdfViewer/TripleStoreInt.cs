using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PolarDB;
using sema2012m;

namespace TrueRdfViewer
{
    public class TripleStoreInt
    {
        private PType tp_otriple_seq;
        private PType tp_dtriple_seq;
        private PType tp_entity;
        private PType tp_dtriple_spf;
        internal EntitiesWideTable ewt;
        internal EntitiesMemoryHashTable ewtHash;
        private void InitTypes()
        {
            tp_entity = new PType(PTypeEnumeration.integer);
            PType tp_rliteral = new PTypeUnion(
                new NamedType("void", new PType(PTypeEnumeration.none)),
                new NamedType("integer", new PType(PTypeEnumeration.integer)),
                new NamedType("string", new PTypeRecord(
                    new NamedType("s", new PType(PTypeEnumeration.sstring)),
                    new NamedType("l", new PType(PTypeEnumeration.sstring)))),
                new NamedType("date", new PType(PTypeEnumeration.longinteger)));
            tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("object", tp_entity)));
            tp_dtriple_seq = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("data", tp_rliteral)));
            // Тип для экономного выстраивания индекса s-p для dtriples
            tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("offset", new PType(PTypeEnumeration.longinteger))));

        }
        private string path;
        public PaCell otriples;
        public PaCell otriples_op; // объектные триплеты, упорядоченные по o-p
        private PaCell dtriples;
        public PaCell dtriples_sp;
        private FlexIndexView<SubjPredObjInt> spo_o_index = null;
        private FlexIndexView<SubjPredInt> sp_d_index = null;
        private FlexIndexView<SubjPredInt> op_o_index = null;
        private PaCell oscale;
        private bool filescale = true;
        //private bool memoryscale = false; // Наоборот от filescale
        private int range = 0;
        //// Идея хорошая, но надо менять схему реализации
        //private GroupedEntities getable;
        //private Dictionary<int, object[]> geHash;

        public TripleStoreInt(string path)
        {
            this.path = path;
            InitTypes();
            otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            otriples_op = new PaCell(tp_otriple_seq, path + "otriples_op.pac", false);
            dtriples = new PaCell(tp_dtriple_seq, path + "dtriples.pac", false);
            dtriples_sp = new PaCell(tp_dtriple_spf, path + "dtriples_spf.pac", false);
            oscale = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "oscale.pac", false);
            if (!oscale.IsEmpty)
            {
                CalculateRange();
            }
            if (!otriples.IsEmpty)
            {
                OpenCreateIndexes();
            }
            ewt = new EntitiesWideTable(path, new DiapasonScanner<int>[]  
            {
                new DiapasonScanner<int>(otriples, ent => (int)((object[])ent.Get())[0]),
                new DiapasonScanner<int>(otriples_op, ent => (int)((object[])ent.Get())[2]),
                new DiapasonScanner<int>(dtriples_sp, ent => (int)((object[])ent.Get())[0])
            });
            ewtHash = new EntitiesMemoryHashTable(ewt);
            ewtHash.Load();
            
            //getable = new GroupedEntities(path); // Это хорошая идея, но нужно менять схему реализации
            //getable.CheckGroupedEntities();
            //geHash = getable.GroupedEntitiesHash();
        }
        public void WarmUp()
        {
            if (otriples.IsEmpty) return;
            foreach (var v in otriples.Root.ElementValues()) ;
            foreach (var v in otriples_op.Root.ElementValues()) ;
            foreach (var v in dtriples.Root.ElementValues()) ;
            foreach (var v in dtriples_sp.Root.ElementValues()) ;
            if (filescale) 
                foreach (var v in oscale.Root.ElementValues()) ;
            foreach (var v in ewt.EWTable.Root.ElementValues()) ; // этая ячейка "подогревается" при начале программы
        }
        private void CalculateRange()
        {
            long len = oscale.Root.Count() - 1;
            int r = 1;
            while (len != 0) { len = len >> 1; r++; }
            range = r + 4;
        }

        private void OpenCreateIndexes()
        {
            spo_o_index = new FlexIndexView<SubjPredObjInt>(path + "spo_o_index", otriples.Root,
                ent => new SubjPredObjInt() { subj = (int)ent.Field(0).Get(), pred = (int)ent.Field(1).Get(), obj = (int)ent.Field(2).Get() });
            sp_d_index = new FlexIndexView<SubjPredInt>(path + "subject_d_index", dtriples.Root,
                ent => new SubjPredInt() { subj = (int)ent.Field(0).Get(), pred = (int)ent.Field(1).Get() });
            op_o_index = new FlexIndexView<SubjPredInt>(path + "obj_o_index", otriples.Root,
                ent => new SubjPredInt() { subj = (int)ent.Field(2).Get(), pred = (int)ent.Field(1).Get() });
            if (!filescale) CreateScale();
        }

        public void LoadTurtle(string filepath)
        {
            DateTime tt0 = DateTime.Now;

            Load(filepath);
            Console.WriteLine("Load ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            
            PrepareArrays();
            Console.WriteLine("PrepareArrays ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;
            
            // Упорядочивание otriples по s-p-o
            SPOComparer spo_compare = new SPOComparer();

            otriples.Root.SortByKey<SubjPredObjInt>(rec => new SubjPredObjInt(rec), spo_compare);
            Console.WriteLine("otriples.Root.Sort ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            SPComparer sp_compare = new SPComparer();
            // Упорядочивание otriples_op по o-p
            otriples_op.Root.SortByKey<SubjPredInt>(rec => 
            {
                object[] r = (object[])rec;
                return new SubjPredInt() { pred = (int)r[1], subj = (int)r[2] };
            }, sp_compare);
            Console.WriteLine("otriples_op Sort ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            // Упорядочивание dtriples_sp по s-p
            dtriples_sp.Root.SortByKey(rec =>
            {
                object[] r = (object[])rec;
                return new SubjPredInt() { pred = (int)r[1], subj = (int)r[0] };
            }, sp_compare);
            Console.WriteLine("dtriples_sp.Root.Sort ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;


            if (filescale)
            {
                // Создание шкалы (Надо переделать)
                CreateScale();
                //ShowScale();
                oscale.Clear();
                oscale.Fill(new object[0]);
                foreach (int v in scale.Values()) oscale.Root.AppendElement(v);
                oscale.Flush();
                CalculateRange(); // Наверное, range считается в CreateScale() 
            }
            Console.WriteLine("CreateScale ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            // Создание "широкой" таблицы
            ewt.Load();
            Console.WriteLine("ewt.Load() ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;
            
            // Вычисление кеша. Это можно не делать, все равно - кеш в оперативной памяти
            ewtHash.Load();
            Console.WriteLine("ewtHash.Load() ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;
            
        }

        private void PrepareArrays()
        {
            // Создание и упорядочивание дополнительных структур
            otriples_op.Clear();
            otriples_op.Fill(new object[0]);
            foreach (object v in otriples.Root.ElementValues()) otriples_op.Root.AppendElement(v);
            otriples_op.Flush();
            dtriples_sp.Clear();
            dtriples_sp.Fill(new object[0]);
            //foreach (PaEntry entry in dtriples.Root.Elements())
            //{
            //    int s = (int)entry.Field(0).Get();
            //    int p = (int)entry.Field(1).Get();
            //    dtriples_sp.Root.AppendElement(new object[] { s, p, entry.offset });
            //}
            dtriples.Root.Scan((off, pobj) =>
            {
                object[] tri = (object[])pobj;
                int s = (int)tri[0];
                int p = (int)tri[1];
                dtriples_sp.Root.AppendElement(new object[] { s, p, off });
                return true;
            });
            dtriples_sp.Flush();
        }

        private void Load(string filepath)
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
        //public void LoadXML(string filepath)
        //{
        //    otriples.Clear();
        //    otriples.Fill(new object[0]);
        //    dtriples.Clear();
        //    dtriples.Fill(new object[0]);
        //    XElement db = XElement.Load(filepath);
        //    foreach (XElement elem in db.Elements())
        //    {
        //        var id_att = elem.Attribute(ONames.rdfabout);
        //        if (id_att == null) continue;
        //        string id = id_att.Value;
        //        string typ = elem.Name.NamespaceName + elem.Name.LocalName;
        //        otriples.Root.AppendElement(
        //            new object[] { id, ONames.rdfnsstring + "type", typ } );
        //        foreach (XElement el in elem.Elements())
        //        {
        //            string prop = el.Name.NamespaceName + el.Name.LocalName;
        //            var resource_att = el.Attribute(ONames.rdfresource);
        //            if (resource_att != null)
        //            { // Объектная ссылка
        //                otriples.Root.AppendElement(
        //                    new object[] { id, prop, resource_att.Value } );
        //            }
        //            else
        //            { // Поле данных
        //                var lang_att = el.Attribute(ONames.xmllang);
        //                string lang = lang_att == null ? "" : lang_att.Value;
        //                dtriples.Root.AppendElement(
        //                    new object[] { id, prop, 
        //                        new object[] { 2, new object[] { el.Value, lang } } } );
        //            }
        //        }
        //    }
        //    otriples.Flush();
        //    dtriples.Flush();
        //    // Индексирование
        //    if (!otriples.IsEmpty)
        //    {
        //        OpenCreateIndexes();
        //    }
        //    spo_o_index.Load(null);
        //    sp_d_index.Load(null);
        //    op_o_index.Load(null);
        //    // Создание шкалы
        //    CreateScale();
        //    ShowScale();
        //    oscale.Clear();
        //    oscale.Fill(new object[0]);
        //    foreach (int v in scale.Values()) oscale.Root.AppendElement(v);
        //    oscale.Flush();
        //}

        private Scale1 scale = null;
        private void CreateScale()
        {
            long len = otriples.Root.Count() - 1;
            int r = 1;
            while (len != 0) { len = len >> 1; r++; }

            range = r + 2; //r + 4; // здесь 4 - фактор "разрежения" шкалы, можно меньше
            scale = new Scale1(range);
            foreach (object[] tr in otriples.Root.ElementValues())
            {
                int subj = (int)tr[0];
                int pred = (int)tr[1];
                int obj = (int)tr[2];
                int code = Scale1.Code(range, subj, pred, obj);
                scale[code] = 1;
            }
        }
        public void ShowScale(long ntriples)
        {
            int c = scale.Count();
            int c1 = 0;
            for (int i=0; i<c; i++)
            {
                int bit = scale[i];
                if (bit > 0) c1++;
            }
            Console.WriteLine("{0} {1} {2}", c, c1, ntriples);
        }
        private Dictionary<KeyValuePair<int, int>, int[]> SpoCache = new Dictionary<KeyValuePair<int, int>, int[]>();
        public IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            int[] res;
            var key = new KeyValuePair<int, int>(obj, pred);
            if (SpoCache.TryGetValue(key, out res)) return res;
            object[] diapason = GetDiapasonFromHash(obj, pred, 1);
            if (diapason == null) return Enumerable.Empty<int>();
            res = otriples_op.Root.ElementValues((long)diapason[0], (long)diapason[1]) // Можно использовать ElementValues потому что результат не итерация, а массив
                .Cast<object[]>()
                .Where(spo => pred == (int)spo[1]) // Если диапазон с учетом предиката, эта проверка не нужна, но операция очень дешевая, можно оставить
                .Select(spo => (int)spo[0]).ToArray();
            SpoCache.Add(key, res);
            return res;
        }

        public IEnumerable<int> GetSubjectByObjPred4(int obj, int pred)
        {
            object[] diapason = GetDiapasonFromHash(obj, pred, 1);
            if (diapason == null) return Enumerable.Empty<int>();
            return otriples_op.Root.Elements((long)diapason[0], (long)diapason[1])
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                //.Cast<object[]>()
                .Select(spo => (int)spo[0]);
        }
        public IEnumerable<int> GetSubjectByObjPred3(int obj, int pred)
        {
            if (otriples_op.IsEmpty) return Enumerable.Empty<int>();
            var itemEntriry = ewtHash.GetEntity(obj);
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[])itemEntriry.Field(2).Get();
            long num= (long)diapason[1];
            if(num<500)
            return otriples_op.Root.Elements((long)diapason[0],num)
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                //.Cast<object[]>()
                .Where(spo => pred == (int)spo[1])
                .Select(spo => (int)spo[0]);
            return otriples_op.Root.BinarySearchAll((long)diapason[0], num,
                entry=>((int)entry.Field(1).Get()).CompareTo(pred))
                  .Select(entry => (int)entry.Field(0).Get());
        }
        public IEnumerable<int> GetSubjectByObjPred2(int obj, int pred)
        {
            if (otriples_op.IsEmpty) return Enumerable.Empty<int>();      
            var itemEntriry = ewt.EWTable.Root.BinarySearchFirst(ent =>
            {
                var rec = (object[]) ent.Get();
                int ob = (int) rec[0];
                int cmp = ob.CompareTo(obj);
                return cmp;
            });
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[])itemEntriry.Field(2).Get();

            return otriples_op.Root.Elements((long)diapason[0], (long)diapason[1])
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                .Where(spo => pred == (int)spo[1])
                .Select(spo => (int)spo[0]);
        }

        public IEnumerable<int> GetSubjectByObjPred1(int obj, int pred)
        {
            var query = otriples_op.Root.BinarySearchAll(ent =>
             {
                 object[] rec = (object[])ent.Get();
                 int ob = (int)rec[2];
                 int cmp = ob.CompareTo(obj);
                 if (cmp != 0) return cmp;
                 int pr = (int)rec[1];
                 return pr.CompareTo(pred);
             });
            return query.Select(en => (int)en.Field(0).Get());
        }      
        public IEnumerable<int> GetSubjectByObjPred0(int obj, int pred)
        {
            return op_o_index.GetAll(ent => 
            {
                int ob = (int)ent.Field(2).Get();
                int cmp = ob.CompareTo(obj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int)en.Field(0).Get());
        }

        private Dictionary<KeyValuePair<int, int>, int[]> spOCache = new Dictionary<KeyValuePair<int, int>, int[]>();
        public IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            int[] res;
            var key = new KeyValuePair<int, int>(subj, pred);
            if (spOCache.TryGetValue(key, out res)) return res;
            object[] diapason = GetDiapasonFromHash(subj, pred, 0);
            if (diapason == null) return Enumerable.Empty<int>();
            res = otriples.Root.ElementValues((long)diapason[0], (long)diapason[1])
                .Cast<object[]>()
                .Where(spo => pred == (int)spo[1]) // Если диапазон с учетом предиката, эта проверка не нужна, но операция очень дешевая, можно оставить
                .Select(spo => (int)spo[2]).ToArray();
            spOCache.Add(key, res);
            return res;
        }
        public IEnumerable<int> GetObjBySubjPred4(int subj, int pred)
        {
            object[] diapason = GetDiapasonFromHash(subj, pred, 0);
            if (diapason == null) return Enumerable.Empty<int>();
            //return otriples.Root.Elements((long)diapason[0], (long)diapason[1])
            //    //.Where(entry => pred == (int)((object[])entry.Get())[1])
            //    .Select(en => (int)((object[])en.Get())[2]);
            return otriples.Root.Elements((long)diapason[0], (long)diapason[1])
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                .Where(spo => pred == (int)spo[1])
                .Select(spo => (int)spo[2]);
        }
        public IEnumerable<int> GetObjBySubjPred3(int subj, int pred)
        {
            if (otriples.IsEmpty) return Enumerable.Empty<int>();
            var itemEntriry = ewtHash.GetEntity(subj);
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[])itemEntriry.Field(1).Get();
            return otriples.Root.Elements((long)diapason[0], (long)diapason[1])
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                .Where(spo => pred == (int)spo[1])
                .Select(spo => (int)spo[2]);
        }
        public IEnumerable<int> GetObjBySubjPred2(int subj, int pred)
        {
            if (otriples.IsEmpty) return Enumerable.Empty<int>();
            var itemEntriry = ewt.EWTable.Root.BinarySearchFirst(ent =>
            {
                var rec = (object[]) ent.Get();
                int ob = (int) rec[0];
                int cmp = ob.CompareTo(subj);
                return cmp;
            });
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[]) itemEntriry.Field(1).Get();
            return otriples.Root.Elements((long)diapason[0], (long)diapason[1])
                .Where(entry => pred == (int) ((object[]) entry.Get())[1])
                .Select(en => (int) ((object[]) en.Get())[2]);
        }

        public IEnumerable<int> GetObjBySubjPred1(int subj, int pred)
        {
            return otriples.Root.BinarySearchAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int)en.Field(2).Get());
        }
        public IEnumerable<int> GetObjBySubjPred0(int subj, int pred)
        {
            return spo_o_index.GetAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int)en.Field(2).Get());
        }

        private object[] GetDiapasonFromHash(int key, int pred, int direction)
        {
            var itemEntry = ewtHash.GetEntity(key);
            if (itemEntry.IsEmpty) return null;
            var diap = (object[])itemEntry.Field(1 + direction).Get();
            return diap;
        }
        private object[] GetDiapasonFromHash1(int key, int pred, int direction)
        {
            var itemEntry = ewt.EWTable.Root.BinarySearchFirst(ent =>
            {
                var rec = (object[])ent.Get();
                int ob = (int)rec[0];
                int cmp = ob.CompareTo(key);
                return cmp;
            });
            if (itemEntry.IsEmpty) return null;
            var diap = (object[])itemEntry.Field(1 + direction).Get();
            return diap;
        }
        ////Здесь надо менять схему использования GroupedEntities
        //private object[] GetDiapasonFromHash0(int key, int pred, int direction)
        //{
        //    object[] row = null;
        //    if (!geHash.TryGetValue(key, out row)) return null;//new object[] { 0L, 0L };
        //    object[] predList = (object[])((object[])row[1 + direction])[1];
        //    object[] diap = null;
        //    foreach (object[] preddiap in predList)
        //    {
        //        int pre = (int)preddiap[0];
        //        if (pre == pred)
        //        {
        //            diap = (object[])preddiap[1];
        //            break;
        //        }
        //    }
        //    if (diap == null) return null;//new object[] { 0L, 0L };
        //    return diap;
        //}
        private Dictionary<KeyValuePair<int, int>, Literal[]> spDCache = new Dictionary<KeyValuePair<int, int>, Literal[]>();
        public IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            Literal[] res;
            var key = new KeyValuePair<int, int>(subj, pred);
            if (spDCache.TryGetValue(key, out res)) return res;

            object[] diapason = GetDiapasonFromHash(subj, pred, 2);
            if (diapason == null) return Enumerable.Empty<Literal>();

            PaEntry dtriple_entry = dtriples.Root.Element(0);
            res = dtriples_sp.Root.Elements((long)diapason[0], (long)diapason[1])
                .Where(entry => pred == (int)((object[])entry.Get())[1])
                .Select(en => 
                {
                    dtriple_entry.offset = (long)en.Field(2).Get();
                    object[] uni = (object[])dtriple_entry.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1)
                    {
                        lit.vid = LiteralVidEnumeration.integer;
                        lit.value = (int)uni[1];
                    }
                    if (vid == 3)
                    {
                        lit.vid = LiteralVidEnumeration.date;
                        lit.value = (long)uni[1];
                    }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                }).ToArray();
            spDCache.Add(key, res);
            return res;
        }
        public IEnumerable<Literal> GetDataBySubjPred4(int subj, int pred)
        {
            object[] diapason = GetDiapasonFromHash(subj, pred, 2);
            if (diapason == null) return Enumerable.Empty<Literal>();

            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.Elements((long)diapason[0], (long)diapason[1])
                //.Where(entry => pred == (int)((object[])entry.Get())[1])
                .Select(en =>
                {
                    dtriple_entry.offset = (long)en.Field(2).Get();
                    object[] uni = (object[])dtriple_entry.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1)
                    {
                        lit.vid = LiteralVidEnumeration.integer;
                        lit.value = (int)uni[1];
                    }
                    if (vid == 3)
                    {
                        lit.vid = LiteralVidEnumeration.date;
                        lit.value = (long)uni[1];
                    }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                });
        }


        public IEnumerable<Literal> GetDataBySubjPred3(int subj, int pred)
        {
            if (dtriples_sp.IsEmpty) return Enumerable.Empty<Literal>();
            var itemEntriry = ewtHash.GetEntity(subj);         
            if (itemEntriry.IsEmpty) return Enumerable.Empty<Literal>();
            var diapason = (object[])itemEntriry.Field(3).Get();
            
            if (dtriples_sp.Root.Count() == 0) return Enumerable.Empty<Literal>();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            long num=(long)diapason[1];

            var query = dtriples_sp.Root.Elements((long)diapason[0], num)
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                .Where(ve => (int)ve[1] == pred)
                .Select(ve =>
                {
                    dtriple_entry.offset = (long)ve[2];
                    object[] uni = (object[])dtriple_entry.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1)
                    {
                        lit.vid = LiteralVidEnumeration.integer;
                        lit.value = (int)uni[1];
                    }
                    else if (vid == 3)
                    {
                        lit.vid = LiteralVidEnumeration.date;
                        lit.value = (long)uni[1];
                    }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                });
            return query;
        }
        public IEnumerable<Literal> GetDataBySubjPred2(int subj, int pred)
        {
            if (dtriples_sp.IsEmpty) return Enumerable.Empty<Literal>();
            var itemEntriry = ewt.EWTable.Root.BinarySearchFirst(ent =>
            {
                var rec = (object[])ent.Get();
                int ob = (int)rec[0];
                int cmp = ob.CompareTo(subj);
                return cmp;
            });
            if (itemEntriry.IsEmpty) return Enumerable.Empty<Literal>();
            var diapason = (object[])itemEntriry.Field(3).Get();

            if (dtriples_sp.Root.Count() == 0) return Enumerable.Empty<Literal>();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.Elements((long)diapason[0], (long)diapason[1])
                .Where(entry => pred == (int)((object[])entry.Get())[1])
                .Select(en =>
                {
                    dtriple_entry.offset = (long)en.Field(2).Get();
                    object[] uni = (object[])dtriple_entry.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1)
                    {
                        lit.vid = LiteralVidEnumeration.integer;
                        lit.value = (int)uni[1];
                    }
                    if (vid == 3)
                    {
                        lit.vid = LiteralVidEnumeration.date;
                        lit.value = (long)uni[1];
                    }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                });
        }
        public IEnumerable<Literal> GetDataBySubjPred1(int subj, int pred)
        {
            if (dtriples.Root.Count() == 0) return Enumerable.Empty<Literal>();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.BinarySearchAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => 
                {
                    dtriple_entry.offset = (long)en.Field(2).Get();
                    object[] uni = (object[])dtriple_entry.Field(2).Get();
                    Literal lit = new Literal();
                    int vid = (int)uni[0];
                    if (vid == 1) { lit.vid = LiteralVidEnumeration.integer; lit.value = (int)uni[1]; }
                    if (vid == 3) { lit.vid = LiteralVidEnumeration.date; lit.value = (long)uni[1]; }
                    else if (vid == 2)
                    {
                        lit.vid = LiteralVidEnumeration.text;
                        object[] txt = (object[])uni[1];
                        lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
                    }
                    return lit;
                });
        }
        public IEnumerable<Literal> GetDataBySubjPred0(int subj, int pred)
        {
            return sp_d_index.GetAll(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en =>
                {
                    return PObjToLiteral(en.Field(2).Get());
                });
        }

        private static Literal PObjToLiteral(object pobj)
        {
            object[] uni = (object[])pobj;
            Literal lit = new Literal();
            int vid = (int)uni[0];
            if (vid == 1) { lit.vid = LiteralVidEnumeration.integer; lit.value = (int)uni[1]; }
            if (vid == 3) { lit.vid = LiteralVidEnumeration.date; lit.value = (long)uni[1]; }
            else if (vid == 2)
            {
                lit.vid = LiteralVidEnumeration.text;
                object[] txt = (object[])uni[1];
                lit.value = new Text() { s = (string)txt[0], l = (string)txt[1] };
            }
            return lit;
        }
        public bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (!ChkInScale(subj, pred, obj)) return false;
            SubjPredObjInt key = new SubjPredObjInt() { subj = subj, pred = pred, obj = obj };
            var entry = otriples.Root.BinarySearchFirst(ent => (new SubjPredObjInt(ent.Get())).CompareTo(key));
            return !entry.IsEmpty;
        }
        // Проверка наличия объектного триплета через шкалу. Если false - точно нет, при true надо продолжать проверку
        public bool ChkInScale(int subj, int pred, int obj)
        {
            if (range > 0)
            {
                int code = Scale1.Code(range, subj, pred, obj);
                int bit;
                if (filescale)
                {
                    int word = (int)oscale.Root.Element(Scale1.GetArrIndex(code)).Get();
                    bit = Scale1.GetFromWord(word, code);
                }
                else // if (memoryscale)
                {
                    bit = scale[code];
                }
                if (bit == 0) return false;
            }
            return true;
        }
        public bool ChkOSubjPredObj0(int subj, int pred, int obj)
        {
            // Шкалу добавлю позднее
            if (false && range > 0)
            {
                int code = Scale1.Code(range, subj, pred, obj);
                //int word = (int)oscale.Root.Element(Scale1.GetArrIndex(code)).Get();
                //int tb = Scale1.GetFromWord(word, code);
                int tb = scale[code];
                if (tb == 0) return false;
                // else if (tb == 1) return true; -- это был источник ошибки
                // else надо считаль длинно, см. далее
            }
            return !spo_o_index.GetFirst(ent =>
            {
                int su = (int)ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int)ent.Field(1).Get();
                cmp = pr.CompareTo(pred);
                if (cmp != 0) return cmp;
                int ob = (int)ent.Field(2).Get();
                return ob.CompareTo(obj);
            }).IsEmpty;
            //return !spo_o_index.GetFirstByKey(new SubjPredObjInt() { subj = subj, pred = pred, obj = obj }).IsEmpty;
        }
        //TODO: Надо переделать
        public XElement GetItem(string subject)
        {
            if (otriples.Root.Count() == 0) return null;
            if (dtriples.Root.Count() == 0) return null;
            XElement res = new XElement("record", new XAttribute("id", subject));
            //PaEntry dent = dtriples.Root.Element(0);
            //foreach (var ent in subject_d_index.GetAllByKey(subject))
            //foreach (var ent in sp_d_index.GetAll(en => ((string)en.Field(0).Get()).CompareTo(subject)))
            //{
            //    object[] tr = (object[])ent.Get();
            //    string predicate = (string)tr[1];
            //    object[] literal = (object[])tr[2];
            //    res.Add(new XElement("field", new XAttribute("prop", predicate),
            //        ((object[])literal[1])[0]));
            //}
            string type = null;
            //PaEntry oent = otriples.Root.Element(0);
            //foreach (var ent in subject_o_index.GetAllByKey(subject))
            foreach (var ent in spo_o_index.GetAll(en => ((string)en.Field(0).Get()).CompareTo(subject)))
            {
                object[] tr = (object[])ent.Get();
                string predicate = (string)tr[1];
                string obj = (string)tr[2];
                if (predicate == ONames.rdftypestring)
                {
                    type = obj;
                }
                else
                {
                    res.Add(new XElement("direct", new XAttribute("prop", predicate),
                        new XElement("record", new XAttribute("id", obj))));
                }
            }
            if (type != null) res.Add(new XAttribute("type", type));
            // Обратные ссылки
            //foreach (var ent in obj_o_index.GetAllByKey(subject))
            foreach (var ent in op_o_index.GetAll(en => ((string)en.Field(2).Get()).CompareTo(subject)))
            {
                object[] tr = (object[])ent.Get();
                string subj = (string)tr[0];
                string predicate = (string)tr[1];
                res.Add(new XElement("inverse", new XAttribute("prop", predicate),
                    subj));
            }

            return res;
        }

        // ================== Семейство методов для OValRowInt - представления (нужно назвать метод или алгоритм) =========
        // Получение диапазонов
        public Diapason GetDiap_spo(int subj)
        {
            return otriples.Root.BinarySearchDiapason(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
        }
        public Diapason GetDiap_op(int obj)
        {
            return otriples_op.Root.BinarySearchDiapason(ent => ((int)ent.Field(2).Get()).CompareTo(obj));
        }
        public Diapason GetDiap_spd(int subj)
        {
            return dtriples_sp.Root.BinarySearchDiapason(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
        }
        public bool CheckPredObjInDiap(Diapason di, int pred, int obj)
        {
            Diapason set = otriples.Root.BinarySearchDiapason(di.start, di.numb, ent =>
            {
                int cmp = ((int)ent.Field(1).Get()).CompareTo(pred);
                if (cmp != 0) return cmp;
                return ((int)ent.Field(2).Get()).CompareTo(obj);
            });
            return !set.IsEmpty();
            //var query = otriples.Root.BinarySearchAll(di.start, di.numb, ent =>
            //    {
            //        int cmp = ((int)ent.Field(1).Get()).CompareTo(pred);
            //        if (cmp != 0) return cmp;
            //        return ((int)ent.Field(2).Get()).CompareTo(obj);
            //    });
            //return query.Any();
        }
        public IEnumerable<int> GetObjInDiap(Diapason di, int pred)
        {
            return otriples.Root.BinarySearchAll(di.start, di.numb, ent => ((int)ent.Field(1).Get()).CompareTo(pred))
                .Select(en => (int)en.Field(2).Get());
        }
        public IEnumerable<int> GetSubjInDiap(Diapason di, int pred)
        {
            //return otriples_op.Root.BinarySearchAll(di.start, di.numb, ent => ((int)ent.Field(1).Get()).CompareTo(pred))
            //    .Select(en => (int)en.Field(0).Get());

            Diapason internal_diap = otriples_op.Root.BinarySearchDiapason(di.start, di.numb, ent =>
            {
                return ((int)ent.Field(1).Get()).CompareTo(pred);
            });
            return otriples_op.Root.Elements(internal_diap.start, internal_diap.numb)
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                .Select(p_obj => (int)((object[])p_obj)[0]);
        }
        public IEnumerable<Literal> GetDatInDiap(Diapason di, int pred)
        {
            Diapason internal_diap = dtriples_sp.Root.BinarySearchDiapason(di.start, di.numb, ent =>
            {
                return ((int)ent.Field(1).Get()).CompareTo(pred);
            });
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.Elements(internal_diap.start, internal_diap.numb)
                .Select<PaEntry, object[]>(ent => (object[])ent.Get())
                .Select(p_obj => 
                    {
                        long off = (long)((object[])p_obj)[2];
                        dtriple_entry.offset = off;
                        return PObjToLiteral(dtriple_entry.Field(2).Get());
                    })
                ;
        }

    }
}
