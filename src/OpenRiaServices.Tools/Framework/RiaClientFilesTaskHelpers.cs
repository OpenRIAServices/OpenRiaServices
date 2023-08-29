﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
#if !NETFRAMEWORK
using System.Reflection;
using System.Runtime.Loader;
#endif

namespace OpenRiaServices.Tools;

internal static class RiaClientFilesTaskHelpers
{
#if !NETFRAMEWORK
    public static bool CodeGenForNet6(string generatedFileName, ClientCodeGenerationOptions options, ILoggingService logger, SharedCodeServiceParameters sharedCodeServiceParameters, string codeGeneratorName)
    {
        // We might want to create and use a separate AssemblyLoadContext to allow unloading of server assemblies 
        // the following proves good information (apart from official documentation)
        // * https://jeremybytes.blogspot.com/2020/01/dynamically-loading-types-in-net-core.html
        // * https://tsuyoshiushio.medium.com/understand-advanced-assemblyloadcontext-with-c-16a9d0cfeae3
        // * https://github.com/dotnet/runtime/issues/6880
        // 
        // If using a custom assemblyLoadContext we need to add `using (var context = loadContext.EnterContextualReflection())` around the call to dispatcher.GenerateCode
        //  OR before calling this method
        Func<AssemblyLoadContext, AssemblyName, Assembly> resolveFromServerAssemblies = GetServerAssemblyResolver(sharedCodeServiceParameters.ServerAssemblies.First());
        var assemblyLoadContext = AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default;
        assemblyLoadContext.Resolving += resolveFromServerAssemblies;
        try
        {
            // Create the "dispatcher" in the 2nd AppDomain.
            // This object will find and invoke the appropriate code generator
            using (var dispatcher = new ClientCodeGenerationDispatcher())
            {
                string generatedFileContent = dispatcher.GenerateCode(options, sharedCodeServiceParameters, logger, codeGeneratorName);

                // Tell the user where we are writing the generated code
                if (!string.IsNullOrEmpty(generatedFileContent))
                {
                    logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Writing_Generated_Code, generatedFileName));
                }

                // If VS is hosting us, write to its TextBuffer, else simply write to disk
                // If the file is empty, delete it.
                return WriteOrDeleteFileToVS(generatedFileName, generatedFileContent, /*forceWriteToFile*/ false, logger);
            }
        }
        finally
        {
            assemblyLoadContext.Resolving -= resolveFromServerAssemblies;
        }
    }

    /// <summary>
    /// Get a method which will resolve assemblies based the server projects dependencies (by looking at .deps.json)
    /// </summary>
    /// <param name="serverAssemblyPath">path to server assembly</param>
    private static Func<AssemblyLoadContext, AssemblyName, Assembly> GetServerAssemblyResolver(string serverAssemblyPath)
    {
        // Assume first item in parameters.ServerAssemblies is output of server project
        var assemblyDependencyResolver = new AssemblyDependencyResolver(serverAssemblyPath);
        return (AssemblyLoadContext loadContext, AssemblyName assemblyName) =>
        {
            if (assemblyName.Name.EndsWith(".resources"))
                return null;

            // Resolve dependency using server projects .deps.json file first
            string path = assemblyDependencyResolver.ResolveAssemblyToPath(assemblyName);
            if (path != null && loadContext.LoadFromAssemblyPath(path) is Assembly assembly)
            {
                return assembly;
            }

            return null;
        };
    }
