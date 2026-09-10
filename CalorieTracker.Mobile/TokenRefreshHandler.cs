using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CalorieTracker.Shared;

namespace CalorieTracker.Mobile;

public class TokenRefreshHandler : DelegatingHandler
{
    private readonly string _baseUrl;

    public TokenRefreshHandler(string baseUrl, HttpMessageHandler innerHandler) 
        : base(innerHandler)
    {
        _baseUrl = baseUrl;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Orijinal isteği gönder
        var response = await base.SendAsync(request, cancellationToken);

        // 2. Eğer token süresi dolmuşsa (401 Unauthorized dönerse)
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var oldToken = await SecureStorage.Default.GetAsync("auth_token");
            
            if (!string.IsNullOrEmpty(oldToken))
            {
                // Geçici bir client ile yenileme isteği at
                using var refreshClient = new HttpClient(new HttpClientHandler 
                { 
                    ServerCertificateCustomValidationCallback = (m, c, ch, e) => true // Geliştirme ortamı için SSL atlama
                });
                
                var refreshResponse = await refreshClient.PostAsJsonAsync($"{_baseUrl}/api/refresh-token", oldToken, cancellationToken);

                if (refreshResponse.IsSuccessStatusCode)
                {
                    var authResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDto>(cancellationToken: cancellationToken);
                    
                    if (authResult != null && !string.IsNullOrEmpty(authResult.Token))
                    {
                        // Yeni token'ı kaydet
                        await SecureStorage.Default.SetAsync("auth_token", authResult.Token);

                        // Orijinal başarısız olan isteğin başlığını yeni token ile değiştir ve TEKRAR DENE
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
            }
        }

        return response;
    }
}