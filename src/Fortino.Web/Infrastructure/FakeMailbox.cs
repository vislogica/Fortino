using Dapper;
using Fortino.Web.Data;
using Fortino.Web.Domain;

namespace Fortino.Web.Infrastructure;

// Fortino non spedisce email vere: le salva nella "posta finta", visibile dal menu.
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken ct = default);
}

public sealed class FakeMailbox(DbConnectionFactory factory, TimeProvider clock) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken ct = default)
    {
        await using var db = await factory.OpenAsync(ct);
        await db.ExecuteAsync(
            "INSERT INTO MailMessage (Recipient, Subject, HtmlBody, SentAt) VALUES (@recipient, @subject, @htmlBody, @sentAt);",
            new { recipient, subject, htmlBody, sentAt = clock.GetLocalNow().DateTime });
    }

    public async Task<IReadOnlyList<MailMessage>> GetAllAsync()
    {
        await using var db = await factory.OpenAsync();
        return (await db.QueryAsync<MailMessage>("SELECT * FROM MailMessage ORDER BY Id DESC;")).AsList();
    }
}
