using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ActiveXConnectLib
{
    class AXCLibrary
    {
        #region Methods

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Initialize(string[] arguments, int numberOfArguments);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Dispose();

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_PollEvent(int timeoutInMilliseconds);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_UnlockDevice(string idleText, string language, string ecrVersion, string operatorID, string operatorName, long amount, string prepareForOperation, long cashbackAmount);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_LockDevice(string idleText);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_CloseDay(string operatorID, string operatorName);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Authorize(long amount, long cashbackAmount, string documentNr, string lastFourDigits);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Credit(long amount, string documentNr, string lastFourDigits, string originalTime, string originalStan, string originalRRN);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Void(string operationID);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_VoidPartial(string operationID, long amountAuthorized, long voidAmount);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_DocClosed(string documentNr, string[] operations, int numberOfOperations);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_RemoveCard(string reasonText);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Input(string inputValue);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_GetNextReceipt();

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Query(string data, string documentNr);

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Inquiry();

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_QueryDeviceStatus();

        #endregion

        #region Property getters

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyOperationResult();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyEventType();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyOperationID();

        [DllImport("libActiveXConnect.so")]
        public static extern long AXC_GetPropertyAmountAuthorized();

        [DllImport("libActiveXConnect.so")]
        public static extern long AXC_GetPropertyAmountTips();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyDocumentNr();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyText();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyAuthCode();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyRRN();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertySTAN();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyCardType();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyReceiptText();

        [DllImport("libActiveXConnect.so")]
        public static extern bool AXC_GetPropertyReceiptIsMerchantCopy();

        [DllImport("libActiveXConnect.so")]
        public static extern bool AXC_GetPropertyReceiptHasSignatureLine();

        [DllImport("libActiveXConnect.so")]
        public static extern bool AXC_GetPropertyReceiptIsClientData();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyReceiptDocumentNr();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyHash([Out]out int outBufferLength);

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyTrack1();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyTrack2();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyTrack3();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyPAN();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyFlags(int index);

        [DllImport("libActiveXConnect.so")]
        public static extern int AXC_GetPropertyFlagsCount();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyCurrencyCode();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyKBDKey();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyPromptText();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyPromptMask();

        [DllImport("libActiveXConnect.so")]
        public static extern int AXC_GetPropertyOptionCount();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyOption(int index);

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyProvider();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyProductCode();

        [DllImport("libActiveXConnect.so")]
        public static extern IntPtr AXC_GetPropertyDeviceStatus();

        #endregion

        #region Memory management

        [DllImport("libActiveXConnect.so")]
        public static extern void AXC_Free(IntPtr buffer);

        #endregion
    }

    class AXC : IActiveXConnectAPI
    {
        private string GetString(IntPtr ptr)
        {
            int offset = 0;
            while (Marshal.ReadByte(ptr, offset) != 0)
            {
                offset++;
            }

            byte[] buffer = new byte[offset];
            Marshal.Copy(ptr, buffer, 0, buffer.Length);
            string result = Encoding.UTF8.GetString(buffer);

            AXCLibrary.AXC_Free(ptr);

            return result;
        }

        public long AmountAuthorized
        {
            get
            {
                return AXCLibrary.AXC_GetPropertyAmountAuthorized();
            }
        }

        public long AmountTips
        {
            get
            {
                return AXCLibrary.AXC_GetPropertyAmountTips();
            }
        }

        public string AuthCode
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyAuthCode());
            }
        }

        public void Authorize(long Amount, long CashbackAmount, string DocumentNr, string LastFourDigits)
        {
            AXCLibrary.AXC_Authorize(Amount, CashbackAmount, DocumentNr, LastFourDigits);
        }

        public string CardType
        {
            get { return GetString(AXCLibrary.AXC_GetPropertyCardType()); }
        }

        public void CloseDay(string OperatorID, string OperatorName)
        {
            AXCLibrary.AXC_CloseDay(OperatorID, OperatorName);
        }

        public void Credit(long Amount, string DocumentNr, string LastFourDigits, string OriginalTime, string OriginalStan, string OriginalRRN)
        {
            AXCLibrary.AXC_Credit(Amount, DocumentNr, LastFourDigits, OriginalTime, OriginalStan, OriginalRRN);
        }

        public string CurrencyCode
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyCurrencyCode());
            }
        }

        public void Dispose()
        {
            AXCLibrary.AXC_Dispose();
        }

        public void DocClosed(string DocumentNr, Array Operations)
        {
            AXCLibrary.AXC_DocClosed(DocumentNr, (string[])Operations, Operations.GetLength(0));
        }

        public string DocumentNr
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyDocumentNr());
            }
        }

        public string EventType
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyEventType());
            }
        }

        public Array Flags
        {
            get
            {
                int flagCount = AXCLibrary.AXC_GetPropertyFlagsCount();

                string[] result = new string[flagCount];

                for (int i = 0; i < flagCount; i++)
                {
                    result[i] = GetString(AXCLibrary.AXC_GetPropertyFlags(i));
                }

                return result;
            }
        }

        public void GetNextReceipt()
        {
            AXCLibrary.AXC_GetNextReceipt();
        }

        public Array Hash
        {
            get
            {
                int len;
                IntPtr nativeBuffer = AXCLibrary.AXC_GetPropertyHash(out len);

                if (nativeBuffer == IntPtr.Zero)
                    return null;

                byte[] buffer = new byte[len];
                Marshal.Copy(nativeBuffer, buffer, 0, buffer.Length);
                AXCLibrary.AXC_Free(nativeBuffer);
                return buffer;
            }
        }

        public void Initialize(Array initParameters)
        {
            string[] array = (string[])initParameters;
            AXCLibrary.AXC_Initialize(array, initParameters.Length);
        }

        public void Input(string InputValue)
        {
            AXCLibrary.AXC_Input(InputValue);
        }

        public string KBDKey
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyKBDKey());
            }
        }

        public void LockDevice(string IdleString)
        {
            AXCLibrary.AXC_LockDevice(IdleString);
        }

        public string OperationID
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyOperationID());
            }
        }

        public string OperationResult
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyOperationResult());
            }
        }

        public string PAN
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyPAN());
            }
        }

        public void PollEvent(int TimeoutInMiliseconds)
        {
            AXCLibrary.AXC_PollEvent(TimeoutInMiliseconds);
        }

        public string PromptMask
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyPromptMask());
            }
        }

        public string PromptText
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyPromptText());
            }
        }

        public string RRN
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyRRN());
            }
        }

        public string ReceiptDocumentNr
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyReceiptDocumentNr());
            }
        }

        public bool ReceiptHasSignatureLine
        {
            get
            {
                return AXCLibrary.AXC_GetPropertyReceiptHasSignatureLine();
            }
        }

        public bool ReceiptIsClientData
        {
            get
            {
                return AXCLibrary.AXC_GetPropertyReceiptIsClientData();
            }
        }

        public bool ReceiptIsMerchantCopy
        {
            get
            {
                return AXCLibrary.AXC_GetPropertyReceiptIsMerchantCopy();
            }
        }

        public string ReceiptText
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyReceiptText());
            }
        }

        public void RemoveCard(string ReasonText)
        {
            AXCLibrary.AXC_RemoveCard(ReasonText);
        }

        public string STAN
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertySTAN());
            }
        }

        public string Text
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyText());
            }
        }

        public string Track1
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyTrack1());
            }
        }

        public string Track2
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyTrack2());
            }
        }

        public string Track3
        {
            get
            {
                return GetString(AXCLibrary.AXC_GetPropertyTrack3());
            }
        }

        public void UnlockDevice(string IdleText, string Language, string OperatorID, string OperatorName, long Amount, string ECRVersion, string PrepareForOperation, long CashbackAmount)
        {
            AXCLibrary.AXC_UnlockDevice(IdleText, Language, ECRVersion, OperationID, OperatorName, Amount, PrepareForOperation, CashbackAmount);
        }

        public void Void(string OperationID)
        {
            AXCLibrary.AXC_Void(OperationID);
        }

        public void Click(string ButtonClicked)
        {
            throw new NotImplementedException();
        }

        public void AuthorizeWithCurrency(long Amount, long CashbackAmount, string DocumentNr, string LastFourDigits, string CurrencyCode)
        {
            throw new NotImplementedException();
        }

        public void CreditWithCurrency(long Amount, string DocumentNr, string LastFourDigits, string OriginalTime, string OriginalStan, string OriginalRRN, string CurrencyCode)
        {
            throw new NotImplementedException();
        }

        public void PrintTotals(string OperatorID, string OperatorName)
        {
            throw new NotImplementedException();
        }

        public void Print(string DocumentNr, string ReceiptText)
        {
            throw new NotImplementedException();
        }

        public void Installment(long Amount, string DocumentNr, string LastFourDigits, long Payments, string Type, string CurrencyCode)
        {
            throw new NotImplementedException();
        }

        public void UnlockDeviceWithCurrency(string IdleText, string Language, string OperatorID, string OperatorName, long Amount, string ECRVersion, string PrepareForOperation, long CashbackAmount, string CurrencyCode)
        {
            throw new NotImplementedException();
        }

        public Array Option => throw new NotImplementedException();

        public string MessageBoxText => throw new NotImplementedException();

        public string MessageBoxType => throw new NotImplementedException();

        public string MessageBoxDisplayReason => throw new NotImplementedException();

        public string DisplayText => throw new NotImplementedException();

        public Array AdditionalCurrencyCodes => throw new NotImplementedException();

        public Array AmountsAdditional => throw new NotImplementedException();

        public void Selected(string Option)
        {
            throw new NotImplementedException();
        }

        public void Query(string Data, string DocumentNr)
        {
            AXCLibrary.AXC_Query(Data, DocumentNr);
        }

        public string ProductCode => GetString(AXCLibrary.AXC_GetPropertyProductCode());

        public string Provider => GetString(AXCLibrary.AXC_GetPropertyProvider());

        public void Inquiry()
        {
            AXCLibrary.AXC_Inquiry();
        }

        public void QueryDeviceStatus()
        {
            AXCLibrary.AXC_QueryDeviceStatus();
        }

        public string DeviceStatus => GetString(AXCLibrary.AXC_GetPropertyDeviceStatus());

        public void VoidPartial(string OperationID, long AmountAuthorized, long VoidAmount)
        {
            AXCLibrary.AXC_VoidPartial(OperationID, AmountAuthorized, VoidAmount);
        }
    }
}
