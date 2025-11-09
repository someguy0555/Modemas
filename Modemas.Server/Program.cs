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

builder.Services.AddHttpClient<QuestionGenerationService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});
builder.Services.AddHttpClient<QuestionGenerationService>();
builder.Services.AddSingleton<LobbyStore>();
builder.Services.AddScoped<MatchService>();
builder.Services.AddScoped<LobbyService>();
builder.Services.AddSingleton<IQuestionParser, QuestionParser>();
builder.Services.AddScoped<IQuestionRepository, EfQuestionRepository>();
builder.Services.AddScoped<LobbyNotifier>(sp =>
{
    var hubContext = sp.GetRequiredService<IHubContext<LobbyHub>>();
    return new LobbyNotifier(hubContext.Clients, hubContext.Groups);
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=modemas.db"));
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

// Idk, used it to migrate and stuff.
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//     db.Database.EnsureDeleted();
//     db.Database.EnsureCreated();
// }

app.Run();
