using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SingleThread
{
    public class Document
    {
        public string DocumentNr { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedIn { get; set; }
        public States State { get; set; }

        public List<Transaction> Transactions = new List<Transaction>();

        

        public enum States
        {
            ToBeReversed,
            Reversed,
            ToBeConfirmed,
            Confirmed
        }

        public Document()
        {
            DocumentNr = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            CreatedOn = DateTime.Now;
            CreatedBy = Environment.UserName;
            CreatedIn = Environment.MachineName;
        }

        
        public string ToXml()
        {
            XmlSerializer ser = new XmlSerializer(typeof(Document));
            System.IO.StringWriter s = new System.IO.StringWriter();
            ser.Serialize(s, this);
            s.Flush();
            string xml = s.ToString();
            return xml;
        }

        public static Document FromXml(string data)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Document));
            return (Document)ser.Deserialize(new System.IO.StringReader(data));
        }

        

        public bool IsClosed()
        {
            switch (State)
            {
                case States.Confirmed:
                case States.Reversed:
                    return true;
                case States.ToBeConfirmed:
                case States.ToBeReversed:
                    return false;
                default:
                    throw new Exception("Unknown state");
            }
        }

        public bool IsPreauth()
        {
            foreach (var transaction in Transactions)
            {
                if (transaction.Cryptogram?.Length > 0)
                    return true;
            }

            return false;
        }

        public void ChangeStateToClosed()
        {
            switch (State)
            {
                case States.ToBeConfirmed:
                    State = States.Confirmed;
                    break;
                case States.ToBeReversed:
                    State = States.Reversed;
                    break;
            }
        }
    }
}
