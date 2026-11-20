using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RealEstateAI.Api.Dtos;
using RealEstateAI.Api.Models;

namespace RealEstateAI.Api.Services;

public interface IWhatsAppService
{
    Task<WhatsAppResultDto> SendLeadFollowUpAsync(Lead lead, PropertyListing? property);
    Task<bool> SendTextAsync(string phone, string message);
    string BuildLink(string phone, string message);
}

/// <summary>
/// Sends WhatsApp follow-ups through the Meta Cloud API when configured.
/// Without credentials it returns a wa.me deep link that opens WhatsApp with the message pre-filled.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly string? _phoneNumberId;
    private readonly string? _accessToken;
    private readonly string _version;

    public WhatsAppService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _phoneNumberId = config["WhatsApp:PhoneNumberId"];
        _accessToken = config["WhatsApp:AccessToken"];
        _version = config["WhatsApp:ApiVersion"] ?? "v21.0";
    }

    public async Task<WhatsAppResultDto> SendLeadFollowUpAsync(Lead lead, PropertyListing? property)
    {
        var propertyText = property is null
            ? "our available properties"
            : $"{property.Title} ({AiService.FormatPkr(property.Price, property.ListingType.ToString())}) in {property.Locality}, {property.City}";

        var message =
            $"Assalam-o-Alaikum {lead.Name}! 👋\n\n" +
            $"Shukriya aapki inquiry ke liye. Aap ne {propertyText} mein dilchaspi dikhai hai.\n\n" +
            "Main RealEstate AI agent hoon. Main aapko similar properties, price details aur site visit ka time bhej sakta hoon. " +
            "Aap apna pasandida time batayein?\n\n" +
            "(Agar ye message galat number par aaya hai to ignore kar dein.)";

        if (!string.IsNullOrWhiteSpace(_phoneNumberId) && !string.IsNullOrWhiteSpace(_accessToken))
        {
            var sent = await SendViaCloudApiAsync(lead.Phone, message);
            if (sent)
                return new WhatsAppResultDto(null, true, message);
        }

        return new WhatsAppResultDto(BuildLink(lead.Phone, message), false, message);
    }

    public async Task<bool> SendTextAsync(string phone, string message)
    {
        if (string.IsNullOrWhiteSpace(_phoneNumberId) || string.IsNullOrWhiteSpace(_accessToken))
            return false;

        return await SendViaCloudApiAsync(phone, message);
    }

    public string BuildLink(string phone, string message)
    {
        var normalized = NormalizePhone(phone);
        if (string.IsNullOrEmpty(normalized)) return string.Empty;
        return $"https://wa.me/{normalized}?text={Uri.EscapeDataString(message)}";
    }

    private async Task<bool> SendViaCloudApiAsync(string phone, string message)
    {
        try
        {
            var to = NormalizePhone(phone);
            if (string.IsNullOrEmpty(to)) return false;

            var body = new
            {
                messaging_product = "whatsapp",
                to,
                type = "text",
                text = new { body = message }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/{_version}/{_phoneNumberId}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("00")) digits = digits[2..];
        if (digits.StartsWith("0")) digits = "92" + digits[1..];
        else if (digits.Length == 10 && digits.StartsWith("3")) digits = "92" + digits;

        return digits.StartsWith("92") ? digits : string.Empty;
    }
}
