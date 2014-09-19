using System;
using System.Collections.Generic;

namespace TripleStoreForDNR
{
    public interface IGraph
    {
        bool IsEmpty { get; }
        Uri Uri { get; }
        INamespaceMapper NamespaceMap { get; }
        //IEnumerable<INode> Nodes { get; } // ѕока вредне не нужен...
        // —оздатели
        IUriNode CreateUriNode(string uriOrQname);
        ILiteralNode CreateLiteralNode(string value); // Ёто когда надо разбиратьс€ с текстом до точки
        ILiteralNode CreateLiteralNode(string value, Uri datatype);
        ILiteralNode CreateLiteralNode(string value, string lang);
        // ќчистка, добавление триплетов, построение графа
        void Clear();
        bool Assert(Triple t);
        void Build(); // Ёто действие отсутствует в стандарте dotnetrdf!



        /// <summary>
        /// Selects all Triples where the Object is a Uri Node with the given Uri
        /// </summary>
        /// <param name="u">Uri</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithObject(Uri u);

        /// <summary>
        /// Selects all Triples where the Object is a given Node
        /// </summary>
        /// <param name="n">Node</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithObject(INode n);

        /// <summary>
        /// Selects all Triples where the Predicate is a given Node
        /// </summary>
        /// <param name="n">Node</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithPredicate(INode n);

        /// <summary>
        /// Selects all Triples where the Predicate is a Uri Node with the given Uri
        /// </summary>
        /// <param name="u">Uri</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithPredicate(Uri u);

        /// <summary>
        /// Selects all Triples where the Subject is a given Node
        /// </summary>
        /// <param name="n">Node</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithSubject(INode n);

        /// <summary>
        /// Selects all Triples where the Subject is a Uri Node with the given Uri
        /// </summary>
        /// <param name="u">Uri</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithSubject(Uri u);

        /// <summary>
        /// Selects all Triples with the given Subject and Predicate
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <param name="pred">Predicate</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred);

        /// <summary>
        /// Selects all Triples with the given Subject and Object
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithSubjectObject(INode subj, INode obj);

        /// <summary>
        /// Selects all Triples with the given Predicate and Object
        /// </summary>
        /// <param name="pred">Predicate</param>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj);
        IEnumerable<Triple> GetTriples();

        bool Contains(IUriNode subject, IUriNode predicate, INode @object);
        

        IUriNode GetUriNode(Uri uri);
        ILiteralNode GetLiteralNode(dynamic value, string lang, Uri type);
        IUriNode CreateUriNode(Uri uri);
    }
}