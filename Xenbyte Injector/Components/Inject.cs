using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Xenbyte_Injector.Components
{
    public enum InjectionRoutine
    {
        LoadLibraryA,
        LoadLibraryW
    }

    public class Inject
    {
        protected uint MEM_RESERVE = 0x00002000;
        protected uint MEM_COMMIT = 0x00001000;
        protected uint PAGE_EXECUTE_READWRITE = 0x40;
        protected uint MEM_RELEASE = 0x00008000;

        [DllImport("ntdll.dll")]
        protected static extern uint NtSuspendProcess(IntPtr handle);

        [DllImport("ntdll.dll")]
        protected static extern uint NtResumeProcess(IntPtr handle);

        [DllImport("kernel32.dll")]
        protected static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll")]
        protected static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        protected static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        protected static extern int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        protected static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        protected static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        protected static extern uint VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        public List<Process> Processes { get; set; }
        public InjectionRoutine Routine { get; set; }
        public bool Freeze { get; set; }

        protected Func<Action<Process>, bool> GenericInjection { get; set; }

        public Inject(Process process, InjectionRoutine routine = InjectionRoutine.LoadLibraryA, bool freeze = true) : this(new List<Process> { process }, routine, freeze) { }

        public Inject(List<Process> processes, InjectionRoutine routine = InjectionRoutine.LoadLibraryA, bool freeze = true)
        {
            Processes = processes;
            Routine = routine;
            Freeze = freeze;

            GenericInjection = procedure =>
            {
                foreach (var process in Processes)
                {
                    procedure(process);
                }

                return true;
            };
        }

        public bool InjectLibrary(List<string> dllList)
        {
            if (dllList.Count == 0)
            {
                return true;
            }

            if (Freeze)
            {
                Suspend();
            }

            bool result = GenericInjection(process =>
            {
                foreach (var dll in dllList)
                {
                    if (!File.Exists(dll))
                    {
                        continue;
                    }

                    if (Routine == InjectionRoutine.LoadLibraryA)
                    {
                        uint dllSize = (uint)dll.Length + 1;

                        withRemoteVirtualMemory(process.Handle, dllSize, remoteAddress =>
                        {
                            IntPtr thread = CreateRemoteThread(process.Handle, IntPtr.Zero, 0, GetProcAddress(GetModuleHandle("kernelbase.dll"), "LoadLibraryA"), remoteAddress, 0, IntPtr.Zero);
                            if (thread != IntPtr.Zero)
                            {
                                WaitForSingleObject(thread, 4000);
                            }
                        }, Encoding.ASCII.GetBytes(dll));
                    }
                    else if (Routine == InjectionRoutine.LoadLibraryW)
                    {
                        uint dllSize = (uint)dll.Length * 2 + 1;

                        withRemoteVirtualMemory(process.Handle, dllSize, remoteAddress =>
                        {
                            IntPtr thread = CreateRemoteThread(process.Handle, IntPtr.Zero, 0, GetProcAddress(GetModuleHandle("kernelbase.dll"), "LoadLibraryW"), remoteAddress, 0, IntPtr.Zero);
                            if (thread != IntPtr.Zero)
                            {
                                WaitForSingleObject(thread, 4000);
                            }
                        }, Encoding.Unicode.GetBytes(dll)); // UTF-16 = Unicode
                    }
                }
            });

            if (Freeze)
            {
                Resume();
            }

            return result;

        }

        public bool Suspend()
        {
            uint STATUS_SUCCESS = 0;

            bool result = true;

            foreach (var process in Processes)
            {
                result &= (NtSuspendProcess(process.Handle) == STATUS_SUCCESS);
            }

            return result;
        }

        public bool Resume()
        {
            uint STATUS_SUCCESS = 0;

            bool result = true;

            foreach (var process in Processes)
            {
                result &= (NtResumeProcess(process.Handle) == STATUS_SUCCESS);
            }

            return result;
        }

        private void withRemoteVirtualMemory(IntPtr handle, uint size, Action<IntPtr> function, byte[] valueAtRemoteMemory)
        {
            IntPtr remoteAddress = VirtualAllocEx(handle, IntPtr.Zero, size, MEM_RESERVE | MEM_COMMIT, PAGE_EXECUTE_READWRITE);
            if (remoteAddress != IntPtr.Zero)
            {
                if (valueAtRemoteMemory.Length != 0)
                {
                    WriteProcessMemory(handle, remoteAddress, valueAtRemoteMemory, size, 0);
                }
                function(remoteAddress);
                VirtualFreeEx(handle, remoteAddress, 0, MEM_RELEASE);
            }
        }
    }
}
