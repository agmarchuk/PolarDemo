using System;
using System.Collections.Generic;
using System.Linq;
using NameTable;

namespace RdfInMemoryCopy
{
    public class NamespaceMapCoding : SNamespaceMap
    {
        public readonly IStringIntCoding coding;
     
        public NamespaceMapCoding(IStringIntCoding coding)
        {      
            this.coding = coding;
            coding.Open(false);        
        }

        public override void Clear()
        {
            base.Clear();
            coding.Clear();
        }

        public static string NodesToString(IEnumerable<IEnumerable<INode>> nodesCollections, INamespaceMapper globalNsMapper)
        {
            SNamespaceMap mapLocal = new SNamespaceMap();

            string resultLines = String.Join(Environment.NewLine,
                nodesCollections.Select(result => String.Join(" ", result.Select<INode, string>(node =>
                {
                    //optional empty
                    if (node == null)
                    {
                        return " ";
                    }
                    IUriNode uriNode = node as IUriNode;
                    if (uriNode != null)
                        return Uri2QName(mapLocal, uriNode.Uri.ToString(), globalNsMapper);
                    ILiteralNode literalNode = node as ILiteralNode;
                    if (literalNode != null)
                    {
                        if (literalNode.DataType != null)
                        {
                            if (literalNode.DataType.AbsoluteUri == XmlSchema.XMLSchemaLangString.AbsoluteUri)
                                return "\"" + literalNode.Value + "\"@" + literalNode.Language;
                            if (literalNode.DataType.AbsoluteUri == XmlSchema.XMLSchemaString.AbsoluteUri)
                                return "\"" + literalNode.Value + "\"";
                            return String.Format("\"{0}\"^^{1}", literalNode.Value,
                                Uri2QName(mapLocal, literalNode.DataType.AbsoluteUri, globalNsMapper));
                        }
                        if (!(String.IsNullOrWhiteSpace(literalNode.Language)))
                            throw new NotImplementedException();
                        //  return literalNode.Value + "@" + literalNode.Language;
                        return literalNode.Value;
                    }
                    throw new NotImplementedException();
                }
                    ))));
            return (mapLocal.Prefixes.Any()
                ? String.Join(Environment.NewLine,
                    mapLocal.Prefixes.Select(
                        prefix => "@prefix " + prefix + " " + mapLocal.GetNamespaceUri(prefix))) +
                  Environment.NewLine + Environment.NewLine
                : String.Empty) + resultLines;
        }

        private static string Uri2QName(SNamespaceMap mapLocal, string uri, INamespaceMapper globalNamespaceMaper)
        {
            string qname;
            if (mapLocal.ReduceToQName(uri, out qname))
                return qname;
            string shortName;
            Uri urlNs;
            SplitUrl(uri, out shortName, out urlNs);
            var prefix = globalNamespaceMaper.GetPrefix(urlNs);
            if (prefix == null)
            {
                string p = "ns";
                int i = 0;
                while (mapLocal.HasNamespace(prefix = p + i + ":")
                       || globalNamespaceMaper.HasNamespace(prefix)) i++;
            }
            mapLocal.AddNamespace(prefix, urlNs);
            return prefix + shortName;
        }
    }
}