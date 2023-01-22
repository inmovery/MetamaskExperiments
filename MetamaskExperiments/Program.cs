namespace MetamaskExperiments;

public class Program
{
	public static void Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration().CreateLogger();

		var builder = CreateHostBuilder(args).Build();
		builder.Run();
	}

	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.ConfigureWebHostDefaults(webBuilder =>
			{
				webBuilder.UseStartup<Startup>();
			})
			.UseSerilog((hostingContext, loggerConfiguration) =>
				loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration)
			);
	}
}