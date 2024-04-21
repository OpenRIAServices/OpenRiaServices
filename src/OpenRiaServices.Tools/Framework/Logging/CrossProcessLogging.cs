// # Cross Process Logger
// 
// The cross process logger uses a shared memory mapped file (to avoid disk IO, and filename clashes).
// The format is a simple binary format with 2 kinds of payloads, a simple string message or a more complex message with line information.
// 
// Goals
// - No msbuild dependencies
// - Simple
// - Fast 
// - Few dependencies for a smaller and faster distributable
// 
// 
// # Binary Format
// 
// BinaryWriter, BinaryReader is used for writing/reading
// There are 2 types of packets: Simple and Complex, a single "Type" byte is used to describe type of payload
// 
// ---------------------------------------------
// | Type (1 byte) |  PAYLOAD (depend on Type) |
// ---------------------------------------------
// 
// ## Type
// 
// Type contains information about log level (and kind of message)
// 
// * Type (1 byte)
//   - 2 lower bits for log level
//       * 01 Message
//       * 10 Warning
//       * 11 Error
//   - 4 upper bits for packet type (Note 
//       * 0000 INVALID/EOF - not allowed so we can distinguish it from 
//       * 0001 Simple (string)
//       * 0010 Complex
//   - 0 is used for EOF 
// 
// ### Simple Packet 
// 
// These packages are used for logging calls to the ILogger where there is a single (string) message parameter
// 
// -------------------
// | Type | string |
// -------------------
// 
// ### Comples Packet 
// 
// These packages are used for logging calls to the ILoggingService where the parameters are
// * string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber
// 
// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
// | Type    | string message | string subcategory | string errorCode | string helpKeyword | string file | int lineNumber | int columnNumber| int endLineNumber | int endColumnNumber |
// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace OpenRiaServices.Tools.Logging;

enum LogLevel
{
    Invalid = 0, Message = 1, Warning = 2, Error = 3
}
enum PackageType
{
    EOF = 0, Simple = 1, Complex = 2
}

internal sealed class CrossProcessLoggingWriter : ILoggingService, IDisposable
{
    private readonly BinaryWriter _binaryWriter;

    public CrossProcessLoggingWriter(string pipeName)
    {
        var pipe = new AnonymousPipeClientStream(PipeDirection.Out, pipeName);
        _binaryWriter = new BinaryWriter(new BufferedStream(pipe), Encoding.Unicode);
    }

    public CrossProcessLoggingWriter(Microsoft.Win32.SafeHandles.SafePipeHandle pipeHandle)
    {
        var pipe = new AnonymousPipeClientStream(PipeDirection.Out, pipeHandle);
        _binaryWriter = new BinaryWriter(new BufferedStream(pipe), Encoding.Unicode);
    }

    public bool HasLoggedErrors { get; private set; }

    void ILoggingService.LogError(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        => Write(LogLevel.Error, message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);

    void ILogger.LogError(string message)
        => Write(LogLevel.Error, message);

    void ILogger.LogException(Exception ex)
        => Write(LogLevel.Error, LoggingHelper.FormatException(ex));

    void ILogger.LogMessage(string message)
        => Write(LogLevel.Message, message);

    void ILoggingService.LogWarning(string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
        => Write(LogLevel.Warning, message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);

    void ILogger.LogWarning(string message)
        => Write(LogLevel.Warning, message);


    private void WriteHeader(LogLevel logLevel, PackageType packageType)
    {
        byte type = (byte)((int)packageType << 4 | (int)logLevel);
        _binaryWriter.Write(type);

        if (logLevel == LogLevel.Error)
            HasLoggedErrors = true;
    }

    private void Write(LogLevel logLevel, string message)
    {
        WriteHeader(logLevel, PackageType.Simple);
        _binaryWriter.Write(message);
    }

    private void Write(LogLevel logLevel, string message, string subcategory, string errorCode, string helpKeyword, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
    {
        WriteHeader(logLevel, PackageType.Complex);
        _binaryWriter.Write(message);
        _binaryWriter.Write(subcategory);
        _binaryWriter.Write(errorCode);
        _binaryWriter.Write(helpKeyword);
        _binaryWriter.Write(file);
        _binaryWriter.Write(lineNumber);
        _binaryWriter.Write(columnNumber);
        _binaryWriter.Write(endLineNumber);
        _binaryWriter.Write(endColumnNumber);
    }

    public void Dispose()
    {
        WriteHeader(LogLevel.Invalid, PackageType.EOF);
        _binaryWriter.Flush();
        _binaryWriter.Dispose();
    }
}

internal sealed class CrossProcessLoggingServer : IDisposable
{
    private readonly AnonymousPipeServerStream _pipe;
    private readonly BinaryReader _binaryReader;
    public CrossProcessLoggingServer()
    {
        _pipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        _binaryReader = new BinaryReader(new BufferedStream(_pipe), Encoding.Unicode);

        PipeName = _pipe.GetClientHandleAsString();
    }

    public string PipeName { get; }

    public SafePipeHandle ClientSafePipeHandle => _pipe.ClientSafePipeHandle;

    /// <summary>
    ///  Read logs and forwards them to <paramref name="logger"/>.
    ///  any exception will catched and logged
    /// </summary>
    public void WriteLogsTo(ILoggingService logger, CancellationToken cancellationToken)
    {
        // The local part of the pipe must be closed (after proces is started and before we read from the pipe)
        _pipe.DisposeLocalCopyOfClientHandle();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                byte type = _binaryReader.ReadByte();
                var logLevel = (LogLevel)(type & 0b0000_1111);
                var packageType = (PackageType)(type >> 4 & 0b0000_1111);

                switch (packageType)
                {
                    case PackageType.EOF:
                        return;
                    case PackageType.Simple:
                        ProcessSimplePackage(logger, logLevel);
                        break;
                    case PackageType.Complex:
                        ProcessComplexPackage(logger, logLevel);
                        break;
                    default:
                        throw new NotSupportedException("Invalid PackageType");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to read log: " + ex.Message);
        }
    }

    private void ProcessComplexPackage(ILoggingService logger, LogLevel logLevel)
    {
        string message = _binaryReader.ReadString();
        string subcategory = _binaryReader.ReadString();
        string errorCode = _binaryReader.ReadString();
        string helpKeyword = _binaryReader.ReadString();
        string file = _binaryReader.ReadString();
        int lineNumber = _binaryReader.ReadInt32();
        int columnNumber = _binaryReader.ReadInt32();
        int endLineNumber = _binaryReader.ReadInt32();
        int endColumnNumber = _binaryReader.ReadInt32();

        switch (logLevel)
        {
            case LogLevel.Error:
                logger.LogError(message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
                break;
            case LogLevel.Warning:
                logger.LogWarning(message, subcategory, errorCode, helpKeyword, file, lineNumber, columnNumber, endLineNumber, endColumnNumber);
                break;
            default:
                throw new NotSupportedException("Invalid LogLevel");
        }
    }

    private void ProcessSimplePackage(ILoggingService logger, LogLevel logLevel)
    {
        string message = _binaryReader.ReadString();
        switch (logLevel)
        {
            case LogLevel.Error:
                logger.LogError(message);
                break;
            case LogLevel.Warning:
                logger.LogWarning(message);
                break;
            case LogLevel.Message:
                logger.LogMessage(message);
                break;
            default:
                throw new NotSupportedException("Invalid LogLevel");
        }
    }

    public void Dispose()
    {
        ((IDisposable)_binaryReader).Dispose();
    }
}
