using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NameTable;
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
                new NamedType("integer", new PType(PTypeEnumeration.real)),
                new NamedType("string", new PTypeRecord(
                    new NamedType("s", new PType(PTypeEnumeration.sstring)),
                    new NamedType("l", new PType(PTypeEnumeration.sstring)))),
                new NamedType("date", new PType(PTypeEnumeration.longinteger)),
                new NamedType("bool", new PType(PTypeEnumeration.boolean)),
                new NamedType("typedObject", new PTypeRecord(
                    new NamedType("s", new PType(PTypeEnumeration.sstring)),
                    new NamedType("t", new PType(PTypeEnumeration.sstring)))));
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
        public PaCell dtriples;
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
            TripleInt.SiCodingEntities = new StringIntMD5RAMCoding(path + "entitiesCodes");
            TripleInt.SiCodingPredicates= new StringIntMD5RAMCoding(path + "predicatesCodes");
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
            ewt = new EntitiesWideTable(path, new[]  
            {
                new DiapasonScanner<int>(otriples, ent => (int)((object[])ent.Get())[0]),
                new DiapasonScanner<int>(otriples_op, ent => (int)((object[])ent.Get())[2]),
                new DiapasonScanner<int>(dtriples_sp, ent => (int)((object[])ent.Get())[0])
            });
            //ewtHash = new EntitiesMemoryHashTable(ewt);
           // ewtHash.Load();
            

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
            TripleInt.SiCodingEntities.WarmUp();
            TripleInt.SiCodingPredicates.WarmUp();
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
            otriples.Root.SortByKey<SubjPredObjInt>(rec => new SubjPredObjInt(rec), new SPOComparer());
            Console.WriteLine("otriples.Root.Sort ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            //SPOComparer spo_compare = new SPOComparer();
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
            //ewtHash.Load();
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
            TripleInt.SiCodingEntities.Clear();
            TripleInt.SiCodingPredicates.Clear();
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
                    switch (lit.vid)
                    {

                        case LiteralVidEnumeration.integer:
                            da = new object[] { 1, lit.Value };
                            break;
                        case LiteralVidEnumeration.date:
                            da = new object[] { 3, lit.Value };
                            break;
                        case LiteralVidEnumeration.boolean:
                            da = new object[] { 4, lit.Value };
                            break;
                        case LiteralVidEnumeration.text:
                        {
                            Text t = (Text)lit.Value;
                            da = new object[] { 2, new object[] { t.Value, t.Lang } };
                    }
                            break;
                        case LiteralVidEnumeration.typedObject:
                            {
                                TypedObject t = (TypedObject)lit.Value;
                                da = new object[] { 5, new object[] { t.Value, t.Type } };
                            }
                            break;              
                        default:
                        da = new object[] { 0, null };
                            break;
                    }
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
        private readonly Dictionary<KeyValuePair<int, int>, int[]> SpoCache = new Dictionary<KeyValuePair<int, int>, int[]>();
        public IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            if (obj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<int>();
            int[] res;
            var key = new KeyValuePair<int, int>(obj, pred);
            object[] diapason = GetDiapasonFromHash(obj, 1);
            if (SpoCache.TryGetValue(key, out res)) return res;
            res = diapason == null
                ? new int[0]
                : otriples_op.Root.ElementValues((long) diapason[0], (long) diapason[1])
                    // Можно использовать ElementValues потому что результат не итерация, а массив
                    .Cast<object[]>()
                    .Where(spo => pred == (int) spo[1])
                    // Если диапазон с учетом предиката, эта проверка не нужна, но операция очень дешевая, можно оставить
                    .Select(spo => (int) spo[0]).ToArray();
            SpoCache.Add(key, res);
            return res;
        }

        #region GetSubjectByObjPred

        public IEnumerable<int> GetSubjectByObjPred4(int obj, int pred)
        {
            object[] diapason = GetDiapasonFromHash(obj, 1);
            if (diapason == null) return Enumerable.Empty<int>();
            return otriples_op.Root.Elements((long) diapason[0], (long) diapason[1])
                .Select<PaEntry, object[]>(ent => (object[]) ent.Get())
                //.Cast<object[]>()
                .Select(spo => (int) spo[0]);
        }

        public IEnumerable<int> GetSubjectByObjPred3(int obj, int pred)
        {
            if (otriples_op.IsEmpty) return Enumerable.Empty<int>();
            var itemEntriry = ewtHash.GetEntity(obj);
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[]) itemEntriry.Field(2).Get();
            long num = (long) diapason[1];
            if (num < 500)
                return otriples_op.Root.Elements((long) diapason[0], num)
                    .Select<PaEntry, object[]>(ent => (object[]) ent.Get())
                    //.Cast<object[]>()
                    .Where(spo => pred == (int) spo[1])
                    .Select(spo => (int) spo[0]);
            return otriples_op.Root.BinarySearchAll((long) diapason[0], num,
                entry => ((int) entry.Field(1).Get()).CompareTo(pred))
                .Select(entry => (int) entry.Field(0).Get());
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
            var diapason = (object[]) itemEntriry.Field(2).Get();

            return otriples_op.Root.Elements((long) diapason[0], (long) diapason[1])
                .Select<PaEntry, object[]>(ent => (object[]) ent.Get())
                .Where(spo => pred == (int) spo[1])
                .Select(spo => (int) spo[0]);
        }

        public IEnumerable<int> GetSubjectByObjPred1(int obj, int pred)
        {
            var query = otriples_op.Root.BinarySearchAll(ent =>
            {
                object[] rec = (object[]) ent.Get();
                int ob = (int) rec[2];
                int cmp = ob.CompareTo(obj);
                if (cmp != 0) return cmp;
                int pr = (int) rec[1];
                return pr.CompareTo(pred);
            });
            return query.Select(en => (int) en.Field(0).Get());
        }

        public IEnumerable<int> GetSubjectByObjPred0(int obj, int pred)
        {
            return op_o_index.GetAll(ent =>
            {
                int ob = (int) ent.Field(2).Get();
                int cmp = ob.CompareTo(obj);
                if (cmp != 0) return cmp;
                int pr = (int) ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int) en.Field(0).Get());
        }

        #endregion     

        private readonly Dictionary<KeyValuePair<int, int>, int[]> spOCache = new Dictionary<KeyValuePair<int, int>, int[]>();
        public IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<int>();
            int[] res;
            var key = new KeyValuePair<int, int>(subj, pred);
            if (spOCache.TryGetValue(key, out res)) return res;
            object[] diapason = GetDiapasonFromHash(subj, 0);
            if (diapason == null) return Enumerable.Empty<int>();
            res = otriples.Root.ElementValues((long)diapason[0], (long)diapason[1])
                .Cast<object[]>()
                .Where(spo => pred == (int)spo[1]) // Если диапазон с учетом предиката, эта проверка не нужна, но операция очень дешевая, можно оставить
                .Select(spo => (int)spo[2]).ToArray();
            spOCache.Add(key, res);
            return res;
        }

        #region GetObjBySubjPred old

        public IEnumerable<int> GetObjBySubjPred4(int subj, int pred)
        {
            object[] diapason = GetDiapasonFromHash(subj, 0);
            if (diapason == null) return Enumerable.Empty<int>();
            //return otriples.Root.Elements((long)diapason[0], (long)diapason[1])
            //    //.Where(entry => pred == (int)((object[])entry.Get())[1])
            //    .Select(en => (int)((object[])en.Get())[2]);
            return otriples.Root.Elements((long) diapason[0], (long) diapason[1])
                .Select<PaEntry, object[]>(ent => (object[]) ent.Get())
                .Where(spo => pred == (int) spo[1])
                .Select(spo => (int) spo[2]);
        }

        public IEnumerable<int> GetObjBySubjPred3(int subj, int pred)
        {
            if (otriples.IsEmpty) return Enumerable.Empty<int>();
            var itemEntriry = ewtHash.GetEntity(subj);
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[]) itemEntriry.Field(1).Get();
            return otriples.Root.Elements((long) diapason[0], (long) diapason[1])
                .Select<PaEntry, object[]>(ent => (object[]) ent.Get())
                .Where(spo => pred == (int) spo[1])
                .Select(spo => (int) spo[2]);
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
            return otriples.Root.Elements((long) diapason[0], (long) diapason[1])
                .Where(entry => pred == (int) ((object[]) entry.Get())[1])
                .Select(en => (int) ((object[]) en.Get())[2]);
        }

        public IEnumerable<int> GetObjBySubjPred1(int subj, int pred)
        {
            return otriples.Root.BinarySearchAll(ent =>
            {
                int su = (int) ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int) ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int) en.Field(2).Get());
        }

        public IEnumerable<int> GetObjBySubjPred0(int subj, int pred)
        {
            return spo_o_index.GetAll(ent =>
            {
                int su = (int) ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int) ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => (int) en.Field(2).Get());
        }

        #endregion

        private object[] GetDiapasonFromHash(int key, int direction)
        {
            if (key < 0 || key > ewt.EWTable.Root.Count()) return null;
            var itemEntry = ewt.EWTable.Root.Element(key); //ewtHash.GetEntity(key);
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
            if (subj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<Literal>();
            Literal[] res;
            var key = new KeyValuePair<int, int>(subj, pred);
            if (spDCache.TryGetValue(key, out res)) return res;

            object[] diapason = GetDiapasonFromHash(subj, 2);
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            res = diapason == null
                ? new Literal[0]
                : dtriples_sp.Root.Elements((long) diapason[0], (long) diapason[1])
                    .Where(entry => pred == (int) ((object[]) entry.Get())[1])
                    .Select(en =>
                    {
                        dtriple_entry.offset = (long) en.Field(2).Get();
                        return ToLiteral((object[]) dtriple_entry.Field(2).Get());
                    }).ToArray();
            spDCache.Add(key, res);
            return res;
        }

        private static Literal ToLiteral(object[] uni)
        {
            switch ((int) uni[0])
            {
                case 1:
                    return new Literal(LiteralVidEnumeration.integer) {Value = Convert.ToDouble(uni[1])};
                case 3:
                    return new Literal(LiteralVidEnumeration.date) {Value = (long) uni[1]};
                case 4:
                    return new Literal(LiteralVidEnumeration.boolean) {Value = (bool) uni[1]};
                case 5:
                {
                    object[] txt = (object[]) uni[1];
                    return new Literal(LiteralVidEnumeration.typedObject)
                    {
                        Value = new TypedObject() {Value = (string) txt[0], Type = (string) txt[1]}
                    };
                }
                case 2:
                    object[] txt1 = (object[]) uni[1];
                    return new Literal(LiteralVidEnumeration.text)
                    {
                        Value = new Text() {Value = (string) txt1[0], Lang = (string) txt1[1]}
                    };
                default:
                    return new Literal(LiteralVidEnumeration.nil);
            }          
    }

        #region GetDataBySubjPred old

        public IEnumerable<Literal> GetDataBySubjPred4(int subj, int pred)
        {
            object[] diapason = GetDiapasonFromHash(subj, 2);
            if (diapason == null) return Enumerable.Empty<Literal>();

            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.Elements((long) diapason[0], (long) diapason[1])
                //.Where(entry => pred == (int)((object[])entry.Get())[1])
                .Select(en =>
                {
                    dtriple_entry.offset = (long) en.Field(2).Get();
                    return ToLiteral((object[]) dtriple_entry.Field(2).Get());
                });
        }

        public IEnumerable<Literal> GetDataBySubjPred3(int subj, int pred)
        {
            if (dtriples_sp.IsEmpty) return Enumerable.Empty<Literal>();
            var itemEntriry = ewtHash.GetEntity(subj);
            if (itemEntriry.IsEmpty) return Enumerable.Empty<Literal>();
            var diapason = (object[]) itemEntriry.Field(3).Get();

            if (dtriples_sp.Root.Count() == 0) return Enumerable.Empty<Literal>();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            long num = (long) diapason[1];

            var query = dtriples_sp.Root.Elements((long) diapason[0], num)
                .Select<PaEntry, object[]>(ent => (object[]) ent.Get())
                .Where(ve => (int) ve[1] == pred)
                .Select(ve =>
                {
                    dtriple_entry.offset = (long) ve[2];
                    return ToLiteral((object[]) dtriple_entry.Field(2).Get());
                });
            return query;
        }

        public IEnumerable<Literal> GetDataBySubjPred2(int subj, int pred)
        {
            if (dtriples_sp.IsEmpty) return Enumerable.Empty<Literal>();
            var itemEntriry = ewt.EWTable.Root.BinarySearchFirst(ent =>
            {
                var rec = (object[]) ent.Get();
                int ob = (int) rec[0];
                int cmp = ob.CompareTo(subj);
                return cmp;
            });
            if (itemEntriry.IsEmpty) return Enumerable.Empty<Literal>();
            var diapason = (object[]) itemEntriry.Field(3).Get();

            if (dtriples_sp.Root.Count() == 0) return Enumerable.Empty<Literal>();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.Elements((long) diapason[0], (long) diapason[1])
                .Where(entry => pred == (int) ((object[]) entry.Get())[1])
                .Select(en =>
                {
                    dtriple_entry.offset = (long) en.Field(2).Get();
                    return ToLiteral((object[]) dtriple_entry.Field(2).Get());
                });
        }

        public IEnumerable<Literal> GetDataBySubjPred1(int subj, int pred)
        {
            if (dtriples.Root.Count() == 0) return Enumerable.Empty<Literal>();
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            return dtriples_sp.Root.BinarySearchAll(ent =>
            {
                int su = (int) ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int) ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en =>
                {
                    dtriple_entry.offset = (long) en.Field(2).Get();
                    return ToLiteral((object[]) dtriple_entry.Field(2).Get());
                });
        }

        public IEnumerable<Literal> GetDataBySubjPred0(int subj, int pred)
        {
            return sp_d_index.GetAll(ent =>
            {
                int su = (int) ent.Field(0).Get();
                int cmp = su.CompareTo(subj);
                if (cmp != 0) return cmp;
                int pr = (int) ent.Field(1).Get();
                return pr.CompareTo(pred);
            })
                .Select(en => PObjToLiteral(en.Field(2).Get()));
        }

        #endregion


        private static Literal PObjToLiteral(object pobj)
        {
            return ToLiteral((object[])pobj);
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
        public Diapason GetDiapason_spd(int subj)
        {
            return dtriples_sp.Root.BinarySearchDiapason(ent => ((int)ent.Field(0).Get()).CompareTo(subj));
        }
        public bool CheckPredObjInDiapason(Diapason di, int pred, int obj)
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
/*
        public IEnumerable<int> GetObjInDiap(Diapason di, int pred)
        {
            return otriples.Root.BinarySearchAll(di.start, di.numb, ent => ((int)ent.Field(1).Get()).CompareTo(pred))
                .Select(en => (int)en.Field(2).Get());
        }
*/
        public IEnumerable<int> GetSubjInDiapason(Diapason di, int pred)
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
        public IEnumerable<Literal> GetDatInDiapason(Diapason di, int pred)
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


        private readonly Dictionary<int, IEnumerable<KeyValuePair<Int32, int>>> SPoCache =
            new Dictionary<int, IEnumerable<KeyValuePair<int, int>>>();

        public IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (obj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<int, int>>();
            IEnumerable<KeyValuePair<Int32, int>> res;
            if (SPoCache.TryGetValue(obj, out res)) return res;
            object[] diapason = GetDiapasonFromHash(obj, 1);
            res = diapason == null
                ? new KeyValuePair<int, int>[0]
                : otriples_op.Root.ElementValues((long) diapason[0], (long) diapason[1])
                    // Можно использовать ElementValues потому что результат не итерация, а массив
                    .Cast<object[]>()
                    //.Where(spo => pred == (int)spo[1]) // Если диапазон с учетом предиката, эта проверка не нужна, но операция очень дешевая, можно оставить
                    .Select(spo => new KeyValuePair<int, int>((int) spo[0], (int)spo[1])).ToArray();
            SPoCache.Add(obj, res);
            return res;
        }

        public IEnumerable<Int32> GetSubjectByDataPred(int p, Literal d)
        {
            return Enumerable.Empty<Int32>();
        }

        private Dictionary<int, IEnumerable<KeyValuePair<Int32, Int32>>> sPOCache =
            new Dictionary<int, IEnumerable<KeyValuePair<int, int>>>();

        public IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)
        {
            if (subj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<int, int>>();
            IEnumerable<KeyValuePair<int, int>> res;  
            if (sPOCache.TryGetValue(subj, out res)) return res;
            object[] diapason = GetDiapasonFromHash(subj, 0);
            res = diapason == null
                ? new KeyValuePair<Int32, Int32>[0]
                : otriples.Root.ElementValues((long) diapason[0], (long) diapason[1])
                    .Cast<object[]>()
                    //.Where(spo => pred == (int)spo[1]) // Если диапазон с учетом предиката, эта проверка не нужна, но операция очень дешевая, можно оставить
                    .Select(spo => new KeyValuePair<int, int>((int) spo[2], (int) spo[1])).ToArray();
            sPOCache.Add(subj, res);
            return res;
        }

        readonly Dictionary<int, IEnumerable<KeyValuePair<Literal, int>>> sPDCache = new Dictionary<int, IEnumerable<KeyValuePair<Literal, int>>>();

        public IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (subj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<Literal, int>>();
            IEnumerable<KeyValuePair<Literal, int>> res;
            if (sPDCache.TryGetValue(subj, out res)) return res;

            object[] diapason = GetDiapasonFromHash(subj, 2);
            PaEntry dtriple_entry = dtriples.Root.Element(0);
            res = diapason == null
                ? new KeyValuePair<Literal, int>[0]
                : dtriples_sp.Root.Elements((long) diapason[0], (long) diapason[1])
                    //.Where(entry => pred == (int)((object[])entry.Get())[1])
                    .Select(en =>
                    {
                        var row = ((object[]) en.Get());
                        var pred = (int) row[1];
                        dtriple_entry.offset = (long) row[2];
                        Literal lit = ToLiteral((object[]) dtriple_entry.Field(2).Get());
                        return new KeyValuePair<Literal, int>(lit, pred);
                    }).ToArray();
            sPDCache.Add(subj, res);
            return res;
        }
    }
}
