<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.ComponentModel.DataAnnotations.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <NuGetReference>DatabaseSchemaReader</NuGetReference>
  <NuGetReference>Humanizer</NuGetReference>
  <NuGetReference>JsonPrettyPrinter</NuGetReference>
  <NuGetReference>MySql.Data</NuGetReference>
  <NuGetReference>Npgsql</NuGetReference>
  <NuGetReference>Oracle.ManagedDataAccess</NuGetReference>
  <NuGetReference>ServiceStack</NuGetReference>
  <Namespace>DatabaseSchemaReader</Namespace>
  <Namespace>DatabaseSchemaReader.DataSchema</Namespace>
  <Namespace>Humanizer</Namespace>
  <Namespace>JsonPrettyPrinterPlus</Namespace>
  <Namespace>JsonPrettyPrinterPlus.JsonSerialization</Namespace>
  <Namespace>ServiceStack</Namespace>
  <Namespace>ServiceStack.Text</Namespace>
  <Namespace>System.ComponentModel</Namespace>
  <Namespace>System.ComponentModel.DataAnnotations</Namespace>
</Query>

// Use customizations here to customize the output of this file beyond command line options
// It's assumed you'd use a custom version of this file for each project
static Customizations config = new Customizations 
{
	RecreateProjectFile = true,
	RecreatePluginFile = true,
	RecreateTableTypeFiles = true,
	
	ClearTypesDirectory = true
};

public class Customizations
{
	public bool RecreateProjectFile { get; set; } = false;
	public bool RecreatePluginFile { get; set; } = false;
	public bool RecreateTableTypeFiles { get; set; } = false;
	
	public bool ClearTypesDirectory { get; set; } = false;

	public Func<string, string> ConvertSchemaNameToFolderName { get; set; } = (schemaName) =>
	{
		switch (schemaName)
		{
			case "Person": return "People";
			default: return schemaName;
		}
	};
	public Func<string, string> ConvertSchemaNameToNamespacePart { get; set; } = (schemaName) => 
	{
		switch(schemaName)
		{
			case "Person": return "People";
			default: return schemaName;
		}
	};
	public Func<string, string, string> ConvertTableNameToClassName { get; set; } = (schemaName, tableName) => {
		var className = tableName.Pascalize().Replace(" ", "").Singularize(false);
		if (tableName.EqualsIgnoreCase("Document"))
			return "Doc";
		return className;
	};
	public Func<string, string, string, string> ConvertTableColumnNameToFieldName { get; set; } = (schemaName, tableName, columnName) => {
		var fieldName = columnName.Pascalize().Replace(" ", "");
		if (tableName.EqualsIgnoreCase("EmailAddress") && columnName.EqualsIgnoreCase("EmailAddress"))
			return "Email";
		if (fieldName.EndsWith("ID") && fieldName.Length > 2 && Char.IsLower(fieldName[fieldName.Length - 3]))
			return fieldName.Substring(0, fieldName.Length - 2) + "Id";
		return fieldName;
	};
	public Func<string, string, string, string, Type> GetCSharpTypeFromDbTypeName { get; set; } = (schemaName, tableName, columnName, columnDataType) =>
	{
		return GetCSharpTypeFromDataTypeName(columnDataType);
	};
}

void Main(string[] args)
{
	JsConfig.EmitCamelCaseNames = true;
	JsConfig.IncludeNullValues = false;
	JsConfig.IncludeNullValuesInDictionaries = true;

	try
	{
		if (args == null || args.Length == 0 || args[0].Contains("?") || args[0].ToLower().Equals("help"))
		{
			Console.WriteLine(AppArgs.HelpFor<ScriptArgs>());
			return;
		}

		var scriptArgs = args.As<ScriptArgs>();

		if (!File.Exists(scriptArgs.JsonFile))
			throw new ArgumentException("Invalid file: " + scriptArgs.JsonFile);
		
		scriptArgs.OutputDir = new DirectoryInfo(scriptArgs.OutputDir).FullName;
		if (!Directory.Exists(scriptArgs.OutputDir))
			Directory.CreateDirectory(scriptArgs.OutputDir);
			
		if (string.IsNullOrWhiteSpace(scriptArgs.Namespace))
			scriptArgs.Namespace = new DirectoryInfo(scriptArgs.OutputDir).Name;

		var xdb = GetXDb(scriptArgs.JsonFile);
		OutputApi(xdb, scriptArgs.OutputDir, scriptArgs.Namespace);
	}
	catch (ArgumentException argex)
	{
		Console.ForegroundColor = ConsoleColor.DarkYellow;
		Console.WriteLine(argex.Message);
		Console.ResetColor();
	}
	catch (Exception ex)
	{
		Console.ForegroundColor = ConsoleColor.DarkRed;
		Console.WriteLine(ex.Message);
		if (ex.InnerException != null)
			Console.WriteLine(ex.InnerException);
		Console.ResetColor();
	}
}

