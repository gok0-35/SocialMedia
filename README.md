## Step 01 — Identity User + Domain Models

Bu adımda:

- Identity tabanlı `ApplicationUser` modeli oluşturduk
- Post / Comment / Tag / PostTag / PostLike / Follow domain modellerini tanımladık
- Ortak audit altyapısı kurduk / AuditableEntity

---

## Terminal adımları

repo kökünde:

```bash
git checkout main
git pull
git checkout -b step/01-models
dotnet add SocialMedia.Api package Microsoft.AspNetCore.Identity --version 8.0.10

#Kodla 
dotnet build
#Hata yoksa devam

git add .
git commit -m "step 01: add identity user and core domain models"

git checkout main
git merge --no-ff step/01-models -m "merge: step 01 models"

git push
git push -u origin step/01-models
