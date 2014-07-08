using System;
using PolarDB;

namespace ScaleBit4Check
{
    public class ScaleCell
    {
      
        private PaCell oscale;
        private bool filescale = true;
        private int range = 0;
        private Scale1 scale = null;

       

        public ScaleCell(PaCell oscale)
        {
            this.oscale = oscale;
            if (!oscale.IsEmpty)
            {
                CalculateRange();
            }
        
        }

        public ScaleCell(string path)
            :this(new PaCell(new PTypeSequence(new PType(PTypeEnumeration.integer)), path + "oscale.pac", false))
        {
            
        }

        public PaCell Cell
        {
            
            get { return oscale; }
        }

        public bool Filescale
        {
            get { return filescale; }
        }

        public int Range
        {
            get { return range; }
        }

        public Scale1 Scale1
        {
            get { return scale; }
        }

        public void CalculateRange()
        {
            long len = oscale.Root.Count() - 1;
            int r = 1;
            while (len != 0) { len = len >> 1; r++; }
            range = r + 4;
        }

        public void CreateScale(PaCell otriples)
        {
            long len = otriples.Root.Count() - 1;
            int r = 1;
            while (len != 0) { len = len >> 1; r++; }

            range = r + 2; //r + 4; // здесь 4 - фактор "разрежения" шкалы, можно меньше
            scale = new Scale1(range);
            foreach (object[] tr in otriples.Root.ElementValues())
            {
                int subj = (int)tr[0];
                int pred = (int)tr[1];
                int obj = (int)tr[2];
                int code = Scale1.Code(range, subj, pred, obj);
                scale[code] = 1;
            }
        }

        public void ShowScale(long ntriples)
        {
            int c = scale.Count();
            int c1 = 0;
            for (int i=0; i<c; i++)
            {
                int bit = scale[i];
                if (bit > 0) c1++;
            }
            Console.WriteLine("{0} {1} {2}", c, c1, ntriples);
        }

        public bool ChkInScale(int subj, int pred, int obj)
        {
            if (range > 0)
            {
                int code = Scale1.Code(range, subj, pred, obj);
                int bit;
                if (filescale)
                {
                    int word = (int)oscale.Root.Element(Scale1.GetArrIndex(code)).Get();
                    bit = Scale1.GetFromWord(word, code);
                }
                else // if (memoryscale)
                {
                    bit = scale[code];
                }
                if (bit == 0) return false;
            }
            return true;
        }

        public void WriteScale(PaCell otriples)
        {
            if (Filescale)
            {
                // Создание шкалы (Надо переделать)
                CreateScale(otriples);
                //ShowScale();
                Cell.Clear();
                Cell.Fill(new object[0]);
                foreach (int v in Scale1.Values()) Cell.Root.AppendElement(v);
                Cell.Flush();
                CalculateRange(); // Наверное, range считается в CreateScale() 
            }
        }

       

        public void WarmUp()
        {
            foreach (var elementValue in this.Cell.Root.ElementValues()) ;
        }

        public void Clear()
        {
                      Cell.Clear();
            Cell.Fill(new object[0]);
        }

        public void Flush()
        {
            Cell.Flush();
        }
    }
}