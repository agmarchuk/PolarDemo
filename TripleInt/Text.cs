using System;

namespace TripleIntClasses
{
    public class Text : ICloneable
    {
        protected bool Equals(Text other)
        {
            return string.Equals(Value, other.Value) && string.Equals(Lang, other.Lang);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (Lang != null ? Lang.GetHashCode() : 0);
            }
        }

        public string Value { get; set; }
        public string Lang { get; set; }
        public override string ToString()
        {
            string result = "\"" + Value + "\"";
            if (!string.IsNullOrWhiteSpace(Lang))
                result += "@" + Lang;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Text)obj);
        }

        public object Clone()
        {
            return new Text() { Lang = Lang, Value = Value };

        }
    }
}