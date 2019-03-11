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

#if false

        internal static string InsecureGetFullPath(string path, string fromDirectory)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            if (path.Trim().Length == 0)
            {
                string msg = "The specified path is not of a legal form (empty).";
                throw new ArgumentException(msg);
            }

            // adjust for drives, i.e. a special case for windows
            if (IsRunningOnWindows)
                path = WindowsDriveAdjustment(path);

            // if the supplied path ends with a separator...
            char end = path[path.Length - 1];

            var canonicalize = true;
            if (path.Length >= 2 &&
            IsDsc(path[0]) &&
            IsDsc(path[1]))
            {
                if (path.Length == 2 || path.IndexOf(path[0], 2) < 0)
                    throw new ArgumentException("UNC paths should be of the form \\\\server\\share.");

                if (path[0] != Path.DirectorySeparatorChar)
                    path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            }
            else
            {
                if (!Path.IsPathRooted(path))
                {

                    // avoid calling expensive CanonicalizePath when possible
                    if (!IsRunningOnWindows)
                    {
                        var start = 0;
                        while ((start = path.IndexOf('.', start)) != -1)
                        {
                            if (++start == path.Length || path[start] == Path.DirectorySeparatorChar || path[start] == Path.AltDirectorySeparatorChar)
                                break;
                        }
                        canonicalize = start > 0;
                    }

                    path = fromDirectory + DirectorySeparatorStr + path;
                }
                else if (Path.DirectorySeparatorChar == '\\' &&
              path.Length >= 2 &&
              IsDsc(path[0]) &&
              !IsDsc(path[1]))
                { // like `\abc\def'
                    string current = fromDirectory;
                    if (current[1] == Path.VolumeSeparatorChar)
                        path = current.Substring(0, 2) + path;
                    else
                        path = current.Substring(0, current.IndexOf('\\', current.IndexOfOrdinalUnchecked("\\\\") + 1));
                }
            }

            if (canonicalize)
                path = CanonicalizePath(path);

            // if the original ended with a [Alt]DirectorySeparatorChar then ensure the full path also ends with one
            if (IsDsc(end) && (path[path.Length - 1] != Path.DirectorySeparatorChar))
                path += Path.DirectorySeparatorChar;

            return path;
        }

        internal static string WindowsDriveAdjustment(string path)
        {
            // two special cases to consider when a drive is specified
            if (path.Length < 2)
                return path;
            if ((path[1] != ':') || !Char.IsLetter(path[0]))
                return path;

            string current = Directory.GetCurrentDirectory();
            // first, only the drive is specified
            if (path.Length == 2)
            {
                // then if the current directory is on the same drive
                if (current[0] == path[0])
                    path = current; // we return it
                else
                    path = Path.GetFullPath(path); // we have to use the GetFullPathName Windows API
            }
            else if ((path[2] != Path.DirectorySeparatorChar) && (path[2] != Path.AltDirectorySeparatorChar))
            {
                // second, the drive + a directory is specified *without* a separator between them (e.g. C:dir).
                // If the current directory is on the specified drive...
                if (current[0] == path[0])
                {
                    // then specified directory is appended to the current drive directory
                    path = Path.Combine(current, path.Substring(2, path.Length - 2));
                }
                else
                {
                    // we have to use the GetFullPathName Windows API
                    path = Path.GetFullPath(path);
                }
            }
            return path;
        }

        static string CanonicalizePath(string path)
        {
            // STEP 1: Check for empty string
            if (path == null)
                return path;
            if (IsRunningOnWindows)
                path = path.Trim();

            if (path.Length == 0)
                return path;

            // STEP 2: Check to see if this is only a root
            string root = Path.GetPathRoot(path);
            // it will return '\' for path '\', while it should return 'c:\' or so.
            // Note: commenting this out makes the need for the (target == 1...) check in step 5
            //if (root == path) return path;

            // STEP 3: split the directories, this gets rid of consecutative "/"'s
            string[] dirs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            // STEP 4: Get rid of directories containing . and ..
            int target = 0;

            bool isUnc = IsRunningOnWindows &&
            root.Length > 2 && IsDsc(root[0]) && IsDsc(root[1]);

            // Set an overwrite limit for UNC paths since '\' + server + share
            // must not be eliminated by the '..' elimination algorithm.
            int limit = isUnc ? 3 : 0;

            for (int i = 0; i < dirs.Length; i++)
            {
                // WIN32 path components must be trimmed
                if (IsRunningOnWindows)
                    dirs[i] = dirs[i].TrimEnd();

                if (dirs[i] == "." || (i != 0 && dirs[i].Length == 0))
                    continue;
                else if (dirs[i] == "..")
                {
                    // don't overwrite path segments below the limit
                    if (target > limit)
                        target--;
                }
                else
                    dirs[target++] = dirs[i];
            }

            // STEP 5: Combine everything.
            if (target == 0 || (target == 1 && dirs[0] == ""))
                return root;
            else
            {
                string ret = String.Join(DirectorySeparatorStr, dirs, 0, target);
                if (IsRunningOnWindows)
                {
                    // append leading '\' of the UNC path that was lost in STEP 3.
                    if (isUnc)
                        ret = Path.DirectorySeparatorStr + ret;

                    if (!SameRoot(root, ret))
                        ret = root + ret;

                    if (isUnc)
                    {
                        return ret;
                    }
                    else if (!IsDsc(path[0]) && SameRoot(root, path))
                    {
                        if (ret.Length <= 2 && !ret.EndsWith(DirectorySeparatorStr)) // '\' after "c:"
                            ret += Path.DirectorySeparatorChar;
                        return ret;
                    }
                    else
                    {
                        string current = Directory.GetCurrentDirectory();
                        if (current.Length > 1 && current[1] == Path.VolumeSeparatorChar)
                        {
                            // DOS local file path
                            if (ret.Length == 0 || IsDsc(ret[0]))
                                ret += '\\';
                            return current.Substring(0, 2) + ret;
                        }
                        else if (IsDsc(current[current.Length - 1]) && IsDsc(ret[0]))
                            return current + ret.Substring(1);
                        else
                            return current + ret;
                    }
                }
                else
                {
                    if (root != "" && ret.Length > 0 && ret[0] != '/')
                        ret = root + ret;
                }
                return ret;
            }
        }

        static bool SameRoot(string root, string path)
        {
            // compare root - if enough details are available
            if ((root.Length < 2) || (path.Length < 2))
                return false;

            // UNC handling
            if (IsDsc(root[0]) && IsDsc(root[1]))
            {
                if (!(IsDsc(path[0]) && IsDsc(path[1])))
                    return false;

                string rootShare = GetServerAndShare(root);
                string pathShare = GetServerAndShare(path);

                return String.Compare(rootShare, pathShare, true, CultureInfo.InvariantCulture) == 0;
            }

            // same volume/drive
            if (!root[0].Equals(path[0]))
                return false;
            // presence of the separator
            if (path[1] != Path.VolumeSeparatorChar)
                return false;
            if ((root.Length > 2) && (path.Length > 2))
            {
                // but don't directory compare the directory separator
                return (IsDsc(root[2]) && IsDsc(path[2]));
            }
            return true;
        }

        static bool IsDsc(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }
        //we're always running on windows.
        public static bool IsRunningOnWindows
        {
            get { return true; }
        }

        static string GetServerAndShare(string path)
        {
            int len = 2;
            while (len < path.Length && !IsDsc(path[len])) len++;

            if (len < path.Length)
            {
                len++;
                while (len < path.Length && !IsDsc(path[len])) len++;
            }

            return path.Substring(2, len - 2).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
#endif
    }


}