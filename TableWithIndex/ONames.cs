using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TableWithIndex
{
    public class ONames
    {
        public static string rdfnsstring = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static string rdftypestring = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        public static XNamespace rdfns = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static XName xmllang = "{http://www.w3.org/XML/1998/namespace}lang";
        public static XName rdfabout = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}about";
        public static XName rdfresource = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}resource";
        public static XName rdfdescription = "{http://www.w3.org/1999/02/22-rdf-syntax-ns#}Description";
        public static XName AttRdf = XName.Get("rdf", XNamespace.Xmlns.NamespaceName);

        public static XName tag_person = "{http://fogid.net/o/}person";
        public static XName tag_name = "{http://fogid.net/o/}name";
        public static XName tag_fromdate = "{http://fogid.net/o/}from-date";
    }
}