[Description("Outputs an api from a json file.")]
public class ScriptArgs
{
	[Required]
	[Display(ShortName = "json", Description = "The json schema file (use the db-to-json script)")]
	public string JsonFile { get; set; }

	[Required]
	[Display(ShortName = "output", Description = "The output directory where the files should be placed")]
	public string OutputDir { get; set; }

	[Display(ShortName = "namespace", Description = "The namespace for the project (will default to the name of the directory)")]
	public string Namespace { get; set; }
}

static XDb GetXDb(string jsonFile)
{
	return File.ReadAllText(jsonFile).FromJson<XDb>();
}

void OutputApi(XDb xdb, string outputDir, string ns)
{
	CreateProjectFile(outputDir, ns);
	CreatePluginFile(outputDir, ns);
	CreateSchemaTypes(xdb, outputDir, ns);
}

void CreateSchemaTypes(XDb xdb, string outputDir, string ns)
{
	var typesDir = Path.Combine(outputDir, "Types");
	if (config.ClearTypesDirectory)
		Directory.Delete(typesDir, true);
	
	Directory.CreateDirectory(typesDir);
	
	var schemaCount = xdb.Schemas.Count();
	foreach (var schema in xdb.Schemas)
	{
		schema.Value.Name = schema.Key; // the json files don't have the name property filled in ??
		var schemaDir = schemaCount == 1 ? typesDir : Path.Combine(typesDir, config.ConvertSchemaNameToFolderName(schema.Key));
		var schemaNs = schemaCount == 1 ? ns + ".Types" : ns + ".Types." + config.ConvertSchemaNameToNamespacePart(schema.Key);
		Directory.CreateDirectory(schemaDir);

		foreach (var xtable in schema.Value.Tables)
		{
			CreateTableType(xdb, schema.Value, xtable.Value, schemaDir, schemaNs);	
		}
	}
}

