﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PObjectives
{
    public class Element
    {
        internal Element() { }
        internal Collection inCollection;
        internal PaEntry entry;
        public int Id { get { return (int)entry.Field(1).Get(); } }
        public object Get() { return entry.Field(2).Get(); } //TODO: не проверяется признак уничтоженности. надо ли?
    }
}
