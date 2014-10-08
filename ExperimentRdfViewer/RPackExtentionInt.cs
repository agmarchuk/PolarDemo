using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;
using SparqlParseRun;

namespace TrueRdfViewer
{
    public static class RPackExtentionInt
    {
        public static IEnumerable<OValRowInt> _Spo(this IEnumerable<OValRowInt> rows, short subj, short pred, short obj)
        {
            // Определены объект и предикат, нужно найти множество субъектов, побочным эффектом будет определение 
            // и фиксация диапазона. Если диапазон уже есть, то диапазон не вычисляется, а используется
            var query = rows.SelectMany(rw =>
            {
                var row = rw.row;
                OVal obj_oval = row[obj];
                Diapason diap = new Diapason();
                if (obj_oval.spo_number >= 0) 
                { // Диапазон определен
                    diap.start = obj_oval.spo_start;
                    diap.numb = obj_oval.spo_number;
                } 
                else 
                {
                    Diapason di = rw.StoreAbstract.GetDiap_op(obj_oval.entity);
                    diap.start = obj_oval.spo_start = di.start;
                    diap.numb = obj_oval.spo_number = di.numb;
                }
                return rw.StoreAbstract.GetSubjInDiapason(diap, row[pred].entity)
                    .Select(su => 
                    {
                        row[subj].entity = su; row[subj].op_number = -1; row[subj].spd_number = -1; row[subj].spo_number = -1;
                        return new OValRowInt(rw.StoreAbstract, row);
                    });
            });
            return query;
        }
        public static IEnumerable<OValRowInt> _spo(this IEnumerable<OValRowInt> rows, short subj, short pred, short obj)
        {
            var query = rows.Where(ovr => 
            {
                OVal[] row = ovr.row;
                // будем "плясать" от субъекта. TODO: наверное можно как-то задействовать и объектную цепочку  
                OVal subj_oval = row[subj];
                OVal pred_oval = row[pred];
                OVal obj_oval = row[obj];
                // Проверим через шкалу
                if (!ovr.StoreAbstract.Scale.ChkInScale(subj_oval.entity, pred_oval.entity, obj_oval.entity)) return false;
                Diapason diap = new Diapason();
                if (subj_oval.spo_number >= 0)
                { // Диапазон определен
                    diap.start = subj_oval.spo_start;
                    diap.numb = subj_oval.spo_number;
                }
                else
                {
                    Diapason di = ovr.StoreAbstract.GetDiap_spo(subj_oval.entity);
                    diap.start = subj_oval.spo_start = di.start;
                    diap.numb = subj_oval.spo_number = di.numb;
                }

                return ovr.StoreAbstract.CheckPredObjInDiapason(diap, ovr.row[pred].entity, ovr.row[obj].entity);
            });
            return query;
        }
        // Определение всех данных по заданным s-p 
        public static IEnumerable<OValRowInt> _spD(this IEnumerable<OValRowInt> rows, short subj, short pred, short dat)
        {
            var query = rows.SelectMany(ovr =>
            {
                OVal[] row = ovr.row;
                OVal subj_oval = row[subj];
                OVal pred_oval = row[pred];
                OVal dat_oval = row[dat];
                Diapason diap = new Diapason();
                if (subj_oval.spd_number >= 0)
                { // Диапазон определен
                    diap.start = subj_oval.spd_start;
                    diap.numb = subj_oval.spd_number;
                }
                else
                {
                    Diapason di = ovr.StoreAbstract.GetDiapason_spd(subj_oval.entity);
                    diap.start = subj_oval.spd_start = di.start;
                    diap.numb = subj_oval.spd_number = di.numb;
                }
                return ovr.StoreAbstract.GetDatInDiapason(diap, row[pred].entity)
                    .Select(lit =>
                    {
                        row[dat].lit = lit;
                        return new OValRowInt(ovr.StoreAbstract, row);
                    });
            });
            return query;
        }

        // для следующих методов subj, pred, obj или short индекс или целое значение закодированного Entity
        public static IEnumerable<QueryNodesSet> spo(this IEnumerable<QueryNodesSet> pack, object subj, object pred, object obj)
        {
            //subj = P(subj); pred = P(pred); obj = P(obj);
            return pack.Where(pk => pk.StoreAbstract.ChkOSubjPredObj(pk.GetE(subj), pk.GetE(pred), pk.GetE(obj)));
        }
        public static IEnumerable<QueryNodesSet> Spo(this IEnumerable<QueryNodesSet> pack, object subj, object pred, object obj)
        {
            if (!(subj is short)) throw new Exception("subject must be an index");
            //pred = P(pred); obj = P(obj);
            return pack.SelectMany(pk => pk.StoreAbstract
                .GetSubjectByObjPred(pk.GetE(obj), pk.GetE(pred))
                .Select(su =>
                {
                    pk.Set(subj, su);
                    return new QueryNodesSet(pk.row, pk.StoreAbstract);
                }));
        }
        public static IEnumerable<QueryNodesSet> spO(this IEnumerable<QueryNodesSet> pack, object subj, object pred, object obj)
        {
            if (!(obj is short)) throw new Exception("object must be an index");
            //subj = P(subj); pred = P(pred);
            return pack.SelectMany(pk => pk.StoreAbstract
                .GetObjBySubjPred(pk.GetE(subj), pk.GetE(pred))
                .Select(ob =>
                {
                    pk.Set(obj, ob);
                    return new QueryNodesSet(pk.row, pk.StoreAbstract);
                }));
        }
        public static IEnumerable<QueryNodesSet> spD(this IEnumerable<QueryNodesSet> pack, object subj, object pred, object dat)
        {
            if (!(dat is short)) throw new Exception("data must be an index");
            //subj = P(subj); pred = P(pred);
            return pack.SelectMany(pk => pk.StoreAbstract
                .GetDataBySubjPred(pk.GetE(subj), pk.GetE(pred))
                .Select(da =>
                {
                    pk.Set(dat, da); //((Text)da.value).s);
                    return new QueryNodesSet(pk.row, pk.StoreAbstract);
                }));
        }
    }
}