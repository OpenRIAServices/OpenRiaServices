using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.Win32.SafeHandles;

/// <summary>
/// Use to coordinate values between different appdomain/process/terminal/users.
/// </summary>
internal partial class ProcessValueCoordinator : IDisposable
{
    /// <summary>Prefix for the named shared memory file.</summary>
    private const string SharedMemoryName = @"Global\DPRuntimeMemory_";

    /// <summary>Prefix for the named shared semaphore.</summary>
    private const string SemaphoreName = @"Global\DPRuntimeSemaphore_";

    /// <summary>The size fo the shared memory file.</summary>
    private const uint BufferSize = 128;

    /// <summary>The named shared semaphaore.</summary>
    private Semaphore semaphore;

    /// <summary>The named shared memory file.</summary>
    private SafeFileMappingHandle sharedMemoryHandle;

    /// <summary>The mapped view of the shared memory file.</summary>
    private SafeMapViewOfFileHandle sharedMemoryMap;

    /// <summary>Create a syncronized shared memory file</summary>
    /// <param name="name">The suffix of the shared memory file and semaphore.</param>
    public ProcessValueCoordinator(string name)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(name);
        }

        bool created = false;
        this.semaphore = new Semaphore(1, 1, SemaphoreName + name, out created);
        this.sharedMemoryHandle = Win32Native.CreateFileMapping(new SafeFileHandle(IntPtr.Zero, false), IntPtr.Zero, FileMapProtection.PageReadWrite, 0, BufferSize, SharedMemoryName + name);
        this.sharedMemoryMap = Win32Native.MapViewOfFile(this.sharedMemoryHandle, FileMapAccess.FileMapAllAccess, 0, 0, BufferSize);
    }

    /// <summary>Specifies the page protection of the file mapping object.</summary>
    [Flags]
    private enum FileMapProtection : uint
    {
        // PageReadonly = 0x02,
        // PageWriteCopy = 0x08,
        // PageExecuteRead = 0x20,
        // PageExecuteReadWrite = 0x40,
        // SectionCommit = 0x8000000,
        // SectionImage = 0x1000000,
        // SectionNoCache = 0x10000000,
        // SectionReserve = 0x4000000,

        /// <summary>Allows views to be mapped for read-only, copy-on-write, or read/write access.</summary>
        PageReadWrite = 0x04,
    }

    /// <summary>The type of access to a file mapping object, which determines the protection of the pages.</summary>
    [Flags]
    private enum FileMapAccess : uint
    {
        // FileMapCopy = 0x0001,
        // FileMapWrite = 0x0002,
        // FileMapRead = 0x0004,
        // fileMapExecute = 0x0020,

        /// <summary>A read/write view of the file is mapped.</summary>
        FileMapAllAccess = 0x001f,
    }

    /// <summary>Dispose of the native resources.</summary>
    public void Dispose()
    {
        Util.Dispose(ref this.sharedMemoryMap);
        Util.Dispose(ref this.sharedMemoryHandle);
        Util.Dispose(ref this.semaphore);
    }

    /// <summary>Read/Write the first integer in the syncronized shared memory</summary>
    /// <param name="action">Delegate that takes the value read from shared memory and returns the value to write to shared memory.</param>
    /// <returns>The value written to shared memory.</returns>
    public int Operate(Func<int, int> action)
    {
        if (null == action)
        {
            throw new ArgumentNullException("action");
        }

        return this.Operate(Marshal.ReadInt32, action, Marshal.WriteInt32, 0);
    }

    /// <summary>Method to syncronize the shared memory.</summary>
    /// <typeparam name="T">The TypeOf the value type structure being read/write in shared memory.</typeparam>
    /// <param name="read">Delegate to read from shared memory.</param>
    /// <param name="action">Delegate that takes the value read from shared memory and returns the value to write to shared memory.</param>
    /// <param name="write">Delegate to write to shared memory.</param>
    /// <param name="offset">Byte offset in the shared memory buffer.</param>
    /// <returns>The value written to shared memory.</returns>
    public T Operate<T>(Func<IntPtr, int, T> read, Func<T, T> action, Action<IntPtr, int, T> write, int offset) where T : struct
    {
        if ((offset < 0) || (BufferSize < (offset + Marshal.SizeOf(typeof(T)))))
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        T current = default(T);
        Semaphore local = this.semaphore;
        SafeMapViewOfFileHandle handle = this.sharedMemoryMap;
        if ((null != local) && (null != handle))
        {
            bool waitSuccess = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                if (WaitOne(local, UInt32.MaxValue, ref waitSuccess))
                {
                    bool memorySuccess = false;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        handle.DangerousAddRef(ref memorySuccess);
                        IntPtr memory = handle.DangerousGetHandle();
                        write(memory, offset, current = action(read(memory, offset)));
                    }
                    finally
                    {
                        if (memorySuccess)
                        {
                            handle.DangerousRelease();
                        }
                    }
                }
            }
            finally
            {
                if (waitSuccess)
                {
                    local.Release();
                }
            }
        }

        return current;
    }

    /// <summary>Reliable WaitOne</summary>
    /// <param name="waitHandle">object to wait on</param>
    /// <param name="millisecondsTimeout">The time-out interval, in milliseconds. If a nonzero value is specified, the function waits until the object is signaled or the interval elapses. If dwMilliseconds is zero, the function does not enter a wait state if the object is not signaled; it always returns immediately. If dwMilliseconds is INFINITE, the function will return only when the object is signaled.</param>
    /// <param name="signaled">True if signaled, false if timeout.</param>
    /// <returns>Returns the <paramref name="signaled"/> value.</returns>
    /// <exception cref="AbandonedMutexException">If mutex was abandoned.</exception>
    [SecurityCritical]
    private static bool WaitOne(WaitHandle waitHandle, uint millisecondsTimeout, ref bool signaled)
    {
        signaled = false;
        SafeWaitHandle safeWaitHandle = waitHandle.SafeWaitHandle;
        RuntimeHelpers.PrepareConstrainedRegions();
        try
        {
            // the default usage of local.WaitOne() can get interrupted on return
            // meaning we may successfully be signalled, but not know we should release
            // this solves that problem by waiting inside a reliability block that won't be interrupted
        }
        finally
        {
            switch (Win32Native.WaitForSingleObject(safeWaitHandle, millisecondsTimeout))
            {
                case Win32Native.WaitObject0:
                    signaled = true;
                    break;
                case Win32Native.WaitAbandoned:
                    throw new AbandonedMutexException();
                case Win32Native.WaitTimeout:
                    signaled = false;
                    break;
                case Win32Native.WaitFailed:
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    break;
            }
        }

        return signaled;
    }

    /// <summary>Win32 native methods</summary>
    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    private static class Win32Native
    {
        /// <summary>name of the file with win32 methods</summary>
        internal const string Kernel32 = "kernel32.dll";

        /// <summary>
        /// The specified object is a mutex object that was not released by the thread that owned the mutex object before the owning thread terminated. Ownership of the mutex object is granted to the calling thread and the mutex state is set to nonsignaled.
        /// </summary>
        internal const UInt32 WaitAbandoned = 0x00000080;

        /// <summary>
        /// The state of the specified object is signaled.
        /// </summary>
        internal const UInt32 WaitObject0 = 0x00000000;

        /// <summary>
        /// The time-out interval elapsed, and the object's state is nonsignaled.
        /// </summary>
        internal const UInt32 WaitTimeout = 0x00000102;

        /// <summary>
        /// The function has failed.
        /// </summary>
        internal const UInt32 WaitFailed = 0xFFFFFFFF;

        /// <summary>
        /// Creates or opens a named or unnamed file mapping object for a specified file.
        /// </summary>
        /// <param name="handle">
        /// A handle to the file from which to create a file mapping object. 
        /// If hFile is INVALID_HANDLE_VALUE, the calling process must also specify a size for the file mapping object in the dwMaximumSizeHigh and dwMaximumSizeLow parameters. In this scenario, CreateFileMapping creates a file mapping object of a specified size that is backed by the system paging file instead of by a file in the file system.
        /// </param>
        /// <param name="attributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether a returned handle can be inherited by child processes.</param>
        /// <param name="protect">Specifies the page protection of the file mapping object. All mapped views of the object must be compatible with this protection.</param>
        /// <param name="maximumSizeHigh">The high-order DWORD of the maximum size of the file mapping object.</param>
        /// <param name="maximumSizeLow">The low-order DWORD of the maximum size of the file mapping object.</param>
        /// <param name="name">The name of the file mapping object.</param>
        /// <returns>If the function succeeds, the return value is a handle to the newly created file mapping object.</returns>
        [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern SafeFileMappingHandle CreateFileMapping(SafeFileHandle handle, IntPtr attributes, FileMapProtection protect, uint maximumSizeHigh, uint maximumSizeLow, string name);

        /// <summary>
        /// Maps a view of a file mapping into the address space of a calling process.
        /// </summary>
        /// <param name="handle">A handle to a file mapping object. The CreateFileMapping and OpenFileMapping functions return this handle.</param>
        /// <param name="desiredAccess">The type of access to a file mapping object, which determines the protection of the pages.</param>
        /// <param name="fileOffsetHigh">A high-order DWORD of the file offset where the view begins.</param>
        /// <param name="fileOffsetLow">A low-order DWORD of the file offset where the view is to begin. The combination of the high and low offsets must specify an offset within the file mapping. They must also match the memory allocation granularity of the system. That is, the offset must be a multiple of the allocation granularity. To obtain the memory allocation granularity of the system, use the GetSystemInfo function, which fills in the members of a SYSTEM_INFO structure.</param>
        /// <param name="numberOfBytesToMap">The number of bytes of a file mapping to map to the view. All bytes must be within the maximum size specified by CreateFileMapping. If this parameter is 0 (zero), the mapping extends from the specified offset to the end of the file mapping.</param>
        /// <returns>If the function succeeds, the return value is the starting address of the mapped view.</returns>
        [DllImport(Kernel32, SetLastError = true, ExactSpelling = true)]
        internal static extern SafeMapViewOfFileHandle MapViewOfFile(SafeFileMappingHandle handle, FileMapAccess desiredAccess, uint fileOffsetHigh, uint fileOffsetLow, uint numberOfBytesToMap);

        /// <summary>
        /// Unmaps a mapped view of a file from the calling process's address space.
        /// </summary>
        /// <param name="baseAddress">
        /// A pointer to the base address of the mapped view of a file that is to be unmapped. This
        /// value must be identical to the value returned by a previous call to the MapViewOfFile
        /// or MapViewOfFileEx function.
        /// </param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport(Kernel32, ExactSpelling = true)]
        internal static extern bool UnmapViewOfFile(IntPtr baseAddress);

        /// <summary>
        /// Waits until the specified object is in the signaled state, an I/O completion routine or
        /// asynchronous procedure call (APC) is queued to the thread, or the time-out interval elapses.
        /// </summary>
        /// <param name="handle">A handle to the object.</param>
        /// <param name="milliseconds">
        /// The time-out interval, in milliseconds. If a nonzero value is specified, the function waits
        /// until the object is signaled or the interval elapses. If dwMilliseconds is zero, the
        /// function does not enter a wait state if the object is not signaled; it always returns
        /// immediately. If dwMilliseconds is INFINITE, the function will return only when the object is
        /// signaled.
        /// </param>
        /// <returns>If the function succeeds, the return value indicates the event that caused the function to return.</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport(Kernel32)]
        internal static extern uint WaitForSingleObject(SafeWaitHandle handle, uint milliseconds);

        /// <summary>Closes an open object handle.</summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport(Kernel32, SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);
    }

    /// <summary>Safe handle for CreateFileMapping</summary>
    [SecurityCritical]
    private sealed class SafeFileMappingHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>default constructor</summary>
        [SecurityCritical]
        internal SafeFileMappingHandle()
            : base(true)
        {
        }

        /// <summary>specific constructor</summary>
        /// <param name="handle">handle</param>
        /// <param name="ownsHandle">ownership</param>
        [SecurityCritical]
        internal SafeFileMappingHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            this.SetHandle(handle);
        }

        /// <summary>critical release</summary>
        /// <returns>true if released</returns>
        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return Win32Native.CloseHandle(this.handle);
        }
    }

    /// <summary>Safe handle for MapViewOfFile</summary>
    [SecurityCritical]
    private sealed class SafeMapViewOfFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>default constructor</summary>
        [SecurityCritical]
        internal SafeMapViewOfFileHandle()
            : base(true)
        {
        }

        /// <summary>specific constructor</summary>
        /// <param name="handle">handle</param>
        /// <param name="ownsHandle">ownership</param>
        [SecurityCritical]
        internal SafeMapViewOfFileHandle(IntPtr handle, bool ownsHandle)
            : base(ownsHandle)
        {
            this.SetHandle(handle);
        }

        /// <summary>critical release</summary>
        /// <returns>true if released</returns>
        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return Win32Native.UnmapViewOfFile(handle);
        }
    }
}