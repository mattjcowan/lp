@echo off 
SET mypath=%~dp0
IF %1.==. GOTO No1
IF %2.==. GOTO No2

%mypath%linqpad\lprun "%mypath%linqpad\queries\%1\%2" %*
GOTO End1

:No1
call lp
GOTO End1

:No2
call lp %1
GOTO End1

:End1
