using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PObjectives
{
    class Person : Element
    {
        public Person() { }
        public string Name
        {
            get { return (string)entry.Field(2).Field(0).Get(); }
            set
            {
                object[] pvalue = (object[])this.Get();
                pvalue[0] = value;
                inCollection.UpdateElement(this, pvalue);
            }
        }
        public int Age
        {
            get { return (int)entry.Field(2).Field(1).Get(); }
            set
            {
                entry.Field(2).Field(1).Set(value);
            }
        }
        public City City
        {
            get
            {
                int ccod = (int)entry.Field(2).Field(2).Get();
                Database db = this.inCollection.InDatabase;
                var collec = db.Collection("cities");
                var q = collec.Element(ccod).entry;
                return new City() { entry = q };
            }
        }
    }
    public class City : Element
    {
        public string Name
        {
            get { return (string)entry.Field(2).Field(0).Get(); }
            set
            {
                object[] pvalue = (object[])this.Get();
                pvalue[0] = value;
                inCollection.UpdateElement(this, pvalue);
            }
        }
    }
}
