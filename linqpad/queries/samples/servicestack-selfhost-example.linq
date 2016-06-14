<Query Kind="Program">
  <NuGetReference>ServiceStack</NuGetReference>
  <Namespace>ServiceStack</Namespace>
  <Namespace>ServiceStack.Text</Namespace>
</Query>

void Main(string[] args) 
{
	// packageName is always args[0] when using lpr.bat
	// scriptName is always args[1] when using lpr.bat
	
    var port = args == null || args.Length < 3 ? 1337 : int.Parse(args[2]); 
    var listeningOn = string.Format("http://*:{0}/", port); 
    var appHost = new AppHost().Init().Start(listeningOn); 
    Console.WriteLine("AppHost Created at {0}, listening on {1}", DateTime.Now, listeningOn); 
    Process.Start(string.Format("http://localhost:{0}/hello/World", port)); 
    Console.Read(); 
} 
 
public class AppHost : AppSelfHostBase 
{ 
    public AppHost() : base("HttpListener Self-Host", typeof(HelloService).Assembly) { } 
    public override void Configure(Funq.Container container) {

		// let's analyze the directories a bit
		Console.WriteLine("Environment.CurrentDirectory: " + Environment.CurrentDirectory);
		Console.WriteLine("AppDomain.CurrentDomain.BaseDirectory: " + AppDomain.CurrentDomain.BaseDirectory);
		
		Console.WriteLine("******** PRE CONFIG *********");
		Console.WriteLine("\"~/\".MapAbsolutePath(): " + "~/".MapAbsolutePath());
		Console.WriteLine("\"~/\".MapHostAbsolutePath(): " + "~/".MapHostAbsolutePath());
		Console.WriteLine("\"~/\".MapProjectPath(): " + "~/".MapProjectPath());
		Console.WriteLine("\"~/\".MapServerPath(): " + "~/".MapServerPath());
		Console.WriteLine("Config.WebHostPhysicalPath: " + Config.WebHostPhysicalPath);
		Console.WriteLine("Config.WebHostUrl: " + Config.WebHostUrl);
		Console.WriteLine("Config.HandlerFactoryPath: " + Config.HandlerFactoryPath);
		Console.WriteLine("Config.DefaultRedirectPath: " + Config.DefaultRedirectPath);
		Console.WriteLine("Config.MetadataRedirectPath: " + Config.MetadataRedirectPath);

		SetConfig(new HostConfig
		{
			WebHostPhysicalPath = Environment.CurrentDirectory,
			DefaultRedirectPath = "/hello/Anonymous"
		});
		
		Console.WriteLine("******** POST CONFIG *********");
		Console.WriteLine("\"~/\".MapAbsolutePath(): " + "~/".MapAbsolutePath());
		Console.WriteLine("\"~/\".MapHostAbsolutePath(): " + "~/".MapHostAbsolutePath());
		Console.WriteLine("\"~/\".MapProjectPath(): " + "~/".MapProjectPath());
		Console.WriteLine("\"~/\".MapServerPath(): " + "~/".MapServerPath());
		Console.WriteLine("Config.WebHostPhysicalPath: " + Config.WebHostPhysicalPath);
		Console.WriteLine("Config.WebHostUrl: " + Config.WebHostUrl);
		Console.WriteLine("Config.HandlerFactoryPath: " + Config.HandlerFactoryPath);
		Console.WriteLine("Config.DefaultRedirectPath: " + Config.DefaultRedirectPath);
		Console.WriteLine("Config.MetadataRedirectPath: " + Config.MetadataRedirectPath);
	}
}

public class HelloService : Service
{
	public object Any(Hello request)
	{
		return new HelloResponse { Result = "Hello, " + request.Name };
	}
}

[Route("/hello/{Name}")]
public class Hello
{
	public string Name { get; set; }
}

public class HelloResponse
{
	public string Result { get; set; }
}