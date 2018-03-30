<Query Kind="Program">
  <NuGetReference>CommandLineParser</NuGetReference>
  <NuGetReference>Microsoft.SqlServer.SqlManagementObjects</NuGetReference>
  <Namespace>CommandLine</Namespace>
  <Namespace>CommandLine.Text</Namespace>
  <Namespace>Microsoft.SqlServer.Management.Sdk.Sfc</Namespace>
  <Namespace>Microsoft.SqlServer.Management.Smo</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.ComponentModel.Composition</Namespace>
  <Namespace>System.ComponentModel.Composition.Hosting</Namespace>
  <Namespace>System.ComponentModel.Composition.Primitives</Namespace>
  <Namespace>System.ComponentModel.Composition.ReflectionModel</Namespace>
  <Namespace>System.Reflection</Namespace>
</Query>

// CALL THIS USING:
// lprun sqlserver_export_ddl.linq -- -s (local) -c Northwind
// lprun sqlserver_export_ddl.linq -- -s (local) -c Northwind -o export_northwind_tables.sql -i tables -t "customers,*prod*"

class Options
{
	[Option('s', "server", Required = true, Default = "(local)", HelpText = "The sql server instance to connect to.")]
	public string Server { get; set; }

	[Option('c', "catalog", Required = true, HelpText = "The sql server instance catalog to connect to.")]
	public string Catalog { get; set; }

	[Option('o', "output", Required = false, HelpText = "File to output results to (otherwise, will output to a random file name in the current directory).")]
	public string Output { get; set; }

	[Option('i', "includes", Required = false, Default = "default", HelpText = "CSV list of items to include in export: default (database,tables,procedures,views), all, database, tables, procedures, views, users, userdefinedfunctions, userdefineddatatypes")]
	public string Includes { get; set; }

	[Option('t', "tables", Required = false, HelpText = "Restrict tables (accepts *, ? wildcards)")]
	public string TablesFilter { get; set; }
	
	[Option('p', "procedures", Required = false, HelpText = "Restrict procedures (accepts *, ? wildcards)")]
	public string ProceduresFilter { get; set; }
	
	[Option('v', "views", Required = false, HelpText = "Restrict views (accepts *, ? wildcards)")]
	public string ViewsFilter { get; set; }

	// Omitting long name, defaults to name of property, ie "--verbose"
	[Option(Default = false, HelpText = "Prints all messages to standard output.")]
	public bool Verbose { get; set; }

	[Option(Default = true, HelpText = "Script drops.")]
	public bool ScriptDrops { get; set; }

	[Option(Default = true, HelpText = "Include if not exists.")]
	public bool IncludeIfNotExists { get; set; }
}

[Flags]
public enum IncludeOptions
{
	None = 0,
	Database = 1,
	Tables = 2,
	Procedures = 4,
	Views = 8,
	Users = 16,
	UserDefinedFunctions = 32,
	UserDefinedDataTypes = 64,
	Default = Database | Tables | Procedures | Views,
	All = Database | Tables | Procedures | Views | Users | UserDefinedFunctions | UserDefinedDataTypes
}

static void Main(string[] args)
{
	if (args == null) args = new string[0];
	CommandLine.Parser.Default.ParseArguments<Options>(args)
	  .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts));
//	  .WithNotParsed<Options>((errs) => HandleParseError(errs));
}

//static void HandleParseError(IEnumerable<Error> errs)
//{
//	Console.Error.WriteLine("Invalid command input: ");
//	foreach (var err in errs)
//	{
//		Console.Error.WriteLine(" - {0}: {1}", err.Tag, err.ToString());
//	}
//}

