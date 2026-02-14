## Step 02 — Docker PostgreSQL + EF Core 8 + Identity Store + Initial Migration

Bu adımda:

- Docker ile PostgreSQL ve pgAdmin kurduk
- Environment (.env) sistemi oluşturduk
- EF Core 8.x + Npgsql + Identity EF Store ekledik
- AppDbContext yazdık
- Fluent API konfigürasyonlarını ekledik
- İlk migration’ı oluşturup DB’ye uyguladık

Bu adım sonunda:
> PostgreSQL container çalışır ve Identity + domain tabloları DB’de oluşur.

---

## Branch oluştur

```bash
git checkout main
git pull
git checkout -b step/02-db-efcore
```

---

## 1) Environment dosyaları(elle manuel olarakta oluşturabilirsin)

```bash
touch .env .env.example
```
> `.env` git’e girmez  
> `.env.example` git'e gider, sonra projeye bakınca anlayayım diye

---

## 2) Docker Compose

Repo kökünde(elle manuel olarakta oluşturabilirsin):

```bash
touch docker-compose.yml
```

Container başlat:

```bash
docker compose up -d
docker compose ps
```

Beklenen: postgres = healthy

---

## 3) EF Core 8 paketleri (versiyon sabit)

> NET 8 ile uyumlu major 8 kullanıyoruz.

```bash
dotnet add SocialMedia.Api package Microsoft.EntityFrameworkCore --version 8.0.10
dotnet add SocialMedia.Api package Microsoft.EntityFrameworkCore.Design --version 8.0.10
dotnet add SocialMedia.Api package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.10
dotnet add SocialMedia.Api package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.10
```
---

## 4) dotnet-ef tool (repo’ya sabitle)

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 8.0.10
dotnet tool restore
```
---

## 5) AppDbContext

Klasörler(elle manuel olarakta oluşturabilirsin):

```bash
mkdir -p SocialMedia.Api/Infrastructure/Persistence
mkdir -p SocialMedia.Api/Infrastructure/Persistence/Configurations
```

#Kodlama

---

## 6) Connection string

`appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dev;Username=yandanyemistwitter;Password=sallakazan"
  }
}
```

---

## 7) Program.cs DI

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddEntityFrameworkStores<AppDbContext>();
```

---

## 8) Migration + DB update

```bash
dotnet build
dotnet ef migrations add InitialCreate -p SocialMedia.Api -s SocialMedia.Api -o Infrastructure/Persistence/Migrations
dotnet ef database update -p SocialMedia.Api -s SocialMedia.Api
```

---

## 9) pgAdmin kontrol

Tarayıcı:

```
http://localhost:5050
```

Login:

- email = `.env`
- password = `.env`

Server ekle:

Host: `postgres`  
Port: `5432`  
DB/user/pass = `.env`

---

## 10) Commit + merge (Gitte olmaması gereken dosyaları mutlaka gitignore'a yaz)

```bash
git add .
git commit -m "step 02: postgres docker + ef core + initial migration"

git checkout main
git merge --no-ff step/02-db-efcore -m "merge: step 02 db setup"
git push
git push -u origin step/02-db-efcore
```