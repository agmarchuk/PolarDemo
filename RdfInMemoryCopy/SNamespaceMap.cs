using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemoryCopy
{
    class SNamespaceMap : INamespaceMapper
    {
        private Dictionary<string, Uri> namespaceByPrefix = new Dictionary<string, Uri>();

        public IEnumerable<string> Prefixes
        {
            get { throw new NotImplementedException(); }
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void AddNamespace(string prefix, Uri uri)
        {
            namespaceByPrefix.Add(prefix, uri);
        }

        public void RemoveNamespace(string prefix)
        {
            throw new NotImplementedException();
        }

        public Uri GetNamespaceUri(string prefix)
        {
            Uri uri;
            if (namespaceByPrefix.TryGetValue(prefix, out uri))
                return uri;
            return null;
        }

        public string GetPrefix(Uri uri)
        {
            throw new NotImplementedException();
        }

        public bool HasNamespace(string prefix)
        {
           return namespaceByPrefix.ContainsKey(prefix);
        }

        public bool ReduceToQName(string uri, out string qname)
        {
            throw new NotImplementedException();
        }
    }
}
