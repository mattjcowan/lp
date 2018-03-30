<Query Kind="Program">
  <Connection>
    <ID>1c997215-a98f-4bd1-bf47-f3d7a2a7d831</ID>
    <Server>(local)</Server>
    <Database>AdventureWorks2016CTP3</Database>
    <NoPluralization>true</NoPluralization>
    <NoCapitalization>true</NoCapitalization>
    <ExcludeRoutines>true</ExcludeRoutines>
  </Connection>
  <NuGetReference>ServiceStack.OrmLite.SqlServer</NuGetReference>
  <NuGetReference>ServiceStack.Text</NuGetReference>
  <Namespace>ServiceStack</Namespace>
  <Namespace>ServiceStack.Data</Namespace>
  <Namespace>ServiceStack.OrmLite</Namespace>
  <Namespace>ServiceStack.OrmLite.SqlServer</Namespace>
  <Namespace>ServiceStack.Text</Namespace>
  <Namespace>ServiceStack.Text.Json</Namespace>
  <Namespace>System.Dynamic</Namespace>
</Query>

void Main()
{
	var config = new Dictionary<string, object>();	
	config["Dialect"] = SqlServer2017Dialect.Provider;
	config["ConnectionString"] = "Data Source=(local);Integrated Security=SSPI;Initial Catalog=AdventureWorks2016CTP3";
	config["TemplateDir"] = Path.GetDirectoryName(Util.CurrentQueryPath);
	config["OutputDir"] = @"C:\Projects\GitHub\mattjcowan\lp\bld\SampleApp\AdventureWorks.Data";
	
	Directory.CreateDirectory((string) config["OutputDir"]);
	
	using (var dbConnection = new OrmLiteConnectionFactory(
		(string) config["ConnectionString"], (IOrmLiteDialectProvider) config["Dialect"]).OpenDbConnection())
	{
		config["Db"] = dbConnection;
		
		Util.Run(Path.Combine((string) config["TemplateDir"], "Types", "TypeGenerator.linq"), QueryResultFormat.Text, false, config);
	}
}


