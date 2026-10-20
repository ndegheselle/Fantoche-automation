using System.IO;
using System.Runtime.InteropServices;

namespace Fantoche.App.Features.Storage
{
    /// <summary>
    /// The space of the disk holding a folder. Read from Windows rather than with a
    /// <see cref="DriveInfo"/>, which only knows the lettered drives : the storage is usually a
    /// network share.
    /// </summary>
    /// <param name="FreeBytes">What is left for this user, the quotas of a share counted.</param>
    public record DiskSpace(long TotalBytes, long FreeBytes)
    {
        /// <summary>Null when it can't be read, the share being unreachable.</summary>
        public static DiskSpace? Of(string folder)
        {
            // The storage folder is only created once a task saves something, its disk is the one of its parents.
            string? existing = Path.GetFullPath(folder);
            while (existing != null && !Directory.Exists(existing))
                existing = Path.GetDirectoryName(existing);
            if (existing == null)
                return null;

            if (!GetDiskFreeSpaceEx(existing, out ulong freeBytes, out ulong totalBytes, out _))
                return null;
            return new DiskSpace((long)totalBytes, (long)freeBytes);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(
            string directoryName,
            out ulong freeBytesAvailableToCaller,
            out ulong totalNumberOfBytes,
            out ulong totalNumberOfFreeBytes);
    }
}
