using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeowlyAPI.Services;

public class PaymentService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<PaymentService> _logger;

    private const string PayMongoBase = "https://api.paymongo.com/v1";

    public PaymentService(
        IConfiguration config,
        IHttpClientFactory httpFactory,
        ILogger<PaymentService> logger)
    {
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        var secretKey = _config["PayMongo:SecretKey"]!;
        var client = _httpFactory.CreateClient();
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey + ":"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    /// Creates a QRPh payment source and returns { sourceId, qrCode, checkoutUrl }
    public async Task<PaymentResult> CreateQrPaymentAsync(
        int amountInCentavos, string description, string referenceId)
    {
        var client = CreateClient();

        var payload = new
        {
            data = new
            {
                attributes = new
                {
                    amount = amountInCentavos,
                    currency = "PHP",
                    type = "qrph",
                    redirect = new
                    {
                        success = _config["PayMongo:SuccessUrl"] ?? "http://localhost:5500/index.html?payment=success",
                        failed = _config["PayMongo:FailedUrl"] ?? "http://localhost:5500/index.html?payment=failed",
                    },
                    billing = new { name = "Meowly Customer", email = "customer@meowly.com" },
                    description = description,
                    metadata = new { reference_id = referenceId }
                }
            }
        };

        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{PayMongoBase}/sources", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayMongo error: {Body}", body);
            throw new InvalidOperationException($"PayMongo error: {body}");
        }

        var obj = JObject.Parse(body);
        var sourceId = obj["data"]!["id"]!.ToString();
        var qrCode = obj["data"]!["attributes"]!["qr_code"]?.ToString() ?? "";
        var checkoutUrl = obj["data"]!["attributes"]!["redirect"]?["checkout_url"]?.ToString() ?? "";

        return new PaymentResult
        {
            SourceId = sourceId,
            QrCode = qrCode,
            CheckoutUrl = checkoutUrl,
            Amount = amountInCentavos,
            Description = description,
            ReferenceId = referenceId
        };
    }

    /// Creates a PayMongo Payment Link and returns the checkout URL
    public async Task<PaymentResult> CreatePaymentLinkAsync(
    int amountInCentavos, string description, string referenceId)
    {
        var client = CreateClient();

        var payload = new
        {
            data = new
            {
                attributes = new
                {
                    amount = amountInCentavos,
                    currency = "PHP",
                    description = description,
                    remarks = referenceId,
                }
            }
        };

        var json = JsonConvert.SerializeObject(payload);
        _logger.LogInformation("PayMongo link request: {Json}", json);  // log what we send

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{PayMongoBase}/links", content);
        var body = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("PayMongo link response ({Status}): {Body}",
            (int)response.StatusCode, body);   // log what PayMongo returns

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"PayMongo error: {body}");

        var obj = JObject.Parse(body);
        var linkId = obj["data"]!["id"]!.ToString();
        var checkoutUrl = obj["data"]!["attributes"]!["checkout_url"]!.ToString();
        var refNo = obj["data"]!["attributes"]!["reference_number"]?.ToString() ?? "";

        return new PaymentResult
        {
            SourceId = linkId,
            QrCode = "",
            CheckoutUrl = checkoutUrl,
            Amount = amountInCentavos,
            Description = description,
            ReferenceId = refNo
        };
    }

    /// Retrieves a payment source status from PayMongo
    public async Task<string> GetSourceStatusAsync(string sourceId)
    {
        var client = CreateClient();
        var response = await client.GetAsync($"{PayMongoBase}/sources/{sourceId}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return "unknown";

        var obj = JObject.Parse(body);
        var status = obj["data"]!["attributes"]!["status"]?.ToString() ?? "unknown";
        return status;
    }
}

public class PaymentResult
{
    public string SourceId { get; set; } = "";
    public string QrCode { get; set; } = "";
    public string CheckoutUrl { get; set; } = "";
    public int Amount { get; set; }
    public string Description { get; set; } = "";
    public string ReferenceId { get; set; } = "";
}