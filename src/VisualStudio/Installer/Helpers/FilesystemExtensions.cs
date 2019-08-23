using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace OpenRiaServices.VisualStudio.Installer.Helpers
{
    public static class FilesystemExtensions
    {
        /// <summary>
        /// Gets the relative path between two paths.
        /// </summary>
        /// <param name="currentDirectory"> The current directory. </param>
        /// <param name="pathToMakeRelative"> The path to make relative. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        /// <from>CoApp</from>
        public static string RelativePathTo(this string currentDirectory, string pathToMakeRelative)
        {
            if (string.IsNullOrEmpty(currentDirectory))
            {
                throw new ArgumentNullException("currentDirectory");
            }

            if (string.IsNullOrEmpty(pathToMakeRelative))
            {
                throw new ArgumentNullException("pathToMakeRelative");
            }

            currentDirectory = Path.GetFullPath(currentDirectory);
            pathToMakeRelative = Path.GetFullPath(pathToMakeRelative);

            if (!Path.GetPathRoot(currentDirectory).Equals(Path.GetPathRoot(pathToMakeRelative), StringComparison.CurrentCultureIgnoreCase))
            {
                return pathToMakeRelative;
            }

            var relativePath = new List<string>();
            var currentDirectoryElements = currentDirectory.Split(Path.DirectorySeparatorChar);
            var pathToMakeRelativeElements = pathToMakeRelative.Split(Path.DirectorySeparatorChar);
            var commonDirectories = 0;

            for (; commonDirectories < Math.Min(currentDirectoryElements.Length, pathToMakeRelativeElements.Length); commonDirectories++)
            {
                if (
                    !currentDirectoryElements[commonDirectories].Equals(pathToMakeRelativeElements[commonDirectories], StringComparison.CurrentCultureIgnoreCase))
                {
                    break;
                }
            }

            for (var index = commonDirectories; index < currentDirectoryElements.Length; index++)
            {
                if (currentDirectoryElements[index].Length > 0)
                {
                    relativePath.Add("..");
                }
            }

            for (var index = commonDirectories; index < pathToMakeRelativeElements.Length; index++)
            {
                relativePath.Add(pathToMakeRelativeElements[index]);
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), relativePath);
        }



        public static string AbsolutePathFrom(this string relativePath, string currentDirectory, IFileSystem fileSystem)
        {
            if (fileSystem.Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            currentDirectory = fileSystem.Path.GetFullPath(currentDirectory);
            var driveletterAndColon = currentDirectory.Substring(0, 2);
            currentDirectory = currentDirectory.Remove(0, 3);

            //we're getting rid of the 

            var finalDirectoryPathPart = new Stack<string>(currentDirectory.Split('\\'));
            var relativePathParts = relativePath.Split('\\', '/');
            foreach (var r in relativePathParts)
            {
                if (r == "..")
                {
                    finalDirectoryPathPart.Pop();
                }
                else if (r == ".")
                {
                    //do nothing
                }
                else
                {
                    finalDirectoryPathPart.Push(r);
                }
            }

            return
                finalDirectoryPathPart.Reverse().Aggregate(new StringBuilder(driveletterAndColon), (sb, s) => sb.Append("\\").Append(s)).ToString();
        }

        /// <summary>
        /// Get you an absolute path from a relativePath
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="currentDirectory"></param>
        /// <returns></returns>
        public static string AbsolutePathFrom(this string relativePath, string currentDirectory)
        {
            return AbsolutePathFrom(relativePath, currentDirectory, new FileSystem());
        }
    }


}
