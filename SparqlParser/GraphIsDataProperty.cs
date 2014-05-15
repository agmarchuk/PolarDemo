using System;
using System.Collections.Generic;
using System.Linq;
using ANTLR_Test.Annotations;
using TrueRdfViewer;

namespace ANTLR_Test
{
    public class GraphIsDataProperty
    {
        private HashSet<GraphIsDataProperty> connected;

        internal bool? IsData { get; set; }
        internal LiteralVidEnumeration vid;

        private static void Connect([NotNull] GraphIsDataProperty g1, [NotNull] GraphIsDataProperty g2)
        {
            if (g1.connected == null)
                if (g2.connected == null)
                    g1.connected = g2.connected = new HashSet<GraphIsDataProperty> {g1, g2};
                else
                {
                    g1.connected = g2.connected;
                    g1.connected.Add(g1);
                }
            else if (g2.connected == null)
            {
                g2.connected = g1.connected;
                g1.connected.Add(g2);
            }
            else
            {
                foreach (var sub2 in g2.connected.Where(sub2 => sub2 != g2))
                {
                    sub2.connected = g1.connected;
                    g1.connected.Add(sub2);
                }
                g2.connected = g1.connected;
                g1.connected.Add(g2);
            }
        }

        public void Set(bool isData)
        {
            if (connected==null) connected=new HashSet<GraphIsDataProperty>();
            //IsData = isData;            .Where(sub => sub != this)
            foreach (var node in connected)
            {
               // if (node.IsData != null) throw new Exception();
                node.IsData = isData;
            }
        }
        public void Set(LiteralVidEnumeration vidEnumeration)
        {
            if (connected == null) connected = new HashSet<GraphIsDataProperty>();
            //IsData = isData;            .Where(sub => sub != this)
            foreach (var node in connected)
            {
                if (node.IsData != null) throw new Exception();
                node.IsData = true;
                node.vid = vidEnumeration;
            }       
        }
        public void ReSet(LiteralVidEnumeration vidEnumeration)
        {
            if (connected == null) connected = new HashSet<GraphIsDataProperty>();
            //IsData = isData;            .Where(sub => sub != this)
            foreach (var node in connected)
            {
                node.IsData = true;
                node.vid = vidEnumeration;
            }
        }
        public void ReSet(bool isData)
        {
            if (connected == null) connected = new HashSet<GraphIsDataProperty>();
            //IsData = isData;            .Where(sub => sub != this)
            foreach (var node in connected)
                node.IsData = isData;
        }
        internal static void Sync([NotNull] GraphIsDataProperty oGraph, [NotNull] GraphIsDataProperty pGraph)
        {
            if (oGraph.IsData != null)
                if (pGraph.IsData != null)
                {
                    if (pGraph.IsData != oGraph.IsData) throw new Exception();
                }
                else if (oGraph.IsData.Value)
                    pGraph.Set(oGraph.vid);
                else pGraph.Set(false);
            else if (pGraph.IsData != null)
                if (pGraph.IsData.Value)
                    oGraph.Set(pGraph.vid);
                else oGraph.Set(false);
            else
                Connect(oGraph, pGraph);
        }
    }
}