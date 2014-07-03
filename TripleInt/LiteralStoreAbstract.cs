using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PolarDB;

namespace TripleIntClasses
{
    public abstract class LiteralStoreAbstract
    {

        private List<Literal> writeBuffer;
        protected string dataCellPath;
        public string integer;
        public  string @double;
        public  string @float;
        public  string boolean;
        public  string date;
        public  string @string;
        public  string dateTime;

        public LiteralStoreAbstract(string dataCellPath,NameSpaceStore nameSpaceStore)
        {
            this.dataCellPath = dataCellPath + "/literals";
            if (!Directory.Exists(dataCellPath)) Directory.CreateDirectory(dataCellPath);
      
            InitConstants(nameSpaceStore);
        }

        public void InitConstants(NameSpaceStore nameSpaceStore)
        {
            integer = nameSpaceStore.GetShortFromFullOrPrefixed("<http://www.w3.org/2001/XMLSchema#integer>");
            @double = nameSpaceStore.GetShortFromFullOrPrefixed("<http://www.w3.org/2001/XMLSchema#double>");
            @float = nameSpaceStore.GetShortFromFullOrPrefixed("<http://www.w3.org/2001/XMLSchema#float>");
            boolean = nameSpaceStore.GetShortFromFullOrPrefixed("<http://www.w3.org/2001/XMLSchema#boolean>");
            date = nameSpaceStore.GetShortFromFullOrPrefixed("<http://www.w3.org/2001/XMLSchema#date>");
            @string = nameSpaceStore.GetShortFromFullOrPrefixed("<http://www.w3.org/2001/XMLSchema#string>");
            dateTime = nameSpaceStore.GetShortFromFullOrPrefixed("<http://www.w3.org/2001/XMLSchema#dateTime>");
        }

        public Literal Create(string datatype, string sdata, string lang)
        {
            if (datatype == integer ||
                datatype == @float ||
                datatype == @double)
                return new Literal(LiteralVidEnumeration.integer)
                {
                    Value = Double.Parse(sdata, NumberStyles.Any)
                };
            else if (datatype == boolean) return new Literal(LiteralVidEnumeration.date) {Value = Boolean.Parse(sdata)};
            else if (datatype == dateTime || datatype == date)
                return new Literal(LiteralVidEnumeration.date) {Value = DateTime.Parse(sdata).ToBinary()};
            else if (datatype == null || datatype == @string)
                return new Literal(LiteralVidEnumeration.text)
                {
                    Value = new Text() {Value = sdata, Lang = lang ?? String.Empty}
                };
            else
                return new Literal(LiteralVidEnumeration.typedObject)
                {
                    Value = new TypedObject() {Value = sdata, Type = datatype}
                };
        }

        public virtual void Open(bool readOnlyMode)
        {      
            
        }

        public abstract void Clear();

        public abstract void WarmUp();

        public abstract Literal Read(long offset, LiteralVidEnumeration? vid);
        

        protected object Read(PaCell fromCell, long offset)
        {
            var paEntry = fromCell.Root.Element(0);
            paEntry.offset = offset;
            return paEntry.Get();
        }

        public abstract Literal Write(Literal lit);


        public abstract void Flush();
    }
}