﻿using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;
using TripleIntClasses;
using TrueRdfViewer;


namespace RdfTrees
{
    public partial class RdfTrees
    {
        private PaCell otriples, dtriples;
        public override void LoadTurtle(string filename)
        {
            // Дополнительные ячейки и индексы
            otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", false);
            dtriples = new PaCell(tp_dtriple_spf, path + "dtriples.pac", false); // Временно выведена в переменные класса, открывается при инициализации

            DateTime tt0 = DateTime.Now;

            // Загрузка otriples, dtriples
            //Load(filename, otriples, dtriples);
            this.Load(filename);
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
            PaCell otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", true);
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

        private void Load(string filepath)
        {
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            foreach (var triple in TurtleInt0.LoadGraph(filepath))
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
                        da = new object[] { 1, lit.Value };
                    else if (lit.vid == LiteralVidEnumeration.date)
                        da = new object[] { 3, lit.Value };
                    else if (lit.vid == LiteralVidEnumeration.text)
                    {
                        Text t = (Text)lit.Value;
                        da = new object[] { 2, new object[] { t.Value, t.Lang } };
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
        
        private static void Load(string filepath, PaCell otriples, PaCell dtriples)
        {
            otriples.Clear();
            otriples.Fill(new object[0]);
            dtriples.Clear();
            dtriples.Fill(new object[0]);
            LiteralStore.Literals.dataCell.Clear();
            LiteralStore.Literals.dataCell.Fill(new object[0]);
            int i = 0;
            //Entity e = new Entity();
            TripleInt.useSimpleCoding = true;
            //TripleInt.SiCodingEntities.Clear();
            //TripleInt.SiCodingPredicates.Clear();
            //TripleInt.EntitiesCodeCache.Clear();
            //TripleInt.PredicatesCodeCache.Clear();


            foreach (var tripletGrpah in TurtleInt.LoadGraphs(filepath))
            {

                if (i % 100000 == 0) Console.Write("w{0} ", i / 100000); i += tripletGrpah.PredicateDataValuePairs.Count + tripletGrpah.PredicateObjValuePairs.Count;
                var subject = TripleInt.EntitiesCodeCache[tripletGrpah.subject];

                foreach (var predicateObjValuePair in tripletGrpah.PredicateObjValuePairs)
                    otriples.Root.AppendElement(new object[]
                    {
                        subject,
                        predicateObjValuePair.Key,
                        TripleInt.EntitiesCodeCache[predicateObjValuePair.Value]
                    });
                LiteralStore.Literals.WriteBufferForce();
                foreach (var predicateDataValuePair in tripletGrpah.PredicateDataValuePairs)
                    dtriples.Root.AppendElement(new object[]
                    {
                        subject,
                        predicateDataValuePair.Key,
                    predicateDataValuePair.Value.Offset
                    });
            }
            
            otriples.Flush();
            dtriples.Flush();
            
        }
    }

    public class TurtleInt0
    {
        // (Только для специальных целей) Это для накапливания идентификаторов собираемых сущностей:
        public static List<string> sarr = new List<string>();

        public static IEnumerable<TripleInt> LoadGraph(string datafile)//EngineVirtuoso engine, string graph, string datafile)
        {
            int ntriples = 0;
            string subject = null;
            Dictionary<string, string> namespaces = new Dictionary<string, string>();
            System.IO.StreamReader sr = new System.IO.StreamReader(datafile);
            int count = 2000000000;
            for (int i = 0; i < count; i++)
            {
                string line = sr.ReadLine();
                //if (i % 10000 == 0) { Console.Write("{0} ", i / 10000); }
                if (line == null) break;
                if (line == "") continue;
                if (line[0] == '@')
                { // namespace
                    string[] parts = line.Split(' ');
                    if (parts.Length != 4 || parts[0] != "@prefix" || parts[3] != ".")
                    {
                        Console.WriteLine("Err: strange line: " + line);
                        continue;
                    }
                    string pref = parts[1];
                    string nsname = parts[2];
                    if (nsname.Length < 3 || nsname[0] != '<' || nsname[nsname.Length - 1] != '>')
                    {
                        Console.WriteLine("Err: strange nsname: " + nsname);
                        continue;
                    }
                    nsname = nsname.Substring(1, nsname.Length - 2);
                    namespaces.Add(pref, nsname);
                }
                else if (line[0] != ' ')
                { // Subject
                    line = line.Trim();
                    subject = GetEntityString(namespaces, line);
                    if (subject == null) continue;
                }
                else
                { // Predicate and object
                    string line1 = line.Trim();
                    int first_blank = line1.IndexOf(' ');
                    if (first_blank == -1) { Console.WriteLine("Err in line: " + line); continue; }
                    string pred_line = line1.Substring(0, first_blank);
                    string predicate = GetEntityString(namespaces, pred_line);
                    string rest_line = line1.Substring(first_blank + 1).Trim();
                    // Уберем последний символ
                    rest_line = rest_line.Substring(0, rest_line.Length - 1).Trim();
                    bool isDatatype = rest_line[0] == '\"';
                    // объект может быть entity или данное, у данного может быть языковый спецификатор или тип
                    string entity = null;
                    string sdata = null;
                    string datatype = null;
                    string lang = null;
                    if (isDatatype)
                    {
                        // Последняя двойная кавычка 
                        int lastqu = rest_line.LastIndexOf('\"');

                        // Значение данных
                        sdata = rest_line.Substring(1, lastqu - 1);

                        // Языковый специализатор:
                        int dog = rest_line.LastIndexOf('@');
                        if (dog == lastqu + 1) lang = rest_line.Substring(dog + 1, rest_line.Length - dog - 1);

                        int pp = rest_line.IndexOf("^^");
                        if (pp == lastqu + 1)
                        {
                            //  Тип данных
                            string qname = rest_line.Substring(pp + 2);
                            //  тип данных может быть "префиксным" или полным
                            if (qname[0] == '<')
                            {
                                datatype = qname.Substring(1, qname.Length - 2);
                            }
                            else
                            {
                                datatype = GetEntityString(namespaces, qname);
                            }
                        }
                        yield return new DTripleInt()
                        {
                            subject = TripleInt.Code(subject),
                            predicate = TripleInt.Code(predicate),
                            data = // d
                                datatype == "http://www.w3.org/2001/XMLSchema#integer" ?
                                    new Literal() { vid = LiteralVidEnumeration.integer, Value = int.Parse(sdata) } :
                                (datatype == "http://www.w3.org/2001/XMLSchema#date" ?
                                    new Literal() { vid = LiteralVidEnumeration.date, Value = DateTime.Parse(sdata).ToBinary() } :
                                (new Literal() { vid = LiteralVidEnumeration.text, Value = new Text() { Value = sdata, Lang = "en" } }))

                        };
                    }
                    else
                    { // entity
                        entity = rest_line[0] == '<' ? rest_line.Substring(1, rest_line.Length - 2) : GetEntityString(namespaces, rest_line);

                        // (Только для специальных целей) Накапливание:
                        if (predicate == "http://www.w3.org/1999/02/22-rdf-syntax-ns#type" &&
                            entity == "http://www4.wiwiss.fu-berlin.de/bizer/bsbm/v01/vocabulary/Product")
                        {
                            sarr.Add(subject);
                        }

                        yield return new OTripleInt()
                        {
                            subject = TripleInt.Code(subject),
                            predicate = TripleInt.Code(predicate),
                            obj = TripleInt.Code(entity)
                        };
                    }
                    ntriples++;
                }
            }
            Console.WriteLine("ntriples={0}", ntriples);
        }

        private static string GetEntityString(Dictionary<string, string> namespaces, string line)
        {
            string subject = null;
            int colon = line.IndexOf(':');
            if (colon == -1) { Console.WriteLine("Err in line: " + line); goto End; }
            string prefix = line.Substring(0, colon + 1);
            if (!namespaces.ContainsKey(prefix)) { Console.WriteLine("Err in line: " + line); goto End; }
            subject = namespaces[prefix] + line.Substring(colon + 1);
        End:
            return subject;
        }
    }

}
