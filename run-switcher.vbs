Option Explicit

Dim shell
Dim fso
Dim scriptDir
Dim psScript
Dim cmd
Dim exitCode

Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
psScript = scriptDir & "\run-switcher.ps1"
cmd = "powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File """ & psScript & """"

exitCode = shell.Run(cmd, 0, True)
WScript.Quit exitCode
