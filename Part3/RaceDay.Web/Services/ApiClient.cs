using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiException : Exception
{
    public HttpStatusCode Status { get; }

    public ApiException(HttpStatusCode status, string message) : base(message)
    {
        Status = status;
    }
}

// The API says the session is missing or expired
public class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The session has expired.") { }
}

public class ApiResult
{
    public HttpStatusCode Status { get; init; }
    public string Body { get; init; } = string.Empty;
    public string? SessionCookie { get; init; }

    public bool Ok => (int)Status >= 200 && (int)Status < 300;

    // A short message that is safe to show to the user
    public string Error => Status switch
    {
        HttpStatusCode.Forbidden => "You are not allowed to do that.",
        HttpStatusCode.NotFound => string.IsNullOrWhiteSpace(Body) ? "That item could not be found." : ReadMessage(Body),
        _ => ReadMessage(Body)
    };

    private static string ReadMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "Something went wrong. Please try again.";

        var text = body.Trim();
        if (text.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                // Validation errors: { "errors": { "Field": ["message", ...] } }
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                            ? p.Value.EnumerateArray().Select(v => v.GetString() ?? "")
                            : Enumerable.Empty<string>())
                        .Where(m => m.Length > 0)
                        .ToList();
                    if (messages.Count > 0) return string.Join(" ", messages);
                }

                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString() ?? "Something went wrong. Please try again.";
            }
            catch (JsonException)
            {
                // fall through to the generic message
            }
            return "Something went wrong. Please try again.";
        }

        // Plain-text messages from the API, for example "Role must be Organizer or Participant."
        return text.Trim('"').Length > 300 ? "Something went wrong. Please try again." : text.Trim('"');
    }
}

public class ApiClient
{
    public const string SessionKey = "ApiSessionCookie";
    private const string ApiCookieName = "RaceDay.Session";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;

    public ApiClient(HttpClient http, IHttpContextAccessor context)
    {
        _http = http;
        _context = context;
    }

    public async Task<ApiResult> SendAsync(HttpMethod method, string path, object? body = null,
        bool throwWhenUnauthorized = true)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        }
        return await ExecuteAsync(request, throwWhenUnauthorized);
    }

    // Sends one image to the API as multipart/form-data. The API stores it in Azure Blob Storage.
    public async Task<ApiResult> UploadAsync(string path, IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        return await ExecuteAsync(request, true);
    }

    // GET that returns the parsed body, or throws when the API refuses
    public async Task<T> GetAsync<T>(string path)
    {
        var result = await SendAsync(HttpMethod.Get, path);
        if (!result.Ok) throw new ApiException(result.Status, result.Error);
        return Parse<T>(result)!;
    }

    public static T? Parse<T>(ApiResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Body)) return default;
        return JsonSerializer.Deserialize<T>(result.Body, Json);
    }

    private async Task<ApiResult> ExecuteAsync(HttpRequestMessage request, bool throwWhenUnauthorized)
    {
        // Pass the user's API session along, so the API knows who is asking
        var cookie = _context.HttpContext?.Session.GetString(SessionKey);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={cookie}");
        }

        using var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized && throwWhenUnauthorized)
        {
            throw new ApiUnauthorizedException();
        }

        string? newCookie = null;
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            var mine = setCookies.FirstOrDefault(c => c.StartsWith(ApiCookieName + "=", StringComparison.Ordinal));
            if (mine != null)
            {
                newCookie = mine.Substring(ApiCookieName.Length + 1).Split(';')[0];
            }
        }

        return new ApiResult { Status = response.StatusCode, Body = body, SessionCookie = newCookie };
    }
}
