# RDP Extender

RDP Extender is a Windows utility that patches `termsrv.dll` to extend Remote Desktop functionality and allow multiple concurrent RDP sessions on supported Windows editions.


---

## Features

- Enables multiple concurrent Remote Desktop sessions
- Patches Windows `termsrv.dll`
- Supports supported Windows client and server editions
- Stops required Remote Desktop services before patching
- Creates a backup of the original `termsrv.dll`
- Takes ownership and updates permissions for patching
- Restores permissions after patching
- Restarts Remote Desktop services after patching
- Detects already patched files
- Supports retry mechanism while replacing protected system files
- Runs with administrator privileges

---

## Supported Windows Versions

- Windows 7 Pro SP1 64-bit
- Windows 10
- Windows 11 22H2
- Windows 11 23H2
- Windows 11 24H2
- Windows 11 25H2
- Windows Server 2016
- Windows Server 2019
- Windows Server 2022
- Windows Server 2025

> **Note:** This tool is intended for systems where Remote Desktop Host is already supported, such as Windows Pro, Enterprise, Education, and Server editions. Windows Home is not officially supported as an RDP host.

---

---

## How It Works

RDP Extender modifies the Windows Remote Desktop service library:

```text
C:\Windows\System32\termsrv.dll
```

Before patching, it stops related services such as:

```text
UmRdpService
SessionEnv
TermService
```

Then it backs up and patches `termsrv.dll`, restores permissions, and starts the services again.

---

## Usage

1. Run the executable as Administrator.
2. Wait for the patching process to complete.
3. Restart the system if required.
4. Connect using Remote Desktop.

---

## Backup

Before modifying `termsrv.dll`, the tool creates a backup copy:

```text
C:\Windows\System32\termsrv.dll.copy
```

Keep this backup safe in case you need to restore the original file.

---

## References

This project is inspired by and references the following public resources:

- http://woshub.com/how-to-allow-multiple-rdp-sessions-in-windows-10
- https://github.com/infosecn1nja/SharpDoor
- https://github.com/fabianosrc/TermsrvPatcher/tree/main
- https://samdecrock.medium.com/patching-microsofts-remote-desktop-service-yourself-db25a4d8bc64

