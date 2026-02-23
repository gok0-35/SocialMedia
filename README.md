## Step 09 — Repository Layer Refactor

Bu stepte servislerin doğrudan `DbContext` bağımlılığı kaldırıldı.
Veri erişimi repository katmanına taşındı ve servisler artık repository arayüzleri üzerinden çalışıyor.

## Bu stepte yapılanlar

- Yeni branch: `step/09-Repository-Layer-Refactor`
- Repository arayüzleri eklendi:
  - `IPostRepository`
  - `ICommentRepository`
  - `IFollowRepository`
  - `IUserRepository`
  - `ITagRepository`
- EF Core repository implementasyonları eklendi:
  - `PostRepository`
  - `CommentRepository`
  - `FollowRepository`
  - `UserRepository`
  - `TagRepository`
- `Post/Comment/Follow/User/Tag` servisleri repository kullanacak şekilde refactor edildi.
- `Program.cs` içine repository DI kayıtları eklendi.

## Yeni dosyalar

- `SocialMedia.Api/Application/Repositories/Abstractions/IPostRepository.cs`
- `SocialMedia.Api/Application/Repositories/Abstractions/ICommentRepository.cs`
- `SocialMedia.Api/Application/Repositories/Abstractions/IFollowRepository.cs`
- `SocialMedia.Api/Application/Repositories/Abstractions/IUserRepository.cs`
- `SocialMedia.Api/Application/Repositories/Abstractions/ITagRepository.cs`
- `SocialMedia.Api/Infrastructure/Persistence/Repositories/PostRepository.cs`
- `SocialMedia.Api/Infrastructure/Persistence/Repositories/CommentRepository.cs`
- `SocialMedia.Api/Infrastructure/Persistence/Repositories/FollowRepository.cs`
- `SocialMedia.Api/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `SocialMedia.Api/Infrastructure/Persistence/Repositories/TagRepository.cs`

## Güncellenen dosyalar

- `SocialMedia.Api/Application/Services/PostService.cs`
- `SocialMedia.Api/Application/Services/CommentService.cs`
- `SocialMedia.Api/Application/Services/FollowService.cs`
- `SocialMedia.Api/Application/Services/UserService.cs`
- `SocialMedia.Api/Application/Services/TagService.cs`
- `SocialMedia.Api/Program.cs`
- `README.md`

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
git commit -m "step/09-Repository-Layer-Refactor"
```
