using System;
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
            PType tp_int = new PType(PTypeEnumeration.integer);
            PType tp_seq_int = new PTypeSequence(new PType(PTypeEnumeration.integer));
            PType tp_seq_seq = new PTypeSequence(new PTypeSequence(new PType(PTypeEnumeration.integer)));
            // Тестирование буфера. Новый релиз 20130929
            GenerateSerialTestFlow(10, new SerialBuffer(new SerialFlowReceiverConsole(tp_seq_seq), 0));


            return;


            PaCell cell = new PaCell(tp_seq_seq, path + "serbuftest.pac", false);
            cell.Clear();

            ISerialFlow input;

            input = cell; // Ввод без буфера
            GenerateSerialTestFlow(900000, input);
            Console.WriteLine("======Fill without buffer ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            cell.Clear();
            input = new SerialBuffer(cell, 4); // Ввод с буфером
            GenerateSerialTestFlow(900000, input);
            Console.WriteLine("======Fill with buffer ok. duration=" + (DateTime.Now - tt0).Ticks / 10000L); tt0 = DateTime.Now;

            cell.Close();
            //System.IO.File.Delete(path + "serbuftest.pac");
        }
        // Генератор тестовых данных
        private static void GenerateSerialTestFlow(int volume, ISerialFlow receiver)
        {
            receiver.StartSerialFlow();
            receiver.S();
            for (int ii = 0; ii < volume; ii++)
            {
                receiver.S();
                int num = ii % 1000 == 0 ? 100 : 1;
                receiver.V(ii);
                for (int jj = 0; jj < num; jj++)
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
