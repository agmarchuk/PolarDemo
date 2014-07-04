using System;
using System.Collections.Generic;
using System.Linq;
using TripleIntClasses;

namespace SparqlParseRun
{
    public static class RPackComplexExtensionInt
    {
        public static IEnumerable<RPackInt> OptionalGroup(this IEnumerable<RPackInt> pack, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> group, params short[] changedVariables)
        {
            var packArray = pack;// as RPackInt[] ?? pack.ToArray();
            var optionalGroup = @group(packArray);//.ToArray();
 
            if (optionalGroup.Any()) return optionalGroup;

            return packArray.Select(pk =>
            {
                for (int i = 0; i < changedVariables.Length; i++)
                    pk.Set(changedVariables[i], string.Empty);
                return pk;
            });
        }

        public static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> Optional(this Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> graphSelector, short parametersStartIndex, short parametersEndIndex)
        {
            return packs => packs.SelectMany(pack => Optional(pack, graphSelector, parametersStartIndex, parametersEndIndex));
        }

        private static IEnumerable<RPackInt> Optional(RPackInt pack, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> graphSelector, short parametersStartIndex, short parametersEndIndex)
        {
            var packBox = Enumerable.Repeat(pack, 1); // as RPackInt[] ?? packs.ToArray();
            bool any = false;
            foreach (var rPackInt in graphSelector(packBox))
            {
                yield return rPackInt;
                if (!any)
                    any = true;
            }
            if (!any)
                yield return pack.ResetDiapason(parametersStartIndex, parametersEndIndex);
        }


        public static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> Union(
            this Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> first, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> second)
        {
            return packs =>
            //{
              packs.SelectMany(p=> UnionRun(p, first, second));
                //var packsArray = packs;// as RPackInt[] ?? packs.ToArray();
                //return first(packsArray).Concat(second(packsArray));
            //};
        }

        private static IEnumerable<RPackInt> UnionRun(RPackInt pack, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> first, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> second)
        {
            var rPackIntBox = Enumerable.Repeat(pack, 1);
            if (first != null)
                foreach (var resultRPackInt in first(rPackIntBox))
                yield return resultRPackInt;
            if (second == null) yield break;
            foreach (var resultRPackInt in second(rPackIntBox))
                yield return resultRPackInt;
        }

        public static IEnumerable<RPackInt> Union(this IEnumerable<RPackInt> pack,
            params Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>>[] groupFuncs)
        {
            return groupFuncs.SelectMany(func => func(pack));
        }

        public static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> spo(object subj, object pred, object obj)
        {
            return pacs => pacs.Where(pk => pk.StoreAbstract.ChkOSubjPredObj(pk.GetE(subj), pk.GetE(pred), pk.GetE(obj)));
        }

