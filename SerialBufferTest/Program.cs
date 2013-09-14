using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace SerialBufferTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path = @"..\..\..\Databases\";
            Console.WriteLine("Start");
            DateTime tt0 = DateTime.Now;
            // Тестовый тип данных
            PType tp_seq_seq = new PTypeSequence(new PTypeSequence(new PType(PTypeEnumeration.integer)));
            PaCell cell = new PaCell(tp_seq_seq, path + "serbuftest.pac", false);
            cell.Clear();
            var input = new SerialBuffer(cell, 3);
            GenerateSerialTestFlow(9000000, input);
            cell.Flush();
            Console.WriteLine("======Fill ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

        }
        // Генератор тестовых данных
        private static void GenerateSerialTestFlow(int volume, ISerialFlow receiver)
        {
            receiver.StartSerialFlow();
            receiver.S();
            for (int ii = 0; ii < volume; ii++)
            {
                receiver.S();
                for (int jj = 0; jj < 3; jj++)
                {
                    receiver.V(jj);
                }
                receiver.Se();
            }
            receiver.Se();
            receiver.EndSerialFlow();
        }
    }
}
