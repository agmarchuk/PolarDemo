using System;
using System.Collections.Generic;
using RdfInMemoryCopy;

namespace SparqlParseRun
{
    public class SparqlTriplet :ISparqlWhereItem
    {
        public SparqlNode Subj, Pred, Obj;

        public SparqlTriplet(SparqlNode subj, SparqlNode pred, SparqlNode obj)
        {
            Subj = subj;
            Pred = pred;
            Obj = obj;
        }

        public void CreateNodes(IStore store)
        {
            Subj.CreateNode(store);
            Pred.CreateNode(store);
            Obj.CreateNode(store);
            //   GraphIsDataProperty.Sync(o.graph, p.graph);
            VariableNode sVariableNode = this.Subj as VariableNode;
            VariableNode pVariableNode = this.Pred as VariableNode;
            VariableNode oVariableNode = this.Obj as VariableNode;

            if (sVariableNode != null && sVariableNode.isNew)
            {
                sVariableNode.isNew = false;
                if (pVariableNode != null && pVariableNode.isNew)
                {
                    pVariableNode.isNew = false;
                    if (oVariableNode != null && oVariableNode.isNew)
                    {
                        oVariableNode.isNew = false;
                        SelectVariableValuesOrFilter = RPackComplexExtensionInt.SPO(store, sVariableNode, pVariableNode, oVariableNode);
                    }
                    else SelectVariableValuesOrFilter = RPackComplexExtensionInt.SPo(store, sVariableNode, pVariableNode, this.Obj);
                }
                else
                {
                    if (oVariableNode != null && oVariableNode.isNew)
                        SelectVariableValuesOrFilter = RPackComplexExtensionInt.SpO(store, sVariableNode, this.Pred, oVariableNode);
                    else SelectVariableValuesOrFilter = RPackComplexExtensionInt.Spo(store, sVariableNode, this.Pred, this.Obj);
                }
            }
            else
            {
                if (pVariableNode != null && pVariableNode.isNew)
                {
                    pVariableNode.isNew = false;
                    if (oVariableNode != null && oVariableNode.isNew)
                    {
                        SelectVariableValuesOrFilter = RPackComplexExtensionInt.sPO(store, this.Subj, pVariableNode, oVariableNode);
                        oVariableNode.isNew = false;
                    }
                    else SelectVariableValuesOrFilter = RPackComplexExtensionInt.sPo(store, this.Subj, pVariableNode, this.Obj);
                }
                else
                {
                    if (oVariableNode != null && oVariableNode.isNew)
                    {
                        SelectVariableValuesOrFilter = RPackComplexExtensionInt.spO(store, this.Subj, this.Pred, oVariableNode);
                        oVariableNode.isNew = false;
                    }
                    else SelectVariableValuesOrFilter = RPackComplexExtensionInt.spo(store, this.Subj, this.Pred, this.Obj);
                }
            }
        }

        public Func<IEnumerable<Action>> SelectVariableValuesOrFilter { get; set; }
    }
}