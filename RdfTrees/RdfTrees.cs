using PolarDB;
using ScaleBit4Check;
using TrueRdfViewer;


namespace RdfTrees
{
    /// <summary>
    /// Класс, представляющий собой хранилище триплетов, его методов, способ загрузки данных и формирования структуры
    /// </summary>
    public partial class RdfTrees     :TripleStoreInt
    {
        // Типы
        private PType tp_entitiesTree;
        private PType tp_literalsTree;
        private PType tp_literal;
            // Дополнительные типы
        private PType tp_entity;
        private PType tp_otriple_seq;
        private PType tp_dtriple_seq;
        private PType tp_dtriple_spf;
        
        // Ячейки
        private PxCell entitiesTree;
        //private PxCell literalsTree;
        private PaCell dtriples;
        // Место для базы данных
        private string path;
        private ScaleCell scale;
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="path">директория базы данных с (обратным) слешем</param>
        public RdfTrees(string path) 
        {
            this.path = path;
            // Построим типы
            InitTypes();
            // Создадим или откроем ячейки
            this.entitiesTree = new PxCell(tp_entitiesTree, path + "entitiesTree.pxc", false);
            //this.literalsTree = new PxCell(tp_literalsTree, path + "literalsTree.pxc", false);
            this.dtriples = new PaCell(tp_dtriple_seq, path + "dtriples.pac", false); // Это вместо не работающего дерева литералов       }
            scale=new ScaleCell(path);
            if (!scale.Filescale) scale.CreateScale(otriples);

        }
        // Построение типов
        private void InitTypes()
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
  tp_literal = new PTypeUnion(
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
            this.tp_literalsTree = new PTypeSequence(new PTypeRecord(
                new NamedType("prop", new PType(PTypeEnumeration.integer)),
                new NamedType("litpairs", new PTypeSequence(new PTypeRecord(
                    new NamedType("source", new PType(PTypeEnumeration.integer)),
                    new NamedType("lit", tp_literal))))));
            // Дополнительные типы
            tp_entity = new PType(PTypeEnumeration.integer);
            tp_otriple_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("object", tp_entity)));
            tp_dtriple_seq = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("data", tp_literal)));
            tp_dtriple_spf = new PTypeSequence(new PTypeRecord(
                new NamedType("subject", tp_entity),
                new NamedType("predicate", tp_entity),
                new NamedType("offset", new PType(PTypeEnumeration.longinteger))));
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
