using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SingleThread
{
    class ECR
    {
        const string DEFAULT_CURRENCY = "978";

        Integration API = new Integration();
        Random random = new Random();

        public void Run()
        {
            var choice = "";
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("---------------------------");
                Console.WriteLine("    Main Menu");
                Console.WriteLine("---------------------------");
                Console.WriteLine(" 1. New tender");
                Console.WriteLine(" 2. Void transaction");
                Console.WriteLine(" 3. Close day");
                Console.WriteLine(" 4. GSM purchase");
                Console.WriteLine(" 5. Partial void");
                Console.WriteLine(" 6. PreAuthorization");
                Console.WriteLine(" 7. Increment PreAuthorization");
                Console.WriteLine(" 8. Complete PreAuthorization");
                Console.WriteLine(" 9. Get PreAuthorization state");
                Console.WriteLine(" X. Exit");
                Console.WriteLine("");

                choice = ConsoleGui.EnterValue<string>("Select a task").ToUpper();
                switch (choice)
                {
                    case "Q":
                    case "X":
                    case "QUIT":
                    case "EXIT":
                        return;

                    case "1":
                        {
                            NewTender();

                            ConsoleGui.Info("Trying to close document that was used for tender...");
                            bool result = AttemptToCloseUnclosedDocuments();
                            // we ignore result because it will not be error if it fails because 
                            // document will be closed after next purchase or before close day
                        }
                        break;

                    case "2":
                        PerformVoid();
                        break;

                    case "3":
                        CloseDay();
                        break;

                    case "4":
                        GsmPurchase();
                        break;

                    case "5":
                        PerformVoidPartial();
                        break;

                    case "6":
                        NewTenderPreAuthorize();
                        break;

                    case "7":
                        PreIncrementGui();
                        break;

                    case "8":
                        PreCompleteGui();
                        break;

                    case "9":
                        PerformGetTranState();
                        break;

                    case "LIST DOC":
                        PickDocuments();
                        break;
                        

                    case "LIST DOC ALL":
                        PickDocuments(ShowCloseds: true);
                        break;

                    case "LIST":
                    case "LIST TRAN":
                        {
                            PickTransaction(ShowClosedDocs:true, ShowVoidedTrans:true);
                            break;
                        }

                    case "?":
                    case "H":
                    case "HELP":
                        Help();
                        break;

                    default:
                        ConsoleGui.Warning("Unknown option");
                        break;
                }
            }
        }

        private void GsmPurchase()
        {
            var doc = new Document();

            // It is very important that we "save" document even before we try any GSM operation. 
            // This way we can guarantee that docclosed will be sent even if computer power is lost / application crashes or some other fatal error occurs.
            DocumentManager.SaveDocumentToFile(doc);

            string errorText;
            
            string data = ConsoleGui.EnterValue<string>("Enter GSM barcode to be queried");

            if (API.GsmPurchase(out errorText, data, doc) <= 0)
            {
                ConsoleGui.Error ("GSM purchase failed: " + errorText);
            }
            else
            {
                ConsoleGui.Ok("GSM purchase succeeded");
            }

            AttemptToCloseUnclosedDocuments();
        }

        private void PerformVoid()
        {
            ConsoleGui.Info($"\n*** VOID");
            var selectedItem = PickTransaction(
                Prompt: "Select transaction"
                );

            var document = selectedItem.SelectedDocument;
            var transaction = selectedItem.SelectedTransaction;

            if(transaction == null)
            {
                ConsoleGui.Warning("No transaction selected");
                return;
            }

            if (!API.UnlockDevice("999", "John", "VOID", 0, ""))
            {
                ConsoleGui.Error("Failed to unlock device. Void will not be performed");
                return;
            }

            string errorText;
            if (API.VoidTransaction(transaction.OperationID, out errorText))
            {
                transaction.IsVoided = true;
                DocumentManager.SaveDocumentToFile(document);

                ConsoleGui.Ok("Transaction was voided");
            }
            else
            {
                ConsoleGui.Error($"Failed to void transaction. {errorText}");
            }
        }

        private void PerformVoidPartial()
        {
            ConsoleGui.Info($"\n*** PARTIALVOID");
            var selectedItem = PickTransaction(
                Prompt: "Select transaction to be voided"
                );

            var transaction = selectedItem.SelectedTransaction;
            var document = selectedItem.SelectedDocument;

            if(transaction == null)
            {
                ConsoleGui.Warning("No transaction selected");
                return;
            }

            var voidAmount = ConsoleGui.EnterValue<long>("Enter amount to be voided");

            if (!API.UnlockDevice("999", "John", "VOID", 0, ""))
            {
                ConsoleGui.Error("Failed to unlock device. Void will not be performed");
                return;
            }

            string errorText;
            if (API.VoidPartialTransaction(transaction.OperationID, transaction.AmountAuthorized, voidAmount, out errorText))
            {
                if (transaction.AmountAuthorized == voidAmount)
                {
                    transaction.IsVoided = true;
                    transaction.AmountVoided = transaction.AmountAuthorized;
                }
                else
                {
                    transaction.AmountAuthorized -= voidAmount;
                    transaction.AmountVoided += voidAmount;
                }

                DocumentManager.SaveDocumentToFile(document);

                ConsoleGui.Ok("Transaction was voided");
            }
            else
            {
                ConsoleGui.Error($"Failed to void transaction: {errorText}");
            }
        }

        private void PerformGetTranState()
        {
            string errorText;
            ConsoleGui.Info($"\n*** GETTRANSTATE");
            var selectedObjects = PickTransaction(
                Prompt: "Select transaction",
                ShowClosedDocs: true,
                PreAuthOnly: true
                );

            var document = selectedObjects.SelectedDocument;
            var transaction = selectedObjects.SelectedTransaction;

            if(transaction == null)
            {
                ConsoleGui.Warning("No transaction selected");
                return;
            }

            ConsoleGui.Info($"Selected {document.DocumentNr} (Amount:{transaction.AmountAuthorized} OperationId:{transaction.OperationID})");
            ConsoleGui.Info($"Calling GetTranState method...");
            if (API.GetTranState(transaction.OperationID, transaction.Cryptogram, out errorText, out var state, out var resultCryptogram))
            {
                if (!"Undefined".Equals(state)) transaction.State = state;
                transaction.Cryptogram = resultCryptogram;
                DocumentManager.SaveDocumentToFile(document);

                ConsoleGui.Ok($"Transaction state: {state}");
            }
            else
            {
                ConsoleGui.Error("Failed to get transaction state. " + errorText);
            }
        }

        private void NewTender()
        {
            var doc = new Document();

            // It is very important that we "save" document even before we try authorize operation. 
            // This way we can guarantee that docclosed will be sent even if computer power is lost / application crashes or some other fatal error occurs.
            DocumentManager.SaveDocumentToFile(doc);

            while (true)
            {
                PerformPayment(doc);

                var YN = ConsoleGui.EnterValue<string>("Do you want to make another payment to this tender", new string[] { "Y", "N" });
                if (YN == "N") break;
            }
        }

        private void PerformPayment(Document doc)
        {
            var amountToPay = ConsoleGui.EnterValue<long>("Enter amount you want to pay to this tender (in minor currency)");

            if (!API.UnlockDevice("999", "John", "AUTHORIZE", amountToPay, "Please insert or swipe card"))
            {
                ConsoleGui.Error("Failed to unlock device, payment cannot be performed");
                return;
            }

            string err;
            string operationID;

            long amountAuthorized = API.Authorize(amountToPay, doc.DocumentNr, DEFAULT_CURRENCY, out err, out operationID);
            if (amountAuthorized == 0)
            {
                ConsoleGui.Error($"Authorization failed. {err}");
            }
            else if (amountAuthorized == amountToPay)
            {
                ConsoleGui.Ok($"Authorized sucessfully {amountAuthorized}");
                // Add operation ID to document to make sure it will be confirmed                
                doc.Transactions.Add(new Transaction() { OperationID = operationID, AmountAuthorized = amountAuthorized });
                // Save document so power outage or other fatal failure will not cause transaction to be reversed
                DocumentManager.SaveDocumentToFile(doc);
            }
            else
            {
                // it is possible that only part of requested sum will be authorized
                // ECR must decide what to do with this. Treat it as error? Or let customer pay remaining 
                // amount with other card or cash?
                // In this example we treat it as authorization decline and reverse the transaction
                ConsoleGui.Error($"Authorization failed. {err}");
            }

            if (!API.LockDevice($"Lock from {AppDomain.CurrentDomain.FriendlyName}"))
            {
                ConsoleGui.Error("Warning: unlock after tender failed");
            }
        }


        private void NewTenderPreAuthorize()
        {
            var doc = new Document();
            DocumentManager.SaveDocumentToFile(doc);

            ConsoleGui.Info($"*** PREAUTHORIZATION {doc.DocumentNr}");

            PerformPaymentPreAuthorize(doc);
            TryCloseDocument(doc);
        }

        void PreIncrementGui()
        {
            ConsoleGui.Info($"\n*** PREINCREMENT");
            var selectedObjects = PickTransaction(
                Prompt: "Select transaction",
                ShowClosedDocs:true,
                PreAuthOnly:true,
                TransactionStates: new string[] { "Authorizing", "Approved", "Timedout "}
                );

            var transaction = selectedObjects.SelectedTransaction;
            var document = selectedObjects.SelectedDocument;

            if (transaction == null)
            {
                ConsoleGui.Warning("No transaction selected");
                return;
            }

            ConsoleGui.Info($"Selected {transaction.OperationID} (amount:{transaction.AmountAuthorized} state:{transaction.State})");
            var amountToInc = ConsoleGui.EnterValue<long>("Enter new amount (in minor currency)");
            if (amountToInc < transaction.AmountAuthorized)
            {
                ConsoleGui.Error("Entered amount is less than transactions");
                return;
            }

            var authorizedAmount = API.PreIncrement(
                amountToInc, 
                DEFAULT_CURRENCY, 
                transaction.OperationID, 
                transaction.Cryptogram, 
                out var NewState,
                out var NewCryptogram,
                out var errorText
                );

            if (!"Undefined".Equals(NewState)) transaction.State = NewState;
            if (authorizedAmount > 0) transaction.AmountAuthorized = authorizedAmount;
            if (NewCryptogram.Length > 0) transaction.Cryptogram = NewCryptogram;
            DocumentManager.SaveDocumentToFile(document);

            if (authorizedAmount == amountToInc)
            {
                ConsoleGui.Ok("PreIncrement Success");
            }
            else
            {
                ConsoleGui.Error($"PreIncrement Failed. {errorText}");
            }
        }

        void PreCompleteGui()
        {
            ConsoleGui.Info($"\n*** PRECOMPLETE");
            var selectedObjects = PickTransaction(
                Prompt: "Select transaction",
                ShowClosedDocs: true,
                PreAuthOnly: true,
                TransactionStates: new string[] { "Authorizing", "Approved", "Timedout " }
                );

            var transaction = selectedObjects.SelectedTransaction;
            var document = selectedObjects.SelectedDocument;

            if (transaction == null)
            {
                ConsoleGui.Warning("No transaction selected");
                return;
            }

            ConsoleGui.Info($"Selected {transaction.OperationID} (amount:{transaction.AmountAuthorized} state:{transaction.State})");
            var amount = ConsoleGui.EnterValue<long>("Enter amount (in minor currency)");
            
            if (amount > transaction.AmountAuthorized)
            {
                ConsoleGui.Error("Entered amount must be equal or less than transaction amount");
                return;
            }

            var authorizedAmount = API.PreComplete(
                amount,
                DEFAULT_CURRENCY,
                transaction.OperationID,
                transaction.Cryptogram,
                out var NewState,
                out var NewCryptogram,
                out var errorText
                );

            if (!"Undefined".Equals(NewState)) transaction.State = NewState; 
            if (authorizedAmount > 0) transaction.AmountAuthorized = authorizedAmount;
            if (NewCryptogram.Length > 0) transaction.Cryptogram = NewCryptogram;
            DocumentManager.SaveDocumentToFile(document);

            if (authorizedAmount == amount)
            {
                ConsoleGui.Info("PreComplete Success");
            }
            else
            {
                ConsoleGui.Info($"PreComplete Failed. {errorText}");
            }
        }


        public void TryCloseDocument(Document document)
        {
            if (!document.IsClosed())
            {
                if (API.CloseDocument(document))
                {
                    document.ChangeStateToClosed();
                    DocumentManager.SaveDocumentToFile(document);
                }
            }
        }

        private void PerformPaymentPreAuthorize(Document doc)
        {
            var amountToPay = ConsoleGui.EnterValue<long>("Enter amount you want to pay to this tender (in minor currency)");

            if (!API.UnlockDevice("999", "John", "PREAUTHORIZE", amountToPay, "Please insert or swipe card"))
            {
                ConsoleGui.Error("Failed to unlock device, payment cannot be performed");
                return;
            }

            string err;
            string operationID;
            byte[] cryptogram;

            long amountAuthorized = API.PreAuthorize(
                amountToPay, 
                doc.DocumentNr, 
                DEFAULT_CURRENCY, 
                out err, 
                out operationID, 
                out var state, 
                out cryptogram);

            if (amountAuthorized == 0)
            {
                ConsoleGui.Error($"PreAuthorization failed. {err}");
            }
            else if (amountAuthorized == amountToPay)
            {
                ConsoleGui.Ok($"PreAuthorized sucessfully for amount:{amountAuthorized}");
                // Add operation ID to document to make sure it will be confirmed                
                doc.Transactions.Add(new Transaction() {
                    OperationID = operationID,
                    AmountAuthorized = amountAuthorized,
                    Cryptogram = cryptogram,
                    State = state
                });
                // Save document so power outage or other fatal failure will not cause transaction to be reversed
                doc.State = Document.States.ToBeConfirmed;
                DocumentManager.SaveDocumentToFile(doc);
            }
            else
            {
                // it is possible that only part of requested sum will be authorized
                // ECR must decide what to do with this. Treat it as error? Or let customer pay remaining 
                // amount with other card or cash?
                // In this example we treat it as authorization decline and reverse the transaction
                ConsoleGui.Error($"PreAuthorization failed. {err}");
            }

            if (!API.LockDevice($"Lock from {AppDomain.CurrentDomain.FriendlyName}"))
            {
                ConsoleGui.Warning("Warning: unlock after tender failed");
            }
        }

        private bool CloseDay()
        {
            if (!AttemptToCloseUnclosedDocuments())
            {
                ConsoleGui.Error("Cannot perform close day because one of the documents was not closed");
                return false;
            }

            if(API.CloseDay("999", "John"))
            {
                ConsoleGui.Ok("Day was closed");
                return true;
            }
            else
            {
                ConsoleGui.Error("Day was NOT closed");
                return false;
            }
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
                        DocumentManager.SaveDocumentToFile(document);
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        Document PickDocuments(bool ShowCloseds = false, string Prompt = null)
        {
            const int MAX_ROWS = 20;
            var docs = new Dictionary<int, Document>();

            Console.WriteLine();
            Console.WriteLine("  #   DocumentNr   State         Amount     Tran-s   CreatedOn          CreatedBy");
            Console.WriteLine("---   ----------   -----------   --------   ------   ----------------   ----------");

            var documents = DocumentManager.GetAllDocuments();
            int num = 0;
            foreach (var document in documents.OrderBy(d => d.CreatedOn))
            {
                var state = document.State.ToString();
                if (state.Length > 12) { state = state.Substring(0, 12); }

                var tr = document.Transactions?.Count>0 ? document.Transactions[0] : null;
                var IsClosed = document.IsClosed();//tr?.IsVoided == true;

                if (ShowCloseds || !IsClosed)
                {
                    if (++num > MAX_ROWS)
                    {
                        ConsoleGui.Warning($"Showing first {MAX_ROWS} documents");
                        break;
                    }
                    Console.WriteLine($"{num,3}   {document.DocumentNr}       {state,-12}  {tr?.AmountAuthorized,8}   {document.Transactions?.Count,6}   {document.CreatedOn:yyyy-MM-dd HH:mm}   {document.CreatedBy}");
                    docs[num] = document;
                }
            }

            if(!string.IsNullOrEmpty(Prompt))
            {
                var selectedId = ConsoleGui.EnterValue<int>(Prompt);
                if (docs.ContainsKey(selectedId)) return docs[selectedId];
            }
            return null;
        }

        (Document SelectedDocument, Transaction SelectedTransaction) PickTransaction(
            string Prompt = null,
            bool PreAuthOnly = false,
            bool ShowVoidedTrans = false,
            bool ShowClosedDocs = false,
            string[] TransactionStates = null
            )
        {
            const int MAX_ROWS = 20;
            var trans = new Dictionary<int, (Document Doc, Transaction Tran)>();
            
            var documents = DocumentManager.GetAllDocuments();
            int num = 0;

            var q = (from d in documents
                     from t in d.Transactions
                     orderby d.CreatedOn
                     where
                        (ShowClosedDocs || d.IsClosed()) &&
                        (!PreAuthOnly || d.IsPreauth()) &&
                        (TransactionStates == null || TransactionStates.Contains(t.State))
                     select new
                     {
                         Doc = d,
                         Tran = t
                     }
                     );


            if (q.Count() > MAX_ROWS)
                ConsoleGui.Info($"Transactions count: {q.Count()}. Showing first {MAX_ROWS}");

            Console.WriteLine();
            Console.WriteLine("  #   DocumentNr   DocState       TranState     Amount     OperationId         CreatedOn          CreatedBy");
            Console.WriteLine("---   ----------   ------------   -----------   --------   -----------------   ----------------   ----------");

            foreach (var t in q)
            {
                if (++num > MAX_ROWS) break;

                Console.WriteLine($"{num,3}   {t.Doc.DocumentNr}       {t.Doc.State,-12}   {t.Tran.State,-12}  {t.Tran.AmountAuthorized,8}   {t.Tran.OperationID,-17}   {t.Doc.CreatedOn:yyyy-MM-dd HH:mm}   {t.Doc.CreatedBy}");
                trans[num] = (Doc: t.Doc, Tran: t.Tran);
            }

            if (!string.IsNullOrEmpty(Prompt))
            {
                var selectedId = ConsoleGui.EnterValue<int>(Prompt);
                if (trans.ContainsKey(selectedId))
                {
                    return trans[selectedId];
                }
            }
            return (null, null);
        }

        void Help()
        {
            Console.WriteLine(@"*** Available commands:
list doc       - list not closed documents
list doc all   - list all documents
list tran      - list transactions
");
        }
    }
}

