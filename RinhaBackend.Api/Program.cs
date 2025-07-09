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

var todosApi = app.MapGroup("/todos");
todosApi.MapPost("/", (Todo todo, IMessenger messenger) =>
{
    messenger.SendAsync(todo, AppJsonSerializerContext.Default.Todo);
});

app.Run();