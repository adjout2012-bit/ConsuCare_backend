# ConsuCare Backend

ASP.NET Core Web API and SignalR backend for ConsuCare. This repository includes a copy of `ConsuCare.Shared` so it builds independently of the frontend repository.

## Run locally

Configure `Jwt:Key` in .NET User Secrets or the `Jwt__Key` environment variable before starting the API:

```powershell
$bytes = New-Object byte[] 64
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Key" $key --project src\ConsuCare.Api\ConsuCare.Api.csproj
Remove-Variable key, bytes
$rng.Dispose()
dotnet run --project src\ConsuCare.Api\ConsuCare.Api.csproj
```

The API uses an in-memory database if `ConnectionStrings:DefaultConnection` is unset. For PostgreSQL, store the connection string in User Secrets or set `ConnectionStrings__DefaultConnection`; never commit credentials.

Allow the frontend origin with `Cors__AllowedOrigins__0` (for example, `https://your-frontend.vercel.app`). The frontend's `ApiBaseUrl` must point to this API.

## Build and tests

```powershell
dotnet build ConsuCare_backend.sln --configuration Release
dotnet test ConsuCare_backend.sln
```

EF Core migrations are under `src\ConsuCare.Api\Migrations`. Configure the target database securely before applying migrations.

## Shared contracts

`src\ConsuCare.Shared` in this backend repository is the canonical contract source. After changing a DTO or model, run this from the backend repository root to update the frontend and original combined repository:

```powershell
.\scripts\Sync-SharedContracts.ps1
```

To check for drift without changing files:

```powershell
.\scripts\Sync-SharedContracts.ps1 -Check
```

Commit and push the updated shared-contract files in each affected repository. GitHub Actions checks the copies for drift after pushes to `main`.
