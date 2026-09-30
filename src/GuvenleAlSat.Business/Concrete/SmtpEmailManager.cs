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
        var apiKey = _configuration["EmailSettings:ResendApiKey"]
                     ?? _configuration["RESEND_API_KEY"]
                     ?? Environment.GetEnvironmentVariable("RESEND_API_KEY");

        // API Key yoksa konsola bas ve çık
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine($"[RESEND UYARI]: API Key bulunamadı. Kod konsola yazdırılıyor -> {toEmail}");
            return;
        }

        Console.WriteLine($"[RESEND İLE GÖNDERİLİYOR]: {toEmail} | Konu: {subject}");

        var payload = new
        {
            from = "GuvenleAlSat <onboarding@resend.dev>",
            to = new[] { toEmail },
            subject = subject,
            html = htmlMessage
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[E-POSTA BAŞARIYLA İLETİLDİ]: {toEmail}");
        }
        else
        {
            Console.WriteLine($"[RESEND API HATASI]: {response.StatusCode} - {responseBody}");
            throw new Exception($"Resend API Hatası: {responseBody}");
        }
    }
}