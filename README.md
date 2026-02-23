## Step 10 — ProblemDetails + Correlation ID Middleware

Bu stepte global exception handling middleware eklendi.
Uygulama artık yakalanmamış hataları RFC 7807 uyumlu `ProblemDetails` formatında döndürüyor ve her isteğe bir `X-Correlation-ID` ekliyor.

## Bu stepte yapılanlar

- Yeni branch: `step/10-ProblemDetails-CorrelationId`
- Correlation ID middleware eklendi:
  - Request header'dan `X-Correlation-ID` okunuyor.
  - Header yoksa yeni bir ID üretiliyor.
  - Correlation ID response header'a yazılıyor.
  - `HttpContext.TraceIdentifier` ve `HttpContext.Items` içine set ediliyor.
- Global exception middleware eklendi:
  - Unhandled exception'lar tek noktada yakalanıyor.
  - Response `application/problem+json` olarak dönülüyor.
  - ProblemDetails içine `correlationId` extension alanı ekleniyor.
  - Development ortamında `detail` alanına exception mesajı yazılıyor.
- Middleware'ler `Program.cs` pipeline'ına eklendi.

## Yeni dosyalar

- `SocialMedia.Api/Infrastructure/Middleware/CorrelationIdMiddleware.cs`
- `SocialMedia.Api/Infrastructure/Middleware/HttpContextCorrelationIdExtensions.cs`
- `SocialMedia.Api/Infrastructure/Middleware/GlobalExceptionMiddleware.cs`

## Güncellenen dosyalar

- `SocialMedia.Api/Program.cs`
- `README.md`

## Örnek hata cevabı

```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
  "title": "Unexpected server error",
  "status": 500,
  "instance": "/api/posts",
  "correlationId": "a13f0d4c1c8a41c5b3d7b1c8f8f5e3e2"
}
```

## Çalıştırma

```bash
docker compose up -d
cd SocialMedia.Api
dotnet watch
```

## Build kontrolü

```bash
dotnet build SocialMedia.sln
```

## Commit

```bash
git add .
git commit -m "step/10-ProblemDetails-CorrelationId"
```
