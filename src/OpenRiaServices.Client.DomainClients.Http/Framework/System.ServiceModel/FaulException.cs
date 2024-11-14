/// This code is based on reverse engineering of FaultException class from System.ServiceModel assembly/nuget
/// so that we don't have to depend on System.ServiceModel nugets

using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace System.ServiceModel
{
    sealed class FaultCode
    {
        public FaultCode(string code)
        {
            Value = code;
        }

        public FaultCode(string code, FaultCode faultCode)
            : this($"{code}, subcode={faultCode}")
        {
        }

        public string Value { get; }

        public override string ToString() => Value;
    }

    sealed class FaultReasonText
    {
        private string text;

        public string XmlLang { get; }

        public string Text => text;

        public FaultReasonText(string text)
        {
            this.text = text;
            XmlLang = CultureInfo.CurrentCulture.Name;
        }

        public FaultReasonText(string text, string xmlLang)
        {
            this.text = text;
            this.XmlLang = xmlLang;
        }

        public FaultReasonText(string text, CultureInfo cultureInfo)
        {
            this.text = text;
            XmlLang = cultureInfo.Name;
        }

        public bool Matches(CultureInfo cultureInfo)
        {
            return XmlLang == cultureInfo.Name;
        }

        public override string ToString() 
            => Text;
    }

    //
    // Summary:
    //     Provides a text description of a SOAP fault.
    sealed class FaultReason
    {
        public FaultReasonText[] Translations { get; private set; }

        public FaultReason(IEnumerable<FaultReasonText> translations)
        {
            Translations = translations.ToArray();
        }

        public override string ToString()
        {
            return string.Join(", ", (IEnumerable<FaultReasonText>)Translations);
        }
    }

    class FaultException : Exception
    {
        public FaultException(string message)
            : base(message)
        {
        }

        public FaultException(FaultReason reason, FaultCode code, string action)
            :base($"Error callong {action}. Reason: {reason?.ToString()}, Code: {code?.ToString()}")
        {
            Reason = reason;
            Code = code;
            Action = action;
        }

        public FaultReason Reason { get; }
        public FaultCode Code { get; }
        public string Action { get; }
    }

    sealed class FaultException<TDetail> : FaultException
    {
        public TDetail Detail { get; }

        public FaultException(TDetail detail)
            : base("NEVER USED")
        {
            this.Detail = detail;
        }
        
        public FaultException(TDetail detail, FaultReason reason, FaultCode code, string action)
            : base(reason, code, action)
        {
            this.Detail = detail;
        }
    }
}
