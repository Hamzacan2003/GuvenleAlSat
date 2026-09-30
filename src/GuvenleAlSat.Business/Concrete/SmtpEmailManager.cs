using System.Net;
using System.Net.Mail;
using GuvenleAlSat.Business.Abstract;
using Microsoft.Extensions.Configuration;

namespace GuvenleAlSat.Business.Concrete;

public class SmtpEmailManager : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailManager(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        var section = _configuration.GetSection("EmailSettings");
        var host = section["SmtpServer"] ?? "smtp.gmail.com";
        var port = int.TryParse(section["Port"], out var p) ? p : 587;
        var senderEmail = section["SenderEmail"] ?? "";
        var senderName = section["SenderName"] ?? "sahibinden.com";
        var password = section["Password"] ?? "";
        var enableSsl = bool.TryParse(section["EnableSsl"], out var ssl) && ssl;

        // Konsola her zaman bas (geliştirme için garanti)
        Console.WriteLine($"[E-POSTA GÖNDERİLİYOR]: {toEmail} | Konu: {subject}");

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(senderEmail, password),
            EnableSsl = enableSsl
        };

        var mail = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        await client.SendMailAsync(mail);
        Console.WriteLine($"[E-POSTA BAŞARIYLA İLETİLDİ]: {toEmail}");
    }
}