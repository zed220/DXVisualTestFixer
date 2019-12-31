using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace DXVisualTestFixer.UI.Native {
	public static class FileSystemHelper {
        /// <summary>
        /// The file or directory is not a reparse point.
        /// </summary>
        private const int ERROR_NOT_A_REPARSE_POINT = 4390;

        /// <summary>
        /// The reparse point attribute cannot be set because it conflicts with an existing attribute.
        /// </summary>
        [UsedImplicitly] private const int ERROR_REPARSE_ATTRIBUTE_CONFLICT = 4391;

        /// <summary>
        /// The data present in the reparse point buffer is invalid.
        /// </summary>
        [UsedImplicitly] private const int ERROR_INVALID_REPARSE_DATA = 4392;

        /// <summary>
        /// The tag present in the reparse point buffer is invalid.
        /// </summary>
        [UsedImplicitly] private const int ERROR_REPARSE_TAG_INVALID = 4393;

        /// <summary>
        /// There is a mismatch between the tag specified in the request and the tag present in the reparse point.
        /// </summary>
        [UsedImplicitly] private const int ERROR_REPARSE_TAG_MISMATCH = 4394;

        /// <summary>
        /// Command to set the reparse point data block.
        /// </summary>
        private const int FSCTL_SET_REPARSE_POINT = 0x000900A4;

        /// <summary>
        /// Command to get the reparse point data block.
        /// </summary>
        private const int FSCTL_GET_REPARSE_POINT = 0x000900A8;

        /// <summary>
        /// Command to delete the reparse point data base.
        /// </summary>
        private const int FSCTL_DELETE_REPARSE_POINT = 0x000900AC;

        /// <summary>
        /// Reparse point tag used to identify mount points and junction points.
        /// </summary>
        private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;

        /// <summary>
        /// This prefix indicates to NTFS that the path is to be treated as a non-interpreted
        /// path in the virtual file system.
        /// </summary>
        private const string NonInterpretedPathPrefix = @"\??\";

        [Flags]
        private enum EFileAccess : uint {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000
        }

        [Flags]
        private enum EFileShare : uint {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004
        }

        private enum ECreationDisposition : uint {
            [UsedImplicitly] New = 1,
            [UsedImplicitly] CreateAlways = 2,
            OpenExisting = 3,
            [UsedImplicitly] OpenAlways = 4,
            [UsedImplicitly] TruncateExisting = 5
        }

        [Flags]
        private enum EFileAttributes : uint {
            [UsedImplicitly] Readonly = 0x00000001,
            [UsedImplicitly] Hidden = 0x00000002,
            [UsedImplicitly] System = 0x00000004,
            [UsedImplicitly] Directory = 0x00000010,
            [UsedImplicitly] Archive = 0x00000020,
            [UsedImplicitly] Device = 0x00000040,
            [UsedImplicitly] Normal = 0x00000080,
            [UsedImplicitly] Temporary = 0x00000100,
            [UsedImplicitly] SparseFile = 0x00000200,
            [UsedImplicitly] ReparsePoint = 0x00000400,
            [UsedImplicitly] Compressed = 0x00000800,
            [UsedImplicitly] Offline = 0x00001000,
            [UsedImplicitly] NotContentIndexed = 0x00002000,
            [UsedImplicitly] ncrypted = 0x00004000,
            [UsedImplicitly] Write_Through = 0x80000000,
            [UsedImplicitly] Overlapped = 0x40000000,
            [UsedImplicitly] NoBuffering = 0x20000000,
            [UsedImplicitly] RandomAccess = 0x10000000,
            [UsedImplicitly] SequentialScan = 0x08000000,
            [UsedImplicitly] DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            [UsedImplicitly] PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            [UsedImplicitly] OpenNoRecall = 0x00100000,
            [UsedImplicitly] FirstPipeInstance = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct REPARSE_DATA_BUFFER {
            /// <summary>
            /// Reparse point tag. Must be a Microsoft reparse point tag.
            /// </summary>
            public uint ReparseTag;

            /// <summary>
            /// Size, in bytes, of the data after the Reserved member. This can be calculated by:
            /// (4 * sizeof(ushort)) + SubstituteNameLength + PrintNameLength + 
            /// (namesAreNullTerminated ? 2 * sizeof(char) : 0);
            /// </summary>
            public ushort ReparseDataLength;

            /// <summary>
            /// Reserved; do not use. 
            /// </summary>
            [PublicAPI]
            public ushort Reserved;

            /// <summary>
            /// Offset, in bytes, of the substitute name string in the PathBuffer array.
            /// </summary>
            public ushort SubstituteNameOffset;

            /// <summary>
            /// Length, in bytes, of the substitute name string. If this string is null-terminated,
            /// SubstituteNameLength does not include space for the null character.
            /// </summary>
            public ushort SubstituteNameLength;

            /// <summary>
            /// Offset, in bytes, of the print name string in the PathBuffer array.
            /// </summary>
            public ushort PrintNameOffset;

            /// <summary>
            /// Length, in bytes, of the print name string. If this string is null-terminated,
            /// PrintNameLength does not include space for the null character. 
            /// </summary>
            public ushort PrintNameLength;

            /// <summary>
            /// A buffer containing the unicode-encoded path string. The path string contains
            /// the substitute name string and print name string.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
            public byte[] PathBuffer;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
            IntPtr InBuffer, int nInBufferSize,
            IntPtr OutBuffer, int nOutBufferSize,
            out int pBytesReturned, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            EFileAccess dwDesiredAccess,
            EFileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            ECreationDisposition dwCreationDisposition,
            EFileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Creates a junction point from the specified directory to the specified target directory.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="targetDir">The target directory to create</param>
        /// <param name="sourceDir">The source directory to alias</param>
        /// <exception cref="IOException">Thrown when the junction point could not be created or when
        /// an existing directory was found and <paramref name="overwrite" /> if false</exception>
        public static void Create(string sourceDir, string targetDir) {
            sourceDir = Path.GetFullPath(sourceDir);

            if (!Directory.Exists(sourceDir))
                throw new IOException("Source path does not exist or is not a directory.");

            if (Directory.Exists(targetDir))
                throw new IOException($"Directory '{targetDir}' already exists.");

            Directory.CreateDirectory(targetDir);

            using SafeFileHandle handle = OpenReparsePoint(targetDir, EFileAccess.GenericWrite);
            byte[] sourceDirBytes = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + Path.GetFullPath(sourceDir));

            REPARSE_DATA_BUFFER reparseDataBuffer = new REPARSE_DATA_BUFFER();

            reparseDataBuffer.ReparseTag = IO_REPARSE_TAG_MOUNT_POINT;
            reparseDataBuffer.ReparseDataLength = (ushort)(sourceDirBytes.Length + 12);
            reparseDataBuffer.SubstituteNameOffset = 0;
            reparseDataBuffer.SubstituteNameLength = (ushort)sourceDirBytes.Length;
            reparseDataBuffer.PrintNameOffset = (ushort)(sourceDirBytes.Length + 2);
            reparseDataBuffer.PrintNameLength = 0;
            reparseDataBuffer.PathBuffer = new byte[0x3ff0];
            Array.Copy(sourceDirBytes, reparseDataBuffer.PathBuffer, sourceDirBytes.Length);

            var inBufferSize = Marshal.SizeOf(reparseDataBuffer);
            var inBuffer = Marshal.AllocHGlobal(inBufferSize);

            try {
                Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                int bytesReturned;
                var result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_SET_REPARSE_POINT,
                    inBuffer, sourceDirBytes.Length + 20, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                if (!result)
                    ThrowLastWin32Error($"Unable to create junction point '{sourceDir}' -> '{targetDir}'.");
            } finally {
                Marshal.FreeHGlobal(inBuffer);
            }
        }

        /// <summary>
        /// Deletes a junction point at the specified source directory along with the directory itself.
        /// Does nothing if the junction point does not exist.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="junctionPoint">The junction point path</param>
        [PublicAPI]
        public static void Delete(string junctionPoint) {
            if (!Directory.Exists(junctionPoint)) {
                if (File.Exists(junctionPoint))
                    throw new IOException("Path is not a junction point.");

                return;
            }

            using SafeFileHandle handle = OpenReparsePoint(junctionPoint, EFileAccess.GenericWrite);
            var reparseDataBuffer = new REPARSE_DATA_BUFFER();

            reparseDataBuffer.ReparseTag = IO_REPARSE_TAG_MOUNT_POINT;
            reparseDataBuffer.ReparseDataLength = 0;
            reparseDataBuffer.PathBuffer = new byte[0x3ff0];

            var inBufferSize = Marshal.SizeOf(reparseDataBuffer);
            var inBuffer = Marshal.AllocHGlobal(inBufferSize);
            try {
                Marshal.StructureToPtr(reparseDataBuffer, inBuffer, false);

                int bytesReturned;
                var result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_DELETE_REPARSE_POINT,
                    inBuffer, 8, IntPtr.Zero, 0, out bytesReturned, IntPtr.Zero);

                if (!result)
                    ThrowLastWin32Error("Unable to delete junction point.");
            } finally {
                Marshal.FreeHGlobal(inBuffer);
            }

            try {
                Directory.Delete(junctionPoint);
            } catch (IOException ex) {
                throw new IOException("Unable to delete junction point.", ex);
            }
        }

        /// <summary>
        /// Determines whether the specified path exists and refers to a junction point.
        /// </summary>
        /// <param name="path">The junction point path</param>
        /// <returns>True if the specified path represents a junction point</returns>
        /// <exception cref="IOException">Thrown if the specified path is invalid
        /// or some other error occurs</exception>
        [PublicAPI]
        public static bool Exists(string path) {
            if (!Directory.Exists(path))
                return false;

            using SafeFileHandle handle = OpenReparsePoint(path, EFileAccess.GenericRead);
            string target = InternalGetTarget(handle);
            return target != null;
        }

        /// <summary>
        /// Gets the target of the specified junction point.
        /// </summary>
        /// <remarks>
        /// Only works on NTFS.
        /// </remarks>
        /// <param name="junctionPoint">The junction point path</param>
        /// <returns>The target of the junction point</returns>
        /// <exception cref="IOException">Thrown when the specified path does not
        /// exist, is invalid, is not a junction point, or some other error occurs</exception>
        [PublicAPI]
        public static string GetTarget(string junctionPoint) {
            using var handle = OpenReparsePoint(junctionPoint, EFileAccess.GenericRead);
            var target = InternalGetTarget(handle);
            if (target == null)
                throw new IOException("Path is not a junction point.");

            return target;
        }

        private static string InternalGetTarget(SafeFileHandle handle) {
            var outBufferSize = Marshal.SizeOf(typeof(REPARSE_DATA_BUFFER));
            var outBuffer = Marshal.AllocHGlobal(outBufferSize);

            try {
                int bytesReturned;
                var result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT,
                    IntPtr.Zero, 0, outBuffer, outBufferSize, out bytesReturned, IntPtr.Zero);

                if (!result) {
                    int error = Marshal.GetLastWin32Error();
                    if (error == ERROR_NOT_A_REPARSE_POINT)
                        return null;

                    ThrowLastWin32Error("Unable to get information about junction point.");
                }

                var reparseDataBuffer = (REPARSE_DATA_BUFFER)
                    Marshal.PtrToStructure(outBuffer, typeof(REPARSE_DATA_BUFFER));

                if (reparseDataBuffer.ReparseTag != IO_REPARSE_TAG_MOUNT_POINT)
                    return null;

                var targetDir = Encoding.Unicode.GetString(reparseDataBuffer.PathBuffer,
                    reparseDataBuffer.SubstituteNameOffset, reparseDataBuffer.SubstituteNameLength);

                if (targetDir.StartsWith(NonInterpretedPathPrefix))
                    targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);

                return targetDir;
            } finally {
                Marshal.FreeHGlobal(outBuffer);
            }
        }

        private static SafeFileHandle OpenReparsePoint(string reparsePoint, EFileAccess accessMode) {
            var reparsePointHandle = new SafeFileHandle(CreateFile(reparsePoint, accessMode,
                EFileShare.Read | EFileShare.Write | EFileShare.Delete,
                IntPtr.Zero, ECreationDisposition.OpenExisting,
                EFileAttributes.BackupSemantics | EFileAttributes.OpenReparsePoint, IntPtr.Zero), true);

            if (Marshal.GetLastWin32Error() != 0)
                ThrowLastWin32Error("Unable to open reparse point.");

            return reparsePointHandle;
        }

        private static void ThrowLastWin32Error(string message) {
            throw new IOException(message, Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }
    }
}