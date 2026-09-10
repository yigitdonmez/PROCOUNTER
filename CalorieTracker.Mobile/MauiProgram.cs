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
			var insecureHandler = new HttpClientHandler();
		#if DEBUG
			insecureHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
		#endif
			return new TokenRefreshHandler("https://10.0.2.2:5119", insecureHandler);
		});

		builder.Services.AddHttpClient("CalorieApi", client =>
		{
			client.BaseAddress = new Uri("https://10.0.2.2:5119");
			client.Timeout = TimeSpan.FromSeconds(30);
		})
		.AddHttpMessageHandler<TokenRefreshHandler>();

		builder.Services.AddTransient<MainPage>();

		return builder.Build();
	}
}
