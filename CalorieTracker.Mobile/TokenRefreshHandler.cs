using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CalorieTracker.Shared;

namespace CalorieTracker.Mobile;

public class TokenRefreshHandler : DelegatingHandler
{
    private readonly string _baseUrl;

    public TokenRefreshHandler(string baseUrl)
    {
        _baseUrl = baseUrl;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var oldToken = await SecureStorage.Default.GetAsync("auth_token");
            
            if (!string.IsNullOrEmpty(oldToken))
            {
                using var refreshClient = new HttpClient(new HttpClientHandler 
                { 
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true
                });
                
                var refreshResponse = await refreshClient.PostAsJsonAsync($"{_baseUrl}/api/refresh-token", oldToken, cancellationToken);

                if (refreshResponse.IsSuccessStatusCode)
                {
                    var authResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken);
                    
                    if (authResult != null && !string.IsNullOrEmpty(authResult.Token))
                    {
                        await SecureStorage.Default.SetAsync("auth_token", authResult.Token);

                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
            }
        }

        return response;
    }
}