using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SingleThread
{
    public class Transaction
    {
        public string OperationID;
        public bool IsVoided;
        public long AmountAuthorized;
        public long AmountVoided;
        public string State;

        [XmlIgnore]
        public byte[] Cryptogram;

        [XmlElement("Cryptogram")]
        public string CryptogramHexString
        {
            get {
                if (Cryptogram == null) return null;
                return BitConverter.ToString(Cryptogram);
            }
            set {
                var hexString = value?.Replace("-", "").Replace(" ", "");
                if (!string.IsNullOrEmpty(hexString))
                {
                    Cryptogram = Enumerable.Range(0, hexString.Length)
                       .Where(x => x % 2 == 0)
                       .Select(x => Convert.ToByte(hexString.Substring(x, 2), 16))
                       .ToArray();
                }
                else { Cryptogram = null; }
            }
        }
    }
}
