using System;
using System.Collections.Generic;
using System.Linq;
using NameTable;
using PolarDB;
using ScaleBit4Check;

namespace TripleStoreForDNR
{
    public class SGraph : IGraph
    {
        private static PType tp_entity = new PType(PTypeEnumeration.integer);

        private static PType tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("object", tp_entity)));
        private static PType tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("offset", new PType(PTypeEnumeration.longinteger))));

        private static readonly PType tp_rliteral = new PTypeUnion(
 new NamedType("void", new PType(PTypeEnumeration.none)),
 new NamedType("integer", new PType(PTypeEnumeration.integer)),
 new NamedType("float", new PType(PTypeEnumeration.real)),
 new NamedType("double", new PType(PTypeEnumeration.real)),
 new NamedType("bool", new PType(PTypeEnumeration.boolean)),
 new NamedType("date", new PType(PTypeEnumeration.longinteger)),
 new NamedType("dateTime", new PType(PTypeEnumeration.longinteger)),
 new NamedType("string", new PType(PTypeEnumeration.sstring)),
 new NamedType("langString", new PTypeRecord(
    new NamedType("s", new PType(PTypeEnumeration.sstring)),
    new NamedType("l", new PType(PTypeEnumeration.sstring)))),
 new NamedType("typedObject", new PTypeRecord(
    new NamedType("s", new PType(PTypeEnumeration.sstring)),
    new NamedType("t", new PType(PTypeEnumeration.sstring)))));
        private static readonly PType tp_data_seq = new PTypeSequence(tp_rliteral);
        private static PType tp_entitiesTree = new PTypeSequence(new PTypeRecord(
                     new NamedType("id", new PType(PTypeEnumeration.integer)),
                     new NamedType("fields", new PTypeSequence(new PTypeRecord(
                         new NamedType("prop", new PType(PTypeEnumeration.integer)),
                         new NamedType("off", new PType(PTypeEnumeration.longinteger))))),
                     new NamedType("direct", new PTypeSequence(new PTypeRecord(
                         new NamedType("prop", new PType(PTypeEnumeration.integer)),
                         new NamedType("ref", new PType(PTypeEnumeration.integer))))),
                     new NamedType("inverse", new PTypeSequence(new PTypeRecord(
                         new NamedType("prop", new PType(PTypeEnumeration.integer)),
                         new NamedType("sources", new PTypeSequence(new PType(PTypeEnumeration.integer))))))));
        private PaCell otriples;
        private PaCell dtriples;
        private PaCell dataCell;
        private PxCell entitiesTree;
        internal NamespaceMapCoding namespaceMaper;
     
        private string path;
        private ScaleCell scale;
        public SGraph(string path, Uri uri)
        {
            this.path = path;
            Uri = uri;
            otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            dtriples = new PaCell(tp_dtriple_spf, path + "dtriples.pac", false); // Временно выведена в переменные класса, открывается при инициализации    
            dataCell = new PaCell(tp_data_seq, path + "data.pac", false);
            entitiesTree = new PxCell(tp_entitiesTree, path + "entitiesTree.pxc", false);

            namespaceMaper = new NamespaceMapCoding(new StringIntMD5RAMUnsafe(path));
            scale=new ScaleCell(path);
           
        }

        public bool IsEmpty
        {
            get { throw new NotImplementedException(); }
        }

        public Uri Uri { get; private set; }

        public INamespaceMapper NamespaceMap
        {
            get { return namespaceMaper; }
        }

        public IUriNode CreateUriNode(string uriOrQname)
        {
            SUriNode sUriNode = new SUriNode(namespaceMaper.coding.InsertOne(GetEntityString(uriOrQname)), this);

        
            return sUriNode;
        }
        public string GetEntityString( string line)
        {
            if (line[0]=='<')
                return line.Substring(1, line.Length - 2);

            string subject = null;
            int colon = line.IndexOf(':');
            if (colon == -1) { Console.WriteLine("Err in line: " + line); goto End; }
            string prefix = line.Substring(0, colon + 1);
            if (!NamespaceMap.HasNamespace(prefix)) { Console.WriteLine("Err in line: " + line); goto End; }
            subject = NamespaceMap.GetNamespaceUri(prefix).OriginalString + line.Substring(colon + 1);
        End:
            return subject;
        }
      
        public ILiteralNode CreateLiteralNode(string value)
        {
            return new SLiteralNode(value, this);
        }

        public ILiteralNode CreateLiteralNode(string value, Uri datatype)
        {
            throw new NotImplementedException();
        }

        public ILiteralNode CreateLiteralNode(string value, string lang)
        {
            throw new Exception();
        }


      public  IUriNode GetUriNode(Uri uri)
        {
          var code = namespaceMaper.coding.GetCode(uri.ToString());
          if (code == int.MinValue) return null;
          SUriNode sUriNode = new SUriNode(code, this);
            return sUriNode;
        }

        public ILiteralNode GetLiteralNode(dynamic value, string lang, Uri type)
        {
           return new SLiteralNode(value, lang, type, this);
        }

        public IUriNode CreateUriNode(Uri uri)
        {

            SUriNode sUriNode = new SUriNode(namespaceMaper.coding.InsertOne(uri.ToString()), this);
            return sUriNode;
        }

        public void Clear()
        {
            dtriples.Clear();
            otriples.Clear();
            dtriples.Fill(new object[0]);
            otriples.Fill(new object[0]);
            dataCell.Clear();
            dataCell.Fill(new object[0]);
             namespaceMaper.Clear();

        }

        public bool Assert(Triple t)
        {
            if (t.Object.NodeType == NodeType.Uri)
            {
                otriples.Root.AppendElement(new object[]{
                   ((SUriNode) t.Subject).Code,
                   ((SUriNode) t.Predicate).Code,
                   ((SUriNode) t.Object).Code
                });
                return true;
            }

            if (t.Object.NodeType == NodeType.Literal)
            {
                dtriples.Root.AppendElement(new object[]{
                   ((SUriNode) t.Subject).Code,
                   ((SUriNode) t.Predicate).Code,
                   ((SLiteralNode) t.Object).Code
                });
                return true;
            }
            return false;
        }

        public void Build()
        {
            DateTime tt0 = DateTime.Now;
            otriples.Flush();
            dtriples.Flush();    
            otriples.Close(); 
            
            // Копирование файла
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
            scale.WriteScale(otriples);
            Console.WriteLine("CreateScale ok. Duration={0} sec.", (DateTime.Now - tt0).Ticks / 10000000L); tt0 = DateTime.Now;
            //int cnt_e = MakeTreeFree(otriples, otriples_op, dtriples_sp);
            //Console.WriteLine("Scan3 ok. Duration={0} msec. cnt_e={1} ", (DateTime.Now - tt0).Ticks / 10000L, cnt_e); tt0 = DateTime.Now;
            //Console.WriteLine("otriples={0} otriples_op={1} dtriples_sp={2}", otriples.Root.Count(), otriples_op.Root.Count(), dtriples_sp.Root.Count());

            otriples.Close();
            otriples_op.Close();
            dtriples.Close();
            // Создает ячейку фиксированного формата tree_fix.pxc
            MakeTreeFix();
            dataCell.Flush();
            namespaceMaper.coding.MakeIndexed();

        }

        public IEnumerable<Triple> GetTriplesWithObject(Uri u)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithObject(INode obj)
        {
            if (obj is SUriNode)
            {
                var objUriCode = (obj as SUriNode).Code;
                if (objUriCode == Int32.MinValue) return new Triple[0];
                var rec_ent = this.entitiesTree.Root.Element(objUriCode);
                // //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
                if (rec_ent.IsEmpty) return new Triple[0];     
                return ((object[])rec_ent.Field(3).Get())
                    .Cast<object[]>()
                    .SelectMany(pair =>((object[])pair[1]).Cast<int>().Select(i =>  new Triple(new SUriNode(i, this), new SUriNode((int)pair[0], this), obj)))
                    .ToArray();
            }
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(INode n)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(Uri u)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubject(INode subj)
        {
            if (subj is SUriNode)
            {
                var subjUriCode = (subj as SUriNode).Code;
                if (subjUriCode == Int32.MinValue) return new Triple[0];
                var rec_ent = this.entitiesTree.Root.Element(subjUriCode);
                    // //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
                if (rec_ent.IsEmpty) return new Triple[0];
                var literals = dataCell.Root.Element(0);
                return ((object[]) rec_ent.Field(2).Get())
                    .Cast<object[]>()
                    .Select(
                        pair => new Triple(subj, new SUriNode((int) pair[0], this), new SUriNode((int) pair[1], this)))
                    .Concat(((object[]) rec_ent.Field(1).Get())
                        .Cast<object[]>()
                        .Select(pair => 
                        {
                            literals.offset = (long) pair[1];
                            return new Triple(subj, new SUriNode((int)pair[0],this), new SLiteralNode(literals.Get(), this));
                        }))
                    .ToArray();
            }
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubject(Uri u)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred)
        {
            if (!(pred is SUriNode)) throw new Exception();
            var predicateUriCode = (pred as SUriNode).Code;
            if (subj is SUriNode)
            {
                var subjUriCode = (subj as SUriNode).Code;
                if (subjUriCode == Int32.MinValue || predicateUriCode == Int32.MinValue) return new Triple[0];
                var key = new KeyValuePair<int, int>(subjUriCode, predicateUriCode);
                var rec_ent = this.entitiesTree.Root.Element(subjUriCode);// //.BinarySearchFirst(ent => ((int) ent.Field(0).Get()).CompareTo(subj));
                if (rec_ent.IsEmpty) return new Triple[0];
                var literals = dataCell.Root.Element(0);
                return ((object[])rec_ent.Field(2).Get())
                    .Cast<object[]>()
                    .Where(pair => (int)pair[0] == predicateUriCode)
                    .Select(pair => (int)pair[1])
                    .Select(i => new Triple(subj, pred, new SUriNode(i, this)))
                    .Concat(((object[])rec_ent.Field(1).Get())
                            .Cast<object[]>()
                            .Where(pair => (int)pair[0] == predicateUriCode)
                            .Select(pair => (long)pair[1])
                            .Select(i =>
                            {
                                literals.offset = i;
                                return new Triple(subj, pred, new SLiteralNode(literals.Get(), this));
                            }))        
                            .ToArray();
            }
            else if (subj is SLiteralNode)
            {
                throw new NotImplementedException();
            }
            else throw new Exception();
        }

        public IEnumerable<Triple> GetTriplesWithSubjectObject(INode subj, INode obj)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj)
        {
            if(!(pred is SUriNode)) throw new Exception();
            var predicateUriCode = (pred as SUriNode).Code;
            if (obj is SUriNode)
            {
                var objectUriCode = (obj as SUriNode).Code;
                if (objectUriCode == Int32.MinValue || predicateUriCode == Int32.MinValue) return new Triple[0];
                 var rec_ent = this.entitiesTree.Root.Element(objectUriCode);
                if (rec_ent.IsEmpty) return new Triple[0];
                var pred_subj = rec_ent.Field(3).Elements()
                    .FirstOrDefault(pred_subjseq => (int)pred_subjseq.Field(0).Get() == predicateUriCode);

                return (pred_subj.offset == 0 ? new int[0] : ((object[])pred_subj.Field(1).Get()).Cast<int>()).Select(i => new Triple(new SUriNode(i, this), pred, obj)).ToArray();
            }
            else if (obj is SLiteralNode)
            {
                throw new NotImplementedException(); 
            }
            else throw new Exception();
        }

        public IEnumerable<Triple> GetTriples()
        {
            PaEntry paEntry = dataCell.Root.Element(0);

            //foreach (var element in entitiesTree.Root.Elements())
            for (int i = 0; i < entitiesTree.Root.Count(); i++)
            {
                var element = entitiesTree.Root.Element(i);
                //(int)element.Field(0).Get(),=WRONG
                SUriNode sUriNode = new SUriNode(i, this);
                foreach (object[] po in (object[]) element.Field(1).Get())
                {
                    paEntry.offset = (long) po[1];
                    yield return new Triple(sUriNode, new SUriNode((int)po[0], this), new SLiteralNode(paEntry.Get(),this)); 
                }
                foreach (object[] po in (object[])element.Field(2).Get())
                {
                    yield return new Triple(sUriNode, new SUriNode((int)po[0], this), new SUriNode((int) po[1], this));
                }
            }
        }


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
            tree_fix.Root.SetRepeat(namespaceMaper.coding.Count);
            Console.WriteLine("tree_fix length={0}", tree_fix.Root.Count());
      

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
               
                tree_fix.Root.Element(id).Set(record); 
            }
            tree_fix.Close();
            this.entitiesTree = new PxCell(tp_entitiesTree, path + "entitiesTree.pxc", false);
            Console.WriteLine("Scan3fix ok. Duration={0} msec. cnt_e={1} ", (DateTime.Now - tt0).Ticks / 10000L, cnt_e); tt0 = DateTime.Now;
            return cnt_e;
        }          

        internal long AddLiteral(object lit)
        {
            return dataCell.Root.AppendElement(lit);
        }


        public bool Contains(IUriNode subject, IUriNode predicate, INode @object)
        {
            if(!(subject is SUriNode))  throw new NotImplementedException();
            if(!(predicate is SUriNode)) throw new NotImplementedException();
            if(!(@object is SUriNode)) throw new NotImplementedException();
            var objectUriCode = (@object as SUriNode).Code;
            //Todo
            var subjCode = (subject as SUriNode).Code;
            return scale.ChkInScale(subjCode, (predicate as SUriNode).Code, objectUriCode)
                   &&
                   GetTriplesWithPredicateObject(predicate, @object)
                       .Select(triple => triple.Subject)
                       .Where(node => node is SUriNode)
                       .Cast<SUriNode>()
                       .Select(node => node.Code)
                       .Contains(subjCode);
        }
    }
}
