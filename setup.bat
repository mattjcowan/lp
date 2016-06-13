@echo off
set run_linqpad=0
if not exist .\linqpad mkdir linqpad & set run_linqpad=1

if not exist .\linqpad\ConnectionsV2.xml goto create_connectionsv2
goto after_create_connectionsv2

:create_connectionsv2
echo ^<?xml version="1.0" encoding="utf-8"?^> > linqpad\ConnectionsV2.xml
echo ^<Connections^> >> linqpad\ConnectionsV2.xml
echo   ^<Connection^> >> linqpad\ConnectionsV2.xml
echo     ^<ID^>76B0EABB-14C5-4950-A178-DEC23DA21F10^</ID^> >> linqpad\ConnectionsV2.xml
echo     ^<Persist^>true^</Persist^> >> linqpad\ConnectionsV2.xml
echo     ^<Server^>.\sqlexpress^</Server^> >> linqpad\ConnectionsV2.xml
echo     ^<Database^>Northwind^</Database^> >> linqpad\ConnectionsV2.xml
echo   ^</Connection^> >> linqpad\ConnectionsV2.xml
echo   ^<Connection^> >> linqpad\ConnectionsV2.xml
echo     ^<ID^>E7CEF7A3-F123-4F42-B1C4-F0538C562781^</ID^> >> linqpad\ConnectionsV2.xml
echo     ^<Persist^>true^</Persist^> >> linqpad\ConnectionsV2.xml
echo     ^<Server^>.\sqlexpress^</Server^> >> linqpad\ConnectionsV2.xml
echo     ^<Database^>AdventureWorks2014^</Database^> >> linqpad\ConnectionsV2.xml
echo   ^</Connection^> >> linqpad\ConnectionsV2.xml
echo ^</Connections^> >> linqpad\ConnectionsV2.xml
echo ConnectionsV2 is ready!

:after_create_connectionsv2

if not exist .\linqpad\setup.ps1 goto create_setup
goto after_create_setup

:create_setup
echo Add-Type -AssemblyName System.IO.Compression.FileSystem > linqpad\setup.ps1
echo function Unzip >> linqpad\setup.ps1
echo { >> linqpad\setup.ps1
echo     param([string]$zipfile, [string]$outpath) >> linqpad\setup.ps1
echo     [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath) >> linqpad\setup.ps1
echo } >> linqpad\setup.ps1
echo $zip="$PSScriptRoot\LINQPad5.zip" >> linqpad\setup.ps1
echo Get-ChildItem -name LINQPad.exe* -force ^| Remove-Item -force >> linqpad\setup.ps1
echo Get-ChildItem -name lprun.exe* -force ^| Remove-Item -force >> linqpad\setup.ps1
echo (new-object System.Net.WebClient).DownloadFile('http://www.linqpad.net/GetFile.aspx?LINQPad5.zip', $zip) >> linqpad\setup.ps1
echo Unzip $zip "$PSScriptRoot" >> linqpad\setup.ps1
echo Get-ChildItem -name "lprun readme.txt" -force ^| Remove-Item -force >> linqpad\setup.ps1
echo Get-ChildItem -name "LINQPad5.zip" -force ^| Remove-Item -force >> linqpad\setup.ps1
cd linqpad
call powershell ./setup.ps1
cd ..
echo LINQPad is ready!

:after_create_setup

if not exist .\linqpad\ngen001 goto create_ngen001
goto after_create_ngen001

:create_ngen001
cd linqpad
call LINQPad.exe -ngen
cd ..
echo LINQPad is optimized!

:after_create_ngen001

if not exist .\linqpad\queries goto create_queries
goto after_create_queries

:create_queries
mkdir linqpad\queries
echo LINQPad queries go here: .\linqpad\queries

:after_create_queries

if not exist .\linqpad\queries\servicestack-selfhost-example.linq goto create_example
goto after_create_example

