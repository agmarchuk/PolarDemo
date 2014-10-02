using System;
using System.Collections.Generic;
using System.Linq;
using TripleStoreForDNR;


namespace SparqlParseRun
{
    public static class RPackComplexExtensionInt
    {
      


      
   
        public static Func<IEnumerable<Action>> spo(PolarTripleStore store, SparqlNode s, SparqlNode p, SparqlNode o)
        {
            return ()=> store.Contains(new Triple(s.Value,p.Value,o.Value)) ? new Action[]{(() =>
            {
                Console.WriteLine("sdf"); 
            })} : Enumerable.Empty<Action>();
        }

        public static Func<IEnumerable<Action>> spd(SparqlNode subj, SparqlNode pred, SparqlNode d)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<Action>> spO(PolarTripleStore store, SparqlNode subjNode, SparqlNode pEntityCode, VariableNode obj)
        {
            return () => store
              .GetTriplesWithSubjectPredicate(subjNode.Value, pEntityCode.Value)
              .Select(triple => triple.Object)
              .Select(newObj => new Action(() =>
              {
                  obj.Value = newObj;
              }));
        }
        internal static Func<IEnumerable<Action>> Spo(PolarTripleStore store, VariableNode subj, SparqlNode pEntityCode, SparqlNode oEntityCode)
        {
            return () => store
                .GetTriplesWithPredicateObject(pEntityCode.Value, oEntityCode.Value)
                .Select(triple => triple.Subject)
                .Select(sub => new Action(() =>
                {
                    subj.Value = sub;
                }));
        }

        internal static Func<IEnumerable<Action>> SpO(PolarTripleStore store, VariableNode s, SparqlNode pEntityCode, VariableNode o)
        {
            throw new NotImplementedException();
        }

        public static Func<IEnumerable<Action>> spD(IStore store, SparqlNode sNode, SparqlNode pNode, VariableNode objectNode)
        {
            return () => store 
                .GetTriplesWithSubjectPredicate(sNode.Value, pNode.Value)
                .Select(triple => triple.Object)
                .Select(node => new Action(() =>
                {
                    objectNode.Value = node;
                }));
        }

   

        internal static Func<IEnumerable<Action>> sPo(PolarTripleStore store, SparqlNode sEntityCode, VariableNode predicateVariableNode, SparqlNode oEntityCode)
        {
            return () => store
                .GetTriplesWithSubjectObject(sEntityCode.Value,  oEntityCode.Value)
                .Select(triple => triple.Predicate)
                .Select(predicate => new Action(()=>
                {
                    predicateVariableNode.Value = predicate;    
                }));
        }

        internal static Func<IEnumerable<Action>> sPd(IStore store, SparqlNode sEntityCode, VariableNode predicate, SparqlNode d)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<Action>> sPO(PolarTripleStore store, SparqlNode sEntityCode, VariableNode pParameter, VariableNode oParameter)
        {
            return () => store
                .GetTriplesWithSubject(sEntityCode.Value)
                .Select(triple => new Action(() =>
                {
                    pParameter.Value = triple.Predicate;
                    oParameter.Value = triple.Object;
                }));
        }

        internal static Func<IEnumerable<Action>> sPD(IStore store, SparqlNode sEntityCode, VariableNode pParameter, VariableNode oParameter)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<Action>> Spd(VariableNode sParameter, object pEntityCode, SparqlNode d)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<Action>> SpD(VariableNode sParameter, object pEntityCode, VariableNode oParameter)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<Action>> SPo(PolarTripleStore store, VariableNode sParameter, VariableNode pParameter, SparqlNode oEntityCode)
        {
            return () => store
                 .GetTriplesWithObject(oEntityCode.Value)
                 .Select(triple => new Action(() =>
                 {
                     pParameter.Value = triple.Predicate;
                     sParameter.Value = triple.Subject;
                 }));
        }

        internal static Func<IEnumerable<Action>> SPd(short sParameter, short pParameter, SparqlNode d)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<Action>> SPO(PolarTripleStore store, VariableNode sParameter, VariableNode pParameter, VariableNode oParameter, SparqlNode graph)
        {
           return () => store.Triplets
        }
    }
}