void CreateTableType(XDb xdb, XSchema schema, XTable table, string schemaDir, string schemaNs)
{
	var tableClassName = config.ConvertTableNameToClassName(schema.Name, table.Name);
	string code;
	using (var block = new TextBlock())
	{
		try
		{
			block.AppendUsings("System", "System.Runtime.Serialization", "ServiceStack", "ServiceStack.DataAnnotations");
			block.StartNamespace(schemaNs);
			block.In();
			
			if (!string.IsNullOrWhiteSpace(table.Description))
			{
				block.AppendLine("///<summary>");
				block.AppendLine("///{0}", table.Description);
				block.AppendLine("///</summary>");
			}
			block.AppendLine("[Alias(\"{0}\")]", table.Name);
			block.AppendLine("[DataContract]");
			if (table.HasCompositeKey) block.AppendLine("[CompositeKey(\"{0}\")]", string.Join("\", \"", table.PrimaryKey.ColumnNames));
			block.StartClass(tableClassName);
			block.In();
			foreach (var column in table.Columns.Values.OrderBy(c => c.Ordinal))
			{
				var fieldName = config.ConvertTableColumnNameToFieldName(schema.Name, table.Name, column.Name);
//				var fieldCSharpType = config.GetCSharpTypeFromDbTypeName(schema.Name, table.Name, column.Name, column.DataTypeName);
				var fieldDataType = xdb.DataTypes.ContainsKey(column.DataTypeName) ? xdb.DataTypes[column.DataTypeName] : xdb.DataTypes["nvarchar"];
				var fieldCSharpType = Type.GetType(fieldDataType.NetDataType);

				var isSingularPrimaryKey = column.IsPrimaryKey && table.PrimaryKey.ColumnNames.Length == 1;
				
				if (!string.IsNullOrWhiteSpace(column.Description))
				{
					block.AppendLine("///<summary>");
					block.AppendLine("///{0}", column.Description);
					block.AppendLine("///</summary>");
				}
				if (isSingularPrimaryKey) block.AppendLine("[PrimaryKey]");
				if (column.IsIdentity) block.AppendLine("[AutoIncrement]");
				if (!column.IsNullable) block.AppendLine("[Required]");
				if (column.IsUnique) block.AppendLine("[Index(true)]");
				if (column.Length.HasValue) block.AppendLine("[StringLength({0})]", column.Length.Value);
				if (column.IsForeignKey) {
					var fk = xdb.Schemas.SelectMany(s => s.Value.Associations.Values).FirstOrDefault(a => a.FkSchema == schema.Name && a.FkTable == table.Name && a.FkColumnName == column.Name);
					if (fk == null)
						Console.WriteLine("No mapping for fk for " + schema.Name + "." + table.Name + "." + column.Name + " (ignoring)");
					else
						block.AppendLine("[References(typeof({0}.{1}))]", config.ConvertSchemaNameToNamespacePart(fk.PkSchema), config.ConvertTableNameToClassName(fk.PkSchema, fk.PkTable));
				}
				block.AppendLine("[DataMember(Order = {0})]", column.Ordinal);
				block.AppendLine("[Alias(\"{0}\")]", column.Name);

				var fieldCSharpTypeName = fieldDataType.NetDataTypeCSharpName;				
				if (fieldCSharpType.IsValueType && column.IsNullable)
					fieldCSharpTypeName += "?";				
				
				var fieldBlock = (isSingularPrimaryKey ?
					"public virtual " + fieldCSharpTypeName + " Id" :
					"public virtual " + fieldCSharpTypeName + " " + config.ConvertTableColumnNameToFieldName(schema.Name, table.Name, column.Name)) + " { get; set; }";
				block.AppendLine(fieldBlock);
				block.EmptyLine();
			}
			block.Out();
			block.StopClass();
			block.Out();
			block.StopNamespace();
		}
		finally
		{
			code = block.ToString();
		}
	}
	var tableTypeFile = Path.Combine(schemaDir, tableClassName + ".cs");
	if (config.RecreateTableTypeFiles || !File.Exists(tableTypeFile))
		File.WriteAllText(tableTypeFile, code);
}

