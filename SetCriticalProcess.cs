using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;



// Created by inputt star my repo! https://github.com/inputtdevv/SetCriticalProcess/
// Also follow my github https://github.com/inputtdevv
namespace SetCriticalProcess
{
    class Program
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        static extern int NtSetInformationProcess(IntPtr hProcess, int processInformationClass, ref int processInformation, int processInformationLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        const uint AdjustPrivileges = 0x0020;
        const uint TokenQuery = 0x0008;
        const uint PrivilegeEnabled_Se = 0x00000002;
        const string SeDebug = "SeDebugPrivilege";

        struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }

        const int BreakOnTermination = 0x1D;

        static void Main(string[] args)
        {
            if (!IsRunningAsAdmin())
            {
                RelaunchAsAdmin();
                return;
            }

            EnableDebugPrivilege();

            int criticalFlag = 1;
            int result = NtSetInformationProcess(Process.GetCurrentProcess().Handle, BreakOnTermination, ref criticalFlag, 4);
            if (result != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] Failed to set critical process flag. Error: " + result);
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[+] Process set as critical.");
                Console.ResetColor();
            }

            int currentPid = Process.GetCurrentProcess().Id;

            ConsoleColor[] gradientColors = {
                ConsoleColor.DarkGreen, ConsoleColor.Green, ConsoleColor.DarkGreen,
                ConsoleColor.Green, ConsoleColor.DarkGreen, ConsoleColor.Green
            };

            string pidText = $"[+] PID : {currentPid}";
            for (int i = 0; i < pidText.Length; i++)
            {
                Console.ForegroundColor = gradientColors[i % gradientColors.Length];
                Console.Write(pidText[i]);
            }
            Console.ResetColor();
            Console.WriteLine("\nSetCriticalProcess -> https://github.com/inputtdevv/SetCriticalProcess/new/main | Follow my github | https://github.com/inputtdevv/ ");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void EnableDebugPrivilege()
        {
            IntPtr token;
            if (OpenProcessToken(Process.GetCurrentProcess().Handle, AdjustPrivileges | TokenQuery, out token))
            {
                try
                {
                    LUID luid;
                    if (LookupPrivilegeValue(null, SeDebug, out luid))
                    {
                        TOKEN_PRIVILEGES tokenPrivileges = new TOKEN_PRIVILEGES
                        {
                            PrivilegeCount = 1,
                            Privileges = new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = PrivilegeEnabled_Se }
                        };
                        AdjustTokenPrivileges(token, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                finally
                {
                    CloseHandle(token);
                }
            }
        }

        static bool IsRunningAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RelaunchAsAdmin()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                Verb = "runas"
            };
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to elevate privileges.");
            }
            Environment.Exit(0);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);
    }
}
