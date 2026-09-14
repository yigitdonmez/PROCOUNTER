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
			return new TokenRefreshHandler("https://procounter.onrender.com");
		});

		builder.Services.AddHttpClient("CalorieApi", client =>
		{
			client.BaseAddress = new Uri("https://procounter.onrender.com");
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

		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
		{
		#if ANDROID
			handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
		#endif
		});

		return builder.Build();
	}
}
