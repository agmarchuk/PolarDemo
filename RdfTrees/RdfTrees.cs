using System.Collections.Generic;
using System.IO;
using NameTable;
using PolarDB;
using ScaleBit4Check;
using TripleIntClasses;
using TrueRdfViewer;


namespace RdfTreesNamespace
{
    /// <summary>
    /// Класс, представляющий собой хранилище триплетов, его методов, способ загрузки данных и формирования структуры
    /// </summary>
    public partial class RdfTrees     :RDFIntStoreAbstract
    {
        // Типы
        private PType tp_entitiesTree;
    //    private PType tp_literalsTree;
        //private PType tp_literal;
            // Дополнительные типы
        private PType tp_entity;
        private PType tp_otriple_seq;
      //  private PType tp_dtriple_seq;
        private PType tp_dtriple_spf;
        
        // Ячейки
        private PxCell entitiesTree;
        //private PxCell literalsTree;
       // private PaCell dtriples;
        // Место для базы данных  
        private ScaleCell scale;
        private string entitiesTreePath;
        private PaCell otriples;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="path">директория базы данных с (обратным) слешем</param>
        /// <param name="literalStore"></param>
        /// <param name="entityCoding"></param>
        /// <param name="nameSpaceStore"></param>
        /// <param name="predicatesCoding"></param>
        public RdfTrees(string path, IStringIntCoding entityCoding, PredicatesCoding predicatesCoding, NameSpaceStore nameSpaceStore, LiteralStoreAbstract literalStore)
            : base(entityCoding, predicatesCoding, nameSpaceStore, literalStore)
        {             
            // Построим типы
            InitTypes();
            // Создадим или откроем ячейки
            this.entitiesTree = new PxCell(tp_entitiesTree, entitiesTreePath = path + "entitiesTree.pxc", false);
            //this.literalsTree = new PxCell(tp_literalsTree, path + "literalsTree.pxc", false);
          //  this.dtriples = new PaCell(tp_dtriple_spf, path + "dtriples.pac", false); // Это вместо не работающего дерева литералов       }
            scale=new ScaleCell(path);
            
                otriples = new PaCell(tp_otriple_seq, path + "otriples.pac", File.Exists(path + "otriples.pac"));
            if (!scale.Filescale) scale.CreateScale(otriples);
                otriples.Close();
                
        }
        // Построение типов
        public override void InitTypes()
        {
            this.tp_entitiesTree = new PTypeSequence(new PTypeRecord(
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
            //this.tp_literalsTree = new PTypeSequence(new PTypeRecord(
            //    new NamedType("prop", new PType(PTypeEnumeration.integer)),
            //    new NamedType("litpairs", new PTypeSequence(new PTypeRecord(
            //        new NamedType("source", new PType(PTypeEnumeration.integer)),
            //        new NamedType("lit", tp_literal))))));
            // Дополнительные типы
            tp_entity = new PType(PTypeEnumeration.integer);
            tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("object", tp_entity)));
            //tp_dtriple_seq = new PTypeSequence(new PTypeRecord(
            //    new NamedType("subject", tp_entity),
            //    new NamedType("predicate", tp_entity),
            //    new NamedType("data", new PType(PTypeEnumeration.longinteger))));
            tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
        }
        // Разогрев


        public override IEnumerable<int> GetSubjectByDataPred(int p, Literal d)
        {
            throw new System.NotImplementedException();
        }

        // Генерация литерала из объектного представления, соответствующего tp_literal 
        //public static Literal GenerateLiteral(object pobj)
        //{
        //    object[] uni = (object[])pobj;
        //    int tag = (int)uni[0];
        //    if (tag == 1) // целое
        //    {
        //        return new Literal() { vid = LiteralVidEnumeration.integer, value = uni[1] };
        //    }
        //    else if (tag == 2) // строка
        //    {
        //        object[] strlangpair = (object[])uni[1];
        //        return new Literal()
        //        {
        //            vid = LiteralVidEnumeration.text,
        //            value = new Text() { s = (string)strlangpair[0], l = (string)strlangpair[1] }
        //        };
        //    }
        //    else if (tag == 3) // дата в виде двойного целого
        //    {
        //        return new Literal() { vid = LiteralVidEnumeration.date, value = uni[1] };
        //    }
        //    else return null; // такого варианта нет
        //}
    }
}
