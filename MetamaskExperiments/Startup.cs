using MapsterMapper;

namespace MetamaskExperiments;

public class Startup
{
	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public IConfiguration Configuration { get; }

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddCors(options =>
		{
			options.AddPolicy("CorsPolicy",
				builder => builder.AllowAnyOrigin()
					.AllowAnyMethod()
					.AllowAnyHeader());
		});

		var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
		typeAdapterConfig.Scan(typeof(MapsterProfile).Assembly);

		var mapper = new Mapper(typeAdapterConfig);
		services.AddSingleton<IMapper>(mapper);

		services.AddControllers();

		services.AddSwaggerGen(options =>
		{
			var xmlCommentsFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			var xmlCommentsPath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFile);
			options.IncludeXmlComments(xmlCommentsPath);
		});
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseSwaggerUI(options =>
			{
				options.SwaggerEndpoint("/swagger/v1/swagger.json", "Step app transactions manager API");
				options.RoutePrefix = string.Empty;
			});
			app.UseSwagger(swaggerOptions => swaggerOptions.SerializeAsV2 = true);
		}

		app.UseCors(builder =>
		{
			builder.AllowAnyHeader()
				.AllowAnyOrigin()
				.AllowAnyMethod();
		});

		app.UseSerilogRequestLogging();

		app.UseHttpsRedirection();
		app.UseRouting();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapDefaultControllerRoute();
		});
	}
}