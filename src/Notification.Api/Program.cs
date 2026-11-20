using Microsoft.AspNetCore.SignalR;
using Notification.Api;
using Tracking.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR().AddJsonProtocol(o =>
{
    foreach (var converter in JsonDefaults.Options.Converters)
        o.PayloadSerializerOptions.Converters.Add(converter);
});
builder.Services.ConfigureHttpJsonOptions(o =>
{
    foreach (var converter in JsonDefaults.Options.Converters)
        o.SerializerOptions.Converters.Add(converter);
});

var app = builder.Build();

app.MapHub<ShipmentHub>(ShipmentHubContract.Path);

app.MapGet("/health", () => Results.Ok(new { service = "Notification.Api", status = "running" }));

app.MapPost("/internal/notifications", async (ShipmentNotification notification, IHubContext<ShipmentHub> hub) =>
{
    if (!TrackingCodeGenerator.TryNormalize(notification.TrackingCode, out var code))
        return Results.BadRequest(new ApiError(ApiError.InvalidEvent, "Mã theo dõi không hợp lệ."));

    await hub.Clients.Group(ShipmentHubContract.GroupName(code)).SendAsync(ShipmentHubContract.StatusChanged, notification);
    return Results.Accepted();
});

app.Run();