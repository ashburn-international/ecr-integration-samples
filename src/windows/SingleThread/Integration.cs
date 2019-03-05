using ActiveXConnectLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SingleThread
{
    class Integration
    {
        /// <summary>
        /// Cached COM object instance
        /// </summary>
        ActiveXConnectAPI _api = null;

        /// <summary>
        /// Gets COM object instance. 
        /// If object is already created then return it. Else attempt to create it.        
        /// </summary>
        /// <returns>API com object instance to be used. If COM object creation fails return null.</returns>
        ActiveXConnectAPI GetAPI()
        {
            if (_api == null)
            {
                _api = new ActiveXConnectAPI();
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
            ActiveXConnectAPI api = GetAPI();

            if (api == null)
                return false;

            if (!UnlockDevice("999", "John", "", 0, ""))
            {
                ConsoleGui.Error("Failed to unlock device");
                return false;
            }

            ConsoleGui.Info("CloseDay method called");
            api.CloseDay(operatorID, operatorName);

            if (!ProcessEventsUntil(api, "OnCloseDayResult", 130))
                return false;

            string result = api.OperationResult;
            ConsoleGui.Info($"OnCloseDayResult received with result={result}");
            return result == "OK";
        }

        public bool CloseDocument(Document document)
        {
            ConsoleGui.Info($"Closing document {document.DocumentNr}");

            ActiveXConnectAPI api = GetAPI();

            if (api == null)
                return false;

            var operationIdList = new List<string>();

            if (document.Transactions != null)
            {
                foreach (var i in document.Transactions)
                {
                    operationIdList.Add(i.OperationID);
                }
            }

            ConsoleGui.Info("Calling DocClosed() method...");
            api.DocClosed(document.DocumentNr, operationIdList.ToArray());

            if (!ProcessEventsUntil(api, "OnDocClosedResult", 15))
            {
                return false;
            }

            string result = api.OperationResult;
            if (result == "OK")
            {
                ConsoleGui.Ok($"OnDocClosedResult event received with OperationResult:{result}");
            }
            else
            {
                ConsoleGui.Error($"OnDocClosedResult event received with OperationResult:{result}");
            }

            return result == "OK";
        }

        public bool LockDevice(string text)
        {
            ActiveXConnectAPI a = GetAPI();

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
            ActiveXConnectAPI a = GetAPI();

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

            ActiveXConnectAPI a = GetAPI();

            if (a == null)
            {
                errorText = "Failed to load API library";
                return 0;
            }

            ConsoleGui.Info("*** Please insert card");
            while (true)
            {
                if (!ProcessEventsUntil(a, "OnCard", 30))
                {
                    errorText = "Timeout waiting for card to be inserted into device";
                    return 0;
                }

                if (ContainsFlag(a.Flags, "AllowAuthorize"))
                {
                    ConsoleGui.Ok("CARD is payment card");
                    break;
                }
                else
                {
                    ConsoleGui.Warning("This is not payment card, most probably it's loyalty/discount card, so calculate discount/loyalty points here... and wait for next CARD event");
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
                lastFourDigits = ConsoleGui.EnterValue<string>("Please enter last 4 digits of the card");
            }

            ConsoleGui.Info("calling Authorize() method...");
            a.Authorize(amount, 0, documentNr, lastFourDigits);
            
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


        public long PreAuthorize(
            long amount, 
            string documentNr, 
            string currencyCode, 
            out string errorText, 
            out string operationID, 
            out string state,
            out byte[] cryptogram)
        {
            errorText = null;
            operationID = null;
            cryptogram = null;
            state = null;

            var api = GetAPI();

            if (api == null)
            {
                errorText = "Failed to load API library";
                return 0;
            }


            var IsCardValid = WaitForCard(
                api: api, 
                requiredFlag:"AllowPreauthorize", 
                ExpectedCardCurrency: currencyCode,
                lastFourDigits: out var lastFourDigits,
                errorText: out errorText);

            if(!IsCardValid) return 0;

            api.PreAuthorize(amount, documentNr, lastFourDigits);
            ConsoleGui.Info("PreAuthorize method called");

            var IsTranStateReceived = WaitTranState(api, documentNr, null, null, out errorText);
            if (!IsTranStateReceived) return 0;

            if (api.DocumentNr != documentNr)
            {
                errorText = String.Format($"DocumentNr mismatch. Expecting:[{documentNr}] received:[{api.DocumentNr}]");
                return 0;
            }

            ConsoleGui.Info($"State:[{api.State}] CryptLenght:[{api.Cryptogram?.Length}]");

            if (api.OperationResult != "OK")
            {
                errorText = api.Text;
                return 0;
            }

            operationID = api.OperationID;
            state = api.State;
            cryptogram = api.Cryptogram.ToBytes();

            if (api.AmountAuthorized == 0)
            {
                errorText = "PreAuthorization declined";
            }

            return api.AmountAuthorized;
        }


        /*
        void PreIncrement(long Amount, string OperationID, Array Cryptogram);
        void PreComplete(long Amount, string OperationID, Array Cryptogram);
        void GetTranState(string DocumentNr, string OperationID, Array Cryptogram);
        */
        public long PreIncrement(
            long NewAmount, 
            string CurrencyCode, 
            string OperationId, 
            byte[] Cryptogram, 
            out string NewState,
            out byte[] NewCryptogram,
            out string errorText)
        {
            errorText = null;
            NewState = null;
            NewCryptogram = null;

            var api = GetAPI();

            if (api == null)
            {
                errorText = "Failed to load API library";
                return 0;
            }

            // No Card needed for PreIncrement
            //var IsCardValid = WaitForCard(
            //    api: api,
            //    requiredFlag: "AllowPreauthorize",
            //    ExpectedCardCurrency: CurrencyCode,
            //    lastFourDigits: out var lastFourDigits,
            //    errorText: out errorText);
            //if (!IsCardValid) return 0;

            api.PreIncrement(NewAmount, OperationId, Cryptogram);
            ConsoleGui.Info($"PreIncrement() method called");

            var IsTranStateReceived = WaitTranState(api, null, OperationId, Cryptogram, out errorText);
            if (!IsTranStateReceived) return 0;

            var operationResult = api.OperationResult;
            if (operationResult != "OK")
            {
                errorText = api.Text;
                // if (string.IsNullOrEmpty(errorText)) errorText = $"operationResult={operationResult}";
                return 0;
            }

            NewState = api.State;
            NewCryptogram = api.Cryptogram.ToBytes();

            // TODO: validation

            return api.AmountAuthorized;
        }

        public long PreComplete(
            long Amount,
            string CurrencyCode,
            string OperationId,
            byte[] Cryptogram,
            out string NewState,
            out byte[] NewCryptogram,
            out string errorText)
        {
            errorText = null;
            NewState = null;
            NewCryptogram = null;

            var api = GetAPI();

            if (api == null)
            {
                errorText = "Failed to load API library";
                return 0;
            }

            // No Card needed for PreIncrement
            //var IsCardValid = WaitForCard(
            //    api: api,
            //    requiredFlag: "AllowPreauthorize",
            //    ExpectedCardCurrency: CurrencyCode,
            //    lastFourDigits: out var lastFourDigits,
            //    errorText: out errorText);
            //if (!IsCardValid) return 0;

            api.PreComplete(Amount, OperationId, Cryptogram);
            ConsoleGui.Info($"PreComplete() method called");

            var IsTranStateReceived = WaitTranState(api, null, OperationId, Cryptogram, out errorText);
            if (!IsTranStateReceived) return 0;

            if (api.OperationResult != "OK")
            {
                errorText = api.Text;
                return 0;
            }

            NewState = api.State;
            NewCryptogram = api.Cryptogram.ToBytes();

            // TODO: validation

            return api.AmountAuthorized;
        }

        bool WaitForCard(
            ActiveXConnectAPI api,
            string requiredFlag,
            string ExpectedCardCurrency,
            out string lastFourDigits,
            out string errorText)
        {
            errorText = null;
            lastFourDigits = null;

            ConsoleGui.Info("*** Please insert card");
            while (true)
            {
                if (!ProcessEventsUntil(api, "OnCard", 30))
                {
                    errorText = "Timeout waiting for card to be inserted into device";
                    return false;
                }

                if (ContainsFlag(api.Flags, requiredFlag))
                {
                    ConsoleGui.Ok("CARD is payment card");
                    break;
                }
                else
                {
                    ConsoleGui.Warning("Not payment card. Probably it's loyalty/discount card, so calculate discount/loyalty points here... and wait for next CARD event");
                }
            }

            if (api.CurrencyCode != ExpectedCardCurrency)
            {
                errorText = $"Currency mismatch. Expecting:{ExpectedCardCurrency} reveiced:[{api.CurrencyCode}]";
                return false;
            }

            if (ContainsFlag(api.Flags, "ReqLastFourDigits"))
            {
                lastFourDigits = ConsoleGui.EnterValue<string>("Please enter last 4 digits of the card");
            }

            return true;
        }

        bool WaitTranState(ActiveXConnectAPI api, string DocumentNr, string OperationID, byte[] Cryptogram, out string errorText)
        {
            const int TIMEOUT_SECONDS = 100;
            errorText = null;

            if (!ProcessEventsUntil(api, "OnTranState", TIMEOUT_SECONDS))
            {
                ConsoleGui.Error("Timeout waiting for OnTranState, trying to perform GetTranState by DocumentNr...");

                ConsoleGui.Info("Calling GetTranState method...");
                api.GetTranState(DocumentNr, OperationID, Cryptogram);
                
                if (!ProcessEventsUntil(api, "OnTranState", TIMEOUT_SECONDS))
                {
                    errorText = "Timeout waiting for OnTranState";
                    return false;
                }
            }
            ConsoleGui.Ok("OnTranState received");
            return true;
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
        private bool ProcessEventsUntil(ActiveXConnectAPI api, string expectedEventType, int timeoutInSeconds)
        {
            ConsoleGui.Info($"Waiting for [{expectedEventType}] event...");

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

                    case "OnTranState":
                        Console.WriteLine("--------------");
                        Console.WriteLine(api.Text);
                        Console.WriteLine("--------------");
                        break;

                    default:
                        {
                            if (!String.IsNullOrEmpty(ev))
                                ConsoleGui.Warning($"Ignoring event:[{ev}]"); //   OperationResult:[{api?.OperationResult}]
                        }
                        break;
                }

                if (sw.ElapsedMilliseconds > timeoutInSeconds * 1000)
                {
                    ConsoleGui.Warning($"Waiting for [{expectedEventType}] timed-out.");
                    return false;
                }

                api.PollEvent((int)(timeoutInSeconds * 1000 - sw.ElapsedMilliseconds));
                ev = api.EventType;
            }

            return true;
        }

        public bool VoidTransaction(string operationID, out string errorText)
        {
            errorText = null;

            ActiveXConnectAPI api = GetAPI();
            if (api == null)
            {
                errorText = "Failed to load API library...";
                return false;
            }

            ConsoleGui.Info($"Voiding [{operationID}]...");
            api.Void(operationID);
            ConsoleGui.Info($"Waiting OnVoidResult...");
            if (!ProcessEventsUntil(api, "OnVoidResult", 100))
            {
                errorText = "Timeout waiting for OnVoidResult";
                return false;
            }

            if (api.OperationResult != "OK")
            {
                errorText = api.Text;
                return false;
            }

            return true;
        }

        public bool VoidPartialTransaction(string operationID, long amountAuthorized, long voidAmount, out string errorText)
        {
            errorText = null;

            ActiveXConnectAPI api = GetAPI();
            if (api == null)
            {
                errorText = "Failed to load API library...";
                return false;
            }

            api.VoidPartial(operationID, amountAuthorized, voidAmount);

            if (!ProcessEventsUntil(api, "OnVoidResult", 100))
            {
                errorText = "Timeout waiting for OnVoidResult";
                return false;
            }

            if (api.OperationResult != "OK")
            {
                errorText = api.Text;
                return false;
            }

            return true;
        }

        public bool GetTranState(string operationID, byte[] cryptogram, out string errorText, out string state, out byte[] resultCryptogram)
        {
            errorText = null;
            resultCryptogram = null;
            state = "Unknown";

            ActiveXConnectAPI api = GetAPI();
            if (api == null)
            {
                errorText = "Failed to load API library...";
                return false;
            }

            api.GetTranState(null, operationID, cryptogram);

            if (!ProcessEventsUntil(api, "OnTranState", 100))
            {
                errorText = "Timeout waiting for OnTranState";
                return false;
            }

            if (api.OperationResult != "OK")
            {
                errorText = api.Text;
                return false;
            }

            state = api.State;
            resultCryptogram = api.Cryptogram.ToBytes();
            return true;
        }

        public long GsmPurchase(out string errorText, string data, Document document)
        {
            errorText = null;
            var api = GetAPI();

            if (api == null)
            {
                errorText = "Failed to load API library";
                return 0;
            }

            // TODO: do i need unlock here?

            api.Query(data, document.DocumentNr);
            if (!ProcessEventsUntil(api, "OnQueryResult", 100))
            {
                errorText = "Error when sending Query";
                return 0;
            }

            string opResult = api.OperationResult;
            if (opResult != "OK")
            {
                errorText = "OnQueryResult returned: " + opResult + ". Error text: " + api.Text;
                return 0;
            }

            if (api.AmountAuthorized > 0)
            {
                document.Transactions.Add(new Transaction() { OperationID = api.OperationID, AmountAuthorized = api.AmountAuthorized });
                DocumentManager.SaveDocumentToFile(document);
            }

            return api.AmountAuthorized;
        }
    }
}
