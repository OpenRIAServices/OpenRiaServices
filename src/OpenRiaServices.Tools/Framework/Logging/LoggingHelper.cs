using System;
using System.Linq;
using System.Reflection;
using System.Text;
#nullable enable

namespace OpenRiaServices.Tools.Logging;

internal static class LoggingHelper
{
    /// <summary>
    /// Formats <paramref name="ex"/> with inner exceptions to a string and returns it
    /// </summary>
    /// <remarks>
    /// The following exception graph
    /// <code>
    /// AggregateException
    /// |-> ArgumentException
    /// |   |-> ReflectionTypeLoadException
    /// |        |-> ArgumentNullException 
    /// |        |-> InvalidArgumentException
    /// |-> InvalidCastException
    /// </code>
    /// Generates an exception similar to 
    /// <code>
    ///AggregateException : AGG:message(ICE:message) (AE:message)
    ///  InvalidCastException : ICE:message
    ///  ArgumentException : AE:message
    ///    ReflectionTypeLoadException : RTE:message
    ///ANE:message(Parameter 'AME:param')
    ///IOE:message
    ///    Classes failed to load: 
    ///     - OpenRiaServices.Tools.Logging.CrossProcessLoggingServer
    ///     - OpenRiaServices.Tools.Logging.CrossProcessLoggingWriter
    ///    LoaderExceptions: 
    ///      ArgumentNullException : ANE:message(Parameter 'AME:param')
    ///      InvalidOperationException : IOE:message
    ///StackTrace:
    ///    at ***
    /// </code>
    /// </remarks>
    public static string FormatException(Exception ex)
    {
        var stringBuilder = new StringBuilder(200);

        FormatException(stringBuilder, ex, indentLevel: 0);

        stringBuilder.AppendLine();
        AddStackTrace(stringBuilder, ex?.StackTrace, indentLevel: 0);
        return stringBuilder.ToString();
    }

    private static void FormatException(StringBuilder stringBuilder, Exception? ex, int indentLevel)
    {
        if (ex is null)
        {
            stringBuilder.Append(' ', indentLevel);
            stringBuilder.AppendLine("null");
            return;
        }

        stringBuilder.Append(' ', indentLevel);
        stringBuilder.Append(ex.GetType().Name);
        stringBuilder.Append(" : ");
        stringBuilder.AppendLine(ex.Message);

        if (ex is TypeLoadException typeLoadException)
        {
            stringBuilder.Append(' ', indentLevel);
            stringBuilder.Append("Caused by Type: ");
            stringBuilder.Append(typeLoadException.TypeName);
        }
        else if (ex is ReflectionTypeLoadException reflectionTypeLoadException)
        {
            var first5failedTypes = reflectionTypeLoadException.Types?.Take(5).Select(t => t.FullName) ?? Enumerable.Empty<string>();
            stringBuilder.Append(' ', indentLevel);
            stringBuilder.AppendLine("Classes failed to load: ");

            foreach (var type in first5failedTypes)
            {
                stringBuilder.Append(' ', indentLevel);
                stringBuilder.Append(" - ");
                stringBuilder.AppendLine(type);
            }

            stringBuilder.Append(' ', indentLevel);
            stringBuilder.AppendLine("LoaderExceptions: ");
            foreach (var loaderException in reflectionTypeLoadException.LoaderExceptions)
            {
                FormatException(stringBuilder, loaderException, indentLevel + 2);
            }
        }
        else if (ex is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                FormatException(stringBuilder, innerException, indentLevel + 2);
                AddStackTrace(stringBuilder, innerException.StackTrace, indentLevel + 2);
            }
            return;
        }

        if (ex.InnerException is not null)
            FormatException(stringBuilder, ex.InnerException, indentLevel + 2);
    }

    private static void AddStackTrace(StringBuilder stringBuilder, string? stackTrace, int indentLevel)
    {
        if (stackTrace is null)
            return;

        stringBuilder.AppendLine();
        stringBuilder.Append(' ', indentLevel);
        stringBuilder.AppendLine($"StackTrace:");
        stringBuilder.Append(' ', indentLevel + 2);
        stringBuilder.Append(stackTrace);
    }
}