void CreatePluginFile(string outputDir, string ns)
{
	var pfile = Path.Combine(outputDir, "Plugin.cs");
	if (config.RecreatePluginFile || !File.Exists(pfile))
	{
		File.WriteAllText(pfile, string.Format(@"
using ServiceStack;

namespace {0}
{{
	public partial class Plugin: IPlugin
	{{
		public Plugin()
		{{
            Config = new PluginConfig();

            InitPlugin();
		}}
		
		partial void InitPlugin();

	    public PluginConfig Config {{ get; }}
		
		public void Register(IAppHost appHost)
		{{
		}}
	}}
}}
		".Trim(), ns));
	}
}

void CreateProjectFile(string outputDir, string ns)
{
	var pfile = Path.Combine(outputDir, ns + ".csproj");
	if (config.RecreateProjectFile || !File.Exists(pfile))
	{
		File.WriteAllText(pfile, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""ServiceStack"" Version=""5.0.2"" />
    <PackageReference Include=""ServiceStack.OrmLite"" Version=""5.0.2"" />
  </ItemGroup>
</Project>
		".Trim());
	}
}

public static Type GetCSharpTypeFromDataTypeName(string dbTypeName)
{
	switch(dbTypeName)
	{
		case "bigint": return typeof(Int64);
		case "binary": return typeof(Byte[]);
		case "bit": return typeof(bool);
		case "char": return typeof(string);
		case "date": return typeof(DateTime);
		case "datetime": return typeof(DateTime);
		case "datetime2": return typeof(DateTime);
		case "datetimeoffset": return typeof(DateTimeOffset);
		case "decimal": return typeof(Decimal);
		case "float": return typeof(Double);
		case "image": return typeof(Byte[]);
		case "int": return typeof(Int32);
		case "money": return typeof(Decimal);
		case "nchar": return typeof(string);
		case "ntext": return typeof(string);
		case "numeric": return typeof(decimal);
		case "nvarchar": return typeof(string);
		case "real": return typeof(Single);
		case "rowversion": return typeof(Byte[]);
		case "smalldatetime": return typeof(DateTime);
		case "text": return typeof(string);
		case "time": return typeof(TimeSpan);
		case "timestamp": return typeof(Byte[]);
		case "tinyint": return typeof(Byte);
		case "uniqueidentifier": return typeof(Guid);
		case "varbinary": return typeof(Byte[]);
		case "varchar": return typeof(string);
		case "xml": return typeof(string);
		default: return typeof(string);
	}
}

public class TextBlock: System.IDisposable
{
	public int i;
	public int c;
	public List<string> lines = new List<string>();
	
	public TextBlock(int numberOfCharactersForIndent = 4, int setIndent = 0)
	{
		c = numberOfCharactersForIndent;
		i = setIndent;
	}
	
	public void SetIndent(int indent) { i = indent; }
	public void In() { i = i + c; }
	public void Out() { i = i - c; if (i < 0) i = 0; }

	public void ModifyLines(Func<string, bool> lineLocator, Func<string, string> lineModifier)
	{
		for (int i = 0; i < lines.Count; i++)
		{
			if (lineLocator(lines[i]))
			{
				lines[i] = lineModifier(lines[i]);
			}
		}
	}
	
	public void ModifyLine(Func<string, bool> lineLocator, Func<string, string> lineModifier)
	{
		for (int i = 0; i < lines.Count; i++)
		{
			if (lineLocator(lines[i])) 			
			{
				lines[i] = lineModifier(lines[i]);
				return;
			}
		}
	}
	
	public void EmptyLine() { AppendLineInternal(""); }
	public void AppendLine(string format, params object[] args)
	{
		if (args == null || args.Length == 0)
			AppendLineInternal(format);
		else			
			AppendLineInternal(string.Format(format, args));
	}
	
	private void AppendLineInternal(string value = "")
	{
		lines.Add((value ?? "").PadLeft(i + (value ?? "").Length, ' '));
	}

	public void AppendUsings(params string[] usings)
	{
		foreach (var u in usings)
			AppendLine("using {0};", u);
		AppendLine("");
	}
	
	public void StartNamespace(string ns)
	{
		AppendLine("namespace {0}", ns);
		AppendLine("{");
	}
	
	public void StopNamespace()
	{
		AppendLine("}");
	}

	public void StartClass(string className, bool isPublic = true, bool isPartial = true, string baseClass = null, params string[] inherits)
	{
		var classInstructions = string.Format("{0} {1} class {2}", (isPublic ? "public": "internal"), (isPartial ? "partial": ""), className);
		if (!string.IsNullOrWhiteSpace(baseClass) || inherits.Length > 0) 
			classInstructions += ": " + (baseClass ?? "") + (inherits.Length > 0 ? (", " + string.Join(", ", inherits)): "");

		while(classInstructions.Contains("  ")) classInstructions = classInstructions.Replace("  ", " ").Trim();
		AppendLine(classInstructions);
		AppendLine("{");
	}

	public void StopClass()
	{
		AppendLine("}");
	}

	public override string ToString()
	{
		var sbx = new StringBuilder();
		foreach(var l in lines)
			sbx.AppendLine(l);
		return sbx.ToString();
	}
	
	public void Dispose()
	{
		i = 0;
		lines.Clear();
		lines = new List<string>();
	}
}

public static class AppArgs
{
	static Regex _pattern = new Regex("[/-](?'key'[^\\s=:]+)"
		+ "([=:]("
			+ "((?'open'\").+(?'value-open'\"))"
			+ "|"
			+ "(?'value'.+)"
		+ "))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	static Regex _uriPattern = new Regex(@"[\\?&](?'key'[^&=]+)(=(?'value'[^&]+))?", RegexOptions.Compiled);
	static Regex _queryStringPattern = new Regex(@"(^|&)(?'key'[^&=]+)(=(?'value'[^&]+))?", RegexOptions.Compiled);

	static IEnumerable<ArgProperty> PropertiesOf<T>()
	{
		return from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty)
			   let d = p.Attribute<DescriptionAttribute>()
			   let alias = p.Attribute<DisplayAttribute>()
			   select new ArgProperty
			   {
				   Property = p,
				   Name = alias == null || String.IsNullOrWhiteSpace(alias.GetShortName()) ? p.Name.ToLower() : alias.GetShortName(),
				   Type = p.PropertyType,
				   Required = p.Attribute<RequiredAttribute>(),
				   RequiresValue = !(p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?)),
				   Description = d != null && !String.IsNullOrWhiteSpace(d.Description) ? d.Description
					  : alias != null ? alias.GetDescription() : String.Empty
			   };
	}

	/// <summary>
	/// Parses the arguments in <paramref name="args"/> and creates an instance of <typeparamref name="T"/> with the
	/// corresponding properties populated.
	/// </summary>
	/// <typeparam name="T">The custom type to be populated from <paramref name="args"/>.</typeparam>
	/// <param name="args">Command-line arguments, usually in the form of "/name=value".</param>
	/// <returns>A new instance of <typeparamref name="T"/>.</returns>
	public static T As<T>(this string[] args) where T : new()
	{
		var arguments = (from a in args
						 let match = _pattern.Match(a)
						 where match.Success
						 select new
						 {
							 Key = match.Groups["key"].Value.ToLower(),
							 match.Groups["value"].Value
						 }
						).ToDictionary(a => a.Key, a => a.Value);

		return arguments.As<T>();
	}

	/// <summary>
	/// Parses the arguments in the supplied string and creates an instance of <typeparamref name="T"/> with the
	/// corresponding properties populated.
	/// The string should be in the format "key1=value1&key2=value2&key3=value3".
	/// </summary>
	/// <typeparam name="T">The custom type to be populated from <paramref name="args"/>.</typeparam>
	/// <param name="args">Command-line arguments, usually in the form of "/name=value".</param>
	/// <returns>A new instance of <typeparamref name="T"/>.</returns>
	public static T As<T>(this string queryString) where T : new()
	{
		var arguments = (from match in _queryStringPattern.Matches(queryString).Cast<Match>()
						 where match.Success
						 select new
						 {
							 Key = match.Groups["key"].Value.ToLower(),
							 match.Groups["value"].Value
						 }
			).ToDictionary(a => a.Key, a => a.Value);

		return arguments.As<T>();
	}

	/// <summary>
	/// Parses the URI parameters in <paramref name="uri"/> and creates an instance of <typeparamref name="T"/> with the
	/// corresponding properties populated.
	/// </summary>
	/// <typeparam name="T">The custom type to be populated from <paramref name="args"/>.</typeparam>
	/// <param name="args">A URI, usually a ClickOnce activation URI.</param>
	/// <returns>A new instance of <typeparamref name="T"/>.</returns>
	public static T As<T>(this Uri uri) where T : new()
	{
		var arguments = (from match in _uriPattern.Matches(uri.ToString()).Cast<Match>()
						 where match.Success
						 select new
						 {
							 Key = match.Groups["key"].Value.ToLower(),
							 match.Groups["value"].Value
						 }
			).ToDictionary(a => a.Key, a => a.Value);

		return arguments.As<T>();
	}

	/// <summary>
	/// Parses the name/value pairs in <paramref name="arguments"/> and creates an instance of <typeparamref name="T"/> with the
	/// corresponding properties populated.
	/// </summary>
	/// <typeparam name="T">The custom type to be populated from <paramref name="args"/>.</typeparam>
	/// <param name="args">The key/value pairs to be parsed.</param>
	/// <returns>A new instance of <typeparamref name="T"/>.</returns>
	public static T As<T>(this Dictionary<string, string> arguments) where T : new()
	{
		T result = new T();

		var props = PropertiesOf<T>().ToList();

		foreach (var arg in arguments)
		{
			var matches = props.Where(p => p.Name.StartsWith(arg.Key, StringComparison.OrdinalIgnoreCase)).ToList();

			if (matches.Count == 0)
			{
				throw new ArgumentException("Unknown argument '" + arg.Key + "'");
			}
			else if (matches.Count > 1)
			{
				throw new ArgumentException("Ambiguous argument '" + arg.Key + "'");
			}

			var prop = matches[0];

			if (!String.IsNullOrWhiteSpace(arg.Value))
			{
				if (prop.Type.IsArray)
				{
					string v = arg.Value;

					if (v.StartsWith("{") && v.EndsWith("}"))
					{
						v = v.Substring(1, arg.Value.Length - 2);
					}

					var values = v.Split(',').ToArray();
					var array = Array.CreateInstance(prop.Type.GetElementType(), values.Length);

					for (int i = 0; i < values.Length; i++)
					{
						var converter = TypeDescriptor.GetConverter(prop.Type.GetElementType());
						array.SetValue(converter.ConvertFrom(values[i]), i);
					}

					var arrayConverter = new ArrayConverter();
					prop.Property.SetValue(result, array, null);
				}
				else
				{
					var converter = TypeDescriptor.GetConverter(prop.Type);
					prop.Property.SetValue(result, converter.ConvertFromString(arg.Value), null);
				}
			}
			else if (prop.Type == typeof(bool))
			{
				prop.Property.SetValue(result, true, null);
			}
			else
			{
				throw new ArgumentException("No value supplied for argument '" + arg.Key + "'");
			}
		}

		foreach (var p in props.Where(p => p.Required != null))
		{
			if (!p.Required.IsValid(p.Property.GetValue(result, null)) || !arguments.Keys.Where(a => p.Name.StartsWith(a)).Any())
			{
				throw new ArgumentException("Argument missing: '" + p.Name + "'");
			}
		}

		return result;
	}

	/// <summary>
	/// Returns a string describing the arguments necessary to populate an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">A class representing the potential application arguments.</typeparam>
	/// <returns>A string describing the arguments necessary to populate an instance of <typeparamref name="T"/></returns>
	public static string HelpFor<T>()
	{
		var sb = new StringBuilder();
		
		var typeDescription = typeof(T).GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
		if (typeDescription != null && !string.IsNullOrWhiteSpace(typeDescription.Description))
		{
			sb.AppendLine(typeDescription.Description);
			sb.AppendLine();
		}
		
		var props = PropertiesOf<T>().OrderBy(p => p.RequiresValue).ThenBy(p => p.Name).ToList();

		var len = props.Max(p => p.Name.Length);

		sb.Append(System.IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]));
		foreach (var p in props.Where(p => p.Required != null))
		{
			sb.Append(" /" + p.Name + (p.RequiresValue ? "=value" : ""));
		}

		foreach (var p in props.Where(p => p.Required == null))
		{
			sb.Append(" [/" + p.Name + (p.RequiresValue ? "=value" : "") + "]");
		}

		sb.AppendLine();
		sb.AppendLine();
		var required = true;

		foreach (var p in props.OrderByDescending(p => p.Required != null).ThenBy(p => p.Name).ToList())
		{
			if (required && p.Required == null)
			{
				required = false;
				sb.AppendLine();
				sb.AppendLine("Optional params:");
				sb.AppendLine();
			}
			sb.AppendLine(" /" + p.Name.PadRight(len) + "\t" + p.Description);
		}

		return sb.ToString();
	}

	class ArgProperty
	{
		public PropertyInfo Property { get; set; }
		public string Name { get; set; }
		public RequiredAttribute Required { get; set; }
		public bool RequiresValue { get; set; }
		public Type Type { get; set; }
		public string Description { get; set; }
	}
}

