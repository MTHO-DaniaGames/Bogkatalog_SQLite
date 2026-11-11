FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY NuGet.Config ./NuGet.Config
COPY src/Bogkatalog.Api/ ./Bogkatalog.Api/
RUN dotnet publish Bogkatalog.Api/Bogkatalog.Api.csproj \
    -c Release -o /publish --configfile NuGet.Config

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /publish/ ./
# Et nyt volume overtager mappens ejer. API'et skal kunne skrive som app-brugeren.
RUN mkdir -p /app/data && chown "$APP_UID:$APP_UID" /app/data
USER $APP_UID
ENTRYPOINT ["dotnet", "Bogkatalog.Api.dll"]