:create_example
echo ^<Query Kind="Program"^> > linqpad\queries\servicestack-selfhost-example.linq
echo   ^<NuGetReference^>ServiceStack^</NuGetReference^> >> linqpad\queries\servicestack-selfhost-example.linq
echo   ^<Namespace^>ServiceStack^</Namespace^> >> linqpad\queries\servicestack-selfhost-example.linq
echo   ^<Namespace^>ServiceStack.Text^</Namespace^> >> linqpad\queries\servicestack-selfhost-example.linq
echo ^</Query^> >> linqpad\queries\servicestack-selfhost-example.linq
echo. >> linqpad\queries\servicestack-selfhost-example.linq
echo void Main(string[] args) >> linqpad\queries\servicestack-selfhost-example.linq >> linqpad\queries\servicestack-selfhost-example.linq
echo { >> linqpad\queries\servicestack-selfhost-example.linq
echo     var port = args == null ^|^| args.Length == 0 ? 1337 : int.Parse(args[0]); >> linqpad\queries\servicestack-selfhost-example.linq
echo     var listeningOn = string.Format("http://*:{0}/", port); >> linqpad\queries\servicestack-selfhost-example.linq
echo     var appHost = new AppHost().Init().Start(listeningOn); >> linqpad\queries\servicestack-selfhost-example.linq
echo     Console.WriteLine("AppHost Created at {0}, listening on {1}", DateTime.Now, listeningOn); >> linqpad\queries\servicestack-selfhost-example.linq
echo     Process.Start(string.Format("http://localhost:{0}/hello/World", port)); >> linqpad\queries\servicestack-selfhost-example.linq
echo     Console.Read(); >> linqpad\queries\servicestack-selfhost-example.linq
echo } >> linqpad\queries\servicestack-selfhost-example.linq
echo. >> linqpad\queries\servicestack-selfhost-example.linq
echo public class AppHost : AppSelfHostBase >> linqpad\queries\servicestack-selfhost-example.linq
echo { >> linqpad\queries\servicestack-selfhost-example.linq
echo     public AppHost() : base("HttpListener Self-Host", typeof(HelloService).Assembly) { } >> linqpad\queries\servicestack-selfhost-example.linq
echo     public override void Configure(Funq.Container container) { } >> linqpad\queries\servicestack-selfhost-example.linq
echo } >> linqpad\queries\servicestack-selfhost-example.linq
echo. >> linqpad\queries\servicestack-selfhost-example.linq
echo public class HelloService : Service >> linqpad\queries\servicestack-selfhost-example.linq
echo { >> linqpad\queries\servicestack-selfhost-example.linq
echo     public object Any(Hello request) >> linqpad\queries\servicestack-selfhost-example.linq
echo     { >> linqpad\queries\servicestack-selfhost-example.linq
echo         return new HelloResponse { Result = "Hello, " + request.Name }; >> linqpad\queries\servicestack-selfhost-example.linq
echo     } >> linqpad\queries\servicestack-selfhost-example.linq
echo } >> linqpad\queries\servicestack-selfhost-example.linq
echo. >> linqpad\queries\servicestack-selfhost-example.linq
echo [Route("/hello/{Name}")] >> linqpad\queries\servicestack-selfhost-example.linq
echo public class Hello >> linqpad\queries\servicestack-selfhost-example.linq
echo { >> linqpad\queries\servicestack-selfhost-example.linq
echo     public string Name { get; set; } >> linqpad\queries\servicestack-selfhost-example.linq
echo } >> linqpad\queries\servicestack-selfhost-example.linq
echo. >> linqpad\queries\servicestack-selfhost-example.linq
echo public class HelloResponse >> linqpad\queries\servicestack-selfhost-example.linq
echo { >> linqpad\queries\servicestack-selfhost-example.linq
echo     public string Result { get; set; } >> linqpad\queries\servicestack-selfhost-example.linq
echo } >> linqpad\queries\servicestack-selfhost-example.linq
echo Sample script created!

:after_create_example

if not exist .\servicestack-selfhost-example.bat goto create_example_runner
goto after_create_example_runner

:create_example_runner
echo @echo off > .\servicestack-selfhost-example.bat
echo SET mypath=%%~dp0>> .\servicestack-selfhost-example.bat
echo %%mypath%%linqpad\lprun "%%mypath%%linqpad\queries\servicestack-selfhost-example.linq" 1338 >> .\servicestack-selfhost-example.bat

:after_create_example_runner

if not exist .\linqpad\queries\lprun-queries.linq goto create_lprun_queries
goto after_create_lprun_queries

:create_lprun_queries

echo ^<Query Kind="Program" /^> >> linqpad\queries\lprun-queries.linq
echo. >> linqpad\queries\lprun-queries.linq
echo void Main() >> linqpad\queries\lprun-queries.linq
echo { >> linqpad\queries\lprun-queries.linq
echo     var scripts = Directory.GetFiles(Path.GetDirectoryName(Util.CurrentQueryPath), "*.linq", SearchOption.TopDirectoryOnly); >> linqpad\queries\lprun-queries.linq
echo     Console.WriteLine("\n**** List of available scripts ****"); >> linqpad\queries\lprun-queries.linq
echo     Console.ForegroundColor = ConsoleColor.DarkGray; >> linqpad\queries\lprun-queries.linq
echo     Console.WriteLine("\nCall the scripts as follows:\n  .\\linqpad\\lprun {script_name} {script_args}\n"); >> linqpad\queries\lprun-queries.linq
echo     Console.ResetColor(); >> linqpad\queries\lprun-queries.linq
echo     foreach (var script in scripts) >> linqpad\queries\lprun-queries.linq
echo     { >> linqpad\queries\lprun-queries.linq
echo         var fileName = Path.GetFileName(script); >> linqpad\queries\lprun-queries.linq
echo         if(fileName.Equals("lprun-queries.linq")) continue;>> linqpad\queries\lprun-queries.linq
echo         Console.WriteLine(" - " + fileName); >> linqpad\queries\lprun-queries.linq
echo     } >> linqpad\queries\lprun-queries.linq
echo } >> linqpad\queries\lprun-queries.linq

:after_create_lprun_queries

if not exist .\lp.bat goto create_lp
goto after_create_lp

:create_lp
echo @echo off > .\lp.bat
echo SET mypath=%%~dp0>> .\lp.bat
echo %%mypath%%linqpad\lprun "%%mypath%%linqpad\queries\lprun-queries.linq" >> .\lp.bat

:after_create_lp

if %run_linqpad%==1 start "" ".\linqpad\linqpad.exe" ".\linqpad\queries\servicestack-selfhost-example.linq"

:done
echo DONE!!
