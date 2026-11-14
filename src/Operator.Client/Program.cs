using System.Text;
using Operator.Client;
using Tracking.Contracts;

// Sử dụng tiếng Việt có dấu trên terminal
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var baseUrl = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("TRACKING_API_URL") ?? "http://localhost:5100";

var api = new TrackingApiClient(baseUrl);
Console.WriteLine($"Operator.Client - API: {baseUrl}");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1. Tạo bưu gửi");
    Console.WriteLine("2. Cập nhật trạng thái");
    Console.WriteLine("3. Xem trạng thái");
    Console.WriteLine("4. Xem lịch sử");
    Console.WriteLine("0. Thoát");
    Console.Write("Chọn: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": await CreateShipmentAsync(); break;
        case "2": await AddEventAsync(); break;
        case "3": await ShowShipmentAsync(); break;
        case "4": await ShowHistoryAsync(); break;
        case "0": return;
        default: Console.WriteLine("Lựa chọn không hợp lệ."); break;
    }
}

async Task CreateShipmentAsync()
{
    var sender = Ask("Tên người gửi");
    var senderAddress = Ask("Địa chỉ người gửi");
    var receiver = Ask("Tên người nhận");
    var receiverAddress = Ask("Địa chỉ người nhận");
    var initial = Ask("Vị trí ban đầu (Enter để bỏ qua)");

    var request = new CreateShipmentRequest(
        sender, senderAddress, receiver, receiverAddress,
        string.IsNullOrWhiteSpace(initial) ? null : initial);

    var result = await api.CreateShipmentAsync(request);
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Lỗi: {result.Error}");
        return;
    }

    Console.WriteLine($"Đã tạo bưu gửi. Mã theo dõi: {result.Data!.TrackingCode}");
    Console.WriteLine($"Trạng thái: {result.Data.Status}");
}

async Task AddEventAsync()
{
    var code = Ask("Mã theo dõi");

    var statuses = Enum.GetValues<ShipmentStatus>();
    for (int i = 0; i < statuses.Length; i++)
        Console.WriteLine($"  {i + 1}. {statuses[i]}");

    if (!int.TryParse(Ask("Chọn trạng thái mới"), out var choice) || choice < 1 || choice > statuses.Length)
    {
        Console.WriteLine("Lựa chọn trạng thái không hợp lệ.");
        return;
    }

    var location = Ask("Vị trí");
    var note = Ask("Ghi chú (Enter để bỏ qua)");

    var request = new AddShipmentEventRequest(
        statuses[choice - 1],
        location,
        string.IsNullOrWhiteSpace(note) ? null : note);

    var result = await api.AddEventAsync(code, request);
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Lỗi: {result.Error}");
        return;
    }

    var e = result.Data!;
    Console.WriteLine($"Đã cập nhật: {e.Status} tại {e.Location} lúc {e.OccurredAt.ToLocalTime():HH:mm:ss dd/MM/yyyy}");
}

async Task ShowShipmentAsync()
{
    var result = await api.GetShipmentAsync(Ask("Mã theo dõi"));
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Lỗi: {result.Error}");
        return;
    }

    var s = result.Data!;
    Console.WriteLine($"Mã: {s.TrackingCode}");
    Console.WriteLine($"Trạng thái: {s.Status} | Vị trí hiện tại: {s.CurrentLocation}");
    Console.WriteLine($"Người gửi: {s.SenderName} ({s.SenderAddress})");
    Console.WriteLine($"Người nhận: {s.ReceiverName} ({s.ReceiverAddress})");
    Console.WriteLine($"Tạo lúc: {s.CreatedAt.ToLocalTime():HH:mm:ss dd/MM/yyyy}");
}

async Task ShowHistoryAsync()
{
    var result = await api.GetHistoryAsync(Ask("Mã theo dõi"));
    if (!result.IsSuccess)
    {
        Console.WriteLine($"Lỗi: {result.Error}");
        return;
    }

    foreach (var e in result.Data!)
    {
        var note = string.IsNullOrWhiteSpace(e.Note) ? "" : $" | {e.Note}";
        Console.WriteLine($"{e.OccurredAt.ToLocalTime():HH:mm:ss dd/MM/yyyy} | {e.Status,-10} | {e.Location}{note}");
    }
}

static string Ask(string prompt)
{
    Console.Write($"{prompt}: ");
    return Console.ReadLine()?.Trim() ?? "";
}