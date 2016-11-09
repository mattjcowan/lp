# lp

## Getting Started

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

## Script Library

Script | Description | |
--- | --- | ---
[**db-to-json.linq**](#db-to-jsonlinq)| Exports a database schema to a json file.  | [Source](https://github.com/mattjcowan/lp/blob/master/linqpad/queries/db-to-json.linq)
[**static-gen.linq**](#static-genlinq)| Generate code or files using handlebars templates, json data, and linq scripts.  | [Source](https://github.com/mattjcowan/lp/blob/master/linqpad/queries/static-gen.linq)

### **db-to-json.linq**

This script will extract the schema of a database (MySql, SqlServer, Oracle, PostgreSql) and export it to a json file.
Use this in combination with the [static-gen.linq](#static-genlinq) script to generate files and/or code from the json file.

Download the script:
```
powershell -Command "Invoke-WebRequest https://raw.githubusercontent.com/mattjcowan/lp/master/linqpad/queries/db-to-json.linq -OutFile .\linqpad\queries\db-to-json.linq"
```

Run the script:
```
.\linqpad\lprun.exe .\linqpad\queries\db-to-json.linq /dialect=SqlServer /output=adventureworks.json /connectionstring="Data Source=.\sqlexpress;Initial Catalog=AdventureWorks2014;Integrated Security=True"
```

### **static-gen.linq**

This script executes the following:

- Executes any *.linq files in a data directory and outputs the results of the scripts (if the result is valid JSON) to *.json files with the same name as the script.
- Reads all *.json files in a data directory and adds them to the global dictionary context with the key being the name of the file (without the extension) and the contents the value
- Compiles all *.hbs (handlebars) files in a templates directory starting with a "_" character and registers them as partials, with the name of the partial being the name of the file (minus the "_" character and without the extension)
- Compiles and renders all other *.hbs (handlebars) files (passing in the global context populated from the data files above) and outputting them to a specified 'output' directory. No file extensions are added to the files; so the *.hbs template file names should have the intended extension as part of their name (i.e.: index.html.hbs,  DbRepository.cs.hbs).

Download the script:
```
powershell -Command "Invoke-WebRequest https://raw.githubusercontent.com/mattjcowan/lp/master/linqpad/queries/static-gen.linq -OutFile .\linqpad\queries\static-gen.linq"
```

Run the script:
```
.\linqpad\lprun.exe .\linqpad\queries\static-gen.linq /data=aw\data /templates=aw\templates /output=aw\output
```

