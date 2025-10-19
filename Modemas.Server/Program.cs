using System.Text.Json.Serialization;

using Modemas.Server.Services;
using Modemas.Server.Hubs;
using Modemas.Server.Models;
using Modemas.Server.Interfaces;

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
builder.Services.AddSingleton<LobbyStore>();
builder.Services.AddSingleton<MatchService>();
builder.Services.AddSingleton<LobbyService>();
builder.Services.AddSingleton<IQuestionParser, QuestionParser>();
builder.Services.AddSingleton<IQuestionRepository, JsonQuestionRepository>();

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

app.Run();
