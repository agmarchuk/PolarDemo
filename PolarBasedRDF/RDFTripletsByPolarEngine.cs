using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PolarBasedRDF
{
    class RDFTripletsByPolarEngine
    {
        private PType ptDirects = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("o", new PType(PTypeEnumeration.sstring))
            ));

        private PType ptData = new PTypeSequence(new PTypeRecord(
            new NamedType("s", new PType(PTypeEnumeration.sstring)),
            new NamedType("p", new PType(PTypeEnumeration.sstring)),
            new NamedType("d", new PType(PTypeEnumeration.sstring)),
            new NamedType("l", new PType(PTypeEnumeration.sstring))
            ));

        private PaCell directCell, dataCell;
        private string directCellPath, dataCellPath;
        public RDFTripletsByPolarEngine(string path)
        {
            directCell = new PaCell(ptDirects, directCellPath = path + "rdf.direct.pac");
            dataCell = new PaCell(ptData, dataCellPath = path + "rdf.data.pac");
        }
    }
}
