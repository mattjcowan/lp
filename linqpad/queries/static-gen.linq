<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.ComponentModel.DataAnnotations.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <NuGetReference Version="1.7.1">Handlebars.Net</NuGetReference>
  <NuGetReference>JsonPrettyPrinter</NuGetReference>
  <NuGetReference>ServiceStack.Text</NuGetReference>
  <Namespace>HandlebarsDotNet</Namespace>
  <Namespace>JsonPrettyPrinterPlus</Namespace>
  <Namespace>ServiceStack</Namespace>
  <Namespace>System.ComponentModel.DataAnnotations</Namespace>
  <Namespace>ServiceStack.Text</Namespace>
  <Namespace>System.ComponentModel</Namespace>
</Query>

void Main(string[] args)
{
	try
	{
		if (args == null || args.Length == 0 || args[0].Contains("?") || args[0].ToLower().Equals("help"))
		{
			Console.WriteLine(AppArgs.HelpFor<ScriptArgs>());
			return;
		}

		var scriptArgs = args.As<ScriptArgs>();

		if (!Directory.Exists(scriptArgs.DataDir))
			throw new ArgumentException(string.Format("{0} directory does not exist", scriptArgs.DataDir)); ;

		if (!Directory.Exists(scriptArgs.TemplateDir))
			throw new ArgumentException(string.Format("{0} directory does not exist", scriptArgs.TemplateDir));

		CodeGen(scriptArgs);
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

[System.ComponentModel.Description(@"Static file generator that can be used to generate code and/or other files as needed. 
Leverages the Handlebars templating system, json files, and other linqpad scripts to give you
ultimate flexibility.")]
public class ScriptArgs
{
	[Required]
	[Display(ShortName = "data", Description = "Directory of data files (*.json) that are loaded into a global dictionary available in the global context of the handlebars templates.")]
	public string DataDir { get; set; }

	[Required]
	[Display(ShortName = "templates", Description = "Directory where all the handlebars (*.hbs) templates are located.")]
	public string TemplateDir { get; set; }

	[Required]
	[Display(ShortName = "output", Description = "Output directory for generated files.")]
	public string OutputDir { get; set; }
}

// MODIFY THIS AND ADD YOUR OWN HELPERS
public void RegisterHandlebarsHelpers()
{
	// Example: {{now "MM-dd-yyyy"}}
	Handlebars.RegisterHelper("now", (writer, context, parameters) =>
	{
		var dtFormat = parameters.Length > 0 && parameters[0] is string ? (string)parameters[0] : "u";
		writer.WriteSafeString(DateTime.Now.ToString(dtFormat));
	});
}

public void CodeGen(ScriptArgs args)
{
	JsConfig.EmitCamelCaseNames = false;
	JsConfig.IncludeNullValues = true;
	JsConfig.IncludeNullValuesInDictionaries = true;
	JsConfig.ConvertObjectTypesIntoStringDictionary = true;
	
	args.DataDir = Path.GetFullPath(args.DataDir);
	args.TemplateDir = Path.GetFullPath(args.TemplateDir);
	args.OutputDir = Path.GetFullPath(args.OutputDir);

	var linqFiles = Directory.GetFiles(args.DataDir, "*.linq", SearchOption.AllDirectories);
	var dataFiles = Directory.GetFiles(args.DataDir, "*.json", SearchOption.AllDirectories);
	var allTemplateFiles = Directory.GetFiles(args.TemplateDir, "*.hbs", SearchOption.AllDirectories);

	var globalData = new Dictionary<string, object>();
	globalData["Arguments"] = args;

	// RUN SCRIPTS
	foreach (var f in linqFiles)
	{
		var scriptOutput = Util.Run(f, QueryResultFormat.Text, true, args).AsString() ?? string.Empty;
		if ((scriptOutput.StartsWith("{") && scriptOutput.EndsWith("}")) &&
		(scriptOutput.StartsWith("[") && scriptOutput.EndsWith("]")))
		{
			File.WriteAllText(f.Substring(f.Length - 5) + ".json", scriptOutput);
		}
	}

	// LOAD DATA FILES
	foreach (var f in dataFiles)
	{
		var key = Path.GetFileNameWithoutExtension(f);
        if (key == null)
			continue;
			
		if (globalData.ContainsKey(key))
		{
			Console.WriteLine("Skipping data file " + key + ". A file by the same name was already loaded.");
			continue;
		}

		Console.WriteLine("Loading data file: " + key);
		var data = File.ReadAllText(f).FromJson<Dictionary<string, object>>();
		globalData.Add(key, data);
	}
	
	// REGISTER HELPERS
	RegisterHandlebarsHelpers();

	// REGISTER PARTIAL TEMPLATES
	var partialTemplateFiles = allTemplateFiles.Where(f => Path.GetFileName(f).StartsWithIgnoreCase("_")).ToArray();
	var partialTemplates = new List<string>();
	foreach (var f in partialTemplateFiles)
	{
		var key = Path.GetFileNameWithoutExtension(f);
        if (key == null)
			continue;
			
		var partialName = key.Substring(1);
		if (partialTemplates.Contains(partialName))
		{
			Console.WriteLine("Skipping partial template " + partialName + ". A partial template by the same name was already registered.");
			continue;
		}

		Console.WriteLine("Registering partial template: " + partialName);
	
		using (var reader = new StringReader(File.ReadAllText(f)))
		{
			var partialTemplate = Handlebars.Compile(reader);
			Handlebars.RegisterTemplate(partialName, partialTemplate);
		}
	}

	// REGISTER TEMPLATES
	var templateFiles = allTemplateFiles.Where(f => !Path.GetFileName(f).StartsWithIgnoreCase("_")).ToArray();
	var templatePaths = new Dictionary<string, string>();
	var templates = new Dictionary<string, Func<object, string>>();
	foreach (var f in templateFiles)
	{
		var templateName = Path.GetFileNameWithoutExtension(f);
        if (templateName == null)
			continue;

		if (templates.ContainsKey(templateName))
		{
			Console.WriteLine("Skipping template " + templateName + ". A template by the same name was already loaded.");
			continue;
		}

		Console.WriteLine("Compiling template: " + templateName);
		var compiledTemplate = Handlebars.Compile(File.ReadAllText(f));
		templates.Add(templateName, compiledTemplate);
		templatePaths.Add(templateName, f);
	}
	
	Directory.CreateDirectory(args.OutputDir);

	// output the globalData to the output directory for debugging purposes
	File.WriteAllText(Path.Combine(args.OutputDir, "data.json"), globalData.ToJson().PrettyPrintJson());
	
	foreach (var template in templates)
	{
		var outputFileName = template.Key + ".cs";
		var outputFile = Path.Combine(args.OutputDir, outputFileName);

		globalData["Template"] = new {
			Name = template.Key,
			Path = templatePaths[template.Key],
			OutputFileName = outputFileName,
			OutputFilePath = outputFile
		};
		var generatedOutput = template.Value(globalData);
		File.WriteAllText(outputFile, generatedOutput ?? string.Empty);
	}
	
	if(globalData.ContainsKey("Template")) 
		globalData.Remove("Template");
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
			   let d = p.Attribute<System.ComponentModel.DescriptionAttribute>()
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

		var typeDescription = typeof(T).GetCustomAttribute(typeof(System.ComponentModel.DescriptionAttribute)) as System.ComponentModel.DescriptionAttribute;
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
