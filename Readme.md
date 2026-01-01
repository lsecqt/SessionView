# SessionView

A lightweight C# console application for enumerating and displaying all user sessions on Windows Server, including session GUIDs.

## Features

- **Complete Session Enumeration**: Lists all active sessions on the Windows server
- **Detailed Session Information**: Displays Session ID, Station Name, Connection State, Username, and Domain
- **Session GUID Retrieval**: Extracts the unique GUID identifier for each session from the Windows registry
- **Connection State Detection**: Shows whether sessions are Active, Disconnected, Idle, etc.
- **Clean Console Output**: Easy-to-read formatted output for quick session analysis

## Requirements

- Windows Server (2012 R2 or later) or Windows Client (Windows 10/11)
- .NET Framework 4.7.2+
- Administrator privileges (required for accessing session information and registry)

## Installation

### Option 1: Build from Source

1. Clone or download the repository
2. Open the solution in Visual Studio 2019 or later
3. Build the solution (Ctrl+Shift+B)
4. The executable will be located in `bin\Debug` or `bin\Release`

### Option 2: Command Line Build

# Using MSBuild (Developer Command Prompt for Visual Studio)
msbuild SessionView.csproj /p:Configuration=Release

# Or using CSC directly
csc /out:SessionView.exe /platform:anycpu Program.cs

## Usage

1. **Run as Administrator**: This toold needs elevated token
2. The application will automatically enumerate all sessions and display the information

### Example Output

```
=== Windows Server Session Enumerator ===

Found 3 session(s)

Session 1:
  Session ID: 0
  Station Name: Services
  State: WTSDisconnected
  Username: (No user logged in)
  Session GUID: (Not available)

Session 2:
  Session ID: 1
  Station Name: Console
  State: WTSActive
  Username: Administrator
  Domain: MYSERVER
  Session GUID: {A1B2C3D4-E5F6-7890-ABCD-EF1234567890}

Session 3:
  Session ID: 2
  Station Name: RDP-Tcp#1
  State: WTSDisconnected
  Username: JohnDoe
  Domain: COMPANY
  Session GUID: {B2C3D4E5-F6A7-8901-BCDE-F12345678901}

Press any key to exit...
```

## Session States

The application can display the following connection states:

- **WTSActive**: User is actively using the session
- **WTSConnected**: Session is connected
- **WTSDisconnected**: Session is disconnected but still running
- **WTSIdle**: Session is idle
- **WTSListen**: Session is listening for connections
- **WTSReset**: Session is being reset
- **WTSDown**: Session is down
- **WTSInit**: Session is initializing

## Technical Details

### APIs Used

- **WTS API (wtsapi32.dll)**: For enumerating sessions and querying session information
- **Windows Registry API (advapi32.dll)**: For retrieving session GUIDs from the registry

### Registry Location

Session GUIDs are stored at:
```
HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Remote\Terminal Services\Session\{SessionId}\SessionGuid
```

### Known Limitations

- Session 0 (Services session) typically does not have a GUID
- Requires administrator privileges to access session information
- Some sessions may not have all information available depending on their state

## Troubleshooting

### "Access Denied" Error
- Ensure you're running the application as Administrator
- Check that you have proper permissions on the server

### Missing Session GUIDs
- This is normal for certain system sessions (like Session 0)
- Some disconnected sessions may not retain their GUID in the registry

### No Sessions Found
- Verify you're running on a Windows Server with Terminal Services enabled
- Check that the Remote Desktop Services role is installed

## Use Cases

- **System Administration**: Monitor active and disconnected user sessions
- **Remote Desktop Management**: Track RDP connections and their states
- **Security Auditing**: Identify user sessions and their unique identifiers
- **Session Management Scripts**: Integrate with automation tools for session cleanup
- **Troubleshooting**: Diagnose session-related issues on Terminal Servers

## License

This project is provided as-is for educational and administrative purposes.

## Contributing

Feel free to submit issues, fork the repository, and create pull requests for any improvements.