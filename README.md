## Step 06 — Social Controllers (Entity Coverage)

Bu adımda servis/repo katmanı olmadan, doğrudan controller + `AppDbContext` ile entity’lerde tanımlı sosyal akışlar genişletildi.

Eklenen/güncellenen controllerlar:

- `PostsController`
  - `POST /api/posts`
  - `POST /api/posts/{postId}/replies`
  - `PATCH /api/posts/{postId}`
  - `GET /api/posts`
  - `GET /api/posts/feed`
  - `GET /api/posts/{postId}`
  - `GET /api/posts/{postId}/replies`
  - `DELETE /api/posts/{postId}`
  - `POST /api/posts/{postId}/like`
  - `DELETE /api/posts/{postId}/like`
  - `GET /api/posts/{postId}/likes`
- `CommentsController`
  - `GET /api/posts/{postId}/comments`
  - `POST /api/posts/{postId}/comments`
  - `GET /api/comments/{commentId}`
  - `GET /api/comments/{commentId}/children`
  - `PATCH /api/comments/{commentId}`
  - `DELETE /api/comments/{commentId}`
- `FollowsController`
  - `POST /api/follows/{followingUserId}`
  - `DELETE /api/follows/{followingUserId}`
  - `GET /api/follows/{userId}/followers`
  - `GET /api/follows/{userId}/following`
- `UsersController`
  - `GET /api/users/{userId}`
  - `GET /api/users/me`
  - `PATCH /api/users/me` (`Bio`, `AvatarUrl`)
  - `GET /api/users/{userId}/posts`
  - `GET /api/users/{userId}/comments`
  - `GET /api/users/{userId}/liked-posts`
- `TagsController`
  - `GET /api/tags`
  - `GET /api/tags/trending`
  - `GET /api/tags/{tagName}/posts`

Notlar:

- Yazma/güncelleme/silme endpointleri JWT ister (`[Authorize]`).
- `POST/PATCH /api/posts` içinde `tags` gönderildiğinde `Tag` + `PostTag` ilişkisi yönetilir.
- Pagination kullanılan endpointlerde `skip >= 0`, `take = 1..100`.

---

## 1) Branch oluştur

Repo kökünde:

```bash
git checkout main
git pull
git checkout -b step/06-social-controllers
```

---

## 2) Çalıştırma

```bash
docker compose up -d
cd SocialMedia.Api
dotnet watch run --urls "http://localhost:5000"
```

---

## 3) Benim adım adım test sıram (curl)

Bu kısmı ben pratikte şu sırayla çalıştırıyorum.  
Ön koşul: En az 2 kullanıcı register + confirm edilmiş olsun.

### 3.1 Ortam değişkenleri

```bash
BASE_URL="http://localhost:5000"
```

### 3.2 User A login (post atacak hesap)

```bash
curl -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"lebron@example.com","password":"12345678"}'
```

Bu cevaptan:

- `token` -> `A_TOKEN`
- `userId` -> `A_USER_ID`

```bash
A_TOKEN="BURAYA_A_TOKEN"
A_USER_ID="BURAYA_A_USER_ID"
```

### 3.3 User B login (etkileşim yapacak hesap)

```bash
curl -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"kobe@example.com","password":"12345678"}'
```

Bu cevaptan:

- `token` -> `B_TOKEN`
- `userId` -> `B_USER_ID`

```bash
B_TOKEN="BURAYA_B_TOKEN"
B_USER_ID="BURAYA_B_USER_ID"
```

### 3.4 A kendi profilini günceller (`Bio`, `AvatarUrl`)

```bash
curl -X PATCH "$BASE_URL/api/users/me" \
  -H "Authorization: Bearer $A_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"bio":"NBA fan","avatarUrl":"https://example.com/lebron.jpg"}'
```

Beklenen: `200` + `"Profil güncellendi."`

### 3.5 A tag’li post atar

```bash
curl -X POST "$BASE_URL/api/posts" \
  -H "Authorization: Bearer $A_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"text":"Step06 ilk post","tags":["dotnet","webapi","socialmedia"]}'
```

Bu cevaptan `postId` değerini al:

```bash
POST_ID="BURAYA_POST_ID"
```

