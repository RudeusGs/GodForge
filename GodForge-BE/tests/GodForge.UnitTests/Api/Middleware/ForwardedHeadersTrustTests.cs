using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodForge.UnitTests.Api.Middleware;

public sealed class ForwardedHeadersTrustTests
{
    [Fact]
    public async Task DirectConnection_KeepsSocketAddress()
    {
        var context = CreateContext("198.51.100.20");

        await CreateMiddleware([]).Invoke(context);

        Assert.Equal(IPAddress.Parse("198.51.100.20"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task TrustedProxy_UsesForwardedClientAddress()
    {
        var context = CreateContext("10.0.0.10", "203.0.113.25");

        await CreateMiddleware([IPAddress.Parse("10.0.0.10")]).Invoke(context);

        Assert.Equal(IPAddress.Parse("203.0.113.25"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task UntrustedPeer_CannotSpoofForwardedClientAddress()
    {
        var context = CreateContext("198.51.100.20", "203.0.113.25");

        await CreateMiddleware([IPAddress.Parse("10.0.0.10")]).Invoke(context);

        Assert.Equal(IPAddress.Parse("198.51.100.20"), context.Connection.RemoteIpAddress);
    }

    private static DefaultHttpContext CreateContext(string remoteAddress, string? forwardedFor = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        if (forwardedFor is not null)
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        return context;
    }

    private static ForwardedHeadersMiddleware CreateMiddleware(IReadOnlyCollection<IPAddress> knownProxies)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
            ForwardLimit = 1
        };
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (var proxy in knownProxies)
            options.KnownProxies.Add(proxy);

        return new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            LoggerFactory.Create(_ => { }),
            Options.Create(options));
    }
}
