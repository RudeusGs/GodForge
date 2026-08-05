using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities.Ops;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Messaging;

public sealed class EmailOutbox : IEmailOutbox
{
    public const string EventType = "email.delivery";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly GodForgeDbContext _context;
    private readonly IClock _clock;
    private readonly byte[] _key;

    public EmailOutbox(GodForgeDbContext context, IClock clock, IOptions<OutboxEncryptionSettings> encryptionOptions)
    {
        _context = context;
        _clock = clock;
        _key = DeriveKey(encryptionOptions.Value.Key);
    }

    public Task EnqueueAsync(string recipient, string subject, string htmlBody, string correlationId, CancellationToken cancellationToken = default)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(new EmailPayload(recipient, subject, htmlBody), JsonOptions);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(_key, tag.Length))
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var protectedPayload = JsonSerializer.Serialize(new ProtectedPayload(
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(tag)), JsonOptions);

        var message = OutboxMessage.Create(
            "Email",
            null,
            EventType,
            protectedPayload,
            "{\"encrypted\":true,\"algorithm\":\"AES-256-GCM\"}",
            correlationId,
            _clock.UtcNow);

        return _context.OutboxMessages.AddAsync(message, cancellationToken).AsTask();
    }

    internal static EmailPayload Decrypt(
        string protectedPayload,
        string encryptionKey,
        string? legacyEncryptionKey = null)
    {
        var envelope = JsonSerializer.Deserialize<ProtectedPayload>(protectedPayload, JsonOptions)
            ?? throw new JsonException("Email outbox payload is empty.");

        try
        {
            return DecryptWithKey(envelope, encryptionKey);
        }
        catch (CryptographicException) when (!string.IsNullOrWhiteSpace(legacyEncryptionKey))
        {
            return DecryptWithKey(envelope, legacyEncryptionKey!);
        }
    }

    private static EmailPayload DecryptWithKey(ProtectedPayload envelope, string encryptionKey)
    {
        var key = DeriveKey(encryptionKey);
        var nonce = Convert.FromBase64String(envelope.Nonce);
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
        var tag = Convert.FromBase64String(envelope.Tag);
        var plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(key, tag.Length))
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return JsonSerializer.Deserialize<EmailPayload>(plaintext, JsonOptions)
            ?? throw new JsonException("Email outbox payload is invalid.");
    }

    private static byte[] DeriveKey(string encryptionKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptionKey);
        return SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
    }

    internal sealed record EmailPayload(string Recipient, string Subject, string HtmlBody);
    private sealed record ProtectedPayload(string Nonce, string Ciphertext, string Tag);
}
