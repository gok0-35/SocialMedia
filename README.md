## Step 03 — JWT Authentication (Register / Login)

Bu adımda:

- JWT authentication altyapısı kurduk
- Identity + SignInManager ile login sistemi ekledik
- Register / Login endpoint yazıldı
- JWT token üretimi eklendi
- Protected endpoint ile auth test edildi (`/api/me`)
- Artık kullanıcılar token ile doğrulanıyor

---

## Terminal adımları

repo kökünde:

```bash
git checkout main
git pull
git checkout -b step/03-jwt-auth

dotnet add SocialMedia.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.10

dotnet build
# Hata yoksa devam
```

---

## JWT config

`SocialMedia.Api/appsettings.Development.json`

```json
"Jwt": {
  "Issuer": "SocialMedia.Api",
  "Audience": "SocialMedia.Client",
  "Key": "En az 32 karakter rastgele döşe",
  "ExpiresMinutes": 60
}
```
---

## Eklenen dosyalar

```
Controllers/AuthController.cs
Controllers/MeController.cs
```

### AuthController

- POST `/api/auth/register`
- POST `/api/auth/login`

Kullanıcı oluşturur ve JWT token döner.

### MeController

- GET `/api/me`
- `[Authorize]` ile korunur
- JWT çalışıyor mu test etmek için vardır

---

## Çalıştırma

repo kökünde:

```bash
docker compose up -d
cd SocialMedia.Api
dotnet watch
```
---

## Test akışı

### Register

POST `/api/auth/register`

```json
{
  "userName": "LeBron",
  "password": "12345678"
}
```

---

### Login

POST `/api/auth/login`

aynı body

---

### Protected endpoint test

Header:

```
Authorization: Bearer (Verilen Token) 
```

GET:

```
/api/me
```

- 200 → JWT çalışıyor
- 401 → header gitmiyor

---

## Terminal adımları 

Repo kökünde:

```bash
git add .
git commit -m "step 03: add jwt authentication (register/login)"

git checkout main
git merge --no-ff step/03-jwt-auth -m "merge: step 03 jwt auth"

git push
git push -u origin step/03-jwt-auth
```