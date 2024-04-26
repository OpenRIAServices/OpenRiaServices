using System;
using System.Globalization;
using System.IO;
using OpenRiaServices.Tools.Logging;

namespace OpenRiaServices.Tools.CodeGenTask;

class ConsoleLogger : ILoggingService
{
    public bool HasLoggedErrors { get; private set; }

    public void LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
    {
        HasLoggedErrors = true;
        Console.WriteLine($"ERROR: {message}, errorCode: {errorCode} file: {file}:{lineNumber}-{endColumnNumber}");
    }

    public void LogError(string message)
    {
        HasLoggedErrors = true;
        Console.WriteLine($"ERROR: {message}");
    }

    public void LogException(Exception ex)
    {
        TextWriter writer = Console.Out;

        writer.WriteLine("-----------------------------------------------------------------------------");
        writer.WriteLine("Date : " + DateTime.Now.ToString(CultureInfo.CurrentCulture));
        writer.WriteLine();
        writer.WriteLine(LoggingHelper.FormatException(ex));
    }

    public void LogMessage(string message)
    {
        Console.WriteLine($"Info: {message}");
    }

    public void LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
    {
        Console.WriteLine($"WARN: {message}, errorCode: {errorCode} file: {file}:{lineNumber}-{endColumnNumber}");
    }

    public void LogWarning(string message)
    {
        Console.WriteLine($"WARN: {message}");
    }
}