public static class PropertyInfoExtensions
{
	public static T Attribute<T>(this PropertyInfo p)
	{
		return p.GetCustomAttributes(typeof(T), true).Cast<T>().FirstOrDefault();
	}
}

#region Schema Types
public class XDb
{
	public string Dialect { get; set; }
	public string Provider { get; set; }
	public Dictionary<string, XDataType> DataTypes { get; set; } = new Dictionary<string, XDataType>();
	public List<string> Users { get; set; } = new List<string>();
	public Dictionary<string, XSchema> Schemas { get; set; } = new Dictionary<string, XSchema>();
}

public class XSchema
{
	public string Name { get; set; }
	public Dictionary<string, XTable> Tables { get; set; } = new Dictionary<string, XTable>();
	public Dictionary<string, XView> Views { get; set; } = new Dictionary<string, XView>();
	public Dictionary<string, XProcedure> Procedures { get; set; } = new Dictionary<string, XProcedure>();
	public Dictionary<string, XFunction> Functions { get; set; } = new Dictionary<string, XFunction>();
	public Dictionary<string, XAssociation> Associations { get; set; } = new Dictionary<string, XAssociation>();
}

public enum XCardinality
{
	One,
	ZeroOrOne,
	Many
}

public class XAssociation
{
	public string Name { get; set; }
	public string FkColumnName { get; set; }
	public string FkTable { get; set; }
	public string FkSchema { get; set; }
	public XCardinality FkCardinality { get; set; }
	public string PkColumnName { get; set; }
	public string PkTable { get; set; }
	public string PkSchema { get; set; }
	public string PkConstraintName { get; set; }
	public string DeleteRule { get; set; }
	public string UpdateRule { get; set; }
	public string SchemaName { get; internal set; }
}

