Set WshShell = CreateObject("WScript.Shell")
WshShell.CurrentDirectory = "C:\Users\SMBK\EnterprisePOS.Desktop\publish_desktop"

' Check if backend is already listening, if not start it silently
On Error Resume Next
Set http = CreateObject("MSXML2.ServerXMLHTTP.6.0")
http.open "GET", "http://127.0.0.1:5288/ping", False
http.send

If http.status <> 200 Then
    WshShell.Run "dotnet EnterprisePOS.dll", 0, False
    WScript.Sleep 3000
End If

WshShell.Run "EnterprisePOS.Desktop.exe", 1, False
