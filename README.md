## Step 00 — Solution + WebAPI + Git Setup

Bu adımda:

- Solution oluşturduk
- Controller tabanlı Web API kurduk
- Git repo başlattık
- README + gitignore ekledik
- Step branch sistemi kurduk
- Main branch her adımda güncellenecek

---

## Terminal adımları

```bash
mkdir SocialMedia
cd SocialMedia

git init

dotnet new sln -n SocialMedia
dotnet new webapi --use-controllers -o SocialMedia.Api
dotnet sln SocialMedia.sln add SocialMedia.Api/SocialMedia.Api.csproj

cd SocialMedia.Api
dotnet build
dotnet watch
# Swagger açıldıysa devam
cd ..

dotnet new gitignore
touch README.md
# README doldur

git checkout -b step/00-sln-webapi
git add .
git commit -m "step 00: create solution and webapi"

git branch main
git checkout main
git merge step/00-sln-webapi

git remote add origin https://github.com/gok0-35/SocialMedia.git
git push -u origin main
git push -u origin step/00-sln-webapi