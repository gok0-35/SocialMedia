## Step 04 — Authentication Pipeline + Config Guard

Bu adımda:

- `app.UseAuthentication()` middleware eklendi
- `app.UseAuthorization()` sırası authentication sonrası olacak şekilde düzeltildi
- JWT ayarları için `JwtSettings` sınıfı eklendi
- Startup sırasında config guard eklendi:
  - `ConnectionStrings:DefaultConnection` boş olamaz
  - `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` zorunlu
  - `Jwt:Key` en az 32 karakter olmalı
  - `Jwt:ExpiresMinutes` pozitif integer olmalı
- `AuthController`, `IConfiguration` yerine `JwtSettings` kullanacak şekilde güncellendi

---

## 1) Branch oluştur

Repo kökünde:

```bash
git checkout main
git pull
git checkout -b step/04-auth-middleware-config-guard
```

---

## 2) Kod değişiklikleri

### Eklenen dosya

```text
SocialMedia.Api/Infrastructure/Auth/JwtSettings.cs
```

### Güncellenen dosyalar

```text
SocialMedia.Api/Program.cs
SocialMedia.Api/Controllers/AuthController.cs
```

---

## 3) Build kontrolü

Repo kökünde:

```bash
dotnet build
```

Beklenen: `0 Error`

---

## 4) Uygulamayı çalıştır

```bash
docker compose up -d
cd SocialMedia.Api
dotnet watch run --urls "http://localhost:5000"
```

---

## 5) Terminalden endpoint testi

Farklı bir terminal aç:

### Register

```bash
curl -X POST "http://localhost:5000/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"userName":"LeBron","password":"12345678"}'
```

### Login

```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"userName":"LeBron","password":"12345678"}'
```

Login response içinden `token` değerini al.

### Protected endpoint (`/api/me`)

```bash
curl "http://localhost:5000/api/me" \
  -H "Authorization: Bearer TOKEN_BURAYA"
```

Beklenen:

- 200 -> token doğrulandı
- 401 -> token/header hatalı veya yok

---

## 6) Commit + merge

Repo kökünde:

```bash
git add .
git commit -m "step 04: add authentication middleware and config guard"

git checkout main
git merge --no-ff step/04-auth-middleware-config-guard -m "merge: step 04 auth middleware + config guard"

git push
git push -u origin step/04-auth-middleware-config-guard
```
