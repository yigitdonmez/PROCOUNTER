using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CalorieTracker.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddTransient<TokenRefreshHandler>(sp => 
		{
			return new TokenRefreshHandler("http://localhost:5119");
		});

		builder.Services.AddHttpClient("CalorieApi", client =>
		{
			client.BaseAddress = new Uri("http://localhost:5119");
			client.Timeout = TimeSpan.FromSeconds(30);
		})
		.ConfigurePrimaryHttpMessageHandler(() =>
		{
			var insecureHandler = new HttpClientHandler();
		#if DEBUG
			insecureHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
		#endif
			return insecureHandler;
		})
		.AddHttpMessageHandler<TokenRefreshHandler>();

		return builder.Build();
	}
}
