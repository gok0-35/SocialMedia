## Step 07 — DTO Refactor

Bu stepte controller içindeki `Request/Response` sınıfları dışarı taşındı ve
`ReadDto / WriteDto` ayrımıyla tek bir DTO katmanında toplandı.
Amaç: controller dosyalarını sadeleştirmek ve API sözleşmesini merkezi dosyalardan yönetmek.

## Bu stepte yapılanlar

- Yeni branch: `step/07-dto-refactor`
- DTO klasörü eklendi: `SocialMedia.Api/Application/Dtos`
- Controller içindeki tüm nested DTO sınıfları kaldırıldı.
- DTO adları standardize edildi:
  - Girdi modelleri: `*WriteDto`
  - Çıktı modelleri: `*ReadDto`
- Anonim response dönen yerler typed DTO ile güncellendi (`MessageReadDto`, `FollowListReadDto`, `PostLikesReadDto`, vb.)
- Build doğrulaması yapıldı (`dotnet build` başarılı)

## Eklenen DTO dosyaları

- `SocialMedia.Api/Application/Dtos/Auth/AuthReadDtos.cs`
- `SocialMedia.Api/Application/Dtos/Auth/AuthWriteDtos.cs`
- `SocialMedia.Api/Application/Dtos/Comments/CommentReadDtos.cs`
- `SocialMedia.Api/Application/Dtos/Comments/CommentWriteDtos.cs`
- `SocialMedia.Api/Application/Dtos/Common/MessageReadDto.cs`
- `SocialMedia.Api/Application/Dtos/Follows/FollowReadDtos.cs`
- `SocialMedia.Api/Application/Dtos/Me/MeReadDto.cs`
- `SocialMedia.Api/Application/Dtos/Posts/PostReadDtos.cs`
- `SocialMedia.Api/Application/Dtos/Posts/PostWriteDtos.cs`
- `SocialMedia.Api/Application/Dtos/Tags/TagReadDtos.cs`
- `SocialMedia.Api/Application/Dtos/Users/UserReadDtos.cs`
- `SocialMedia.Api/Application/Dtos/Users/UserWriteDtos.cs`

## Güncellenen controllerlar

- `SocialMedia.Api/Controllers/AuthController.cs`
- `SocialMedia.Api/Controllers/PostsController.cs`
- `SocialMedia.Api/Controllers/CommentsController.cs`
- `SocialMedia.Api/Controllers/FollowsController.cs`
- `SocialMedia.Api/Controllers/UsersController.cs`
- `SocialMedia.Api/Controllers/TagsController.cs`
- `SocialMedia.Api/Controllers/MeController.cs`

## Çalıştırma

```bash
docker compose up -d
cd SocialMedia.Api
dotnet watch run --urls "http://localhost:5000"
```

## Build kontrolü

```bash
dotnet build SocialMedia.sln
```

## Commit

```bash
git add .
git commit -m "step 07: split read/write dtos and refactor controllers"
```
