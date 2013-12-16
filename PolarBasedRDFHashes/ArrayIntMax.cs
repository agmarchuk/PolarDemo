using System;

    class ArrayIntMax<T>
    {
        const int MaxArrayIndex = 2146435071;
        const int LastCount = Int32.MaxValue - MaxArrayIndex; 
        private readonly T[] negativeLast, negativeFirst, positiveFirst, positiveLast;


        public ArrayIntMax()
        {
            negativeLast = new T[LastCount];
            negativeFirst = new T[MaxArrayIndex];
            positiveFirst = new T[MaxArrayIndex];
            positiveLast = new T[LastCount];
        }

        public T this[int index]
        {
            get
            {
                return index >= 0
                    ? (index >= MaxArrayIndex ? positiveLast[index - MaxArrayIndex] : positiveFirst[index])
                    : (index <= -MaxArrayIndex ? negativeLast[-index - MaxArrayIndex] : negativeFirst[-index]);
            }
            set
            {
                if (index > 0)
                    if (index >= MaxArrayIndex) positiveLast[index - MaxArrayIndex] = value;
                    else positiveFirst[index]=value;
                else if (index <= -MaxArrayIndex) negativeLast[-index - MaxArrayIndex] = value;
                else negativeFirst[-index]=value;
            }
        }
    }
