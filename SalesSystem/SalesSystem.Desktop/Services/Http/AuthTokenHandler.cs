using System.Net;
using System.Net.Http.Headers;
using SalesSystem.Desktop.Services.Interfaces;
using SalesSystem.Desktop.Messages;

namespace SalesSystem.Desktop.Services.Http;

public sealed class AuthTokenHandler : DelegatingHandler
{
    private readonly ISessionService _sessionService;
    private readonly IEventBus _eventBus;

    public AuthTokenHandler(ISessionService sessionService, IEventBus eventBus)
    {
        _sessionService = sessionService;
        _eventBus = eventBus;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _sessionService.Current;
        if (session != null && !string.IsNullOrEmpty(session.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _sessionService.SignOut();
            _eventBus.Publish(new SessionExpiredMessage());
        }

        return response;
    }
}
