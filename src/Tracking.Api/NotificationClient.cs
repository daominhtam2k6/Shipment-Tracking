using System.Net.Http.Json;
using Tracking.Contracts;

namespace Tracking.Api;

// Tuần 2 gọi HTTP trực tiếp sang Notification.Api. Sẽ được thay thế trong tương lai bằng RabbitMQ
public sealed class NotificationClient(HttpClient http, ILogger<NotificationClient> logger)
{
    public async Task NotifyAsync(ShipmentNotification notification)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(
                "/internal/notifications", notification, JsonDefaults.Options);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Notification.Api trả {Status} cho sự kiện {EventId}",
                    (int)response.StatusCode, notification.EventId);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning("Không gửi được thông báo cho {Code}: {Message}",
                notification.TrackingCode, ex.Message);
        }
    }
}