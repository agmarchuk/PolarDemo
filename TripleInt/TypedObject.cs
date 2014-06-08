using System;

namespace TripleIntClasses
{
    public class TypedObject : ICloneable
    {
        protected bool Equals(TypedObject other)
        {
            return string.Equals(Value, other.Value) && string.Equals(Type, other.Type);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ (Type != null ? Type.GetHashCode() : 0);
            }
        }

        public string Value { get; set; }
        public string Type { get; set; }
        public override string ToString()
        {
            string result = "\"" + Value + "\"";
            if (!string.IsNullOrWhiteSpace(Type))
                result += "^^" + Type;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TypedObject)obj);
        }

        public object Clone()
        {
            return new TypedObject() { Type = Type, Value = Value };
        }
    }
}