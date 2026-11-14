using Tracking.Api;
using Tracking.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    foreach (var converter in JsonDefaults.Options.Converters)
        o.SerializerOptions.Converters.Add(converter);
});

builder.Services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "Tracking.Api",
    status = "running"
}));

var shipments = app.MapGroup("/shipments");

shipments.MapPost("/", async (CreateShipmentRequest request, IShipmentRepository repo) =>
{
    string?[] required =
        [request.SenderName, request.SenderAddress, request.ReceiverName, request.ReceiverAddress];

    if (required.Any(string.IsNullOrWhiteSpace))
        return Results.BadRequest(new ApiError(
            ApiError.InvalidShipment,
            "Thieu ten hoac dia chi cua nguoi gui/nguoi nhan."));

    var created = await repo.CreateAsync(request);
    return Results.Created($"/shipments/{created.TrackingCode}", created);
});

shipments.MapGet("/{trackingCode}", async (string trackingCode, IShipmentRepository repo) =>
{
    if (!TrackingCodeGenerator.TryNormalize(trackingCode, out var code))
        return NotFound();

    var shipment = await repo.GetAsync(code);
    return shipment is null ? NotFound() : Results.Ok(shipment);
});

shipments.MapPost("/{trackingCode}/events", async (
    string trackingCode, AddShipmentEventRequest request, IShipmentRepository repo) =>
{
    if (!TrackingCodeGenerator.TryNormalize(trackingCode, out var code))
        return NotFound();

    if (string.IsNullOrWhiteSpace(request.Location)
        || !Enum.IsDefined(request.Status)
        || request.Status == ShipmentStatus.Created)
        return Results.BadRequest(new ApiError(
            ApiError.InvalidEvent,
            "Thieu vi tri, hoac trang thai khong hop le (khong duoc gui Created)."));

    var result = await repo.AddEventAsync(code, request);

    return result.Outcome switch
    {
        AddEventOutcome.Success => Results.Created($"/shipments/{code}/history", result.Event),
        AddEventOutcome.NotFound => NotFound(),
        AddEventOutcome.InvalidTransition => Results.Conflict(new ApiError(
            ApiError.InvalidTransition,
            "Khong the chuyen sang trang thai nay tu trang thai hien tai.")),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
});

shipments.MapGet("/{trackingCode}/history", async (string trackingCode, IShipmentRepository repo) =>
{
    if (!TrackingCodeGenerator.TryNormalize(trackingCode, out var code))
        return NotFound();

    var history = await repo.GetHistoryAsync(code);
    return history is null ? NotFound() : Results.Ok(history);
});

app.Run();

static IResult NotFound() => Results.NotFound(new ApiError(
    ApiError.ShipmentNotFound,
    "Khong tim thay buu gui voi ma da cho."));