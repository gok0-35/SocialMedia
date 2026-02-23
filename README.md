## Step 08 — Service Layer Refactor

Bu stepte controller içindeki iş kuralları servis katmanına taşındı.
Controller'lar artık ağırlıklı olarak HTTP/route/auth ve response mapping yapıyor.

## Bu stepte yapılanlar

- Yeni branch: `step/08-service-layer`
- Service altyapısı eklendi:
  - `ServiceResult<T>`
  - `ServiceError` + `ServiceErrorType`
  - `ServiceErrorExtensions` (`ServiceError -> IActionResult` mapping)
- Servis arayüzleri eklendi:
  - `IPostService`
  - `ICommentService`
  - `IFollowService`
  - `IUserService`
  - `ITagService`
- Servis implementasyonları eklendi:
  - `PostService`
  - `CommentService`
  - `FollowService`
  - `UserService`
  - `TagService`
- `Program.cs` içine DI kayıtları eklendi.
- `Posts/Comments/Follows/Users/Tags` controllerları servis kullanacak şekilde sadeleştirildi.

## Yeni dosyalar

- `SocialMedia.Api/Application/Services/ServiceResult.cs`
- `SocialMedia.Api/Application/Services/ServiceErrorExtensions.cs`
- `SocialMedia.Api/Application/Services/Abstractions/IPostService.cs`
- `SocialMedia.Api/Application/Services/Abstractions/ICommentService.cs`
- `SocialMedia.Api/Application/Services/Abstractions/IFollowService.cs`
- `SocialMedia.Api/Application/Services/Abstractions/IUserService.cs`
- `SocialMedia.Api/Application/Services/Abstractions/ITagService.cs`
- `SocialMedia.Api/Application/Services/PostService.cs`
- `SocialMedia.Api/Application/Services/CommentService.cs`
- `SocialMedia.Api/Application/Services/FollowService.cs`
- `SocialMedia.Api/Application/Services/UserService.cs`
- `SocialMedia.Api/Application/Services/TagService.cs`

## Güncellenen dosyalar

- `SocialMedia.Api/Program.cs`
- `SocialMedia.Api/Controllers/PostsController.cs`
- `SocialMedia.Api/Controllers/CommentsController.cs`
- `SocialMedia.Api/Controllers/FollowsController.cs`
- `SocialMedia.Api/Controllers/UsersController.cs`
- `SocialMedia.Api/Controllers/TagsController.cs`

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
git commit -m "step 08: add service layer and refactor social controllers"
```
