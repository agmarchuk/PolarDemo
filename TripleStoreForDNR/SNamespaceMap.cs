using System;
using System.Collections.Generic;

namespace TripleStoreForDNR
{
  public  class SNamespaceMap : INamespaceMapper
    {
        private Dictionary<string, Uri> namespaceByPrefix = new Dictionary<string, Uri>();
        private Dictionary<string, string> prefixByNamespace = new Dictionary<string, string>();
        private int generatedprefix=0;

        public IEnumerable<string> Prefixes
        {
            get { return namespaceByPrefix.Keys; }
        }

        public virtual void Clear()
        {
         namespaceByPrefix.Clear();
            prefixByNamespace.Clear();
            
        }

        public void AddNamespace(string prefix, Uri uri)
        {
            namespaceByPrefix.Add(prefix, uri);
            prefixByNamespace.Add(uri.ToString(), prefix);
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
            string prefix;
            return prefixByNamespace.TryGetValue(uri.AbsoluteUri, out prefix) ? prefix : null;
        }


      public bool HasNamespace(string prefix)
        {
           return namespaceByPrefix.ContainsKey(prefix);
        }

        public bool ReduceToQName(string uri, out string qName)
        {   
            Uri urlNs;
            string shortName;
            SplitUrl(uri, out shortName, out urlNs);

            string prefix;
            string ns=urlNs.AbsoluteUri;
            if (prefixByNamespace.TryGetValue(ns, out prefix))
            {
                qName = prefix + shortName;
                return true;
            }
            qName = null;
            return false;
        }

      public static void SplitUrl(string uri, out string shortUrl, out Uri urlNs)
        {
            Uri uri1 = new Uri(uri);
          shortUrl = uri1.Fragment=="" ? uri1.Segments[uri1.Segments.Length - 1] : uri1.Fragment.Substring(1);
          urlNs = new Uri(uri.Substring(0, uri.Length - shortUrl.Length));
        }

        public INode type { get; set; }
        
    }


}
