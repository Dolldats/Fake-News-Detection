
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace FND.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options => options.AddPolicy("ChromeExtension", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
            builder.Services.AddDbContext<FND.Infastructure.Data.FNDDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
            builder.Services.AddScoped<FND.Infastructure.Repositories.INewsRepository, FND.Infastructure.Repositories.NewsRepository>();
            builder.Services.AddScoped<FND.Application.Interfaces.INewsService, FND.Application.Services.NewsService>();
            var factCheckApiKey = builder.Configuration["GoogleFactCheck:ApiKey"] ?? string.Empty;
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<FND.Application.Interfaces.IDetectionEngine>(serviceProvider =>
                new FND.Application.Services.DetectionEngine(
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(), factCheckApiKey));

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FND.Infastructure.Data.FNDDbContext>();
                db.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();

            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            app.UseCors("ChromeExtension");

            app.UseAuthorization();


            app.MapControllers();
            app.MapGet("/", () => Results.Ok(new { status = "online", service = "Fake News Detection API" }));

            app.Run();
        }
    }
}
