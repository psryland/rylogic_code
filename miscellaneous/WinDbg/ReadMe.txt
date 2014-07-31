http://www.codeproject.com/Articles/6084/Windows-Debuggers-Part-A-WinDbg-Tutorial

Use the Application Verifier to set the program you want debugged
 -might need to install Debugging Tools for Windows

Run WinDbg, open exe, hit F5

Set Symbol Paths:
  SRV*P:\local\symbols*http://msdl.microsoft.com/download/symbols;P:\obj
  
Update symbols using:
   C:\Program Files (x86)\Windows Kits\8.1\Debuggers\x64\symchk.exe /r c:\windows\system32 /s SRV*P:\Local\Symbols\*http://msdl.microsoft.com/download/symbols
 -warning tho, this will download .pdb files for all dlls in \windows\system32, ie. several Gb
  you probably don't want to run it, just set the symbol path to download on demand 
