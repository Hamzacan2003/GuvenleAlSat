using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GuvenleAlSat.Business.Abstract;
using Microsoft.Extensions.Configuration;

namespace GuvenleAlSat.Business.Concrete;

public class SmtpEmailManager : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public SmtpEmailManager(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var apiKey = _configuration["EmailSettings:BrevoApiKey"]
                     ?? _configuration["BREVO_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");

        var senderEmail = _configuration["EmailSettings:SenderEmail"]
                          ?? "hamzacana98@gmail.com";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine($"[BREVO UYARI]: API Key eksik. Hedef: {toEmail}");
            return;
        }

        Console.WriteLine($"[BREVO İLE GÖNDERİLİYOR]: {toEmail} | Konu: {subject}");

        var payload = new
        {
            sender = new { name = "Güvenle Al Sat", email = senderEmail.Trim() },
            to = new[] { new { email = toEmail.Trim() } },
            subject = subject,
            htmlContent = htmlMessage
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey.Trim());
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[E-POSTA BAŞARIYLA İLETİLDİ]: {toEmail}");
        }
        else
        {
            Console.WriteLine($"[BREVO API HATASI]: {response.StatusCode} - {responseBody}");
            throw new Exception($"Brevo API Hatası: {responseBody}");
        }
    }
}