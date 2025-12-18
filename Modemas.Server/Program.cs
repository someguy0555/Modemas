using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.SignalR;
using Modemas.Server.Services;
using Modemas.Server.Hubs;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;
using Modemas.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// "ASP.NET SignalR is a library for ASP.NET developers that simplifies the process of adding real-time web functionality to applications."
// https://learn.microsoft.com/en-us/aspnet/signalr/overview/getting-started/introduction-to-signalr
builder.Services.AddSignalR();
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Register typed HttpClient and map it to the interface
builder.Services.AddHttpClient<QuestionGenerationService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddScoped<IQuestionGenerationService>(sp => sp.GetRequiredService<QuestionGenerationService>());

builder.Services.AddSingleton<ILobbyStore, LobbyStore>();
builder.Services.AddScoped<ILobbyManager, LobbyManager>();
builder.Services.AddSingleton<IQuestionParser, QuestionParser>();

builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<ILobbyService, LobbyService>();
builder.Services.AddScoped<IQuestionRepository, EfQuestionRepository>();
builder.Services.AddSingleton(typeof(IStatisticsCalculator<,>), typeof(StatisticsCalculator<,>));
builder.Services.AddScoped<ILobbyNotifier, LobbyNotifier>(sp =>
{
    var hubContext = sp.GetRequiredService<IHubContext<LobbyHub>>();
    return new LobbyNotifier(hubContext.Clients, hubContext.Groups);
});

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=modemas.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.MapHub<LobbyHub>("/lobbyhub");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program { }
