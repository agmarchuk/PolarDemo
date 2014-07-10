using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace RdfInMemory
{
    public class Graph : IGraph
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
        public Graph(string path)
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
        public INamespaceMapper NamespaceMap { get { throw new Exception("NamespaceMap does not implemented"); } }

        public void Clear()
        {
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
                otriples.Root.AppendElement(new object[] { t.Subject, t.Predicate, t.Object }); // НАдо сде5лать как надо  
            }
            else if (t.Object.NodeType == NodeType.Literal)
            {
            }
            else return false;
            return true;
        }
        public void Build()
        {
            throw new Exception("Clear does not implemented");
        }
        // Создатели
        public IUriNode CreateUriNode(string urivalue)
        {
            throw new Exception("CreateUriNode does not implemented");
            return null;
        }
        public ILiteralNode CreateLiteralNode(string value)
        {
            throw new Exception("CreateLiteralNode does not implemented");
            return null;
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
    }
}
