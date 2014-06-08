using System.Collections.Generic;

namespace ScaleBit4Check
{
    public class Scale1
    {
        // ���� �����, �.�. ����� ������� � �����, ����. ��� �����, ���������� 1024 ������� ��������, ���� ����� 10  
        private int range;
        public int[] arr;
        public Scale1(int range)
        {
            this.range = range;
            arr = new int[1 << (range - 5)];
        }
        public int this[int ind]
        {
            get
            {
                return (arr[ind >> 5] >> (ind & 31)) & 1;
            }
            set
            {
                arr[ind >> 5] = arr[ind >> 5] & (~(1 << (ind & 31))) | ((value & 1) << (ind & 31));
            }
        }
        // ������ ��� ��������� �������� ����� �� �������� ������� ����� (����� ������������������ �����)
        public static int GetArrIndex(int ind) { return ind >> 5; }
        public static int GetFromWord(int w, int ind) { return (w >> (ind & 31)) & 1; }

        public static int Code(int range, int subj, int pred, int obj)
        {
            return (subj ^ pred ^ obj) & ((1 << range) - 1);
        }
        public int Count() { return 1 << range; }
        public IEnumerable<int> Values()
        {
            return arr;
        }
    }
}