static void RunOptionsAndReturnExitCode(Options opts)
{
	var outputFile = string.IsNullOrWhiteSpace(opts.Output) ?
		Path.Combine(Directory.GetCurrentDirectory(), "export_" + DateTime.Now.Ticks + ".sql") : opts.Output;

	if (opts.Verbose)
	{
		Console.WriteLine("Outputting to file: " + outputFile);
	}

	var includeOptions = opts.Includes.Split(new []{','}, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray();
	var scriptIncludes = includeOptions.Select(i => {
	 	IncludeOptions io;
		if (Enum.TryParse<IncludeOptions>(i, true, out io))
			return io;
		else
			return IncludeOptions.None;
	}).ToArray();
	opts.Dump("OPTS");
	scriptIncludes.Dump("scriptIncludes");

	Func<string, string, bool> tableFilter = null;
	Func<string, string, bool> procedureFilter = null;
	Func<string, string, bool> viewFilter = null;
	var TablesFilter = opts.TablesFilter?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray();
	var ProceduresFilter = opts.ProceduresFilter?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray();
	var ViewsFilter = opts.ViewsFilter?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray();
	if (TablesFilter != null && TablesFilter.Count() > 0) tableFilter = (s, t) => TablesFilter.Any(filter => t.MatchesPattern(filter));
	if (ProceduresFilter != null && ProceduresFilter.Count() > 0) procedureFilter = (s, t) => ProceduresFilter.Any(filter => t.MatchesPattern(filter));
	if (ViewsFilter != null && ViewsFilter.Count() > 0) viewFilter = (s, t) => ViewsFilter.Any(filter => t.MatchesPattern(filter));

	ScriptDatabaseObjects(outputFile, opts.Server, opts.Catalog, 
		scriptIncludes.Any(i => i.HasFlag(IncludeOptions.Database)), 
		scriptIncludes.Any(i => i.HasFlag(IncludeOptions.Tables)),
		scriptIncludes.Any(i => i.HasFlag(IncludeOptions.Procedures)),
		scriptIncludes.Any(i => i.HasFlag(IncludeOptions.Views)),
		scriptIncludes.Any(i => i.HasFlag(IncludeOptions.Users)),
		scriptIncludes.Any(i => i.HasFlag(IncludeOptions.UserDefinedFunctions)),
		scriptIncludes.Any(i => i.HasFlag(IncludeOptions.UserDefinedDataTypes)),
		tableFilter, procedureFilter, viewFilter,
		scriptDrops: opts.ScriptDrops,
		includeIfNotExists: opts.IncludeIfNotExists,
		verbose: opts.Verbose);
}

public static class StringExtensions
{
	/// <summary>
	/// Compares the string against a given pattern.
	/// (from: https://stackoverflow.com/questions/188892/glob-pattern-matching-in-net)
	/// </summary>
	/// <param name="str">The string.</param>
	/// <param name="pattern">The pattern to match, where "*" means any sequence of characters, and "?" means any single character.</param>
	/// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
	public static bool MatchesPattern(this string str, string pattern)
	{
		return new Regex(
			"^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
			RegexOptions.IgnoreCase | RegexOptions.Singleline
		).IsMatch(str);
	}
}

//void Main()
//{
//	string schemaFile = @"C:\Users\Administrator\Documents\___delete\northwind.sql";
//	ScriptDatabaseObjects(schemaFile, "(local)", "northwind", false, false, false, true, false, false, false);
//	ScriptDatabaseObjects(schemaFile, "(local)", "northwind");
//
//	//	string dataFile = @"C:\Users\Administrator\Documents\___delete\northwind.data.sql";
////	ScriptDatabaseData(dataFile, "(local)", "northwind");
//}

// Define other methods and classes here
public static void ScriptDatabaseObjects(string outputFile, string serverName, string catalogName, 
	bool scriptDatabase = true, bool scriptTables = true, bool scriptStoredProcedures = true, bool scriptViews = true, 
	bool scriptUsers = true, bool scriptUserDefinedFunctions = true, bool scriptUserDefinedDataTypes = true,
	Func<string, string, bool> tableFilterBySchemaAndName = null, Func<string, string, bool> spFilterBySchemaAndName = null, Func<string, string, bool> viewFilterBySchemaAndName = null,
	Func<string, string, bool> udfFilterBySchemaAndName = null, Func<string, bool> userFilterByName = null,
	bool scriptDrops = true, bool includeIfNotExists = true, bool verbose = false)
{
	if (tableFilterBySchemaAndName == null) tableFilterBySchemaAndName = (s1, s2) => true;
	if (spFilterBySchemaAndName == null) spFilterBySchemaAndName = (s1, s2) => true;
	if (viewFilterBySchemaAndName == null) viewFilterBySchemaAndName = (s1, s2) => true;
	if (udfFilterBySchemaAndName == null) udfFilterBySchemaAndName = (s1, s2) => true;
	if (userFilterByName == null) userFilterByName = (s) => true;

	if (File.Exists(outputFile))
	{
		var fileExtension = new FileInfo(outputFile).Extension;
		Console.WriteLine("Backup file created at: " + outputFile.Substring(0, outputFile.Length - fileExtension.Length) + "." + DateTime.Now.Ticks + fileExtension);
		File.Move(outputFile, outputFile.Substring(0, outputFile.Length - fileExtension.Length) + "." + DateTime.Now.Ticks + fileExtension);
	}
	
	Server server = new Server(serverName);
	Scripter scripter = new Scripter(server);
	Database db = server.Databases[catalogName];

	Func<Action<ScriptingOptions>, ScriptingOptions> options = action => 
	{
		var o = new ScriptingOptions { 
			AllowSystemObjects = false, WithDependencies = false, FileName = outputFile, AppendToFile = true, 
			IncludeHeaders = true, IncludeIfNotExists = includeIfNotExists, ToFileOnly = true, 
			ContinueScriptingOnError = true,
			BatchSize = 1000 };
		action?.Invoke(o);
		return o;
	};

	if (scriptDatabase)
	{
		if (scriptDrops) db.Script(options(o => o.ScriptDrops = true));
		db.Script(options(o => o.ScriptSchema = true));
	}

	if (scriptUserDefinedDataTypes)
	{
		scripter.Options = options(o => o.ScriptDrops = true);
		scripter.Script(db.UserDefinedDataTypes.Cast<UserDefinedDataType>().ToArray());

		scripter.Options = options(o => { });
		scripter.Script(db.UserDefinedDataTypes.Cast<UserDefinedDataType>().ToArray());

		//		foreach (UserDefinedDataType obj in db.UserDefinedDataTypes)
		//		{
		//			if (scriptDrops) obj.Script(options(o => o.ScriptDrops = true));
		//			obj.Script(options(o => { o.ScriptSchema = true; }));
		//		}
	}

	if (scriptUserDefinedFunctions)
	{
		scripter.Options = options(o => o.ScriptDrops = true);
		scripter.Script(db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(t => udfFilterBySchemaAndName(t.Schema, t.Name)).ToArray());

		scripter.Options = options(o => { });
		scripter.Script(db.UserDefinedFunctions.Cast<UserDefinedFunction>().Where(t => udfFilterBySchemaAndName(t.Schema, t.Name)).ToArray());

		//		foreach (UserDefinedFunction obj in db.UserDefinedFunctions)
		//		{
		//			if (obj.IsSystemObject == true) continue;
		//			if (udfFilterBySchemaAndName != null && !udfFilterBySchemaAndName(obj.Schema, obj.Name)) continue;
		//			
		//			if (scriptDrops) obj.Script(options(o => o.ScriptDrops = true));
		//			obj.Script(options(o => { o.ScriptSchema = true; }));
		//		}
	}

	if (scriptTables)
	{
		scripter.Options = options(o => o.ScriptDrops = true);
		scripter.Script(db.Tables.Cast<Table>().Where(t => !t.IsSystemObject && tableFilterBySchemaAndName(t.Schema, t.Name)).ToArray());

		scripter.Options = options(o => { o.Default = true; o.ClusteredIndexes = true; o.NonClusteredIndexes = true; o.Indexes = true; o.FullTextIndexes = true; o.DriAll = true; });
		scripter.Script(db.Tables.Cast<Table>().Where(t => !t.IsSystemObject && tableFilterBySchemaAndName(t.Schema, t.Name)).ToArray());
		
//		foreach (Table obj in db.Tables)
//		{
//			if (obj.IsSystemObject == true) continue;
//			if (tableFilterBySchemaAndName != null && !tableFilterBySchemaAndName(obj.Schema, obj.Name)) continue;
//			
//			if (scriptDrops) obj.Script(options(o => o.ScriptDrops = true));
//			obj.Script(options(o => { o.ScriptSchema = true; o.Default = true; o.DriAll = true; o.ClusteredIndexes = true; o.NonClusteredIndexes = true; o.Indexes = true; o.FullTextIndexes = true; }));
//		}
	}

	if (scriptViews)
	{
		scripter.Options = options(o => o.ScriptDrops = true);
		scripter.Script(db.Views.Cast<View>().Where(t => !t.IsSystemObject && viewFilterBySchemaAndName(t.Schema, t.Name)).ToArray());

		scripter.Options = options(o => {});
		scripter.Script(db.Views.Cast<View>().Where(t => !t.IsSystemObject && viewFilterBySchemaAndName(t.Schema, t.Name)).ToArray());
		
//		foreach (View obj in db.Views)
//		{
//			if (obj.IsSystemObject == true) continue;
//			if (viewFilterBySchemaAndName != null && !viewFilterBySchemaAndName(obj.Schema, obj.Name)) continue;
//			
//			if (scriptDrops) obj.Script(options(o => o.ScriptDrops = true));
//			obj.Script(options(o => { o.ScriptSchema = true; }));
//		}
	}

	if (scriptStoredProcedures)
	{
		scripter.Options = options(o => o.ScriptDrops = true);
		scripter.Script(db.StoredProcedures.Cast<StoredProcedure>().Where(t => !t.IsSystemObject && spFilterBySchemaAndName(t.Schema, t.Name)).ToArray());

		scripter.Options = options(o => { });
		scripter.Script(db.StoredProcedures.Cast<StoredProcedure>().Where(t => !t.IsSystemObject && spFilterBySchemaAndName(t.Schema, t.Name)).ToArray());

//		foreach (StoredProcedure obj in db.StoredProcedures)
//		{
//			if (obj.IsSystemObject == true) continue;
//			if (spFilterBySchemaAndName != null && !spFilterBySchemaAndName(obj.Schema, obj.Name)) continue;
//			
//			if (scriptDrops) obj.Script(options(o => o.ScriptDrops = true));
//			obj.Script(options(o => { o.ScriptSchema = true; }));
//		}
	}

	if (scriptUsers)
	{
		scripter.Options = options(o => o.ScriptDrops = true);
		scripter.Script(db.Users.Cast<User>().Where(t => userFilterByName(t.Name)).ToArray());

		scripter.Options = options(o => { });
		scripter.Script(db.Users.Cast<User>().Where(t => userFilterByName(t.Name)).ToArray());

//		foreach (User obj in db.Users)
//		{
//			if (obj.IsSystemObject == true) continue;
//			if (userFilterByName != null && !userFilterByName(obj.Name)) continue;
//
//			if (scriptDrops) obj.Script(options(o => o.ScriptDrops = true));
//			obj.Script(options(o => { o.ScriptSchema = true; }));
//		}
	}

	Console.WriteLine("File created at: " + outputFile);
}


public static void ScriptDatabaseData(string outputFile, string serverName, string catalogName, Func<string, string, bool> tableFilterBySchemaAndName = null)
{
	if (File.Exists(outputFile))
	{
		var fileExtension = new FileInfo(outputFile).Extension;
		Console.WriteLine("Backup file created at: " + outputFile.Substring(0, outputFile.Length - fileExtension.Length) + "." + DateTime.Now.Ticks + fileExtension);
		File.Move(outputFile, outputFile.Substring(0, outputFile.Length - fileExtension.Length) + "." + DateTime.Now.Ticks + fileExtension);
	}

	Server server = new Server(serverName);
	Scripter scripter = new Scripter(server);
	Database db = server.Databases[catalogName];
	
	scripter.Options.ScriptSchema = false;
	scripter.Options.ScriptData = true;

	using (var f = File.OpenWrite(outputFile))
	{
		using (var fw = new System.IO.StreamWriter(f))
		{
			foreach (Table obj in db.Tables)
			{
				if (obj.IsSystemObject == true) continue;
				if (tableFilterBySchemaAndName != null && !tableFilterBySchemaAndName(obj.Schema, obj.Name)) continue;
				
				foreach (string s in scripter.EnumScript(new Urn[] { obj.Urn }))
					fw.WriteLine(s);
			}
		}
	}
	
	Console.WriteLine("File created at: " + outputFile);
}