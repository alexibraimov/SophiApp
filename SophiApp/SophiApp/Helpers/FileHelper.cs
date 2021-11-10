﻿using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace SophiApp.Helpers
{
    internal class FileHelper
    {
        private const int maxRelativePathLengthUnicodeChars = 260;
        private const int targetIsADirectory = 1;

        private enum MoveFileFlags
        {
            MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        private static string GetTargetPathRelativeToLink(string linkPath, string targetPath, bool linkAndTargetAreDirectories = false)
        {
            string returnPath;

            FileAttributes relativePathAttribute = 0;
            if (linkAndTargetAreDirectories)
            {
                relativePathAttribute = FileAttributes.Directory;
                // set the link path to the parent directory, so that PathRelativePathToW returns a path that works
                // for directory symlink traversal
                linkPath = Path.GetDirectoryName(linkPath.TrimEnd(Path.DirectorySeparatorChar));
            }

            StringBuilder relativePath = new StringBuilder(maxRelativePathLengthUnicodeChars);
            if (!PathRelativePathToW(relativePath, linkPath, relativePathAttribute, targetPath, relativePathAttribute))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                returnPath = targetPath;
            }
            else
            {
                returnPath = relativePath.ToString();
            }

            return returnPath;
        }

        private static void MarkFileDelete(params string[] files)
        {
            foreach (var file in files)
                _ = MoveFileEx(file, null, MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool PathRelativePathToW(
            StringBuilder pszPath,
            string pszFrom,
            FileAttributes dwAttrFrom,
            string pszTo,
            FileAttributes dwAttrTo);

        internal static void CreateDirectory(string dirPath)
        {
            try
            {
                _ = Directory.CreateDirectory(dirPath);
            }
            catch (Exception)
            {
            }
        }

        internal static void CreateDirectory(params string[] dirsPath)
        {
            foreach (var path in dirsPath)
                CreateDirectory(path);
        }

        internal static void CreateDirectoryLink(string linkPath, string targetPath)
        {
            CreateDirectoryLink(linkPath, targetPath, false);
        }

        internal static void CreateDirectoryLink(string linkPath, string targetPath, bool makeTargetPathRelative)
        {
            if (makeTargetPathRelative)
            {
                targetPath = GetTargetPathRelativeToLink(linkPath, targetPath, true);
            }

            if (!CreateSymbolicLink(linkPath, targetPath, targetIsADirectory) || Marshal.GetLastWin32Error() != 0)
            {
                try
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                catch (COMException exception)
                {
                    throw new IOException(exception.Message, exception);
                }
            }
        }

        internal static void DirectoryDelete(string dirPath)
        {
            Directory.Delete(dirPath, recursive: true);
        }

        internal static bool DirectoryIsEmpty(string dirPath)
        {
            byte count = 0;

            foreach (var entry in Directory.GetFileSystemEntries(dirPath))
            {
                count++;
                break;
            }

            return count == 0;
        }

        internal static void DirectoryLazyDelete(string dirPath)
        {
            try
            {
                Directory.Delete(dirPath, true);
            }
            catch (Exception)
            {
                MarkFileDelete(Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories));
            }
        }

        internal static void Download(string from, string save)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(from, save);
            }
        }

        internal static void FileDelete(string filePath) => File.Delete(filePath);

        internal static void FileDelete(params string[] filesPath)
        {
            foreach (var file in filesPath)
                FileDelete(file);
        }

        internal static bool IsSymbolicLink(string dirPath)
        {
            var di = new DirectoryInfo(dirPath);
            return di.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        internal static void TryDeleteDirectory(string dirPath)
        {
            Directory.Delete(dirPath, recursive: true);
        }
    }
}