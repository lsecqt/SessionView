using System;
using System.Runtime.InteropServices;
using System.Text;
using System.ComponentModel;

namespace SessionView
{
    class Program
    {
        // Constants
        const int WTS_CURRENT_SERVER_HANDLE = 0;

        // Enums
        enum WTS_INFO_CLASS
        {
            WTSSessionId = 4,
            WTSUserName = 5,
            WTSDomainName = 7,
            WTSConnectState = 8,
            WTSSessionInfo = 24
        }

        enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        // Structs
        [StructLayout(LayoutKind.Sequential)]
        struct WTS_SESSION_INFO
        {
            public int SessionId;
            public IntPtr pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WTSINFO
        {
            public int SessionId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
            public string WinStationName;
            public WTS_CONNECTSTATE_CLASS State;
            // Other fields omitted for brevity
        }

        // P/Invoke declarations
        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            ref IntPtr ppSessionInfo,
            ref int pCount);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            int sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out int pBytesReturned);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int ProcessIdToSessionId(int dwProcessId, out int pSessionId);

        // Registry access for session GUID
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegOpenKeyEx(
            IntPtr hKey,
            string lpSubKey,
            int ulOptions,
            int samDesired,
            out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegQueryValueEx(
            IntPtr hKey,
            string lpValueName,
            IntPtr lpReserved,
            out uint lpType,
            byte[] lpData,
            ref int lpcbData);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegCloseKey(IntPtr hKey);

        static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
        const int KEY_READ = 0x20019;
        const int ERROR_SUCCESS = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("=== Windows Server Session Enumerator ===\n");

            IntPtr serverHandle = IntPtr.Zero;
            IntPtr sessionInfoPtr = IntPtr.Zero;
            int sessionCount = 0;

            try
            {
                // Enumerate sessions
                bool result = WTSEnumerateSessions(
                    serverHandle,
                    0,
                    1,
                    ref sessionInfoPtr,
                    ref sessionCount);

                if (!result)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                Console.WriteLine($"Found {sessionCount} session(s)\n");

                int structSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                IntPtr currentSession = sessionInfoPtr;

                for (int i = 0; i < sessionCount; i++)
                {
                    WTS_SESSION_INFO sessionInfo = (WTS_SESSION_INFO)Marshal.PtrToStructure(
                        currentSession,
                        typeof(WTS_SESSION_INFO));

                    string stationName = Marshal.PtrToStringAnsi(sessionInfo.pWinStationName);

                    Console.WriteLine($"Session {i + 1}:");
                    Console.WriteLine($"  Session ID: {sessionInfo.SessionId}");
                    Console.WriteLine($"  Station Name: {stationName}");
                    Console.WriteLine($"  State: {sessionInfo.State}");

                    // Get username
                    string username = GetSessionInfo(serverHandle, sessionInfo.SessionId, WTS_INFO_CLASS.WTSUserName);
                    if (!string.IsNullOrEmpty(username))
                    {
                        Console.WriteLine($"  Username: {username}");

                        // Get domain
                        string domain = GetSessionInfo(serverHandle, sessionInfo.SessionId, WTS_INFO_CLASS.WTSDomainName);
                        if (!string.IsNullOrEmpty(domain))
                        {
                            Console.WriteLine($"  Domain: {domain}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  Username: (No user logged in)");
                    }

                    // Get session GUID from registry
                    string guid = GetSessionGuid(sessionInfo.SessionId);
                    if (!string.IsNullOrEmpty(guid))
                    {
                        Console.WriteLine($"  Session GUID: {guid}");
                    }
                    else
                    {
                        Console.WriteLine($"  Session GUID: (Not available)");
                    }

                    Console.WriteLine();

                    currentSession = (IntPtr)((long)currentSession + structSize);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                if (sessionInfoPtr != IntPtr.Zero)
                {
                    WTSFreeMemory(sessionInfoPtr);
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static string GetSessionInfo(IntPtr serverHandle, int sessionId, WTS_INFO_CLASS infoClass)
        {
            IntPtr buffer = IntPtr.Zero;
            int bytesReturned = 0;

            try
            {
                if (WTSQuerySessionInformation(serverHandle, sessionId, infoClass, out buffer, out bytesReturned))
                {
                    return Marshal.PtrToStringAnsi(buffer);
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    WTSFreeMemory(buffer);
                }
            }

            return string.Empty;
        }

        static string GetSessionGuid(int sessionId)
        {
            IntPtr keyHandle = IntPtr.Zero;

            try
            {
                string keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Remote\Terminal Services\Session\{sessionId}";

                int result = RegOpenKeyEx(
                    HKEY_LOCAL_MACHINE,
                    keyPath,
                    0,
                    KEY_READ,
                    out keyHandle);

                if (result != ERROR_SUCCESS || keyHandle == IntPtr.Zero)
                {
                    return null;
                }

                uint type;
                int dataSize = 256;
                byte[] data = new byte[dataSize];

                result = RegQueryValueEx(
                    keyHandle,
                    "SessionGuid",
                    IntPtr.Zero,
                    out type,
                    data,
                    ref dataSize);

                if (result == ERROR_SUCCESS && dataSize > 0)
                {
                    return Encoding.Unicode.GetString(data, 0, dataSize - 2); // -2 to exclude null terminator
                }
            }
            catch
            {
                // Session might not have a GUID in registry
            }
            finally
            {
                if (keyHandle != IntPtr.Zero)
                {
                    RegCloseKey(keyHandle);
                }
            }

            return null;
        }
    }
}