public class XColumn
{
	public string Name { get; set; }
	public bool IsReadOnly { get; set; }
	public int Ordinal { get; set; }
	public string DataTypeName { get; set; }
	public string DbDataTypeName { get; set; }
	public int? Length { get; set; }
	public int? Precision { get; set; }
	public int? Scale { get; set; }
	public int? DateTimePrecision { get; set; }
	public string ComputedDefinition { get; set; }
	public string DefaultValue { get; set; }
	public string Description { get; set; }
	public bool IsIdentity { get; set; }
	public bool IsComputed { get; set; }
	public bool IsForeignKey { get; set; }
	public bool IsIndexed { get; set; }
	public bool IsPrimaryKey { get; set; }
	public bool IsNullable { get; set; }
	public bool IsPartOfUniqueKeyIndex { get; set; }
	public bool IsUnique { get; set; }
	public string UniqueKeyIndexName { get; set; }
	public DatabaseColumnIdentity IdentityDefinition { get; set; }
	public string ForeignKeyName { get; set; }
	public string TableName { get; internal set; }
	public string ViewName { get; internal set; }
}

public class XArgument
{
	public string Name { get; set; }
	public string DataTypeName { get; set; }
	public string DbDataTypeName { get; set; }
	public bool In { get; set; }
	public int? Length { get; set; }
	public decimal Ordinal { get; set; }
	public bool Out { get; set; }
	public int? Precision { get; set; }
	public int? Scale { get; set; }
	public string ProcedureName { get; internal set; }
	public string FunctionName { get; internal set; }
}

