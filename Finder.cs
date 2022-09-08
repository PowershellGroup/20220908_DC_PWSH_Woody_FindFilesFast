using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FindFilesFast;

public static class Finder
{
    //https://docs.microsoft.com/en-us/windows/win32/fileio/file-attribute-constants
    private static readonly uint FILE_ATTRIBUTE_DIRECTORY = 0x10;

    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    private static readonly uint FILE_ATTRIBUTE_HIDDEN = 0x2;

    private static readonly uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;

    private static readonly uint FILE_ATTRIBUTE_SYSTEM = 0x4;
 
    // private static readonly int FILE_ATTRIBUTE_READONLY = 0x1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WIN32_FIND_DATA
    {
        public uint dwFileAttributes;

        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;

        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;

        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;

        public uint nFileSizeHigh;

        public uint nFileSizeLow;

        public uint dwReserved0;

        public uint dwReserved1;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindFirstFile(String lpFileName, out WIN32_FIND_DATA lpFindFileData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool FindClose(IntPtr hFindFile);

    public struct FindFileResult 
    {
        public uint Attributes { get; }

        public DateTimeOffset CreationTime { get; }

        public DateTimeOffset LastAccessTime { get; }

        public DateTimeOffset LastWriteTime { get; }

        public long FileSize { get; }

        public string FileName { get; }

        public string FullName { get; }

        public string AlternateFileName { get; }

        public bool IsDirectory { get; }

        public bool IsReparsePoint { get; }

        public bool IsSystem { get; }

        public bool IsHidden { get; }

        private static long IntsToLong(uint high, uint low)
            => (long)high << 32 | low;
        
        private static long IntsToLong(int high, int low)
            => IntsToLong((uint)high, (uint)low);

        private static DateTimeOffset DateTimeFromFileTime(System.Runtime.InteropServices.ComTypes.FILETIME fileTime)
            => DateTimeOffset.FromFileTime(IntsToLong(fileTime.dwHighDateTime, fileTime.dwLowDateTime));

        // internal FindFileResult(string debug) 
        // {
        //     this.FullName = debug;
        // }

        internal FindFileResult(WIN32_FIND_DATA win32file, string parentDirectory)
        {
            this.Attributes = win32file.dwFileAttributes;
            this.CreationTime = DateTimeFromFileTime(win32file.ftCreationTime);
            this.LastAccessTime = DateTimeFromFileTime(win32file.ftLastAccessTime);
            this.LastWriteTime = DateTimeFromFileTime(win32file.ftLastWriteTime);
            this.FileSize = IntsToLong(win32file.nFileSizeHigh, win32file.nFileSizeLow);
            this.FileName = win32file.cFileName;
            this.AlternateFileName = win32file.cAlternateFileName;
            this.IsDirectory = (win32file.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) > 0;
            this.IsReparsePoint = (win32file.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) > 0;
            this.IsSystem = (win32file.dwFileAttributes & FILE_ATTRIBUTE_SYSTEM) > 0;
            this.IsHidden = (win32file.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN) > 0;

            this.FullName = parentDirectory + "\\" + this.FileName;
        }
    }

    public static IEnumerable<FindFileResult> FindFiles(string directory, bool recurse = false, bool recurseOnReparse = false)
    {
        // yield return new FindFileResult($"DIR: {directory} RECURSE: {recurse}");

        bool IsRecurse(FindFileResult currentResult) 
            => recurse 
            && currentResult.IsDirectory
            // && !currentResult.IsHidden
            // && !currentResult.IsSystem
            && (!currentResult.IsReparsePoint || recurseOnReparse);

        bool IsSkip(FindFileResult currentResult)
            => (currentResult.FileName == ".")
            || (currentResult.FileName == "..");

        var searchPointer = FindFirstFile(directory + "\\*", out var currentFile);

        if (searchPointer == INVALID_HANDLE_VALUE) 
        {
            yield break;
        }

        try
        {
            do
            {
                var currentResult = new FindFileResult(currentFile, directory);

                if (IsSkip(currentResult)) 
                {
                    continue;
                }

                yield return currentResult;

                var isRecurse = IsRecurse(currentResult);
                
                // yield return new FindFileResult($"shall we recurse? {isRecurse}");

                if (isRecurse)
                {
                    foreach (var subfolderResult in FindFiles(currentResult.FullName, recurse)) 
                    {
                        yield return subfolderResult;
                    }
                }
            }
            while (FindNextFile(searchPointer, out currentFile));
        }
        finally
        {
            FindClose(searchPointer);
        }
    }
}
