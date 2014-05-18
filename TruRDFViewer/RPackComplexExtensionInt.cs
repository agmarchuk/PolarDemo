using System;
using System.Collections.Generic;
using System.Linq;

namespace TruRDFViewer
{
    public static class RPackComplexExtension
    {
        public static IEnumerable<RPack> OptionalGroup(this IEnumerable<RPack> pack, Func<IEnumerable<RPack>, IEnumerable<RPack>> group, params short[] changedVariables)
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

        public static Func<IEnumerable<RPack>, IEnumerable<RPack>> Optional(this Func<IEnumerable<RPack>, IEnumerable<RPack>> graphSelector, short parametersStartIndex, short parametersEndIndex)
        {
            return packs => packs.SelectMany(pack => Optional(pack, graphSelector, parametersStartIndex, parametersEndIndex));
        }

        private static IEnumerable<RPack> Optional(RPack pack, Func<IEnumerable<RPack>, IEnumerable<RPack>> graphSelector, short parametersStartIndex, short parametersEndIndex)
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


        public static Func<IEnumerable<RPack>, IEnumerable<RPack>> Union(
            this Func<IEnumerable<RPack>, IEnumerable<RPack>> first, Func<IEnumerable<RPack>, IEnumerable<RPack>> second)
        {
            return packs =>
            //{
              packs.SelectMany(p=> UnionRun(p, first, second));
                //var packsArray = packs;// as RPackInt[] ?? packs.ToArray();
                //return first(packsArray).Concat(second(packsArray));
            //};
        }

        private static IEnumerable<RPack> UnionRun(RPack pack, Func<IEnumerable<RPack>, IEnumerable<RPack>> first, Func<IEnumerable<RPack>, IEnumerable<RPack>> second)
        {
            var rPackIntBox = Enumerable.Repeat(pack, 1);
            if (first != null)
                foreach (var resultRPackInt in first(rPackIntBox))
                yield return resultRPackInt;
            if (second == null) yield break;
            foreach (var resultRPackInt in second(rPackIntBox))
                yield return resultRPackInt;
        }

        public static IEnumerable<RPack> Union(this IEnumerable<RPack> pack,
            params Func<IEnumerable<RPack>, IEnumerable<RPack>>[] groupFuncs)
        {
            return groupFuncs.SelectMany(func => func(pack));
        }

        public static Func<IEnumerable<RPack>, IEnumerable<RPack>> spo(object subj, object pred, object obj)
        {
            return pacs => pacs.Where(pk => pk.Store.ChkOSubjPredObj(pk.GetE(subj), pk.GetE(pred), pk.GetE(obj)));
        }

