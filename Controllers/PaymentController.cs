using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeowlyAPI.Services;

namespace MeowlyAPI.Controllers;

// ═══════════════════════════════════════════════════════════
//  PAYMENTS
// ═══════════════════════════════════════════════════════════
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _payment;
    private readonly AppointmentService _appt;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        PaymentService payment,
        AppointmentService appt,
        ILogger<PaymentsController> logger)
    {
        _payment = payment;
        _appt = appt;
        _logger = logger;
    }

    // POST /api/payments/qr
    // Creates a QRPh payment source — returns qrCode + sourceId
    [HttpPost("qr")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateQr([FromBody] CreatePaymentDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than 0." });

        try
        {
            // PayMongo expects amount in centavos (multiply by 100)
            var amountCentavos = (int)(dto.Amount * 100);
            var result = await _payment.CreateQrPaymentAsync(
                amountCentavos,
                dto.Description ?? "Meowly Payment",
                dto.ReferenceId ?? Guid.NewGuid().ToString()
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create QR payment");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST /api/payments/link
    // Creates a PayMongo payment link — returns checkoutUrl
    [HttpPost("link")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateLink([FromBody] CreatePaymentDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest(new { message = "Amount must be greater than 0." });

        try
        {
            var amountCentavos = (int)(dto.Amount * 100);
            var result = await _payment.CreatePaymentLinkAsync(
                amountCentavos,
                dto.Description ?? "Meowly Payment",
                dto.ReferenceId ?? Guid.NewGuid().ToString()
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment link");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET /api/payments/status/{sourceId}
    // Polls the status of a QR payment source
    [HttpGet("status/{sourceId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(string sourceId)
    {
        try
        {
            var status = await _payment.GetSourceStatusAsync(sourceId);
            return Ok(new { status });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }


    // POST /api/payments/webhook
    // Receives PayMongo webhook events and auto-confirms bookings
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            _logger.LogInformation("PayMongo webhook: {Body}", body);

            var json = Newtonsoft.Json.Linq.JObject.Parse(body);
            var type = json["data"]?["attributes"]?["type"]?.ToString();
            var data = json["data"]?["attributes"]?["data"];

            if (type == "payment.paid" || type == "link.payment.paid")
            {
                // Try to get reference from metadata or remarks
                var referenceId =
                    data?["attributes"]?["metadata"]?["reference_id"]?.ToString()
                    ?? data?["attributes"]?["remarks"]?.ToString()
                    ?? "";

                if (!string.IsNullOrEmpty(referenceId))
                {
                    // Auto-confirm the appointment if referenceId matches
                    try
                    {
                        await _appt.UpdateStatusAsync(referenceId, "Confirmed");
                        _logger.LogInformation("Auto-confirmed appointment {Id}", referenceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Could not confirm appointment {Id}: {Msg}", referenceId, ex.Message);
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook error");
            return StatusCode(500);
        }
    }

    // GET /api/payments/test
    [HttpGet("test")]
    [AllowAnonymous]
    public async Task<IActionResult> Test(
        [FromServices] IConfiguration config,
        [FromServices] IHttpClientFactory httpFactory)
    {
        try
        {
            var secretKey = config["PayMongo:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
                return BadRequest(new { message = "SecretKey is missing from config." });

            var encoded = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(secretKey + ":"));

            var client = httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var payload = new
            {
                data = new
                {
                    attributes = new
                    {
                        amount = 30000,
                        currency = "PHP",
                        description = "Test"
                    }
                }
            };

            var content = new System.Net.Http.StringContent(
                Newtonsoft.Json.JsonConvert.SerializeObject(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://api.paymongo.com/v1/links", content);
            var body = await response.Content.ReadAsStringAsync();

            return Ok(new
            {
                status = (int)response.StatusCode,
                secretKeyUsed = secretKey[..8] + "...",
                body
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

// ── DTOs ──────────────────────────────────────────────────
public class CreatePaymentDto
{
    public decimal Amount { get; set; }   // in PHP (e.g. 300.00)
    public string? Description { get; set; }
    public string? ReferenceId { get; set; }   // appointment or order ID
}