FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CalorieTracker.Api.csproj", "./"]
RUN dotnet restore "CalorieTracker.Api.csproj"
COPY . .
RUN dotnet publish "CalorieTracker.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CalorieTracker.Api.dll"]