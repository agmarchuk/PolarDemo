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
            Console.WriteLine("Start test. Wait appoximately 10 sec. ");
            DateTime tt0 = DateTime.Now;
            // Тестовый тип данных
            PType tp_seq_seq = new PTypeSequence(new PTypeSequence(new PType(PTypeEnumeration.integer)));
            PaCell cell = new PaCell(tp_seq_seq, path + "serbuftest.pac", false);
            cell.Clear();

            ISerialFlow input;

            input = cell; // Ввод без буфера
            GenerateSerialTestFlow(900000, input);
            Console.WriteLine("======Fill without buffer ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            cell.Clear();
            input = new SerialBuffer(cell, 2); // Ввод с буфером
            GenerateSerialTestFlow(900000, input);
            Console.WriteLine("======Fill with buffer ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            cell.Close();
            System.IO.File.Delete(path + "serbuftest.pac");
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