### 3.6 B, A’yı follow eder ve feed kontrol edilir

```bash
curl -X POST "$BASE_URL/api/follows/$A_USER_ID" \
  -H "Authorization: Bearer $B_TOKEN"
```

```bash
curl "$BASE_URL/api/posts/feed?skip=0&take=20" \
  -H "Authorization: Bearer $B_TOKEN"
```

Beklenen: B feed’inde A’nın postu görünür.

### 3.7 B postu beğenir + yorum yapar

```bash
curl -X POST "$BASE_URL/api/posts/$POST_ID/like" \
  -H "Authorization: Bearer $B_TOKEN"
```

```bash
curl -X POST "$BASE_URL/api/posts/$POST_ID/comments" \
  -H "Authorization: Bearer $B_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"body":"Eline saglik kral"}'
```

Yorum cevabından `commentId` al:

```bash
COMMENT_ID="BURAYA_COMMENT_ID"
```

### 3.8 B yorumu günceller, A yorumları ve like listesini görür

```bash
curl -X PATCH "$BASE_URL/api/comments/$COMMENT_ID" \
  -H "Authorization: Bearer $B_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"body":"Eline sağlık kral - edit"}'
```

```bash
curl "$BASE_URL/api/posts/$POST_ID/comments?skip=0&take=20"
curl "$BASE_URL/api/posts/$POST_ID/likes?skip=0&take=20"
```

### 3.9 A postu update eder + reply atar

```bash
curl -X PATCH "$BASE_URL/api/posts/$POST_ID" \
  -H "Authorization: Bearer $A_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"text":"Step06 ilk post (edit)","tags":["dotnet","api"]}'
```

```bash
curl -X POST "$BASE_URL/api/posts/$POST_ID/replies" \
  -H "Authorization: Bearer $A_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"text":"Bu post bir reply"}'
```

```bash
curl "$BASE_URL/api/posts/$POST_ID/replies?skip=0&take=20"
```

### 3.10 Tag ve user endpointleri hızlı kontrol

```bash
curl "$BASE_URL/api/tags?skip=0&take=20"
curl "$BASE_URL/api/tags/trending?take=10&days=7"
curl "$BASE_URL/api/tags/dotnet/posts?skip=0&take=20"

curl "$BASE_URL/api/users/$A_USER_ID"
curl "$BASE_URL/api/users/$A_USER_ID/posts?skip=0&take=20"
curl "$BASE_URL/api/users/$B_USER_ID/comments?skip=0&take=20"
curl "$BASE_URL/api/users/$B_USER_ID/liked-posts?skip=0&take=20"
```

### 3.11 Temizlik testi (silme akışı)

```bash
curl -X DELETE "$BASE_URL/api/comments/$COMMENT_ID" \
  -H "Authorization: Bearer $B_TOKEN"

curl -X DELETE "$BASE_URL/api/posts/$POST_ID/like" \
  -H "Authorization: Bearer $B_TOKEN"

curl -X DELETE "$BASE_URL/api/follows/$A_USER_ID" \
  -H "Authorization: Bearer $B_TOKEN"

curl -X DELETE "$BASE_URL/api/posts/$POST_ID" \
  -H "Authorization: Bearer $A_TOKEN"
```

Beklenen: tamamı `200` dönmeli.

---

## 4) Swagger adımları

1. Uygulamayı çalıştır.
2. `http://localhost:5000/swagger` aç.
3. `POST /api/auth/login` ile token al.
4. Sağ üst `Authorize` alanına `Bearer TOKEN_BURAYA` gir.
5. Sırasıyla test et:

- `PATCH /api/users/me`
- `POST /api/posts`
- `PATCH /api/posts/{postId}`
- `POST /api/posts/{postId}/replies`
- `POST /api/posts/{postId}/comments`
- `PATCH /api/comments/{commentId}`
- `POST /api/posts/{postId}/like`
- `POST /api/follows/{followingUserId}`
- `GET /api/posts/feed`
- `GET /api/tags/trending`

---

## 5) Commit (merge öncesi)

Repo kökünde:

```bash
git add .
git commit -m "step 06: expand social controllers with users, tags, update endpoints"
```
