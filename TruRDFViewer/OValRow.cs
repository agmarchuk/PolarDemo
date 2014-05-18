namespace TruRDFViewer
{
    public enum OValEnumeration
    {
        obj, val, unknown
    }
    public class OVal
    {
        public OValEnumeration vid;
        public Literal lit = null; // Если значение. null - неопределено 
        public bool odefined = false; // Значение Entity задано или вычислено
        public int entity;
        public long spo_start; // Начальный индекс диапазона значений entity в отсортированных по spo объектных триплетах
        public long spo_number = -1; // Число значений в диапазоне. -1 - диапазон не вычислялся
        public long op_start; // То же самое для сортировки op
        public long op_number = -1;
        public long spd_start; // Начальный индекс диапазона для отсортированных по sp триплетов данных
        public long spd_number = -1;
        public override string ToString()
        {
            if (vid == OValEnumeration.obj) return "<" + entity + ">";
            else if (vid == OValEnumeration.val) return lit.ToString();
            else return "unknown";
        }
    }
    public class OValRowInt
    {
        public OVal[] row;
        private TripleStore ts;
        public TripleStore Store { get { return ts; } }
        public OValRowInt(TripleStore ts, OVal[] row)
        {
            this.row = row;
            this.ts = ts;
        }
        public OVal Get(short ind) { return row[ind]; }
        public void Set(short ind, OVal oval) { row[ind] = oval; }

    }
}
