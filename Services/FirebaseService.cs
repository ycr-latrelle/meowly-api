using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeowlyAPI.Services;

/// <summary>
/// Low-level service that talks to Firebase Realtime Database via its REST API.
/// Uses FirebaseAdmin to mint short-lived access tokens automatically.
/// </summary>
public class FirebaseService
{
    private readonly string _dbUrl;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<FirebaseService> _logger;

    public FirebaseService(
        IConfiguration config,
        IHttpClientFactory httpFactory,
        ILogger<FirebaseService> logger)
    {
        _dbUrl      = config["Firebase:DatabaseUrl"]!.TrimEnd('/');
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    // ── TOKEN ──────────────────────────────────────────
    /// Gets a short-lived Google OAuth2 access token via the Admin SDK credential.
    private async Task<string> GetAccessTokenAsync()
    {
        // FirebaseAdmin uses the service-account credential to produce tokens
        var token = await FirebaseAdmin.FirebaseApp.DefaultInstance
            .Options.Credential.UnderlyingCredential
            .GetAccessTokenForRequestAsync();
        return token;
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var token  = await GetAccessTokenAsync();
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── HELPERS ────────────────────────────────────────
    private string NodeUrl(string path) => $"{_dbUrl}/{path}.json";

    // ── READ ───────────────────────────────────────────

    /// Returns all children of a node as a Dictionary<firebaseKey, T>.
    public async Task<Dictionary<string, T>> GetAllAsync<T>(string path)
    {
        var client = await AuthenticatedClientAsync();
        var res    = await client.GetStringAsync(NodeUrl(path));
        if (res == "null" || string.IsNullOrWhiteSpace(res))
            return new Dictionary<string, T>();

        var obj = JObject.Parse(res);
        var result = new Dictionary<string, T>();
        foreach (var prop in obj.Properties())
        {
            var item = prop.Value.ToObject<T>();
            if (item != null) result[prop.Name] = item;
        }
        return result;
    }

    /// Returns a single node value.
    public async Task<T?> GetOneAsync<T>(string path)
    {
        var client = await AuthenticatedClientAsync();
        var res    = await client.GetStringAsync(NodeUrl(path));
        if (res == "null" || string.IsNullOrWhiteSpace(res)) return default;
        return JsonConvert.DeserializeObject<T>(res);
    }

    // ── WRITE ──────────────────────────────────────────

    /// Firebase POST → generates a unique push key, returns it.
    public async Task<string> PushAsync(string path, object data)
    {
        var client  = await AuthenticatedClientAsync();
        var content = new StringContent(JsonConvert.SerializeObject(data),
                                        System.Text.Encoding.UTF8, "application/json");
        var res     = await client.PostAsync(NodeUrl(path), content);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        var obj  = JObject.Parse(body);
        return obj["name"]!.ToString();   // Firebase returns { "name": "<pushKey>" }
    }

    /// Firebase PUT → sets an exact path (overwrites).
    public async Task SetAsync(string path, object data)
    {
        var client  = await AuthenticatedClientAsync();
        var content = new StringContent(JsonConvert.SerializeObject(data),
                                        System.Text.Encoding.UTF8, "application/json");
        var res = await client.PutAsync(NodeUrl(path), content);
        res.EnsureSuccessStatusCode();
    }

    /// Firebase PATCH → merges fields (partial update).
    public async Task PatchAsync(string path, object data)
    {
        var client  = await AuthenticatedClientAsync();
        var content = new StringContent(JsonConvert.SerializeObject(data),
                                        System.Text.Encoding.UTF8, "application/json");
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), NodeUrl(path))
        { Content = content };
        var res = await client.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    /// Firebase DELETE → removes a node.
    public async Task DeleteAsync(string path)
    {
        var client = await AuthenticatedClientAsync();
        var res    = await client.DeleteAsync(NodeUrl(path));
        res.EnsureSuccessStatusCode();
    }

    // ── QUERY HELPERS ──────────────────────────────────

    /// Returns items where child field equals a value.
    /// Requires an index rule in Firebase: ".indexOn": ["fieldName"]
    public async Task<Dictionary<string, T>> QueryByFieldAsync<T>(
        string path, string field, string value)
    {
        var client = await AuthenticatedClientAsync();
        var url = $"{_dbUrl}/{path}.json?orderBy=\"{field}\"&equalTo=\"{value}\"";

        // Use GetAsync instead of GetStringAsync so we can check status code
        var response = await client.GetAsync(url);

        // 404 means the node doesn't exist yet — treat as empty, not an error
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return new Dictionary<string, T>();

        response.EnsureSuccessStatusCode();

        var res = await response.Content.ReadAsStringAsync();
        if (res == "null" || string.IsNullOrWhiteSpace(res))
            return new Dictionary<string, T>();

        var obj = JObject.Parse(res);
        var result = new Dictionary<string, T>();
        foreach (var prop in obj.Properties())
        {
            var item = prop.Value.ToObject<T>();
            if (item != null) result[prop.Name] = item;
        }
        return result;
    }
}
