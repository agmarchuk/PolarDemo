using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdfInMemory
{
    public class TNamespaceMapper : INamespaceMapper 
    {
        private Dictionary<string, Uri> prefUri = new Dictionary<string, Uri>();
        //private Dictionary<string, string> uriPref = new Dictionary<string, string>();
        public IEnumerable<string> Prefixes { get { return prefUri.Keys; } }
        public void Clear() { prefUri = new Dictionary<string, Uri>(); }
        public void AddNamespace(string prefix, Uri uri) { prefUri.Add(prefix, uri); } // Нужна защита
        public void RemoveNamespace(string prefix) { prefUri.Remove(prefix); } // Нужна защита
        public Uri GetNamespaceUri(string prefix) { return prefUri[prefix]; } // Нужна защита?
        public string GetPrefix(Uri uri) { return prefUri.Where(pu => pu.Value == uri).Select(pu => pu.Key).FirstOrDefault(); }
        public bool HasNamespace(string prefix) { return prefUri.ContainsKey(prefix); }
        /// <summary>
        /// A Function which attempts to reduce a Uri to a QName. 
        /// This function will return a Boolean indicated whether it succeeded in reducing the Uri to a QName. 
        /// If it did then the out parameter qname will contain the reduction, otherwise it will be the empty string.
        /// </summary>
        /// <param name="uri">The Uri to attempt to reduce</param>
        /// <param name="qname">The value to output the QName to if possible</param>
        /// <returns></returns>
        public bool ReduceToQName(string uri, out string qname)
        {
            // Последний # или /
            int pos = uri.LastIndexOfAny(new char[] { '#', '/' });
            qname = "";
            if (pos < 0) return false;
            string pref = GetPrefix(new Uri(uri.Substring(0, pos + 1)));
            if (pref == null) return false;
            qname = pref + ":" + uri.Substring(pos + 1);
            return true;
        }

    }
}
