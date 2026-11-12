Set WshShell = CreateObject("WScript.Shell")
WshShell.CurrentDirectory = "E:\NexusIsland\bin\Release\net8.0-windows10.0.19041.0"
WshShell.Run """NexusIsland.exe""", 0, False
