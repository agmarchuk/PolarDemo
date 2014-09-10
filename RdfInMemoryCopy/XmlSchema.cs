using System;

namespace RdfInMemoryCopy
{
    public static class XmlSchema
    {
        public static readonly Uri XMLSchemaInteger = new Uri("http://www.w3.org/2001/XMLSchema#integer");
        public static readonly Uri XMLSchemaFloat = new Uri("http://www.w3.org/2001/XMLSchema#float");
        public static readonly Uri XMLSchemaDouble = new Uri("http://www.w3.org/2001/XMLSchema#double");
        public static readonly Uri XMLSchemaBool = new Uri("http://www.w3.org/2001/XMLSchema#boolean");
        public static readonly Uri XMLSchemaDate = new Uri("http://www.w3.org/2001/XMLSchema#date");
        public static readonly Uri XMLSchemaDateTime = new Uri("http://www.w3.org/2001/XMLSchema#dateTime");
        public static readonly Uri XMLSchemaLangString = new Uri("http://www.w3.org/2001/XMLSchema#langString");
        public static readonly Uri XMLSchemaString = new Uri("http://www.w3.org/2001/XMLSchema#string");
        

        //public XmlSchema(IStore store)
        //{

        //    TypeInteger = store.GetUriNode(XMLSchemaInteger);
        //    TypeFloat = store.GetUriNode(XMLSchemaFloat);
        //    TypeDouble = store.GetUriNode(XMLSchemaDouble);
        //    TypeBool = store.GetUriNode(XMLSchemaBool);
        //    TypeDate = store.GetUriNode(XMLSchemaDate);
        //    TypeDateTime = store.GetUriNode(XMLSchemaDateTime);
        //    //TypeLangString = graph.GetUriNode(XMLSchemaInteger);
        //    TypeString = store.GetUriNode(XMLSchemaString);
        //}

        //public void Create(IGraph graph)
        //{
        //    TypeInteger = graph.CreateUriNode(XMLSchemaInteger);
        //    TypeFloat = graph.CreateUriNode(XMLSchemaFloat);
        //    TypeDouble = graph.CreateUriNode(XMLSchemaDouble);
        //    TypeBool = graph.CreateUriNode(XMLSchemaBool);
        //    TypeDate = graph.CreateUriNode(XMLSchemaDate);
        //    TypeDateTime = graph.CreateUriNode(XMLSchemaDateTime);
        //    //TypeLangString = graph.CreateUriNode(XMLSchemaInteger);
        //    TypeString = graph.CreateUriNode(XMLSchemaString);
        //}

        //public IUriNode TypeFloat { get; set; }

        //public IUriNode TypeDouble { get; set; }

        //public IUriNode TypeBool { get; set; }

        //public IUriNode TypeDate { get; set; }

        //public IUriNode TypeDateTime { get; set; }

        //public IUriNode TypeString { get; set; }

        //public IUriNode TypeInteger { get; private set; }
    }
}