:: This works by forwarding all arguments to a simple program called
:: 'fwd.exe' that has the 'Run as Administrator' property set.
:: This is better than the 'runas' command because it causes a UAC dialog
:: to pop up rather than asking for a password. Plus the executed program
:: is still run under the current user account, not under the Administrator
:: account.
@echo off
\bin\fwd.exe %*
