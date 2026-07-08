FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/ClaimsModule.Domain/ClaimsModule.Domain.csproj src/ClaimsModule.Domain/
COPY src/ClaimsModule.Application/ClaimsModule.Application.csproj src/ClaimsModule.Application/
COPY src/ClaimsModule.Infrastructure/ClaimsModule.Infrastructure.csproj src/ClaimsModule.Infrastructure/
COPY src/ClaimsModule.Persistence/ClaimsModule.Persistence.csproj src/ClaimsModule.Persistence/
COPY src/ClaimsModule.API/ClaimsModule.API.csproj src/ClaimsModule.API/
# Restore the API project (and its transitive project refs) rather than the whole solution -
# the test project is intentionally not part of the runtime image build.
RUN dotnet restore src/ClaimsModule.API/ClaimsModule.API.csproj

COPY src/ ./src/
RUN dotnet publish src/ClaimsModule.API/ClaimsModule.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClaimsModule.API.dll"]
