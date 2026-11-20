using System.Text.Json;
using Tracking.Contracts;

namespace Customer.Client;

public sealed class TrackingReadClient(string baseUrl)
{
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(10)
    };

    public Task<(ShipmentResponse? Data, string? Error)> GetShipmentAsync(string code)
        => GetAsync<ShipmentResponse>($"/shipments/{Uri.EscapeDataString(code)}");

    public Task<(List<ShipmentEventResponse>? Data, string? Error)> GetHistoryAsync(string code)
        => GetAsync<List<ShipmentEventResponse>>($"/shipments/{Uri.EscapeDataString(code)}/history");

    private async Task<(T? Data, string? Error)> GetAsync<T>(string path)
    {
        try
        {
            using var response = await _http.GetAsync(path);
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return (JsonSerializer.Deserialize<T>(text, JsonDefaults.Options), null);

            ApiError? error = null;
            try { error = JsonSerializer.Deserialize<ApiError>(text, JsonDefaults.Options); }
            catch (JsonException) { }

            return (default, !string.IsNullOrWhiteSpace(error?.Code)
                ? $"[{(int)response.StatusCode}] {error.Code}: {error.Message}"
                : $"[{(int)response.StatusCode}] {text}");
        }
        catch (HttpRequestException)
        {
            return (default, $"Không kết nối được Tracking.Api tại {_http.BaseAddress}.");
        }
        catch (TaskCanceledException)
        {
            return (default, "Tracking.Api không phản hồi.");
        }
        catch (JsonException)
        {
            return (default, "Phản hồi từ Tracking.Api không đúng định dạng.");
        }
    }
}