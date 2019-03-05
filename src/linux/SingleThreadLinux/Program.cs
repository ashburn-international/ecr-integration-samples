using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using ActiveXConnectLib;

namespace SingleThread
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new ECR().Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Press enter to exit...");
                Console.ReadLine();
            }
        }
    }

    class ECR
    {
        Integration API = new Integration();
        Random random = new Random();

        public void Run()
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Welcome to example ECR implementation");
                Console.WriteLine("1. Begin new tender");
                Console.WriteLine("2. Void transaction");
                Console.WriteLine("3. Close day");
                Console.WriteLine("4. GSM purchase");
                Console.WriteLine("To exit press enter without entering anything...");
                Console.WriteLine();

                switch (Console.ReadLine())
                {
                    case "":
                        return;

                    case "1":
                        {
                            NewTender();

                            Console.WriteLine("Trying to close document that was used for tender...");
                            bool result = AttemptToCloseUnclosedDocuments();
                            // we ignore result because it will not be error if it fails because 
                            // document will be closed after next purchase or before close day
                        }
                        break;

                    case "2":
                        {
                            Document d;
                            Document.Transaction t;

                            if (VoidMenu(out d, out t))
                            {
                                PerformVoid(d, t);
                            }
                        }
                        break;

                    case "3":
                        {
                            if (CloseDay())
                            {
                                Console.WriteLine("Day was closed");
                            }
                            else
                            {
                                Console.WriteLine("Failed to close day");
                            }
                        }
                        break;

                    case "4":
                        {
                            GsmPurchase();
                        }
                        break;

                    default:
                        Console.WriteLine("Unknown option");
                        break;
                }
            }
        }

        private void GsmPurchase()
        {
            Document doc = new Document();
            doc.GenerateNewUniqueDocumentNr();

            // It is very important that we "save" document even before we try any GSM operation. 
            // This way we can guarantee that docclosed will be sent even if computer power is lost / application crashes or some other fatal error occurs.
            DocumentManager.SaveDocument(doc);

            string errorText;
            Console.WriteLine("Enter GSM barcode to be queried");
            string data = Console.ReadLine();

            if (API.GsmPurchase(out errorText, data, doc) <= 0)
            {
                Console.WriteLine("GSM purchase failed: " + errorText);
            }
            else
            {
                Console.WriteLine("GSM purchase succeeded");
            }

            AttemptToCloseUnclosedDocuments();
        }

        private void PerformVoid(Document document, Document.Transaction transaction)
        {
            if (!API.UnlockDevice("999", "John", "VOID", 0, ""))
            {
                Console.WriteLine("Failed to unlock device. Void will not be performed");
                return;
            }

            string errorText;
            if (API.VoidTransaction(transaction.OperationID, out errorText))
            {
                transaction.IsVoided = true;
                DocumentManager.SaveDocument(document);

                Console.WriteLine("Transaction was voided");
            }
            else
            {
                Console.WriteLine("Failed to void transaction: " + errorText);
            }
        }

        private bool VoidMenu(out Document chosenDocument, out Document.Transaction chosenTransaction)
        {
            Dictionary<int, KeyValuePair<Document, Document.Transaction>> transactionNumberLookup = new Dictionary<int, KeyValuePair<Document, Document.Transaction>>();
            int currentTransactionId = 0;

            foreach (Document document in DocumentManager.GetAllDocuments())
            {
                if (document.IsClosed())
                {
                    foreach (Document.Transaction transaction in document.Transactions)
                    {
                        if (!transaction.IsVoided)
                        {
                            transactionNumberLookup[++currentTransactionId] = new KeyValuePair<Document, Document.Transaction>(document, transaction);
                            Console.WriteLine("{0}. Document: {1} OperationId: {2}", currentTransactionId, document.DocumentNr, transaction.OperationID);
                        }
                    }
                }
            }

            Console.WriteLine("Please enter transaction number to be voided");

            int chosenTransactionNumber;
            if (!Int32.TryParse(Console.ReadLine(), out chosenTransactionNumber))
            {
                Console.WriteLine("Invalid transaction number");
            }

            KeyValuePair<Document, Document.Transaction> item;
            if (transactionNumberLookup.TryGetValue(chosenTransactionNumber, out item))
            {
                chosenDocument = item.Key;
                chosenTransaction = item.Value;
                return true;
            }

            chosenDocument = null;
            chosenTransaction = null;
            return false;
        }

        private void NewTender()
        {
            Document doc = new Document();
            doc.GenerateNewUniqueDocumentNr();

            // It is very important that we "save" document even before we try authorize operation. 
            // This way we can guarantee that docclosed will be sent even if computer power is lost / application crashes or some other fatal error occurs.
            DocumentManager.SaveDocument(doc);

            while (true)
            {
                PerformPayment(doc);

                Console.WriteLine("Do you want to make another payment to this tender (Y/N)?");
                switch (Console.ReadLine().ToUpper())
                {
                    case "Y":
                        continue;
                    case "N":
                        return;
                }
            }
        }

        private void PerformPayment(Document doc)
        {
            Console.WriteLine("Enter amount you want to pay to this tender (in minor currency)");
            long amountToPay = long.Parse(Console.ReadLine());

            if (!API.UnlockDevice("999", "John", "AUTHORIZE", amountToPay, "Please insert or swipe card"))
            {
                Console.WriteLine("Failed to unlock device, payment cannot be performed");
                return;
            }

            string err;
            string operationID;

            long amountAuthorized = API.Authorize(amountToPay, doc.DocumentNr, "978", out err, out operationID);
            if (amountAuthorized == 0)
            {
                Console.WriteLine("Authorization failed. Error {0}", err);
            }
            else if (amountAuthorized == amountToPay)
            {
                Console.WriteLine("Authorized sucessfully {0}", amountAuthorized);
                // Add operation ID to document to make sure it will be confirmed                
                doc.Transactions.Add(new Document.Transaction() { OperationID = operationID });
                // Save document so power outage or other fatal failure will not cause transaction to be reversed
                DocumentManager.SaveDocument(doc);
            }
            else
            {
                // it is possible that only part of requested sum will be authorized
                // ECR must decide what to do with this. Treat it as error? Or let customer pay remaining 
                // amount with other card or cash?
                // In this example we treat it as authorization decline and reverse the transaction
                Console.WriteLine("Authorization failed. Error {0}", err);
            }

            if (!API.LockDevice("Welcome"))
            {
                Console.WriteLine("Warning: unlock after tender failed");
            }
        }

        private bool CloseDay()
        {
            if (!AttemptToCloseUnclosedDocuments())
            {
                Console.WriteLine("Cannot perform close day because one of the documents was not closed");
                return false;
            }
            else
                return API.CloseDay("999", "John");
        }

        private bool AttemptToCloseUnclosedDocuments()
        {
            foreach (Document document in DocumentManager.GetAllDocuments())
            {
                if (!document.IsClosed())
                {
                    if (API.CloseDocument(document))
                    {
                        document.ChangeStateToClosed();
                        DocumentManager.SaveDocument(document);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    class Integration
    {
        /// <summary>
        /// Cached COM object instance
        /// </summary>
        IActiveXConnectAPI _api = null;

        /// <summary>
        /// Gets COM object instance. 
        /// If object is already created then return it. Else attempt to create it.        
        /// </summary>
        /// <returns>API com object instance to be used. If COM object creation fails return null.</returns>
        IActiveXConnectAPI GetAPI()
        {
            if (_api == null)
            {
                _api = new AXC();
                _api.Initialize(new string[] { "ReceiptControlSymbolsNotSupported", "OnDisplayTextEventSupported", "OnMessageBoxEventSupported" });
                if (_api.OperationResult != "OK")
                {
                    _api.Dispose();
                    _api = null;
                    return null;
                }

                return _api;
            }
            else
            {
                return _api;
            }
        }

        public bool CloseDay(string operatorID, string operatorName)
        {
            IActiveXConnectAPI a = GetAPI();

            if (a == null)
                return false;

            if (!UnlockDevice("999", "John", "", 0, ""))
            {
                Console.WriteLine("Failed to unlock device");
                return false;
            }

            Console.WriteLine("CloseDay method called");
            a.CloseDay(operatorID, operatorName);

            Console.WriteLine("Waiting for OnCloseDayResult event...");
            if (!ProcessEventsUntil(a, "OnCloseDayResult", 130))
                return false;

            string result = a.OperationResult;
            Console.WriteLine("OnCloseDayResult received with result={0}", result);
            return result == "OK";
        }

        public bool CloseDocument(Document document)
        {
            Console.WriteLine("Closing document {0}", document.DocumentNr);

            IActiveXConnectAPI a = GetAPI();

            if (a == null)
                return false;

            List<string> operationsID = new List<string>();

            if (document.Transactions != null)
            {
                foreach (var i in document.Transactions)
                {
                    operationsID.Add(i.OperationID);
                }
            }

            a.DocClosed(document.DocumentNr, operationsID.ToArray());
            Console.WriteLine("DocClosed method called, waiting for OnDocClosedResult event...");

            if (!ProcessEventsUntil(a, "OnDocClosedResult", 15))
            {
                Console.WriteLine("Timeout waiting for OnDocClosedResult");
                return false;
            }

            string result = a.OperationResult;
            Console.WriteLine("OnDocClosedResult event received with OperationResult={0}", result);
            return result == "OK";
        }

        public bool LockDevice(string text)
        {
            IActiveXConnectAPI a = GetAPI();

            if (a == null)
            {
                return false;
            }

            a.LockDevice(text);

            if (!ProcessEventsUntil(a, "OnLockDeviceResult", 15) || a.OperationResult != "OK")
            {
                return false;
            }

            return true;
        }

        public bool UnlockDevice(string operatorID, string operatorName, string operation, long amount, string text)
        {
            IActiveXConnectAPI a = GetAPI();

            if (a == null)
            {
                return false;
            }

            a.UnlockDevice(text, "EN", operatorID, operatorName, amount, "1.0", operation, 0);

            if (!ProcessEventsUntil(a, "OnUnlockDeviceResult", 15) || a.OperationResult != "OK")
            {
                return false;
            }

            return true;
        }

        public long Authorize(long amount, string documentNr, string currencyCode, out string errorText, out string operationID)
        {
            errorText = null;
            operationID = null;

            IActiveXConnectAPI a = GetAPI();

            if (a == null)
            {
                errorText = "Failed to load API library";
                return 0;
            }

            Console.WriteLine("Waiting for OnCard event (insert card)...");
            while (true)
            {
                if (!ProcessEventsUntil(a, "OnCard", 30))
                {
                    errorText = "Timeout waiting for card to be inserted into device";
                    return 0;
                }

                if (ContainsFlag(a.Flags, "AllowAuthorize"))
                {
                    Console.WriteLine("CARD is payment card");
                    break;
                }
                else
                {
                    Console.WriteLine("This is not payment card, most probably it's loyalty/discount card, so calculate discount/loyalty points here... and wait for next CARD event");
                }
            }

            if (a.CurrencyCode != currencyCode)
            {
                errorText = String.Format("Currency '{0}' code is not supported", a.CurrencyCode);
                return 0;
            }

            string lastFourDigits = null;

            if (ContainsFlag(a.Flags, "ReqLastFourDigits"))
            {
                Console.WriteLine("Please enter last 4 digits of the card");
                lastFourDigits = Console.ReadLine();
            }

            a.Authorize(amount, 0, documentNr, lastFourDigits);
            Console.WriteLine("Authorize method called, waiting for OnAuthorizeResult event...");
            if (!ProcessEventsUntil(a, "OnAuthorizeResult", 100))
            {
                errorText = "Timeout waiting for OnAuthorizeResult";
                return 0;
            }

            if (a.OperationResult != "OK")
            {
                errorText = a.Text;
                return 0;
            }

            operationID = a.OperationID;

            if (a.AmountAuthorized == 0)
            {
                errorText = "Authorization declined";
            }

            return a.AmountAuthorized;
        }

        private bool ContainsFlag(Array array, string flag)
        {
            if (array == null)
                return false;

            foreach (string s in array)
            {
                if (s == flag)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Processes events until event of expected type is received.        
        /// </summary>
        /// <param name="api">COM object</param>
        /// <param name="expectedEventType">Event type to wait for. When this event is received method returns true</param>
        /// <param name="timeoutInSeconds">Timeout in seconds to wait for expected event</param>
        /// <returns>True if expected event was received before timeout. Otherwise returns false.</returns>
        private bool ProcessEventsUntil(IActiveXConnectAPI api, string expectedEventType, int timeoutInSeconds)
        {
            Stopwatch sw = Stopwatch.StartNew();
            api.PollEvent(200);

            string ev = api.EventType;
            while (ev != expectedEventType)
            {
                switch (ev)
                {
                    case "OnPrompt":
                        {
                            Console.WriteLine(api.PromptText);
                            string input = Console.ReadLine();

                            api.Input(input);
                            if (!ProcessEventsUntil(api, "OnInputResult", 15))
                                return false;

                            if (api.OperationResult != "OK")
                                return false;
                        }
                        break;

                    case "OnPrint":
                        {
                            api.GetNextReceipt();
                            while (!String.IsNullOrEmpty(api.ReceiptText))
                            {
                                Console.WriteLine("Printing received receipt:");
                                Console.WriteLine("ReceiptIsClientData: {0}", api.ReceiptIsClientData);
                                Console.WriteLine("ReceiptIsMerchantCopy: {0}", api.ReceiptIsMerchantCopy);
                                Console.WriteLine("Document nr: {0}", api.ReceiptDocumentNr);

                                Console.WriteLine("--------------------------");
                                Console.WriteLine(api.ReceiptText);
                                Console.WriteLine("--------------------------");

                                api.GetNextReceipt();
                            }
                        }
                        break;

                    case "OnSelect":
                        {
                            Console.WriteLine(api.Text);

                            string input = Console.ReadLine();
                            api.Input(input);
                            if (!ProcessEventsUntil(api, "OnInputResult", 15))
                                return false;

                            if (api.OperationResult != "OK")
                                return false;
                        }
                        break;

                    case "OnDisplayText":
                        {
                            Console.WriteLine("--------------");
                            Console.WriteLine(api.DisplayText);
                            Console.WriteLine("--------------");
                        }
                        break;

                    case "OnMessageBox":
                        {
                            Console.WriteLine("-----------------");
                            Console.WriteLine(api.MessageBoxText);
                            Console.WriteLine("-----------------");

                            string[] validButtons = null;
                            switch (api.MessageBoxType)
                            {
                                case "Ok":
                                    validButtons = new[] { "Ok" };
                                    break;
                                case "YesNo":
                                    validButtons = new[] { "Yes", "No" };
                                    break;
                                case "OkCancel":
                                    validButtons = new[] { "Ok", "Cancel" };
                                    break;
                                case "YesNoCancel":
                                    validButtons = new[] { "Yes", "No", "Cancel" };
                                    break;
                            }

                            Console.WriteLine("Please choose on of following buttons:");
                            for (int i = 0; i < validButtons.Length; i++)
                            {
                                Console.WriteLine("{0} {1}", i + 1, validButtons[i]);
                            }

                            int index;
                            if (!Int32.TryParse(Console.ReadLine(), out index))
                            {
                                Console.WriteLine("You must enter a number from the list above");
                                return false;
                            }
                            index--;
                            if (index < 0 || index >= validButtons.Length)
                            {
                                Console.WriteLine("You must enter a number from the list above");
                                return false;
                            }
                            else
                            {
                                api.Click(validButtons[index]);
                                ProcessEventsUntil(api, "OnClickResult", 15);
                            }
                        }
                        break;

                    default:
                        {
                            if (!String.IsNullOrEmpty(ev))
                                Console.WriteLine("Ignoring event: {0}", ev);
                        }
                        break;
                }

                if (sw.ElapsedMilliseconds > timeoutInSeconds * 1000)
                    return false;

                api.PollEvent((int)(timeoutInSeconds * 1000 - sw.ElapsedMilliseconds));
                ev = api.EventType;
            }

            return true;
        }

        public bool VoidTransaction(string operationID, out string errorText)
        {
            errorText = null;

            IActiveXConnectAPI a = GetAPI();
            if (a == null)
            {
                errorText = "Failed to load API library...";
                return false;
            }

            a.Void(operationID);

            if (!ProcessEventsUntil(a, "OnVoidResult", 100))
            {
                errorText = "Timeout waiting for OnVoidResult";
                return false;
            }

            if (a.OperationResult != "OK")
            {
                errorText = a.Text;
                return false;
            }

            return true;
        }

        public long GsmPurchase(out string errorText, string data, Document document)
        {
            errorText = null;
            var a = GetAPI();

            if (a == null)
            {
                errorText = "Failed to load API library";
                return 0;
            }

            // TODO: do i need unlock here?

            a.Query(data, document.DocumentNr);
            if (!ProcessEventsUntil(a, "OnQueryResult", 100))
            {
                errorText = "Error when sending Query";
                return 0;
            }

            string opResult = a.OperationResult;
            if (opResult != "OK")
            {
                errorText = "OnQueryResult returned: " + opResult + ". Error text: " + a.Text;
                return 0;
            }

            if (a.AmountAuthorized > 0)
            {
                document.Transactions.Add(new Document.Transaction() { OperationID = a.OperationID });
                DocumentManager.SaveDocument(document);
            }

            return a.AmountAuthorized;
        }
    }

    class DocumentManager
    {
        public static List<Document> GetAllDocuments()
        {
            List<Document> result = new List<Document>();

            foreach (string f in Directory.GetFiles(".", "*.docXml", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (f.EndsWith(".docXml"))
                    {
                        Document d = Document.Deserialize(File.ReadAllText(f));
                        result.Add(d);
                    }
                }
                catch { } // Ignore document that cannot be deserialized
            }

            return result;
        }

        public static void SaveDocument(Document doc)
        {
            File.WriteAllText(doc.DocumentNr + ".docXml", doc.Serialize());
        }

        public static void DeleteDocument(Document doc)
        {
            string filename = doc.DocumentNr + ".docXml";
            if (File.Exists(filename))
                File.Delete(filename);
        }
    }

    public class Document
    {
        public string DocumentNr = null;
        public List<Transaction> Transactions = new List<Transaction>();

        public States State;

        public enum States
        {
            ToBeReversed,
            Reversed,
            ToBeConfirmed,
            Confirmed
        }

        public void GenerateNewUniqueDocumentNr()
        {
            DocumentNr = Guid.NewGuid().ToString("N").Substring(0, 20);
        }

        public string Serialize()
        {
            XmlSerializer ser = new XmlSerializer(typeof(Document));
            System.IO.StringWriter s = new System.IO.StringWriter();
            ser.Serialize(s, this);
            s.Flush();
            string xml = s.ToString();
            return xml;
        }

        public static Document Deserialize(string data)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Document));
            return (Document)ser.Deserialize(new System.IO.StringReader(data));
        }

        public class Transaction
        {
            public string OperationID;
            public bool IsVoided;
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