#endif

    /// <summary>
    /// Create and write arguments to a new file
    /// </summary>
    /// <param name="arguments">The arguments that should be written to a file</param>
    /// <returns>The name of the file created</returns>
    internal static string CreateAndWriteArgumentsToNewTempFile(IEnumerable<string> arguments)
    {
        string fileName = Path.Combine(Path.GetTempPath(), $"openria-codegen-{DateTime.Now:yyyyMMdd-HHmmss-fff}.tmp");

        try
        {
            WriteContentsToNewFile(fileName, arguments);
        }
        catch (IOException)
        {
            // The file could already exist, which would throw an IOExcpetion
            // Add random file name to the file name and try again
            fileName += Path.GetRandomFileName();
            WriteContentsToNewFile(fileName, arguments);
        }

        return fileName;
    }

    /// <summary>
    /// Create a new file with <see cref="FileShare.Read"/> and write the contents to it
    /// </summary>
    /// <param name="filePath">The path to the file to be created</param>
    /// <param name="contents">The contents that should be written to the file</param>
    internal static void WriteContentsToNewFile(string filePath, IEnumerable<string> contents)
    {
        using (var fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(fileStream))
        {
            foreach (string line in contents)
            {
                writer.WriteLine(line);
            }
        }
    }

    /// <summary>
    /// Deletes the specified file in VS-compatible way
    /// </summary>
    /// <param name="fileName">Full path of the file to delete.  It may or may not exist on disk.</param>
    /// <param name="logger">Logger to keep track of warnings and errors.</param>
    internal static void DeleteFileFromVS(string fileName, ILoggingService logger)
    {
        if (File.Exists(fileName))
        {
            // Reset Read-Only file attribute
            // We do this because we set this attribute on files we generate.
            SafeSetReadOnlyAttribute(fileName, false, logger);

            // If VS does not delete this file or if VS is not present, delete it
            // from the file system manually.
            SafeFileDelete(fileName, logger);

            // If anything failed here, log a warning so user knows the file could not be removed.
            if (File.Exists(fileName))
            {
                logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Delete_File, fileName));
            }
        }
    }

    /// <summary>
    /// Safe form of File.Delete that catches and logs exceptions as warnings.
    /// </summary>
    /// <param name="fileName">The full path to the file to delete.</param>
    /// <param name="logger">Logger to keep track of warnings and errors.</param>
    /// <returns><c>false</c> if an error occurred and the file could not be deleted.</returns>
    internal static bool SafeFileDelete(string fileName, ILoggingService logger)
    {
        string errorMessage = null;
        if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
        {
            // Ensure readonly bit is turned off
            if (!SafeSetReadOnlyAttribute(fileName, false, logger))
            {
                return false;
            }

            try
            {
                File.Delete(fileName);
            }
            catch (IOException ioe)
            {
                errorMessage = ioe.Message;
            }
            catch (NotSupportedException nse)
            {
                errorMessage = nse.Message;
            }
            catch (UnauthorizedAccessException uae)
            {
                errorMessage = uae.Message;
            }

            if (errorMessage != null)
            {
                logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Delete_File_Error, fileName, errorMessage));
            }
        }

        return (errorMessage == null);
    }

    /// <summary>
    /// Safe form of writing string contents to a file.
    /// </summary>
    /// <param name="fileName">The full path to the file to write.</param>
    /// <param name="contents">The string content to write.</param>
    /// <param name="logger">Logger to keep track of warnings and errors.</param>
    /// <returns><c>false</c> if an error occurred and the file could not be written.</returns>
    internal static bool SafeFileWrite(string fileName, string contents, ILoggingService logger)
    {
        string errorMessage = null;

        try
        {
            File.WriteAllText(fileName, contents);
        }
        catch (IOException ioe)
        {
            errorMessage = ioe.Message;
        }
        catch (NotSupportedException nse)
        {
            errorMessage = nse.Message;
        }
        catch (UnauthorizedAccessException uae)
        {
            errorMessage = uae.Message;
        }

        if (errorMessage != null)
        {
            logger.LogWarning(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Write_File, fileName, errorMessage));
        }

        return (errorMessage == null);
    }

    /// <summary>
    /// Safe form of <see cref="Directory.CreateDirectory(string)"/>
    /// </summary>
    /// <remarks>
    /// This method will do nothing if the folder already exists, otherwise it
    /// will create it.  Any error in creation will log an appropriate error message
    /// and return <c>false</c>
    /// </remarks>
    /// <param name="directoryPath">The full path to the folder to create.</param>
    /// <param name="logger">Logger to keep track of warnings and errors.</param>
    /// <returns><c>false</c> if an error occurred and the folder could not be created.</returns>
    internal static bool SafeFolderCreate(string directoryPath, ILoggingService logger)
    {
        string errorMessage = null;
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (IOException ioe)
            {
                errorMessage = ioe.Message;
            }
            catch (NotSupportedException nse)
            {
                errorMessage = nse.Message;
            }
            catch (UnauthorizedAccessException uae)
            {
                errorMessage = uae.Message;
            }

            if (errorMessage != null)
            {
                logger.LogError(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Create_Folder, directoryPath, errorMessage));
            }
        }
        return (errorMessage == null);
    }

    /// <summary>
    /// Sets or resets the read-only file attribute of the given file
    /// </summary>
    /// <param name="fileName">Full path to the file to modify</param>
    /// <param name="newState">The desired state of the bit</param>
    /// <param name="logger">Logger to keep track of warnings and errors.</param>
    /// <returns><c>false</c> if a problem occurred and the bit could not be altered.</returns>
    internal static bool SafeSetReadOnlyAttribute(string fileName, bool newState, ILoggingService logger)
    {
        string errorMessage = null;

        if (File.Exists(fileName))
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(fileName);
                bool isSet = ((attributes & FileAttributes.ReadOnly) != 0);
                if (isSet != newState)
                {
                    attributes ^= FileAttributes.ReadOnly;
                    File.SetAttributes(fileName, attributes);
                }
            }
            catch (IOException ioe)
            {
                errorMessage = ioe.Message;
            }
            catch (NotSupportedException nse)
            {
                errorMessage = nse.Message;
            }
            catch (UnauthorizedAccessException uae)
            {
                errorMessage = uae.Message;
            }

            if (errorMessage != null)
            {
                logger.LogError(string.Format(CultureInfo.CurrentCulture, Resource.Failed_To_Modify_ReadOnly, fileName, errorMessage));
            }
        }

        return (errorMessage == null);
    }

    /// <summary>
    /// Writes the given file to VS.  If VS is not present, it just writes to the file system
    /// </summary>
    /// <param name="destinationFile">Name of the file to create/write</param>
    /// <param name="content">String content of file to write</param>
    /// <param name="forceWriteToFile">If <c>true</c>, write file always, even if Intellisense only build.</param>
    /// <param name="logger">Logger to keep track of warnings and errors.</param>
    /// <returns><c>true</c> if the write succeeded</returns>
    internal static bool WriteFileToVS(string destinationFile, string content, bool forceWriteToFile, ILoggingService logger)
    {
        // Create the folder as late as possible, but a failure here
        // logs a message and does not do the write
        string folder = Path.GetDirectoryName(destinationFile);
        if (!SafeFolderCreate(folder, logger))
        {
            return false;
        }

        // Reset read-only bit first or write may fail.
        // We do this because we are the ones who set that attribute in the first place (to discourage user edits)
        if (!SafeSetReadOnlyAttribute(destinationFile, false, logger))
        {
            return false;
        }

        SafeFileWrite(destinationFile, content, logger);

        // Set ReadOnly attribute to prevent casual edits.
        // Failure here is logged but does not affect the success of the write.
        SafeSetReadOnlyAttribute(destinationFile, true, logger);

        return true;
    }

    /// <summary>
    /// Writes the given content to the given file if it is non-empty, else deletes the file
    /// </summary>
    /// <param name="destinationFile">Full path to file to write or delete</param>
    /// <param name="content">Content to write to file</param>
    /// <param name="forceWriteToFile">If <c>true</c>, write file always, even if Intellisense only build.</param>
    /// <param name="logger">Logger to keep track of warnings and errors.</param>
    /// <returns><c>true</c> if the write succeeded, <c>false</c> if it was deleted or the write failed.</returns>
    internal static bool WriteOrDeleteFileToVS(string destinationFile, string content, bool forceWriteToFile, ILoggingService logger)
    {
        if (string.IsNullOrEmpty(content))
        {
            // Delete the old generated file -- but only if it exists.
            // If there was nothing generated and no file exists, it is better simply to say nothing.
            if (File.Exists(destinationFile))
            {
                logger.LogMessage(string.Format(CultureInfo.CurrentCulture, Resource.Deleting_Empty_File, destinationFile));
                DeleteFileFromVS(destinationFile, logger);
            }
            return false;
        }
        else
        {
            return WriteFileToVS(destinationFile, content, forceWriteToFile, logger);
        }
    }
}