        public static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> spd(object subj, object pred, Literal d)
        {
            return pacs => pacs.Where(pk => pk.StoreAbstract.GetDataBySubjPred(pk.GetE(subj), pk.GetE(pred)).Any(d1=>d1.Equals(d)));
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> spO(object sEntityCode, object pEntityCode, short obj)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
                .GetObjBySubjPred(pk.GetE(sEntityCode), pk.GetE(pEntityCode))
                .Select(ob =>
                {
                    pk.Set(obj, ob);
                    return new RPackInt(pk.row, pk.StoreAbstract);
                }));
        }
        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> Spo(short subj, object pEntityCode, object oEntityCode)
        {
          return pack => pack.SelectMany(pk => pk.StoreAbstract
                .GetSubjectByObjPred(pk.GetE(oEntityCode), pk.GetE(pEntityCode))
                .Select(su =>
                {
                    pk.Set(subj, su);
                    return new RPackInt(pk.row, pk.StoreAbstract);
                }));
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> SpO(short s, object pEntityCode, short o)
        {
            throw new NotImplementedException();
        }

        public static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> spD(object sEntityCode, object pEntityCode, short oParamIndex)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
             .GetDataBySubjPred(pk.GetE(sEntityCode), pk.GetE(pEntityCode))
             .Select(ob =>
             {
                 pk.Set(oParamIndex, ob);
                 return new RPackInt(pk.row, pk.StoreAbstract);
             }));
        }

        private static IEnumerable<RPackInt> CallObjectAndData(
            Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> oCall,
            Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> dCall, 
            IEnumerable<RPackInt> pack)
        {
            var beforeSequene = pack.Select(i => Enumerable.Repeat(i,1));
            foreach (var before in beforeSequene)
            {
                foreach (var rPackInt in oCall(before))
                    yield return rPackInt;

                foreach (var rPackInt in dCall(before))
                    yield return rPackInt;
            }
        }
        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> CallObjectAndData(
            Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> oCall,
            Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> dCall)
        {
            return pack => CallObjectAndData(oCall, dCall, pack);
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> CallObjectOrData(GraphIsDataProperty pGraph, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> oCall, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> dCall)
        {
            return pack => pGraph.IsData != null
                ? (pGraph.IsData.Value ? dCall(pack) : oCall(pack))
                : pack.Select(rPackInt => Enumerable.Repeat(rPackInt, 1))
                      .SelectMany(before => CallObjectOrDataRunOnSingle(before, pGraph, oCall, dCall)); ;
        }  

        private static IEnumerable<RPackInt> CallObjectOrDataRunOnSingle(IEnumerable<RPackInt> before, GraphIsDataProperty pGraph, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> oCall, Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> dCall)
        {
            foreach (var oCallRPac in oCall(before))
            {
                if (pGraph.IsData == null)
                    pGraph.Set(false);
                yield return oCallRPac;
            }
            if (pGraph.IsData != null && !pGraph.IsData.Value) yield break; // yield break;
            foreach (var dCallRPac in dCall(before))
            {
                if (pGraph.IsData == null) pGraph.Set(true);
                yield return dCallRPac;
            }
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> sPo(object sEntityCode, short pParamIndex, object oEntityCode)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
                .GetObjBySubj(pk.GetE(sEntityCode))
                .Where(po => po.Key == pk.GetE(oEntityCode)) //только один
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    return new RPackInt(pk.row, pk.StoreAbstract);
                }));
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> sPd(object sEntityCode, short pParamIndex, Literal d)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
                .GetDataBySubj(pk.GetE(sEntityCode))
                .Where(po => po.Key.Equals(d.Value)) //только один
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    return new RPackInt(pk.row, pk.StoreAbstract);
                }));
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> sPO(object sEntityCode, short pParamIndex, short oParamIndex, GraphIsDataProperty graph)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
                 .GetObjBySubj(pk.GetE(sEntityCode))
                 .Select(po =>
                 {
                     pk.Set(pParamIndex, po.Value);
                     pk.Set(oParamIndex, po.Key);
                     if (graph.IsData == null || graph.IsData.Value)
                         graph.ReSet(false);
                     return new RPackInt(pk.row, pk.StoreAbstract);
                 }));
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> sPD(object sEntityCode, short pParamIndex, short oParamIndex, GraphIsDataProperty graph)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
                .GetDataBySubj(pk.GetE(sEntityCode))
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    pk.Set(oParamIndex, po.Key);
                    if (graph.IsData == null || !graph.IsData.Value)
                        graph.ReSet(po.Key.vid);
                    return new RPackInt(pk.row, pk.StoreAbstract);
                })); 
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> Spd(short sParamIndex, object pEntityCode, Literal d)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
                .GetSubjectByDataPred(pk.GetE(pEntityCode), d)
                .Select(su =>
                {
                    pk.Set(sParamIndex, su);
                    return new RPackInt(pk.row, pk.StoreAbstract);
                }));
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> SpD(short sParamIndex, object pEntityCode, short oParamIndex)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> SPo(short sParamIndex, short pParamIndex, object oEntityCode)
        {
            return pack => pack.SelectMany(pk => pk.StoreAbstract
                .GetSubjectByObj(pk.GetE(oEntityCode))
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    pk.Set(sParamIndex, po.Key);
                    return new RPackInt(pk.row, pk.StoreAbstract);
                }));
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> SPd(short sParamIndex, short pParamIndex, Literal d)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> SPO(short sParamIndex, short pParamIndex, short oParamIndex)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<RPackInt>, IEnumerable<RPackInt>> SPD(short sParamIndex, short pParamIndex, short oParamIndex)
        {
            throw new NotImplementedException();
        }

    }

}