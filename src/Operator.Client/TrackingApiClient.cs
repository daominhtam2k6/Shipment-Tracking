using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tracking.Contracts;

namespace Operator.Client;

public sealed record ApiResult<T>(T? Data, string? Error)
{
    public bool IsSuccess => Error is null;
}

public sealed class TrackingApiClient
{
    private readonly HttpClient _http;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    public TrackingApiClient(string baseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = _timeout
        };
    }

    public Task<ApiResult<ShipmentResponse>> CreateShipmentAsync(CreateShipmentRequest request)
        => SendAsync<ShipmentResponse>(HttpMethod.Post, "/shipments", request);

    public Task<ApiResult<ShipmentEventResponse>> AddEventAsync(string trackingCode, AddShipmentEventRequest request)
        => SendAsync<ShipmentEventResponse>(
            HttpMethod.Post, $"/shipments/{Uri.EscapeDataString(trackingCode)}/events", request);

    public Task<ApiResult<ShipmentResponse>> GetShipmentAsync(string trackingCode)
        => SendAsync<ShipmentResponse>(
            HttpMethod.Get, $"/shipments/{Uri.EscapeDataString(trackingCode)}");

    public Task<ApiResult<List<ShipmentEventResponse>>> GetHistoryAsync(string trackingCode)
        => SendAsync<List<ShipmentEventResponse>>(
            HttpMethod.Get, $"/shipments/{Uri.EscapeDataString(trackingCode)}/history");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
                request.Content = JsonContent.Create(body, body.GetType(), options: JsonDefaults.Options);

            using var response = await _http.SendAsync(request);

            // Đọc body thành CHUỖI trước, chưa ép sang kiểu nào
            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<T>(text, JsonDefaults.Options);
                return new ApiResult<T>(data, null);
            }

            return new ApiResult<T>(default, FormatError(response.StatusCode, text));
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>(default, $"Không kết nối được tới API tại {_http.BaseAddress}. API đã chạy chưa?");
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T>(default, $"API không phản hồi sau {_timeout.TotalSeconds} giây.");
        }
        catch (JsonException)
        {
            return new ApiResult<T>(default, "Phản hồi từ API không đúng định dạng mong đợi.");
        }
    }

    private static string FormatError(HttpStatusCode status, string body)
    {
        ApiError? error = null;

        try
        {
            error = JsonSerializer.Deserialize<ApiError>(body, JsonDefaults.Options);
        }
        catch (JsonException)
        {
            // Không đọc được: để error = null, rơi xuống nhánh "lỗi framework" bên dưới
        }

        if (error is not null && !string.IsNullOrWhiteSpace(error.Code))
            return $"[{(int)status}] {error.Code}: {error.Message}";

        // LỖI FRAMEWORK: body không phải dạng ApiError
        return $"[{(int)status}] Phản hồi không phải ApiError: {body}";
    }
}