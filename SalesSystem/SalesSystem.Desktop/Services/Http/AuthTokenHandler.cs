using SalesSystem.Desktop.Messages;
using SalesSystem.Desktop.Services.Interfaces;
using System.Net.Http.Headers;

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
        if (_sessionService.IsAuthenticated)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _sessionService.Current!.Token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _sessionService.SignOut();
            _eventBus.Publish(new SessionExpiredMessage());
        }

        return response;
    }
}
