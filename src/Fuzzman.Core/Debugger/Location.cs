using System;
using System.Xml.Serialization;

namespace Fuzzman.Core.Debugger
{
    [Serializable]
    public sealed class Location
    {
        public string ModuleName { get; set; }

        [XmlIgnore]
        public uint Offset { get; set; }

        [XmlElement(ElementName = "Offset")]
        public string HexValue
        {
            get
            {
                return "0x" + this.Offset.ToString("X");
            }
            set
            {
                string temp = value;
                if (temp.StartsWith("0x") || temp.StartsWith("0X"))
                    temp = temp.Substring(2);
                this.Offset = uint.Parse(temp, System.Globalization.NumberStyles.HexNumber);
            }
        }

        public static bool operator ==(Location lhs, Location rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Location lhs, Location rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Location))
                return false;
            Location other = obj as Location;
            return this.ModuleName == other.ModuleName && this.Offset == other.Offset;
        }

        public override int GetHashCode()
        {
            return this.ModuleName.GetHashCode() ^ this.Offset.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format("{0}+0x{1:X8}", this.ModuleName, this.Offset);
        }
    }
}
