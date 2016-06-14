@echo off 
SET mypath=%~dp0
%mypath%linqpad\lprun "%mypath%linqpad\queries\lprun-queries.linq" %1
