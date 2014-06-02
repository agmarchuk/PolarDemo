using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace RdfTrees
{
    /// <summary>
    /// Класс, представляющий собой хранилище триплетов, его методов, способ загрузки данных и формирования структуры
    /// </summary>
    public partial class RdfTrees
    {
        // Типы
        private PType tp_entitiesTree;
        private PType tp_literalsTree;
        private PType tp_literal;
        // Ячейки
        private PxCell entitiesTree;
        private PxCell literalsTree;

        // Место для базы данных
        private string path;
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
            this.literalsTree = new PxCell(tp_literalsTree, path + "literalsTree.pxc", false);
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
            this.tp_literal = new PTypeUnion(
                new NamedType("void", new PType(PTypeEnumeration.none)),
                new NamedType("integer", new PType(PTypeEnumeration.integer)),
                new NamedType("string", new PTypeRecord(
                    new NamedType("s", new PType(PTypeEnumeration.sstring)),
                    new NamedType("l", new PType(PTypeEnumeration.sstring)))),
                new NamedType("date", new PType(PTypeEnumeration.longinteger)));
            this.tp_literalsTree = new PTypeSequence(new PTypeRecord(
                new NamedType("prop", new PType(PTypeEnumeration.integer)),
                new NamedType("litpairs", new PTypeSequence(new PTypeRecord(
                    new NamedType("source", new PType(PTypeEnumeration.integer)),
                    new NamedType("lit", tp_literal))))));
        }
    }
}
