using System;
using System.Collections.Generic;
using System.Linq;
using PolarDB;
using TripleIntClasses;

namespace TrueRdfViewer
{
    public class RPackInt : ICloneable
    {
        //public bool result;
        public object[] row;
        private readonly TripleStoreInt ts;
        public TripleStoreInt Store { get { return ts; } }

        public RPackInt(object[] row, TripleStoreInt ts)
        {
            this.row = row;
            this.ts = ts;
        }
        public string Get(object si)
        {
            if (!(si is short)) 
                return TripleInt.DecodeEntities((int)si);
            var index = (short)si;
            var literal = (row[index] as Literal);
            if (literal != null) return literal.ToString();
            else return TripleInt.DecodeEntities((int) row[index]);
        }

        public int GetE(object si)
        {
            return si is short ? (int)row[(short)si] : (int)si;
        }

        public bool Hasvalue(short si)
        {
            return
                (row[si] is int && row[si] != (object)Int32.MinValue)
                || (row[si] is Literal &&
                    ((Literal)row[si]).HasValue);

        }
        public Literal Val(short ind)
        {
            return (Literal)row[ind];
        }
        public double Vai(short ind)
        {
            Literal lit = (Literal)row[ind];
            //if (lit.vid != LiteralVidEnumeration.integer) throw new Exception("Wrong literal vid in Vai method");
            return (double)lit.Value;
        }
        public void Set(object si, object valu)
        {
            if (!(si is short)) throw new Exception("argument must be an index");
            short ind = (short)si;
            row[ind] = valu;
        }

        public RPackInt ResetDiapason(short parametersStartIndex, short parametersEndIndex)
        {
            for (short i = parametersStartIndex; i < parametersEndIndex; i++)
                Reset(i);
            return this;
        }
        public void Reset(short si)
        {
            if (row[si] == null) return;
            if (row[si] is int)
                row[si] = int.MinValue;
            else
            {
                var oldliteral = row[si] as Literal;
                if (oldliteral == null) throw new Exception(); //return;
                var newLiteral = new Literal(oldliteral.vid);
                row[si] = newLiteral;
                switch (oldliteral.vid)
                {
                    case LiteralVidEnumeration.integer:
                        newLiteral.Value = 0.0;
                        break;
                    case LiteralVidEnumeration.typedObject:
                        newLiteral.Value = new TypedObject();
                        break;
                    case LiteralVidEnumeration.text:
                        newLiteral.Value = new Text { };
                        break;
                    case LiteralVidEnumeration.date:
                        newLiteral.Value = DateTime.MinValue.ToBinary();
                        break;
                    case LiteralVidEnumeration.boolean:
                        newLiteral.Value = false;
                        break;
                    case LiteralVidEnumeration.nil:
                        break;
                }
            }
        }

        public object this[short index]
        {
            get { return row[index]; }
            set
            {
                row[index] = value;
            }
        }
        object ICloneable.Clone()
        {
            var newRow = new object[row.Length];
            for (int i = 0; i < newRow.Length; i++)
            {
                var literal = row[i] as Literal;
                if (literal != null)
                {
                    var value = literal.Value;
                    if (value is Text)
                        newRow[i] = new Literal(literal.vid) { Value = ((ICloneable)value).Clone() };
                    else if (value is TypedObject)
                        newRow[i] = new Literal(literal.vid) { Value = ((ICloneable)value).Clone() };
                    else
                        newRow[i] = new Literal(literal.vid) { Value = value };
                }
                else
                    newRow[i] = row[i];
            }
            return new RPackInt(newRow, ts);
        }
    }

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
                    Diapason di = rw.Store.GetDiap_op(obj_oval.entity);
                    diap.start = obj_oval.spo_start = di.start;
                    diap.numb = obj_oval.spo_number = di.numb;
                }
                return rw.Store.GetSubjInDiapason(diap, row[pred].entity)
                    .Select(su => 
                    {
                        row[subj].entity = su; row[subj].op_number = -1; row[subj].spd_number = -1; row[subj].spo_number = -1;
                        return new OValRowInt(rw.Store, row);
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
                if (!ovr.Store.scale.ChkInScale(subj_oval.entity, pred_oval.entity, obj_oval.entity)) return false;
                Diapason diap = new Diapason();
                if (subj_oval.spo_number >= 0)
                { // Диапазон определен
                    diap.start = subj_oval.spo_start;
                    diap.numb = subj_oval.spo_number;
                }
                else
                {
                    Diapason di = ovr.Store.GetDiap_spo(subj_oval.entity);
                    diap.start = subj_oval.spo_start = di.start;
                    diap.numb = subj_oval.spo_number = di.numb;
                }

                return ovr.Store.CheckPredObjInDiapason(diap, ovr.row[pred].entity, ovr.row[obj].entity);
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
                    Diapason di = ovr.Store.GetDiapason_spd(subj_oval.entity);
                    diap.start = subj_oval.spd_start = di.start;
                    diap.numb = subj_oval.spd_number = di.numb;
                }
                return ovr.Store.GetDatInDiapason(diap, row[pred].entity)
                    .Select(lit =>
                    {
                        row[dat].lit = lit;
                        return new OValRowInt(ovr.Store, row);
                    });
            });
            return query;
        }

        // для следующих методов subj, pred, obj или short индекс или целое значение закодированного Entity
        public static IEnumerable<RPackInt> spo(this IEnumerable<RPackInt> pack, object subj, object pred, object obj)
        {
            //subj = P(subj); pred = P(pred); obj = P(obj);
            return pack.Where(pk => pk.Store.ChkOSubjPredObj(pk.GetE(subj), pk.GetE(pred), pk.GetE(obj)));
        }
        public static IEnumerable<RPackInt> Spo(this IEnumerable<RPackInt> pack, object subj, object pred, object obj)
        {
            if (!(subj is short)) throw new Exception("subject must be an index");
            //pred = P(pred); obj = P(obj);
            return pack.SelectMany(pk => pk.Store
                .GetSubjectByObjPred(pk.GetE(obj), pk.GetE(pred))
                .Select(su =>
                {
                    pk.Set(subj, su);
                    return new RPackInt(pk.row, pk.Store);
                }));
        }
        public static IEnumerable<RPackInt> spO(this IEnumerable<RPackInt> pack, object subj, object pred, object obj)
        {
            if (!(obj is short)) throw new Exception("object must be an index");
            //subj = P(subj); pred = P(pred);
            return pack.SelectMany(pk => pk.Store
                .GetObjBySubjPred(pk.GetE(subj), pk.GetE(pred))
                .Select(ob =>
                {
                    pk.Set(obj, ob);
                    return new RPackInt(pk.row, pk.Store);
                }));
        }
        public static IEnumerable<RPackInt> spD(this IEnumerable<RPackInt> pack, object subj, object pred, object dat)
        {
            if (!(dat is short)) throw new Exception("data must be an index");
            //subj = P(subj); pred = P(pred);
            return pack.SelectMany(pk => pk.Store
                .GetDataBySubjPred(pk.GetE(subj), pk.GetE(pred))
                .Select(da =>
                {
                    pk.Set(dat, da); //((Text)da.value).s);
                    return new RPackInt(pk.row, pk.Store);
                }));
        }
    }
}