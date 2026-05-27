# RDPExtender

**Let more than one person sign in to your Windows PC at the same time — over Remote Desktop.**

Out of the box, Windows only allows one person to be logged in remotely at a time. If someone else is already connected, you get kicked out. RDPExtender flips that limit off, so multiple people can connect to the same PC at the same time without disturbing each other.

You get two ways to use it:

- **RDPExtender app** — a simple window with a big "Enable Multiple User Access" button and an "Undo" button. Made for people who just want it to work.
- **RDPExtender command line** — for IT folks and scripts that need to run it unattended.

Both do exactly the same thing under the hood.

---

## What you can do with it

- **Enable multiple user access** with one click.
- **Restore the original settings** at any time, just as easily.
- **See live status** at a glance — is your Windows version supported? Is multiple access turned on? Is the Remote Desktop service running?
- **Safe by default** — a backup of the original file is made before anything is changed, so you can always go back.
- **Self-healing** — if the file is already enabled, it just tells you and does nothing.
- **No install needed** — it's a single `.exe`. Run it and you're done.
- **Works on 64-bit, 32-bit, and ARM PCs.**

---

## Will it work on my PC?

Yes, on any of these:

| Your Windows | Works? |
| --- | --- |
| Windows 7 SP1 Pro / Enterprise / Ultimate (64-bit) | Yes |
| Windows 10 Pro / Enterprise / Education | Yes |
| Windows 11 22H2 / 23H2 / 24H2 / 25H2 | Yes |
| Windows Server 2016 / 2019 / 2022 / 2025 | Yes |
| Windows Home (any version) | No — Windows Home cannot host Remote Desktop at all |

Not sure which Windows you have? Press `Win + Pause/Break`, or type **About your PC** in the Start menu.

---

## How to use it

### The easy way (app)

1. Download `RDPExtender.exe` and double-click it.
2. Windows will ask for permission (User Account Control) — click **Yes**.
3. The window will show you four status items:
   - **Windows Compatibility** — should say *Supported*.
   - **Multiple User Access** — *Not Enabled* yet.
   - **Restore Point** — whether a backup exists.
   - **Remote Desktop Service** — should say *Running*.
4. Click the big blue **Enable Multiple User Access** button.
5. Wait a few seconds. The bottom of the window will tell you when it's done.
6. That's it — you and others can now Remote Desktop into this PC at the same time.

Want to go back to the original Windows behaviour? Click **Restore Original Settings** in the same window.

### The command-line way

For sysadmins, scripts, or servers without a desktop, download `RDPExtender-CLI.exe`:

```powershell
# Turn on multiple user access
RDPExtender-CLI.exe

# Turn it back off (restore the original)
RDPExtender-CLI.exe revert
```

Run it from an Administrator PowerShell, or just double-click it — it will ask for elevation if needed.

---

## Frequently asked questions

**Is this safe?**
The app makes a backup of the file it changes before touching anything. If anything ever goes wrong, click **Restore Original Settings** and you're back to factory behaviour.

**Do I need to restart my PC?**
Usually no. RDPExtender stops the Remote Desktop service, makes the change, and starts it back up for you.

**Will Windows Update undo it?**
Sometimes, yes. When Microsoft ships a Windows update that replaces the Remote Desktop file, the change is lost. Just open RDPExtender again after the update and click the button — it takes seconds.

**It says "The pattern was not found." What does that mean?**
Microsoft updated the Remote Desktop file in a way the current version of RDPExtender doesn't recognise yet. Check for a newer release of RDPExtender, or open an issue.

**Why does it ask for Administrator?**
Because it changes a protected Windows system file. There's no way around that — the same is true of every tool that enables multi-session Remote Desktop.

---

## Credits

This project builds on the great work of:

- http://woshub.com/how-to-allow-multiple-rdp-sessions-in-windows-10
- https://github.com/infosecn1nja/SharpDoor
- https://github.com/fabianosrc/TermsrvPatcher
- https://samdecrock.medium.com/patching-microsofts-remote-desktop-service-yourself-db25a4d8bc64

---

## A quick word of caution

RDPExtender changes a built-in Windows file. It's designed for your own PCs — for testing, learning, home labs, and personal use. If you're using it on a work or production machine, please check your Windows licence terms first. Use at your own risk.
