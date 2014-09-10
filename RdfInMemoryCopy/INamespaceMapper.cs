using System;
using System.Collections.Generic;

namespace RdfInMemoryCopy
{
    public interface INamespaceMapper
    {
        IEnumerable<string> Prefixes { get; }
        void Clear();
        void AddNamespace(string prefix, Uri uri);
        void RemoveNamespace(string prefix);
        Uri GetNamespaceUri(string prefix);
        string GetPrefix(Uri uri);
        bool HasNamespace(string prefix);

        /// <summary>
        /// A Function which attempts to reduce a Uri to a QName. 
        /// This function will return a Boolean indicated whether it succeeded in reducing the Uri to a QName. 
        /// If it did then the out parameter qname will contain the reduction, otherwise it will be the empty string.
        /// </summary>
        /// <param name="uri">The Uri to attempt to reduce</param>
        /// <param name="qName"></param>
        /// <returns></returns>
        bool ReduceToQName(string uri, out string qName);

        INode type { get; set; }
    }
}