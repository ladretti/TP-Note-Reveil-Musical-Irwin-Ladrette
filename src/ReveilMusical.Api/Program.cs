using ReveilMusical.Api;
using ReveilMusical.Application;
using ReveilMusical.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddProblemDetails()
    .ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.RespectRequiredConstructorParameters = true;
        options.SerializerOptions.RespectNullableAnnotations = true;
    })
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapPost("/wakeups", WakeUpEndpoint.HandleAsync);

app.Run();
