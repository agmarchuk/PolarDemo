using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NameTable;
using PolarDB;
using ScaleBit4Check;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class ColumnsStoreAbstract : RDFIntStoreAbstract
    {
        private PType tp_otriple_seq;

        private Dictionary<int, Dictionary<int,object>[]> positionBy_bjectPredicateCashe=new Dictionary<int, Dictionary<int, object>[]>();

        private PType tp_entity;
        private PType tp_dtriple_spf;

        internal EntitiesWideTable ewt;
        internal EntitiesMemoryHashTable ewtHash;
        public ScaleCell scale = null;
        private PType tp_entities_column;
        private PTypeSequence tp_Data_column;

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
            tp_entities_column = new PTypeSequence(tp_entity);
            tp_Data_column = new PTypeSequence(new PType(PTypeEnumeration.longinteger));

        }
        private string path;
        private PaCell objPredicates;
        private PaCell objects; // объектные триплеты, упорядоченные по o-p
        private PaCell inversePredicates;
        private PaCell inverses; // объектные триплеты, упорядоченные по o-p
        private PaCell dataPredicates;
        private PaCell data;
        private string dataPredicatesColumn_filePath;
        private string dataColumn_filePath;
        private string objPredicatesColumn_filePath;
        private string invPredicatesColumn_filePath;
        private string invSubjectsColumn_filePath;
        private string objectsColumn_filePath;

        //// Идея хорошая, но надо менять схему реализации
        //private GroupedEntities getable;
        //private Dictionary<int, object[]> geHash;

        public ColumnsStoreAbstract(string path, IStringIntCoding entityCoding, PredicatesCoding predicatesCoding, NameSpaceStore nameSpaceStore, LiteralStoreAbstract literalStore)
            : base(entityCoding, predicatesCoding, nameSpaceStore, literalStore)

        {
            this.path = path;

            InitTypes();
            otriplets_op_filePath = path + "otriples_op.pac";
            otriples_filePath = path + "otriples.pac";
            dtriples_filePath = path + "dtriples.pac";
            dataPredicatesColumn_filePath = path + "dataPredicatesColumn.pac";
            objPredicatesColumn_filePath = path + "objPredicatesColumn.pac";
            invPredicatesColumn_filePath = path + "invPredicatesColumn.pac";
            invSubjectsColumn_filePath = path + "invSubjectsColumn.pac";
            objectsColumn_filePath = path + "objectsColumn.pac";
            dataColumn_filePath = path + "dataColumn.pac";
           
            
                Open(File.Exists(otriples_filePath));
            

            if (!scale.Cell.IsEmpty)
               scale.CalculateRange();


            ewt = new EntitiesWideTable(path, 3);
            //ewtHash = new EntitiesMemoryHashTable(ewt);
            // ewtHash.Load();



            //getable = new GroupedEntities(path); // Это хорошая идея, но нужно менять схему реализации
            //getable.CheckGroupedEntities();
            //geHash = getable.GroupedEntitiesHash();

        }

        private void Open(bool readOnlyMode)
        {
            objPredicates = new PaCell(tp_entities_column, objPredicatesColumn_filePath, readOnlyMode);
            objects = new PaCell(tp_entities_column, objectsColumn_filePath, readOnlyMode);
            inversePredicates = new PaCell(tp_entities_column, invPredicatesColumn_filePath, readOnlyMode);
            inverses = new PaCell(tp_entities_column, invSubjectsColumn_filePath, readOnlyMode);
            dataPredicates = new PaCell(tp_entities_column, dataPredicatesColumn_filePath, readOnlyMode);
            data = new PaCell(tp_Data_column, dataColumn_filePath, readOnlyMode);
            // LiteralStore.Literals.Open(readOnlyMode);
            scale = new ScaleCell(path);

        }


        private void RemoveColumns(PaCell otriples, PaCell otriples_op, PaCell dtriples_sp)
        {
            objPredicates.Clear();
            objects.Clear();
            inversePredicates.Clear();
            inverses.Clear();
            dataPredicates.Clear();
            data.Clear();


            objPredicates.Fill(new object[0]);
            objects.Fill(new object[0]);
            inversePredicates.Fill(new object[0]);
            inverses.Fill(new object[0]);
            dataPredicates.Fill(new object[0]);
            data.Fill(new object[0]);



            foreach (object[] elementValue in otriples_op.Root.ElementValues())
            {
                inversePredicates.Root.AppendElement(elementValue[1]);
                inverses.Root.AppendElement(elementValue[0]);
            }
            foreach (object[] elementValue in otriples.Root.ElementValues())
            {
                objPredicates.Root.AppendElement(elementValue[1]);
                objects.Root.AppendElement(elementValue[2]);
            }
            foreach (object[] elementValue in dtriples_sp.Root.ElementValues())
            {
                dataPredicates.Root.AppendElement(elementValue[1]);
                data.Root.AppendElement(elementValue[2]);
            }
            objPredicates.Flush();
            objects.Flush();
            inversePredicates.Flush();
            inverses.Flush();
            dataPredicates.Flush();
            data.Flush();

            otriples.Close();
            otriples_op.Close();
            dtriples_sp.Close();
            File.Delete(otriplets_op_filePath + "tmp");
            File.Delete(otriples_filePath + "tmp");
            File.Delete(dtriples_filePath + "tmp");         
 
        }

        public override void WarmUp()
        {
            if (ewt.EWTable.IsEmpty) return;
            foreach (var v in objPredicates.Root.ElementValues()) ;
            foreach (var v in objects.Root.ElementValues()) ;
           LiteralStore.WarmUp();
            foreach (var v in dataPredicates.Root.ElementValues()) ;
            foreach (var v in data.Root.ElementValues()) ;
            foreach (var v in inversePredicates.Root.ElementValues()) ;
            foreach (var v in inverses.Root.ElementValues()) ;


            if (scale.Filescale)
                foreach (var v in scale.Cell.Root.ElementValues()) ;
            foreach (var v in ewt.EWTable.Root.ElementValues()) ; // этая ячейка "подогревается" при начале программы
       WarmUp();
            
        }
        



        public override void LoadTurtle(string filepath)
        {
            DateTime tt0 = DateTime.Now;

            Close();

            Open(false);

            PaCell otriples=new PaCell(tp_otriple_seq, otriples_filePath, false);
            PaCell dtriples_sp=new PaCell(tp_dtriple_spf, dtriples_filePath,false);
            
            TurtleInt.LoadTriplets(filepath, otriples, dtriples_sp, this);

            Console.WriteLine("Load ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

            if (File.Exists(otriplets_op_filePath)) File.Delete(otriplets_op_filePath);
            otriples.Close();
            File.Copy(otriples_filePath, otriplets_op_filePath);
            PaCell otriples_op = new PaCell(tp_otriple_seq, otriplets_op_filePath, false);
            otriples = new PaCell(tp_otriple_seq, otriples_filePath, false);
            Console.WriteLine("copy objects ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;

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

          scale.WriteScale(otriples);
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

          
            RemoveColumns(otriples, otriples_op, dtriples_sp);
            Console.WriteLine("RemoveColumns() ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;
          
        }



        private void Close()
        {
            // LiteralStore.Literals.dataCell.Close();

            objPredicates.Close();
            objects.Close();
            inversePredicates.Close();
            inverses.Close();
            dataPredicates.Close();
            data.Close();
            scale.Cell.Close();
        }




        public override IEnumerable<int> GetSubjectByObjPred(int obj, int pred)
        {
            if (obj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<int>();
            return TakeValuesByPrevaricate<int>(obj, pred, 1, inversePredicates, inverses);
        }

        private IEnumerable<T> TakeValuesByPrevaricate<T>(int _bject, int pred, int direction, PaCell predicateColumn, PaCell valueColumn, object[] diapason=null)
        {                     
            Dictionary<int, object>[] subDiapasonsAllDirections;
            if (!positionBy_bjectPredicateCashe.TryGetValue(_bject, out subDiapasonsAllDirections))
                positionBy_bjectPredicateCashe.Add(_bject, subDiapasonsAllDirections=Enumerable.Range(0,3).Select(i=>new Dictionary<int, object>()).ToArray());
            Dictionary<int, object> subDiapasons = subDiapasonsAllDirections[direction];
            object subDiapason;
            if (!subDiapasons.TryGetValue(pred, out subDiapason))
            {
                if (subDiapasons.ContainsKey(-1)) subDiapason = new Diapason();    
                else
                {
                    diapason = diapason ?? GetDiapasonFromHash(_bject, direction);
                    if (diapason == null) subDiapason = new Diapason();
                    else
                    {
                        Diapason readed = new Diapason();
                        var last = subDiapasons.LastOrDefault();
                        if (!last.Equals(default(KeyValuePair<int, object>)))
                            if (last.Value is Diapason)
                            {
                                var lastSubDiapason = ((Diapason)last.Value);
                                readed.start = lastSubDiapason.start + lastSubDiapason.numb;
                            }
                            else readed.start = (long)last.Value + 1;
                        else readed.start = (long) diapason[0];

                        long all = (long) diapason[1] + (long) diapason[0] - readed.start;
                        long countReaded = 0;
                        int lastPredicate=-1;
                        foreach (int predicateReaded in predicateColumn.Root.ElementValues(readed.start, all)
                            .Cast<int>()
                            .TakeWhile(predicate => predicate <= pred)
                            .Select(p =>
                            {
                                countReaded++;
                                return p;
                            }))
                        {
                            if (lastPredicate==predicateReaded) {readed.numb++; continue;}
                            if(lastPredicate>-1)    
                            subDiapasons.Add(lastPredicate, readed.numb == 1 ? (object) readed.start : readed);
                            readed = new Diapason() {start = readed.start + readed.numb, numb = 1};
                            lastPredicate = predicateReaded;
                        }
                        if (lastPredicate > -1)
                            subDiapasons.Add(lastPredicate, readed.numb == 1 ? (object)readed.start : readed);

                        if (countReaded == all)
                            subDiapasons.Add(-1, null);
                        if (!subDiapasons.TryGetValue(pred, out subDiapason))
                            subDiapason = new Diapason();
                    }
                }
            }
            return (subDiapason is long
                ? valueColumn.Root.ElementValues((long) subDiapason, 1)
                : valueColumn.Root.ElementValues(((Diapason) subDiapason).start, ((Diapason) subDiapason).numb))
                    .Cast<T>()
                    .ToArray();
        }


        public override IEnumerable<int> GetObjBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<int>();
            return TakeValuesByPrevaricate<int>(subj, pred, 0, objPredicates, objects);
        }

        private object[] GetDiapasonFromHash(int key, int direction)
        {
            if (key < 0 || key > ewt.EWTable.Root.Count()) return null;
            var itemEntry = ewt.EWTable.Root.Element(key); //ewtHash.GetEntity(key);
            if (itemEntry.IsEmpty) return null;
            return (object[])itemEntry.Field(1 + direction).Get();
        }
      

        public override IEnumerable<Literal> GetDataBySubjPred(int subj, int pred)
        {
            if (subj == Int32.MinValue || pred == Int32.MinValue) return Enumerable.Empty<Literal>();
            var offsets = TakeValuesByPrevaricate<long>(subj, pred, 2, dataPredicates, data);
            
                return offsets
                    .Select(offset=>LiteralStore.Read(offset, PredicatesCoding.LiteralVid[pred]))
                    .ToArray();
        }    
   
        public override bool ChkOSubjPredObj(int subj, int pred, int obj)
        {
            if (subj == Int32.MinValue || obj == Int32.MinValue || pred == Int32.MinValue) return false;
            return CheckContains(subj, pred, obj);
        }

        private bool CheckContains(int subj, int pred, int obj)
        {
            if (!CheckInScale(subj, pred, obj)) return false;  
            IEnumerable<int> resSubj;
            IEnumerable<int> resObj;

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
                resSubj = TakeValuesByPrevaricate<int>(subj, pred, 0, objPredicates, objects, subjDiapason);
                return resSubj.Contains(obj);
            }
            else
            {
                resObj = TakeValuesByPrevaricate<int>(obj,pred, 1, inversePredicates, inverses,objDiapason);
                return resObj.Contains(subj);
            }
        }
     
        public override bool CheckInScale(int subj, int pred, int obj)
        {
            return scale.ChkInScale(subj, pred, obj);
        }

     
       public override IEnumerable<KeyValuePair<int, int>> GetSubjectByObj(int obj)
        {
            if (obj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<int, int>>();

            object[] diapason = GetDiapasonFromHash(obj, 1);
           if (diapason == null) return new KeyValuePair<int, int>[0];

           var values = inverses.Root.ElementValues((long) diapason[0], (long) diapason[1])
               .Cast<int>()
               .ToArray();
           return inversePredicates.Root.ElementValues((long) diapason[0], (long) diapason[1])
               .Cast<int>()
               .Select((pred, i) => new KeyValuePair<int, int>(values[i], pred)).ToArray();       
        }

        public override IEnumerable<Int32> GetSubjectByDataPred(int p, Literal d)
        {
            return Enumerable.Empty<Int32>();
        }


        public override IEnumerable<KeyValuePair<Int32, Int32>> GetObjBySubj(int subj)
        {
            if (subj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<int, int>>();

            object[] diapason = GetDiapasonFromHash(subj, 0);

            var values = objects.Root.ElementValues((long)diapason[0], (long)diapason[1])
               .Cast<int>()
               .ToArray();
            return objPredicates.Root.ElementValues((long)diapason[0], (long)diapason[1])
                .Cast<int>()
                .Select((pred, i) => new KeyValuePair<int, int>(values[i], pred))
                .ToArray(); 
        }

        public override IEnumerable<KeyValuePair<Literal, int>> GetDataBySubj(int subj)
        {
            if (subj == Int32.MinValue) return Enumerable.Empty<KeyValuePair<Literal, int>>();


            object[] diapason = GetDiapasonFromHash(subj, 2);
                                              
            var values = data.Root.ElementValues((long)diapason[0], (long)diapason[1])
               .Cast<long>() 
               .ToArray();
            return dataPredicates.Root.ElementValues((long)diapason[0], (long)diapason[1])
                .Cast<int>()
                .Select((pred, i) => new KeyValuePair<long, int>(values[i], pred))
                .ToArray()
                .Select(pair => new KeyValuePair<Literal, int>(LiteralStore.Read(pair.Key, PredicatesCoding.LiteralVid[pair.Value]), pair.Value) ); 
        }  
    }
}