using System.Net;
using System.Net.Mail;

namespace KetNoiGiaSu.Services;

public sealed class AccountEmailSender(IConfiguration config, IWebHostEnvironment environment)
{
    public async Task SendResetAsync(string email, string link)
    {
        using var message = new MailMessage(config["Email:From"] ?? "no-reply@educonnect.local", email)
        {
            Subject = "Đặt lại mật khẩu EduConnect",
            Body = $"Bạn đã yêu cầu đặt lại mật khẩu EduConnect.\n\nMở liên kết sau trong vòng 30 phút:\n{link}\n\nLiên kết chỉ dùng một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này."
        };
        using var client = new SmtpClient();
        if (environment.IsDevelopment() && config.GetValue("Email:UsePickupDirectory", true))
        {
            var directory = Path.Combine(environment.ContentRootPath, ".maildrop");
            Directory.CreateDirectory(directory);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = directory;
        }
        else
        {
            client.Host = config["Email:Host"] ?? throw new InvalidOperationException("SMTP host is not configured.");
            client.Port = config.GetValue("Email:Port", 587);
            client.EnableSsl = true;
            if (!string.IsNullOrEmpty(config["Email:Username"]))
                client.Credentials = new NetworkCredential(config["Email:Username"], config["Email:Password"]);
        }
        await client.SendMailAsync(message);
    }
}
