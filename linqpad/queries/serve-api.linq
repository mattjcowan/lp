<Query Kind="Program">
  <NuGetReference>ServiceStack</NuGetReference>
  <Namespace>ServiceStack</Namespace>
  <Namespace>ServiceStack.Text</Namespace>
</Query>

void Main(string[] args) 
{ 
    var port = args == null || args.Length == 0 ? 1337 : int.Parse(args[0]); 
	var assemblyFiles = (args ?? new string[0]).Where(a => a.EndsWith(".dll") && File.Exists(a)).ToArray();
	var assemblies = assemblyFiles.Select(a => Assembly.LoadFrom(a)).ToArray();
    var listeningOn = string.Format("http://*:{0}/", port); 
    var appHost = (assemblies.Length > 0 ? new AppHost(assemblies): new AppHost()).Init().Start(listeningOn); 
    Console.WriteLine("AppHost Created at {0}, listening on {1}", DateTime.Now, listeningOn); 
    Process.Start(string.Format("http://localhost:{0}/metadata", port)); 
    Console.Read(); 
} 
 
public class AppHost : AppSelfHostBase
{
	public AppHost() : base("API", typeof(HelloService).Assembly) { }
	public AppHost(Assembly[] assemblies) : base("API", assemblies) { } 
    public override void Configure(Funq.Container container) { } 
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
