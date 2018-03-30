<Query Kind="Program" />

void Main(IDictionary<string, Object> config)
{
	if (config == null) 
		throw new ArgumentNullException(nameof(config), "Missing config");
	
	Console.WriteLine(config["Dialect"]);
}

// Define other methods and classes here
