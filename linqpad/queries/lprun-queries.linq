<Query Kind="Program" />

void Main(string[] args) 
{ 
    Console.ForegroundColor = ConsoleColor.DarkGray;
	Console.WriteLine("\n Usage:  lpr {package_name} {script_name.linq} {script_args*}\n");
	Console.ResetColor();
	
	var package = args != null && args.Length > 0 ? args[0] : null;
	AnalyzePackages(package);
}

void AnalyzePackages(string specificPackage = null)
{
	if(specificPackage == null)
		Console.WriteLine("\nList of packages: " + specificPackage + "\n");
	var packages = Directory.GetDirectories(Path.GetDirectoryName(Util.CurrentQueryPath), "*", SearchOption.TopDirectoryOnly);
	foreach (var package in packages)
	{
		var packageName = new DirectoryInfo(package).Name;
		if (specificPackage != null)
		{
			Console.WriteLine("Scripts in package: " + specificPackage + "\n");
			if(packageName.Equals(specificPackage, StringComparison.OrdinalIgnoreCase))
				AnalyzeScripts(package);
			break;
		}
		else
		{
			Console.WriteLine(" - " + packageName);
		}
	}
}

void AnalyzeScripts(string directory)
{
	foreach (var script in Directory.GetFiles(directory, "*.linq", SearchOption.TopDirectoryOnly))
	{
		var fileName = Path.GetFileName(script);
		if (fileName.Equals("lprun-queries.linq")) continue;
		Console.WriteLine(" - " + fileName);
	}
}