        public static Func<IEnumerable<RPack>, IEnumerable<RPack>> spd(object subj, object pred, Literal d)
        {
            return pacs => pacs.Where(pk => pk.Store.GetDataBySubjPred(pk.GetE(subj), pk.GetE(pred)).Any(d1=>d1.Equals(d)));
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> spO(object sEntityCode, object pEntityCode, short obj)
        {
            return pack => pack.SelectMany(pk => pk.Store
                .GetObjBySubjPred(pk.GetE(sEntityCode), pk.GetE(pEntityCode))
                .Select(ob =>
                {
                    pk.Set(obj, ob);
                    return new RPack(pk.row, pk.Store);
                }));
        }
        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> Spo(short subj, object pEntityCode, object oEntityCode)
        {
          return pack => pack.SelectMany(pk => pk.Store
                .GetSubjectByObjPred(pk.GetE(oEntityCode), pk.GetE(pEntityCode))
                .Select(su =>
                {
                    pk.Set(subj, su);
                    return new RPack(pk.row, pk.Store);
                }));
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> SpO(short s, object pEntityCode, short o)
        {
            throw new NotImplementedException();
        }

        public static Func<IEnumerable<RPack>, IEnumerable<RPack>> spD(object sEntityCode, object pEntityCode, short oParamIndex)
        {
            return pack => pack.SelectMany(pk => pk.Store
             .GetDataBySubjPred(pk.GetE(sEntityCode), pk.GetE(pEntityCode))
             .Select(ob =>
             {
                 pk.Set(oParamIndex, ob);
                 return new RPack(pk.row, pk.Store);
             }));
        }

        private static IEnumerable<RPack> CallObjectAndData(
            Func<IEnumerable<RPack>, IEnumerable<RPack>> oCall,
            Func<IEnumerable<RPack>, IEnumerable<RPack>> dCall, 
            IEnumerable<RPack> pack)
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
        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> CallObjectAndData(
            Func<IEnumerable<RPack>, IEnumerable<RPack>> oCall,
            Func<IEnumerable<RPack>, IEnumerable<RPack>> dCall)
        {
            return pack => CallObjectAndData(oCall, dCall, pack);
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> CallObjectOrData(GraphIsDataProperty pGraph, Func<IEnumerable<RPack>, IEnumerable<RPack>> oCall, Func<IEnumerable<RPack>, IEnumerable<RPack>> dCall)
        {
            return pack => pGraph.IsData != null
                ? (pGraph.IsData.Value ? dCall(pack) : oCall(pack))
                : pack.Select(rPackInt => Enumerable.Repeat(rPackInt, 1))
                      .SelectMany(before => CallObjectOrDataRunOnSingle(before, pGraph, oCall, dCall)); ;
        }  

        private static IEnumerable<RPack> CallObjectOrDataRunOnSingle(IEnumerable<RPack> before, GraphIsDataProperty pGraph, Func<IEnumerable<RPack>, IEnumerable<RPack>> oCall, Func<IEnumerable<RPack>, IEnumerable<RPack>> dCall)
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

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> sPo(object sEntityCode, short pParamIndex, object oEntityCode)
        {
            return pack => pack.SelectMany(pk => pk.Store
                .GetObjBySubj(pk.GetE(sEntityCode))
                .Where(po => po.Key == pk.GetE(oEntityCode)) //только один
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    return new RPack(pk.row, pk.Store);
                }));
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> sPd(object sEntityCode, short pParamIndex, Literal d)
        {
            return pack => pack.SelectMany(pk => pk.Store
                .GetDataBySubj(pk.GetE(sEntityCode))
                .Where(po => po.Key.Equals(d.Value)) //только один
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    return new RPack(pk.row, pk.Store);
                }));
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> sPO(object sEntityCode, short pParamIndex, short oParamIndex, GraphIsDataProperty graph)
        {
            return pack => pack.SelectMany(pk => pk.Store
                 .GetObjBySubj(pk.GetE(sEntityCode))
                 .Select(po =>
                 {
                     pk.Set(pParamIndex, po.Value);
                     pk.Set(oParamIndex, po.Key);
                     if (graph.IsData == null || graph.IsData.Value)
                         graph.ReSet(false);
                     return new RPack(pk.row, pk.Store);
                 }));
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> sPD(object sEntityCode, short pParamIndex, short oParamIndex, GraphIsDataProperty graph)
        {
            return pack => pack.SelectMany(pk => pk.Store
                .GetDataBySubj(pk.GetE(sEntityCode))
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    pk.Set(oParamIndex, po.Key);
                    if (graph.IsData == null || !graph.IsData.Value)
                        graph.ReSet(po.Key.vid);
                    return new RPack(pk.row, pk.Store);
                })); 
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> Spd(short sParamIndex, object pEntityCode, Literal d)
        {
            return pack => pack.SelectMany(pk => pk.Store
                .GetSubjectByDataPred(pk.GetE(pEntityCode), d)
                .Select(su =>
                {
                    pk.Set(sParamIndex, su);
                    return new RPack(pk.row, pk.Store);
                }));
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> SpD(short sParamIndex, object pEntityCode, short oParamIndex)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> SPo(short sParamIndex, short pParamIndex, object oEntityCode)
        {
            return pack => pack.SelectMany(pk => pk.Store
                .GetSubjectByObj(pk.GetE(oEntityCode))
                .Select(po =>
                {
                    pk.Set(pParamIndex, po.Value);
                    pk.Set(sParamIndex, po.Key);
                    return new RPack(pk.row, pk.Store);
                }));
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> SPd(short sParamIndex, short pParamIndex, Literal d)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> SPO(short sParamIndex, short pParamIndex, short oParamIndex)
        {
            throw new NotImplementedException();
        }

        internal static Func<IEnumerable<RPack>, IEnumerable<RPack>> SPD(short sParamIndex, short pParamIndex, short oParamIndex)
        {
            throw new NotImplementedException();
        }

    }

}