public class XPrimaryKey
{
	public string Name { get; set; }
	public string[] ColumnNames { get; set; }
}

public class XIndex
{
	public string Name { get; set; }
	public string[] ColumnNames { get; set; }
	public bool IsUnique { get; set; }
}

public class XTable
{
	public string Name { get; set; }
	public bool IsManyToManyTable { get; set; }
	public bool HasCompositeKey { get; set; }
	public Dictionary<string, XColumn> Columns { get; set; } = new Dictionary<string, XColumn>();
	public bool HasIdentityColumn { get; set; }
	public string[] ForeignKeyAssociationNames { get; set; }
	public string[] ReverseForeignKeyAssociationNames { get; set; }
	public string Description { get; set; }
	public XPrimaryKey PrimaryKey { get; set; }
	public Dictionary<string, XIndex> Indexes { get; set; } = new Dictionary<string, XIndex>();
	public string SchemaName { get; internal set; }
}

public class XView
{
	public string Name { get; set; }
	public string[] ForeignKeyAssociationNames { get; set; }
	public Dictionary<string, XColumn> Columns { get; set; } = new Dictionary<string, XColumn>();
	public string Sql { get; set; }
}

public class XProcedure
{
	public string Name { get; set; }
	public Dictionary<string, XArgument> Arguments { get; set; } = new Dictionary<string, XArgument>();
	public string Sql { get; set; }
}

public class XFunction
{
	public string Name { get; set; }
	public string FullName { get; set; }
	public string Language { get; set; }
	public string ReturnType { get; set; }
	public int ResultSetCount { get; set; }

	public Dictionary<string, XArgument> Arguments { get; set; } = new Dictionary<string, XArgument>();
	public string Sql { get; set; }
}

public class XResultSet
{
}

public class XDataType
{
	public string CreateFormat { get; set; }
	public bool IsDateTime { get; set; }
	public bool IsFloat { get; set; }
	public bool IsInt { get; set; }
	public bool IsNumeric { get; set; }
	public bool IsString { get; set; }
	public bool IsStringClob { get; set; }
	public object LiteralPrefix { get; set; }
	public object LiteralSuffix { get; set; }
	public string NetDataType { get; set; }
	public string NetDataTypeCSharpName { get; set; }
	public int ProviderDbType { get; set; }
	public string TypeName { get; set; }
}
#endregion