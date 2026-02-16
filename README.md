## Step 05 — Email Login + Email Confirmation + Password Recovery + Secret Management (.env)

Bu adımda:

- Login `username` yerine `email` ile çalışır
- Email onaylanmadan login engellenir
- Register sonrası email onay linki gönderilir
- `confirm-email` endpointi eklendi
- `resend-confirmation-email` endpointi eklendi
- `forgot-password` endpointi eklendi (mail ile reset token gönderir)
- `reset-password` endpointi eklendi
- SMTP mail altyapısı eklendi (`IEmailSender`, `SmtpEmailSender`, `EmailSettings`)
- Hassas yapılandırmalar (`ConnectionStrings`, `Jwt`, `Email`) tek noktada yönetim için `.env` dosyasında toplandı
- Uygulama başlangıcında `.env` yükleme eklendi (`DotEnvLoader`)
- Identity ayarları güncellendi:
  - `RequireUniqueEmail = true`
  - `RequireConfirmedEmail = true`
  - `AddDefaultTokenProviders()`

---

## 1) Branch oluştur

Repo kökünde:

```bash
git checkout main
git pull
git checkout -b step/05-email-auth-recovery
```

---

## 2) Çalıştırma

```bash
docker compose up -d
cd SocialMedia.Api
dotnet watch run --urls "http://localhost:5000"
```

---

## 3) Endpoint test akışı

Farklı bir terminal aç:

### 3.1 Register

```bash
curl -X POST "http://localhost:5000/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"userName":"LeBron","email":"lebron@example.com","password":"12345678"}'
```

Beklenen: kayıt başarılı mesajı + mail kutusuna onay maili.

### 3.2 Confirm email

Mailden gelen linki aç veya doğrudan:

```bash
curl "http://localhost:5000/api/auth/confirm-email?userId=USER_ID&token=TOKEN"
```

### 3.3 Login (email ile)

```bash
curl -X POST "http://localhost:5000/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"lebron@example.com","password":"12345678"}'
```

### 3.4 Forgot password

```bash
curl -X POST "http://localhost:5000/api/auth/forgot-password" \
  -H "Content-Type: application/json" \
  -d '{"email":"lebron@example.com"}'
```

Mailden gelen reset token'ı al.

### 3.5 Reset password

```bash
curl -X POST "http://localhost:5000/api/auth/reset-password" \
  -H "Content-Type: application/json" \
  -d '{"email":"lebron@example.com","token":"TOKEN","newPassword":"87654321"}'
```

### 3.6 Resend confirmation mail

```bash
curl -X POST "http://localhost:5000/api/auth/resend-confirmation-email" \
  -H "Content-Type: application/json" \
  -d '{"email":"lebron@example.com"}'
```

---

## 4) Swagger'da adım adım ne yapmalıyım?

1. Uygulamayı çalıştır:

```bash
docker compose up -d
cd SocialMedia.Api
dotnet watch run --urls "http://localhost:5000"
```

2. Swagger aç:

```text
http://localhost:5000/swagger
```

3. `POST /api/auth/register` çağır:

```json
{
  "userName": "LeBron",
  "email": "lebron@example.com",
  "password": "12345678"
}
```

4. Mail kutusuna gelen onay linkine tıkla (veya `GET /api/auth/confirm-email` endpointini `userId` + `token` ile çağır).

5. `POST /api/auth/login` çağır:

```json
{
  "email": "lebron@example.com",
  "password": "12345678"
}
```

6. Dönen `token` değerini kopyala, Swagger'da sağ üstte `Authorize` butonuna basıp aşağıdaki formatla gir:

```text
Bearer TOKEN_BURAYA
```

7. Token ile korunan endpoint'i test et:

```text
GET /api/me
```

8. Şifre unutma akışı için:
- `POST /api/auth/forgot-password` çağır
- Mailden gelen token ile `POST /api/auth/reset-password` çağır

Not: Email onaylamadan `login` denersen `401` dönmesi beklenir.

---

## 5) Commit

Repo kökünde:

```bash
git add .
git commit -m "step 05: email login + confirmation + forgot/reset password"
```
