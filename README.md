# lp
Get started with LINQPad and the lprun cli scripting environment with one file.

Clone the repo or download the setup.bat file:

```
powershell -Command "Invoke-WebRequest https://raw.githubusercontent.com/mattjcowan/lp/master/setup.bat -OutFile setup.bat"
```

From the command line, run:

```
.\setup.bat
```

Once everything is up and running, optionally add the directory to your path variable to easily run your scripts.

Refer to the [LINQPad](https://www.linqpad.net/) and [lprun](https://www.linqpad.net/lprun.aspx) documentation for more information.

## Sample scripts

Script | Description | |
--- | --- | ---
[**db-to-json.linq**](#db-to-jsonlinq)| Exports a database schema to a json file.  | [Source](https://github.com/mattjcowan/lp/blob/master/linqpad/queries/db-to-json.linq)

### **db-to-json.linq**

Download the script:
```
powershell -Command "Invoke-WebRequest https://raw.githubusercontent.com/mattjcowan/lp/master/linqpad/queries/db-to-json.linq -OutFile .\linqpad\queries\db-to-json.linq"
```

Run the script:
```
.\linqpad\lprun.exe .\linqpad\queries\db-to-json.linq /dialect=SqlServer /output=adventureworks.json /connectionstring="Data Source=.\sqlexpress;Initial Catalog=AdventureWorks2014;Integrated Security=True"
```
