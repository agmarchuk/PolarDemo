using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteralStores;
using NameTable;
using PolarDB;
using RDFStores;
using ScaleBit4Check;


namespace TripleIntClasses
{
    public class TripleStoreInt : RDFIntStoreAbstract
    {
        private PType tp_otriple_seq;
        private PType tp_triple_seq_two;

        private PType tp_entity;
        private PType tp_dtriple_spf;
        private PType tp_dtriple_spf_two;
        internal EntitiesWideTable ewt;
        internal EntitiesMemoryHashTable ewtHash;
    

        private string otriplets_op_filePath;
        private string otriples_filePath;
        private string dtriples_filePath;

        public override void InitTypes()
        {
            tp_entity = new PType(PTypeEnumeration.integer);
            tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("object", tp_entity)));
            // Тип для экономного выстраивания индекса s-p для dtriples
            tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                    new NamedType("subject", tp_entity),
                    new NamedType("predicate", tp_entity),
                    new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
            tp_dtriple_spf_two = new PTypeSequence(new PTypeRecord(
                new NamedType("predicate", tp_entity),
                new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
            tp_triple_seq_two = new PTypeSequence(new PTypeRecord(
                new NamedType("predicate", tp_entity),
                new NamedType("_bject", tp_entity)));

        }
     
        private PaCell otriples;
        private PaCell otriples_op; // объектные триплеты, упорядоченные по o-p
        private PaCell dtriples_sp;
        private FlexIndexView<SubjPredObjInt> spo_o_index = null;
        private FlexIndexView<SubjPredInt> sp_d_index = null;
        private FlexIndexView<SubjPredInt> op_o_index = null;

        //private bool memoryscale = false; // Наоборот от filescale
        private int range = 0;
        //// Идея хорошая, но надо менять схему реализации
        //private GroupedEntities getable;
        //private Dictionary<int, object[]> geHash;

        public TripleStoreInt(string path, IStringIntCoding entityCoding, PredicatesCoding predicatesCoding, NameSpaceStore nameSpaceStore, LiteralStoreAbstract literalStore)
            : base(path, entityCoding, predicatesCoding, nameSpaceStore, literalStore)
        {
            this.path = path;

            InitTypes();
            otriplets_op_filePath = path + "otriples_op.pac";
            otriples_filePath = path + "otriples.pac";
            dtriples_filePath = path + "dtriples_spf.pac";
           
            if (File.Exists(otriples_filePath))
                Open(true);
            else
            {
                otriples = new PaCell(tp_triple_seq_two, otriples_filePath, false);
                otriples_op = new PaCell(tp_triple_seq_two, otriplets_op_filePath, false);
                dtriples_sp = new PaCell(tp_dtriple_spf_two, dtriples_filePath, false);
               
            }

            if (!Scale.Cell.IsEmpty)
               Scale.CalculateRange();


            ewt = new EntitiesWideTable(path, 3);
            //ewtHash = new EntitiesMemoryHashTable(ewt);
            // ewtHash.Load();



            //getable = new GroupedEntities(path); // Это хорошая идея, но нужно менять схему реализации
            //getable.CheckGroupedEntities();
            //geHash = getable.GroupedEntitiesHash();

        }

        private void Open(bool readOnlyMode)
        {
            if (readOnlyMode)
            {
                otriples = new PaCell(tp_triple_seq_two, otriples_filePath);
                otriples_op = new PaCell(tp_triple_seq_two, otriplets_op_filePath);
                dtriples_sp = new PaCell(tp_dtriple_spf_two, dtriples_filePath);
            }
            else
            {
                otriples = new PaCell(tp_otriple_seq, otriples_filePath + "tmp", false);
                otriples_op = new PaCell(tp_otriple_seq, otriplets_op_filePath + "tmp", false);
                dtriples_sp = new PaCell(tp_dtriple_spf, dtriples_filePath + "tmp", false);
            }    
        }


        private void RemoveColumns()
        {
            otriples = new PaCell(tp_triple_seq_two, otriples_filePath, false);
            otriples_op = new PaCell(tp_triple_seq_two, otriplets_op_filePath, false);
            dtriples_sp = new PaCell(tp_dtriple_spf_two, dtriples_filePath, false);
            otriples.Clear();
            otriples_op.Clear();
            dtriples_sp.Clear();
            otriples.Fill(new object[0]);
            dtriples_sp.Fill(new object[0]);
            otriples_op.Fill(new object[0]);

            var otriples_tmp = new PaCell(tp_otriple_seq, otriples_filePath + "tmp");
            var otriples_op_tmp = new PaCell(tp_otriple_seq, otriplets_op_filePath + "tmp");
            var dtriples_sp_tmp = new PaCell(tp_dtriple_spf, dtriples_filePath + "tmp");

            foreach (object[] elementValue in otriples_tmp.Root.ElementValues())
                otriples.Root.AppendElement(new[] { elementValue[1], elementValue[2] });
            foreach (object[] elementValue in otriples_op_tmp.Root.ElementValues())
                otriples_op.Root.AppendElement(new[] { elementValue[1], elementValue[0] });
            foreach (object[] elementValue in dtriples_sp_tmp.Root.ElementValues())
                dtriples_sp.Root.AppendElement(new[] { elementValue[1], elementValue[2] });

            otriples_tmp.Close();
            otriples_op_tmp.Close();
            dtriples_sp_tmp.Close();
            File.Delete(otriplets_op_filePath + "tmp");
            File.Delete(otriples_filePath + "tmp");
            File.Delete(dtriples_filePath + "tmp");

            otriples.Close();
            otriples_op.Close();
            dtriples_sp.Close();
        }

        public override void WarmUp()
        {
            if (otriples.IsEmpty) return;
            foreach (var v in otriples.Root.ElementValues()) ;
            foreach (var v in otriples_op.Root.ElementValues()) ;
       
            foreach (var v in dtriples_sp.Root.ElementValues()) ;

        
            foreach (var v in ewt.EWTable.Root.ElementValues()) ; // этая ячейка "подогревается" при начале программы
        base.WarmUp();
        }
        



        public override void LoadTurtle(string filepath, bool useBuffer)
        {
            DateTime tt0 = DateTime.Now;

            Close();

            Open(false);

                     if(useBuffer)
            TurtleInt.LoadByGraphsBuffer(filepath, otriples, dtriples_sp, this);
            else TurtleInt.LoadTriplets(filepath, otriples, dtriples_sp, this);

            Console.WriteLine("Load ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            PrepareArrays();
            Console.WriteLine("PrepareArrays ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            // Упорядочивание otriples по s-p-o
            otriples.Root.SortByKey(rec => new SubjPredObjInt(rec), new SPOComparer());
            Console.WriteLine("otriples.Root.Sort ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            //SPOComparer spo_compare = new SPOComparer();
            SPComparer sp_compare = new SPComparer();
            // Упорядочивание otriples_op по o-p
            otriples_op.Root.SortByKey(rec =>
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

          Scale.WriteScale(otriples);
            Console.WriteLine("CreateScale ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;


            // Создание "широкой" таблицы
            ewt.Load(new[]  
            {
                new DiapasonScanner<int>(otriples, ent => (int)((object[])ent.Get())[0]),
                new DiapasonScanner<int>(otriples_op, ent => (int)((object[])ent.Get())[2]),
                new DiapasonScanner<int>(dtriples_sp, ent => (int)((object[])ent.Get())[0])
            });
            Console.WriteLine("ewt.Load() ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            // Вычисление кеша. Это можно не делать, все равно - кеш в оперативной памяти
            //ewtHash.Load();
            // Console.WriteLine("ewtHash.Load() ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            Close();
            RemoveColumns();
            Console.WriteLine("RemoveColumns() ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;
            Open(true);
        }

        private void Test()
        {
            var first = otriples_op.Root.ElementValues().Cast<object[]>().First();
            int currentPredicate = (int)first[1];
            var currentDiapason = new Diapason();
            var currentObjectDiapason = new Diapason();
            int currentObj = (int)first[2];

            using (StreamWriter wr = new StreamWriter(@"..\..\temp.txt"))
                foreach (object[] spo in otriples_op.Root.ElementValues())
                {

                    if (currentPredicate == (int)spo[1] && currentObj == (int)spo[2])
                    {
                        currentDiapason.numb++;
                    }
                    else
                    {
                        currentPredicate = (int)spo[1];
                        wr.WriteLine("diapasonPredicate " + currentDiapason.numb);
                        currentDiapason.start += currentDiapason.numb;
                        currentDiapason.numb = 1;
                        if (currentObj == (int)spo[2])
                            currentObjectDiapason.numb++;
                        else
                        {
                            currentObj = (int)spo[2];
                            currentObjectDiapason.start += currentObjectDiapason.numb;
                            //      wr.WriteLine("currentObjectDiapason " + currentObjectDiapason.numb);
                            currentObjectDiapason.numb = 1;
                        }
                    }
                }

        }

        private void PrepareArrays()
        {
            // Создание и упорядочивание дополнительных структур

            otriples_op.Close();
            otriples.Close();
            if (File.Exists(otriplets_op_filePath + "tmp")) File.Delete(otriplets_op_filePath + "tmp");
            File.Copy(otriples_filePath + "tmp", otriplets_op_filePath + "tmp");
            otriples_op = new PaCell(tp_otriple_seq, otriplets_op_filePath + "tmp", false);
            otriples = new PaCell(tp_otriple_seq, otriples_filePath + "tmp", false);
            //otriples_op.Clear();
            //otriples_op.Fill(new object[0]);
            //foreach (object v in otriples.Root.ElementValues()) otriples_op.Root.AppendElement(v);
            //otriples_op.Flush();

            //foreach (PaEntry entry in dtriples.Root.Elements())
            //{
            //    int s = (int)entry.Field(0).Get();
            //    int p = (int)entry.Field(1).Get();
            //    dtriples_sp.Root.AppendElement(new object[] { s, p, entry.offset });
            //}
            //       dataCell.Root.Scan((off, pobj) =>
            //{
            //    object[] tri = (object[])pobj;
            //    int s = (int)tri[0];
            //    int p = (int)tri[1];
            //    dtriples_sp.Root.AppendElement(new object[] { s, p, off });
            //    return true;
            //});
            //dtriples_sp.Flush();
        }



        private void Close()
        {
        // LiteralStore.Literals.dataCell.Close();
            dtriples_sp.Close();
            otriples.Close();
            otriples_op.Close();
            Scale.Cell.Close();
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




        public override IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            if (obj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<int>();
            object[] diapason = GetDiapasonFromHash(obj, 1);
            return diapason == null
                ? new int[0]
                : otriples_op.Root.ElementValues((long)diapason[0], (long)diapason[1])
                    .Cast<object[]>()
                    .SkipWhile(spo => pred != (int)spo[0])
                    .TakeWhile(spo => pred == (int)spo[0])
                    .Select(spo => (int)spo[1])
                    .ToArray();
        }

        #region GetSubjectByObjPred

        public IEnumerable<int> GetSubjectByObjPred4(int obj, int pred)
        {
            object[] diapason = GetDiapasonFromHash(obj, 1);
            if (diapason == null) return Enumerable.Empty<int>();
            return otriples_op.Root.Elements((long)diapason[0], (long)diapason[1])
                .Select(ent => (object[])ent.Get())
                //.Cast<object[]>()
                .Select(spo => (int)spo[0]);
        }

        public IEnumerable<int> GetSubjectByObjPred3(int obj, int pred)
        {
            if (otriples_op.IsEmpty) return Enumerable.Empty<int>();
            var itemEntriry = ewtHash.GetEntity(obj);
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[])itemEntriry.Field(2).Get();
            long num = (long)diapason[1];
            if (num < 500)
                return otriples_op.Root.Elements((long)diapason[0], num)
                    .Select(ent => (object[])ent.Get())
                    //.Cast<object[]>()
                    .Where(spo => pred == (int)spo[1])
                    .Select(spo => (int)spo[0]);
            return otriples_op.Root.BinarySearchAll((long)diapason[0], num,
                entry => ((int)entry.Field(1).Get()).CompareTo(pred))
                .Select(entry => (int)entry.Field(0).Get());
        }

        public IEnumerable<int> GetSubjectByObjPred2(int obj, int pred)
        {
            if (otriples_op.IsEmpty) return Enumerable.Empty<int>();
            var itemEntriry = ewt.EWTable.Root.BinarySearchFirst(ent =>
            {
                var rec = (object[])ent.Get();
                int ob = (int)rec[0];
                int cmp = ob.CompareTo(obj);
                return cmp;
            });
            if (itemEntriry.IsEmpty) return Enumerable.Empty<int>();
            var diapason = (object[])itemEntriry.Field(2).Get();

            return otriples_op.Root.Elements((long)diapason[0], (long)diapason[1])
                .Select(ent => (object[])ent.Get())
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
            return Enumerable.Select<PaEntry, int>(op_o_index.GetAll(ent =>
                {
                    int ob = (int)ent.Field(2).Get();
                    int cmp = ob.CompareTo(obj);
                    if (cmp != 0) return cmp;
                    int pr = (int)ent.Field(1).Get();
                    return pr.CompareTo(pred);
                }), en => (int)en.Field(0).Get());
        }

        #endregion

        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<int>();
            var key = new KeyValuePair<int, int>(subj, pred);
            object[] diapason = GetDiapasonFromHash(subj, 0);
            if (diapason == null) return Enumerable.Empty<int>();
            return otriples.Root.ElementValues((long)diapason[0], (long)diapason[1])
                .Cast<object[]>()
                .SkipWhile(entry => pred != (int)entry[0])
                .TakeWhile(entry => pred == (int)entry[0])
                .Select(entry => (int)entry[1])
                .ToArray();
        }

        private object[] GetDiapasonFromHash(int key, int direction)
        {
            if (key < 0 || key > ewt.EWTable.Root.Count()) return null;
            var itemEntry = ewt.EWTable.Root.Element(key); //ewtHash.GetEntity(key);
            if (itemEntry.IsEmpty) return null;
            return (object[])itemEntry.Field(1 + direction).Get();
        }
        [Obsolete]
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

        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<Literal>();


            object[] diapason = GetDiapasonFromHash(subj, 2);

            return diapason == null
                ? new Literal[0]
                : dtriples_sp.Root.ElementValues((long)diapason[0], (long)diapason[1])
                    .Cast<object[]>()
                    .SkipWhile(entry => pred != (int)entry[0])
                    .TakeWhile(entry => pred == (int)entry[0])
                    .Select(en =>(long)en[1])
                    .Select(offset=>LiteralStore.Read(offset, PredicatesCoding.LiteralVid[pred]))
                    .ToArray();
        }

        private static Literal ToLiteral(object[] uni)
        {
            switch ((int)uni[0])
            {
                case 1:
                    return new Literal(LiteralVidEnumeration.integer) { Value = Convert.ToDouble(uni[1]) };
                case 3:
                    return new Literal(LiteralVidEnumeration.date) { Value = (long)uni[1] };
                case 4:
                    return new Literal(LiteralVidEnumeration.boolean) { Value = (bool)uni[1] };
                case 5:
                    {
                        object[] txt = (object[])uni[1];
                        return new Literal(LiteralVidEnumeration.typedObject)
                        {
                            Value = new TypedObject() { Value = (string)txt[0], Type = (string)txt[1] }
                        };
                    }
                case 2:
                    object[] txt1 = (object[])uni[1];
                    return new Literal(LiteralVidEnumeration.text)
                    {
                        Value = new Text() { Value = (string)txt1[0], Lang = (string)txt1[1] }
                    };
                default:
                    return new Literal(LiteralVidEnumeration.nil);
            }
        }



        private static Literal PObjToLiteral(object pobj)
        {
            return ToLiteral((object[])pobj);
        }
        public override bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (subj == Int32.MinValue || obj == Int32.MinValue || pred == Int32.MinValue) return false;
            return CheckContains(subj, pred, obj);
        }

        private bool CheckContains(int subj, int pred, int obj)
        {
            if (!CheckInScale(subj, pred, obj)) return false;  
            int[] resSubj, resObj;   
           
            object[] subjDiapason = null;
            subjDiapason = GetDiapasonFromHash(subj, 0);
            if (subjDiapason == null) return false;
            long subjLength = (long) subjDiapason[1];
            object[] objDiapason = null;
            objDiapason = GetDiapasonFromHash(obj, 1);
            if (objDiapason == null) return false;
            long objLength = (long) objDiapason[1];

            if (subjLength < objLength)
            {
                resSubj = otriples.Root.ElementValues((long) subjDiapason[0], subjLength)
                    .Cast<object[]>()
                    .SkipWhile(entry => pred != (int) entry[0])
                    .TakeWhile(entry => pred == (int) entry[0])
                    .Select(entry => (int) entry[1])
                    .ToArray();
                return resSubj.Contains(obj);
            }
            else
            {
                resObj = otriples_op.Root.ElementValues((long) objDiapason[0], objLength)
                    .Cast<object[]>()
                    .SkipWhile(entry => pred != (int) entry[0])
                    .TakeWhile(entry => pred == (int) entry[0])
                    .Select(entry => (int) entry[1])
                    .ToArray();
                return resObj.Contains(subj);
            }
        }
       /* private bool CheckContains(int subj, int pred, int obj)
        {
            if (!CheckInScale(subj, pred, obj)) return false;

            //SubjPredObjInt key = new SubjPredObjInt() { subj = subj, pred = pred, obj = obj };
            //var entry = otriples.Root.BinarySearchFirst(ent => (new SubjPredObjInt(ent.Get())).CompareTo(key));
            int[] resSubj, resObj;
            var keySub = new KeyValuePair<int, int>(subj, pred);
            var keyObj = new KeyValuePair<int, int>(obj, pred);

            //если даже в оперативной памяти закешированы предикаты, но их больше, чем эта константа, то будет проверено количество предикатов в другом
            const int limit = 1000;


            // проверка кешей 
            var subjExists = spOCache.TryGetValue(keySub, out resSubj);
            var objExists = SpoCache.TryGetValue(keyObj, out resObj);
            long subjLength = 0;
            if (subjExists)
            {
                subjLength = resSubj.Length;
                // но если и обратыне тоже есть в кеше, то будет сравниваться их количество
                if (!objExists && subjLength < limit)
                    return resSubj.Contains(obj);
            }
            long objLength = 0;
            if (objExists)
            {
                objLength = resObj.Length;
                if (!subjExists && objLength < limit)
                    return resObj.Contains(subj);
            }
            if (subjExists && objExists)
                if (subjLength > objLength)
                    return resObj.Contains(subj);
                else return resSubj.Contains(obj);

            //теперь либо один из массивов есть в кеше и слишком большой, либо обоих нет.
            object[] subjDiapason = null;
            if (!subjExists)
            {
                subjDiapason = GetDiapasonFromHash(subj, 0);
                if (subjDiapason == null) return false;
                subjLength = (long)subjDiapason[1];
            }
            object[] objDiapason = null;
            if (!objExists)
            {
                objDiapason = GetDiapasonFromHash(obj, 1);
                if (objDiapason == null) return false;
                objLength = (long)objDiapason[1];
            }

            if (subjLength < objLength)
            {
                if (!subjExists)
                {
                    resSubj = otriples.Root.ElementValues((long)subjDiapason[0], subjLength)
                        .Cast<object[]>()
                        .SkipWhile(entry => pred != (int)entry[0])
                        .TakeWhile(entry => pred == (int)entry[0])
                        .Select(entry => (int)entry[1])
                        .ToArray();
                    spOCache.Add(keySub, resSubj);
                }
                return resSubj.Contains(obj);
            }
            else
            {
                if (!objExists)
                {
                    resObj = otriples_op.Root.ElementValues((long)objDiapason[0], objLength)
                        .Cast<object[]>()
                        .SkipWhile(entry => pred != (int)entry[0])
                        .TakeWhile(entry => pred == (int)entry[0])
                        .Select(entry => (int)entry[1])
                        .ToArray();
                    SpoCache.Add(keyObj, resObj);
                }
                return resObj.Contains(subj);
            }
        }
        */
        public override bool CheckInScale(int subj, int pred, int obj)
        {
            return Scale.ChkInScale(subj, pred, obj);
        }

        public bool ChkOSubjPredObj0(int subj, int pred, int obj)
        {
            // Шкалу добавлю позднее
            if (false && range > 0)
            {
                int code = Scale1.Code(range, subj, pred, obj);
                //int word = (int)oscale.Root.Element(Scale1.GetArrIndex(code)).Get();
                //int tb = Scale1.GetFromWord(word, code);
                int tb = Scale.Scale1[code];
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

            Diapason internal_diap = otriples_op.Root.BinarySearchDiapason(di.start, di.numb, ent => ((int)ent.Field(0).Get()).CompareTo(pred));
            return otriples_op.Root.Elements(internal_diap.start, internal_diap.numb)
                .Select(ent => (object[])ent.Get())
                .Select(p_obj => (int)((object[])p_obj)[1]);
        }
        public IEnumerable<Literal> GetDatInDiapason(Diapason di, int pred)
        {
            Diapason internal_diap = dtriples_sp.Root.BinarySearchDiapason(di.start, di.numb, ent =>
            {
                return ((int)ent.Field(1).Get()).CompareTo(pred);
            });
            
            return dtriples_sp.Root.ElementValues(internal_diap.start, internal_diap.numb)
                .Cast<object[]>()
                .Select(en => (long)en[1])
                    .Select(offset=>LiteralStore.Read(offset, PredicatesCoding.LiteralVid[pred]));
        }



        public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (obj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<int, int>>();

            object[] diapason = GetDiapasonFromHash(obj, 1);
            return diapason == null
                ? new KeyValuePair<int, int>[0]
                : otriples_op.Root.ElementValues((long)diapason[0], (long)diapason[1])
                    // Можно использовать ElementValues потому что результат не итерация, а массив
                    .Cast<object[]>()
                    //.Where(spo => pred == (int)spo[1]) // Если диапазон с учетом предиката, эта проверка не нужна, но операция очень дешевая, можно оставить
                    .Select(spo => new KeyValuePair<int, int>((int)spo[1], (int)spo[0])).ToArray();
        }

        public override IEnumerable<Int32> GetSubjectByDataPred(int p, Literal d)
        {
            return Enumerable.Empty<Int32>();
        }


        public override IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)
        {
            if (subj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<int, int>>();

            object[] diapason = GetDiapasonFromHash(subj, 0);

            return diapason == null
                ? new KeyValuePair<Int32, Int32>[0]
                : otriples.Root.ElementValues((long)diapason[0], (long)diapason[1])
                    .Cast<object[]>()
                    .Select(spo => new KeyValuePair<int, int>((int)spo[1], (int)spo[0])).ToArray();
        }

        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (subj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<Literal, int>>();


            object[] diapason = GetDiapasonFromHash(subj, 2);

            return diapason == null
                ? new KeyValuePair<Literal, int>[0]
                : dtriples_sp.Root.ElementValues((long)diapason[0], (long)diapason[1])
                    .Cast<object[]>()
                    .Select(row => new KeyValuePair<Literal, int>(LiteralStore.Read((long)row[1], PredicatesCoding.LiteralVid[(int)row[0]]), (int)row[0])).ToArray();
        }


/*
        public IEnumerable<TripleInt> GetAllTriplets()
        {
            int subjectsCount = Convert.ToInt32(ewt.EWTable.Root.Count());
            int bufferSubjectssMax = Math.Min(subjectsCount, 1000 * 1000);
            int[] objDiapasons = new int[bufferSubjectssMax];
            int[] literalDiapasons = new int[bufferSubjectssMax];
            long objTrippletsBufferCount = 0, objTrippletsBufferStart = 0;
            long literalsTrippletsBufferCount = 0, literalsTrippletsBufferStart = 0;
            for (int kod = 0, index_in_buffer = 0; ; kod++, index_in_buffer++)
            {
                if (index_in_buffer == bufferSubjectssMax)
                {
                    int s = kod - index_in_buffer;
                    int s_local_index = 0;
                    int s_count = objDiapasons[0];


                    //Если триплеты нужно возращать в порядке возрастания субъекта  (и предиката), то нужно заполнить массив и затем возразщать пары списков объектных и литералов (соединить и отсортировать по предикату)
                    //List<KeyValuePair<int, int>>[] opLists = new List<KeyValuePair<int, int>>[bufferTripletsMax];
                    if (!otriples.IsEmpty && otriples.Root.Count() != 0)
                        foreach (object[] pred_obj in otriples.Root.ElementValues(objTrippletsBufferStart, objTrippletsBufferCount)
                            //что бы чтение не проводилось паралелльно с декодированием
                            .ToArray())
                        {
                            if (s_count-- == 0)
                            {
                                s_local_index++;
                                s++;
                                s_count = objDiapasons[s_local_index];
                            }
                            yield return new OTripleInt { subject = s, predicate = (int)pred_obj[0], obj = (int)pred_obj[1] };
                        }
                    if (!dataCell.IsEmpty && dataCell.Root.Count() > 0)
                    {
                        //буфер dtriples_sp записывается в массив, что бы не прыгать по офсетам к литералам
                        var predicateLiteralsOfsset =
                            dtriples_sp.Root.ElementValues(literalsTrippletsBufferStart, literalsTrippletsBufferCount)
                                .Cast<object[]>()
                                .ToArray();
                        var dataEntry = dataCell.Root.Element(0);
                        s = kod - index_in_buffer;
                        s_local_index = 0;
                        s_count = objDiapasons[0];
                        //когда буыффер dtriples_sp считан, читаются литералы. тут придётся попрыгать.
                        for (int j = 0; j < literalsTrippletsBufferCount; j++)
                        {
                            if (s_count-- == 0)
                            {
                                s_local_index++;
                                s++;
                                s_count = objDiapasons[s_local_index];
                            }
                            dataEntry.offset = (long)predicateLiteralsOfsset[j][1];
                            yield return
                                new DTripleInt
                                {
                                    subject = s,
                                    predicate = (int)predicateLiteralsOfsset[j][0],
                                    data = ToLiteral((object[])dataEntry.Get())
                                };
                        }
                    }
                    if (kod == subjectsCount) yield break;

                    index_in_buffer = 0;
                    objTrippletsBufferStart += objTrippletsBufferCount;
                    objTrippletsBufferCount = 0;
                    literalsTrippletsBufferStart += literalsTrippletsBufferCount;
                    literalsTrippletsBufferCount = 0;
                }
                var wideRow = (object[])ewt.EWTable.Root.Element(kod).Get();
                objTrippletsBufferCount += objDiapasons[index_in_buffer] = Convert.ToInt32((long)((object[])wideRow[1])[1]);
                literalsTrippletsBufferCount += literalDiapasons[index_in_buffer] = Convert.ToInt32((long)((object[])wideRow[3])[1]);
            }
        }
*/

        public IEnumerable<Tuple<string, string, string>> DecodeTriplets(IEnumerable<TripleInt> triplets)
        {
            var enumerator = triplets.GetEnumerator();
            //TODO переделать
            Dictionary<int, string> predicatesCodes = new Dictionary<int, string>();
            for (int i = 0; i < predicatesCodes.Count; i++)
                predicatesCodes.Add(i, DecodePredicateFullName(i));
            int subjectCodesBuffer = 100 * 1000 * 1000;

            var entitiesCodes = new Dictionary<int, string>(subjectCodesBuffer);
            for (int kod = 0, bufferIndex = 0; kod < entitiesCodes.Count; kod++, bufferIndex++)
            {
                if (bufferIndex == subjectCodesBuffer)
                {
                    while (enumerator.MoveNext() && enumerator.Current.subject < kod)
                    {
                        var dTripleInt = enumerator.Current as DTripleInt;
                        if (dTripleInt != null)
                            yield return
                                Tuple.Create(entitiesCodes[dTripleInt.subject],
                                    predicatesCodes[dTripleInt.predicate], dTripleInt.data.ToString());
                        else
                        {
                            var oTripleInt = enumerator.Current as OTripleInt;
                            string obj;
                            Tuple.Create(entitiesCodes[oTripleInt.subject], predicatesCodes[oTripleInt.predicate],
                                entitiesCodes.TryGetValue(oTripleInt.obj, out obj)
                                    ? obj
                                    : DecodeEntityFullName(oTripleInt.obj));
                        }
                    }

                    bufferIndex = 0;
                    entitiesCodes.Clear();
                }
                entitiesCodes.Add(kod, DecodeEntityFullName(kod));
            }
        }
        private void TestEWT()
        {
            DateTime tt0 = DateTime.Now;
            EntitiesMemoryHashTable hashTable = new EntitiesMemoryHashTable(ewt);
            hashTable.Load();
            // Проверка построенной ewt
            Console.WriteLine("n_entities={0}", this.ewt.EWTable.Root.Count());
            bool notfirst = false;
            int code = Int32.MinValue;
            long cnt_otriples = 0;
            foreach (object[] row in ewt.EWTable.Root.ElementValues())
            {
                int cd = (int)row[0];
                // Проверка на возрастание значений кода
                if (notfirst && cd <= code) { Console.WriteLine("ERROR!"); }
                code = cd;
                notfirst = true;
                // Проверка на то, что коды в диапазонах индексов совпадают с cd. Подсчитывается количество
                object[] odia = (object[])row[1];
                long start = (long)odia[0];
                long number = (long)odia[1];
                foreach (object[] tri in otriples.Root.ElementValues(start, number))
                {
                    int c = (int)tri[0];
                    if (c != cd) Console.WriteLine("ERROR2!");
                }
                cnt_otriples += number;
            }
            if (cnt_otriples != otriples.Root.Count()) Console.WriteLine("ERROR3! cnt_triples={0} otriples.Root.Count()={1}", cnt_otriples, otriples.Root.Count());
            Console.WriteLine("Проверка ewt OK. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;
        }

        public DateTime TestsOfMethods(string[] ids, TripleStoreInt ts)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            DateTime tt0 = DateTime.Now;
            // ======================= Сравнение бинарного поиска с вычислением диапазона =============
            int pf19 = ids[5].GetHashCode();
            List<long> trace = new List<long>();
            int counter=0;
            Func<PaEntry, int> fdepth = ent => { counter++; trace.Add(ent.offset); return ((int)ent.Field(2).Get()).CompareTo(pf19); };

            sw.Restart();
            counter = 0; trace.Clear();
            var query = ts.otriples_op.Root.BinarySearchAll(fdepth);
            tt0 = DateTime.Now;
            int cc = query.Count();
            sw.Stop();
            Console.Write("Test BinarySearchAll: {0} ", cc);
            Console.WriteLine("Test swduration={0} duration={2} counter={1}", sw.Elapsed.Ticks, counter, (DateTime.Now - tt0).Ticks); tt0 = DateTime.Now;
            //foreach (int point in trace) Console.Write("{0} ", point); Console.WriteLine();

            sw.Restart();
            counter = 0; trace.Clear();
            ts.otriples_op.Root.BinarySearchScan(0, ts.otriples_op.Root.Count(), fdepth);
            sw.Stop();
            Console.Write("Test of BinaryScan: ");
            Console.WriteLine("swduration={0} counter={1}", sw.ElapsedTicks, trace.Count()); tt0 = DateTime.Now;
            //foreach (int point in trace) Console.Write("{0} ", point); Console.WriteLine();

            sw.Restart();
            counter = 0; trace.Clear();
            ts.otriples_op.Root.BinarySearchScan(0, ts.otriples_op.Root.Count(), fdepth);
            sw.Stop();
            Console.Write("Test of BinaryScan: ");
            Console.WriteLine("swduration={0} counter={1}", sw.ElapsedTicks, trace.Count()); tt0 = DateTime.Now;
            //foreach (int point in trace) Console.Write("{0} ", point); Console.WriteLine();

            sw.Restart();
            counter = 0; trace.Clear();
            ts.otriples_op.Root.BinarySearchFirst(fdepth);
            sw.Stop();
            Console.Write("Test of BinarySearchFirst: ");
            Console.WriteLine("swduration={0} counter={1}", sw.ElapsedTicks, trace.Count()); tt0 = DateTime.Now;
            //foreach (int point in trace) Console.Write("{0} ", point); Console.WriteLine();

            sw.Restart();
            counter = 0; trace.Clear();
            Diapason diap = ts.otriples_op.Root.BinarySearchDiapason(0, ts.otriples_op.Root.Count(), fdepth);
            sw.Stop();
            Console.Write("Test of Diapason: {0} {1} ", diap.start, diap.numb);
            Console.WriteLine(" swduration={0} counter={1}", sw.ElapsedTicks, counter); tt0 = DateTime.Now;
            //foreach (int point in trace) Console.Write("{0} ", point); Console.WriteLine();

            sw.Restart();
            counter = 0; trace.Clear();
            ts.otriples_op.Root.BinarySearchFirst(fdepth);
            sw.Stop();
            Console.Write("Test of BinarySearchFirst: ");
            Console.WriteLine("swduration={0} counter={1}", sw.ElapsedTicks, trace.Count()); tt0 = DateTime.Now;
            //foreach (int point in trace) Console.Write("{0} ", point); Console.WriteLine();

            sw.Restart();
            PaEntry test_ent = ts.otriples_op.Root.Element(0).Field(2);
            int val = -1;
            foreach (var point in trace)
            {
                test_ent.offset = point;
                val = (int)test_ent.Get();
            }
            sw.Stop();
            Console.Write("Test of series: ");
            Console.WriteLine("swduration={0}", sw.ElapsedTicks); tt0 = DateTime.Now;

            // ============ Конец сравнения ================
            return tt0;
        }
    }
}