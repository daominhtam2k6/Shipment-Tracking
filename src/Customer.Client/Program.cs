using System.Text;
using Customer.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Tracking.Contracts;
using Microsoft.Extensions.DependencyInjection;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var trackingUrl = Environment.GetEnvironmentVariable("TRACKING_API_URL") ?? "http://localhost:5100";
var notificationUrl = Environment.GetEnvironmentVariable("NOTIFICATION_API_URL") ?? "http://localhost:5200";

var tracking = new TrackingReadClient(trackingUrl);

// Trạng thái phía client: mã đang theo dõi (đã chuẩn hóa) và các EventId đã hiển thị
var watched = new HashSet<string>();
var seenEvents = new HashSet<Guid>();
var sync = new object(); // thông báo đến trên luồng khác

var connection = new HubConnectionBuilder()
    .WithUrl(notificationUrl + ShipmentHubContract.Path)
    .WithAutomaticReconnect()
    .AddJsonProtocol(o =>
    {
        foreach (var converter in JsonDefaults.Options.Converters)
            o.PayloadSerializerOptions.Converters.Add(converter);
    })
    .Build();

// Nhận thông báo đẩy từ server
connection.On<ShipmentNotification>(ShipmentHubContract.StatusChanged, n =>
{
    lock (sync)
    {
        if (!seenEvents.Add(n.EventId)) return; // đã hiển thị (qua REST) -> bỏ qua bản trùng
    }
    Console.WriteLine();
    Console.WriteLine($"[THÔNG BÁO] {n.TrackingCode}: {n.Status} tại {n.Location} " +
                      $"lúc {n.OccurredAt.ToLocalTime():HH:mm:ss dd/MM/yyyy}");
});

connection.Reconnecting += _ =>
{
    Console.WriteLine("\n[Mất kết nối Notification.Api, đang thử kết nối lại...]");
    return Task.CompletedTask;
};

connection.Reconnected += async _ =>
{
    Console.WriteLine("\n[Đã kết nối lại Notification.Api]");
    await ResubscribeAsync();
};

connection.Closed += async error =>
{
    if (error is null) return;

    // Tự kết nối lại đã bỏ cuộc: kết nối lại từ đầu
    Console.WriteLine("\n[Kết nối đã đóng. Thử kết nối lại từ đầu...]");
    await ConnectAsync();
    await ResubscribeAsync();
};

await ConnectAsync();

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1. Theo dõi một bưu gửi");
    Console.WriteLine("2. Bỏ theo dõi");
    Console.WriteLine("3. Xem lịch sử");
    Console.WriteLine("0. Thoát");
    Console.Write("Chọn: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": await WatchAsync(); break;
        case "2": await UnwatchAsync(); break;
        case "3":
            if (TryReadCode(out var code)) await ShowHistoryAsync(code);
            break;
        case "0":
            await connection.DisposeAsync();
            return;
        default: Console.WriteLine("Lựa chọn không hợp lệ."); break;
    }
}

// ---------- Kết nối ----------

async Task ConnectAsync()
{
    while (true)
    {
        try
        {
            await connection.StartAsync();
            Console.WriteLine($"Đã kết nối Notification.Api tại {notificationUrl}");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chưa kết nối được Notification.Api ({ex.Message}). Thử lại sau 3 giây...");
            await Task.Delay(3000);
        }
    }
}

// Vào lại nhóm cho mọi mã đang theo dõi, rồi gọi REST để bù sự kiện đã lỡ
async Task ResubscribeAsync()
{
    string[] codes;
    lock (sync) codes = watched.ToArray();

    foreach (var code in codes)
    {
        try
        {
            await connection.InvokeAsync(ShipmentHubContract.Watch, code);
            await ShowHistoryAsync(code);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Thao tác không khả dụng: {ex.Message}");
        }
    }
}

// ---------- Thao tác menu ----------

async Task WatchAsync()
{
    if (!TryReadCode(out var code)) return;

    try
    {
        await connection.InvokeAsync(ShipmentHubContract.Watch, code);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Không theo dõi được: {ex.Message}");
        return;
    }
    lock (sync) watched.Add(code);
    await ShowHistoryAsync(code);
}

async Task UnwatchAsync()
{
    if (!TryReadCode(out var code)) return;

    try
    {
        await connection.InvokeAsync(ShipmentHubContract.Unwatch, code);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Không bỏ theo dõi được: {ex.Message}");
    }

    lock (sync) watched.Remove(code);
    Console.WriteLine($"Đã bỏ theo dõi {code}.");
}

async Task ShowHistoryAsync(string code)
{
    var (shipment, error) = await tracking.GetShipmentAsync(code);
    if (error is not null)
    {
        Console.WriteLine($"Lỗi: {error}");
        return;
    }

    var (history, historyError) = await tracking.GetHistoryAsync(code);
    if (historyError is not null)
    {
        Console.WriteLine($"Lỗi: {historyError}");
        return;
    }

    Console.WriteLine($"--- {shipment!.TrackingCode}: {shipment.Status} tại {shipment.CurrentLocation} ---");
    foreach (var e in history!)
    {
        lock (sync) seenEvents.Add(e.EventId); // đánh dấu đã hiển thị, để thông báo trùng bị bỏ qua
        var note = string.IsNullOrWhiteSpace(e.Note) ? "" : $" | {e.Note}";
        Console.WriteLine($"{e.OccurredAt.ToLocalTime():HH:mm:ss dd/MM/yyyy} | {e.Status,-10} | {e.Location}{note}");
    }
}

static bool TryReadCode(out string code)
{
    Console.Write("Mã theo dõi: ");
    var input = Console.ReadLine();
    if (TrackingCodeGenerator.TryNormalize(input, out code)) return true;

    Console.WriteLine("Mã theo dõi không đúng định dạng.");
    return false;
}