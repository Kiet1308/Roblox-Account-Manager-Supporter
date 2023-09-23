using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

public class DllInject
{
    private const int PROCESS_CREATE_THREAD = 2;
    private const int PROCESS_QUERY_INFORMATION = 1024;
    private const int PROCESS_VM_OPERATION = 8;
    private const int PROCESS_VM_WRITE = 32;
    private const int PROCESS_VM_READ = 16;
    private const uint MEM_COMMIT = 4096;
    private const uint MEM_RESERVE = 8192;
    private const uint PAGE_READWRITE = 4;

    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(
      int dwDesiredAccess,
      bool bInheritHandle,
      int dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(
      IntPtr hProcess,
      IntPtr lpAddress,
      uint dwSize,
      uint flAllocationType,
      uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(
      IntPtr hProcess,
      IntPtr lpBaseAddress,
      byte[] lpBuffer,
      uint nSize,
      out UIntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    private static extern IntPtr CreateRemoteThread(
      IntPtr hProcess,
      IntPtr lpThreadAttributes,
      uint dwStackSize,
      IntPtr lpStartAddress,
      IntPtr lpParameter,
      uint dwCreationFlags,
      IntPtr lpThreadId);


    static public int Inject(Process process, string dllLocation)
    {
        IntPtr hProcess = OpenProcess(1082, false, process.Id);
        IntPtr procAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        string str = dllLocation;
        FileInfo fileInfo = new FileInfo(str);
        FileSecurity accessControl = fileInfo.GetAccessControl();
        FileSystemAccessRule rule = new FileSystemAccessRule((IdentityReference)new SecurityIdentifier("S-1-15-2-1"), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow);
        accessControl.AddAccessRule(rule);
        fileInfo.SetAccessControl(accessControl);
        IntPtr num = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((str.Length + 1) * Marshal.SizeOf(typeof(char))), 12288U, 4U);
        WriteProcessMemory(hProcess, num, Encoding.Default.GetBytes(str), (uint)((str.Length + 1) * Marshal.SizeOf(typeof(char))), out UIntPtr _);
        CreateRemoteThread(hProcess, IntPtr.Zero, 0U, procAddress, num, 0U, IntPtr.Zero);
        return 0;
    }

}