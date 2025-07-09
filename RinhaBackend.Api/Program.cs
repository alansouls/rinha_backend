using System.Text.Json.Serialization;
using RinhaBackend.Shared.JsonSerialization;
using RinhaBackend.Shared.Messaging.Interfaces;
using RinhaBackend.Shared.Messaging.RabbitMq.Extensions;
using RinhaBackend.Shared.Models;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddRabbitMqMessaging();

var app = builder.Build();

var sampleTodos = new Todo[]
{
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapPost("/", (Todo todo, IMessenger messenger) =>
{
    messenger.SendAsync(todo, AppJsonSerializerContext.Default.Todo);
});

app.Run();