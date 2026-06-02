' Starts Microled NFe Local Agent without a visible console window.
' Used by Startup folder and optional shortcuts.

Option Explicit

Dim shell, fso, installDir, exePath, launcherPath

Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

installDir = fso.GetParentFolderName(WScript.ScriptFullName)
exePath = installDir & "\Microled.Nfe.LocalAgent.Api.exe"
launcherPath = installDir & "\StartLocalAgent.cmd"

If Not fso.FileExists(exePath) Then
    MsgBox "Microled NFe Local Agent not found:" & vbCrLf & exePath, vbCritical, "Microled NFe"
    WScript.Quit 1
End If

' Avoid duplicate instances (simple check via WMI)
Dim wmi, processes, proc, alreadyRunning
alreadyRunning = False
Set wmi = GetObject("winmgmts:\\.\root\cimv2")
Set processes = wmi.ExecQuery("SELECT Name FROM Win32_Process WHERE Name = 'Microled.Nfe.LocalAgent.Api.exe'")
For Each proc In processes
    alreadyRunning = True
    Exit For
Next

If alreadyRunning Then
    WScript.Quit 0
End If

shell.CurrentDirectory = installDir
shell.Run """" & exePath & """", 0, False
