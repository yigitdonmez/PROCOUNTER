FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["CalorieTracker.Api/CalorieTracker.Api.csproj", "CalorieTracker.Api/"]
COPY ["CalorieTracker.Shared/CalorieTracker.Shared.csproj", "CalorieTracker.Shared/"]
RUN dotnet restore "CalorieTracker.Api/CalorieTracker.Api.csproj"

COPY . .
WORKDIR "/src/CalorieTracker.Api"
RUN dotnet publish "CalorieTracker.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CalorieTracker.Api.dll"]