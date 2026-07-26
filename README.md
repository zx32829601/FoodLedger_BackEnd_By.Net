# FoodLedger 後端專案

FoodLedger 是一套飲食紀錄與營養管理系統，目標是協助使用者記錄每日飲食、查詢食物營養資訊，並依據飲食紀錄統計每日營養攝取狀況。

目前專案處於後端資料模型與 API 骨架階段，已建立 ASP.NET Core Web API、Entity Framework Core、PostgreSQL、ASP.NET Core Identity、.NET Aspire AppHost、ServiceDefaults、Swagger / OpenAPI、Web API Dockerfile 與本機 Docker Compose 基礎環境。

## TODO List

### P0：帳號與核心架構

- [x] 導入 ASP.NET Core Identity，使用 `ApplicationUser : IdentityUser<long>` 作為使用者模型。
- [x] 不再使用早期自建帳號模型 `UserAccount`；既有 migration 已移除 legacy `user_account` 資料表，帳號、密碼、角色與授權統一交由 ASP.NET Core Identity 管理。
- [x] 建立 `ICurrentUserService`，讓 Service 層可取得目前登入使用者，不直接依賴 `HttpContext`。
- [x] 新增 `GET /api/users/me`，讓登入使用者可查詢目前 request 解析出的使用者資訊。
- [x] 設定 Swagger Bearer token 驗證輸入，方便在 Swagger UI 測試需要登入的 API。
- [x] 檢查 `DailyRecord.UserId` 與 `ApplicationUser.Id` 的關聯、索引與刪除行為，並以模型測試固定 FK、`Restrict` 與 `UserId + ConsumedAt` 複合索引。
- [x] 確認所有需要登入的 API 加上 `[Authorize]`，並讓開發診斷用 Controller 僅在 Development 環境註冊。
- [x] 建立管理員授權規範與測試，未來管理員 API 必須套用 Admin role 或 Admin policy。

### P1：後端分層與核心功能

- [ ] 建立 Service 層架構，避免 Controller 直接放商業邏輯。目前已建立 `DailyRecordService` 新增紀錄、依 UTC 日期查詢目前登入使用者紀錄切片，以及刪除目前登入使用者自己的紀錄，支援未登入拒絕、使用目前登入者建立紀錄、拒絕 0 或負數份量紀錄、拒絕無效食物識別碼、拒絕不存在的食物，以及拒絕未來時間紀錄。
- [ ] 建立 Request / Response DTO，API response 不直接暴露 Entity。
- [ ] 實作食物查詢 API，支援關鍵字、分類與語系查詢。
- [ ] 實作每日飲食紀錄 API，支援新增、查詢、修改與刪除自己的紀錄。目前已建立 `POST /api/daily-records` 第一個 Controller 邊界切片，將 Service 欄位範圍錯誤轉為 400 驗證回應、資源不存在錯誤轉為 404 回應，並將未授權錯誤轉為 401 回應；也已建立 `DELETE /api/daily-records/{recordId}` 成功刪除時回傳 204 No Content 的 Controller 邊界切片。
- [ ] 實作營養攝取統計 Service，依每日紀錄彙總熱量與各營養素攝取量。
- [ ] 補齊一致的錯誤回應格式，避免對外回傳內部 exception detail。

### P2：測試與資料品質

- [x] 新增獨立測試專案 `FoodLedger.Tests`，使用 NUnit 4.x。
- [x] 為 `ICurrentUserService` 與 `UsersController` 補上 NUnit 測試。
- [x] 為 `DailyRecord.UserId` 與 `ApplicationUser.Id` 的模型關聯、刪除行為與查詢索引補上 NUnit 測試。
- [x] 為 Controller 授權邊界與 Development-only Controller 註冊規則補上 NUnit 測試。
- [x] 為管理員授權角色、policy 與 `*AdminController` 防回歸規則補上 NUnit 測試。
- [x] 為 `DailyRecordService` 未登入新增飲食紀錄的拒絕行為補上第一個 Service 層測試。
- [x] 為 `DailyRecordService` 使用目前登入者建立飲食紀錄的成功路徑補上 Service 層測試。
- [x] 為 `DailyRecordService` 餐點份量為 0 或負數時拒絕新增並避免寫入資料庫補上 Service 層測試。
- [x] 為 `DailyRecordService` 指定食物不存在時拒絕新增並避免寫入資料庫補上 Service 層測試。
- [x] 為 `DailyRecordService` 用餐時間晚於目前 UTC 時拒絕新增並避免寫入資料庫補上 Service 層測試。
- [x] 為 `DailyRecordService` 用餐時間等於目前 UTC 時允許新增補上邊界測試。
- [x] 為 `DailyRecordService` 非 UTC offset 但實際時間點未晚於目前 UTC 時允許新增，並正規化為 UTC 儲存補上測試。
- [x] 為 `DailyRecordService` 非 UTC offset 但實際時間點晚於目前 UTC 時拒絕新增補上測試。
- [x] 為 `DailyRecordService` 餐點份量超過業務上限 `10000` 時拒絕新增並避免寫入資料庫補上 Service 層測試。
- [x] 為 `DailyRecordService` 餐點份量等於業務上限 `10000` 時允許新增補上 Service 層邊界測試。
- [x] 為 `DailyRecordService` 食物識別碼為 0 時拒絕新增並避免查詢不存在食物流程補上 Service 層測試。
- [x] 為 `DailyRecordService` 依 UTC 日期查詢目前登入使用者自己的飲食紀錄補上 Service 層測試。
- [x] 為 `DailyRecordService` 依 UTC 日期查詢時包含當日開始並排除隔日開始補上 Service 層邊界測試。
- [x] 為 `DailyRecordService` 依 UTC 日期查詢多筆飲食紀錄時依食用時間由早到晚排序補上 Service 層測試。
- [x] 為 `DailyRecordService` 依 UTC 日期查詢多筆同食用時間紀錄時依紀錄識別碼排序補上 Service 層測試。
- [x] 為 `DailyRecordService` 未登入查詢飲食紀錄時拒絕讀取私有資料補上 Service 層測試。
- [x] 為 `DailyRecordService` 刪除屬於目前登入使用者的飲食紀錄補上 Service 層測試。
- [x] 為 `DailyRecordService` 未登入刪除飲食紀錄時拒絕操作並保留資料補上 Service 層測試。
- [x] 為 `DailyRecordService` 嘗試刪除其他使用者飲食紀錄時以找不到資料語意拒絕並保留資料補上 Service 層測試。
- [x] 為 `DailyRecordService` 刪除不存在的飲食紀錄時以找不到資料語意拒絕並保留同使用者既有資料補上 Service 層測試。
- [x] 為 `DailyRecordsController` 查詢飲食紀錄成功時呼叫 Service 並回傳 200 OK 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 查詢飲食紀錄時處理 Service 未授權錯誤並回傳 401 Unauthorized 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 處理 Service 欄位範圍錯誤並回傳 400 ValidationProblem 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 成功新增時呼叫 Service 並回傳 204 No Content 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 處理 Service 資源不存在錯誤並回傳 404 Not Found 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 處理 Service 未授權錯誤並回傳 401 Unauthorized 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 成功刪除飲食紀錄時呼叫 Service 並回傳 204 No Content 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 刪除飲食紀錄時處理 Service 未授權錯誤並回傳 401 Unauthorized 補上 Controller 測試。
- [x] 為 `DailyRecordsController` 刪除飲食紀錄時處理 Service 資源不存在錯誤並回傳 404 Not Found 補上 Controller 測試。
- [x] 為 `POST /api/daily-records` 未登入 request 會被授權 middleware 擋下並回傳 401 Unauthorized 補上 API 整合測試。
- [x] 為 `GET /api/daily-records` 未登入 request 會被授權 middleware 擋下並回傳 401 Unauthorized 補上 API 整合測試。
- [x] 為 `DELETE /api/daily-records/{recordId}` 未登入 request 會被授權 middleware 擋下並回傳 401 Unauthorized 補上 API 整合測試。
- [x] 為 `DELETE /api/daily-records/{recordId}` 已驗證 request 會通過授權 middleware、呼叫 Service 並回傳 204 No Content 補上 API 整合測試。
- [x] 為 `DELETE /api/daily-records/{recordId}` Service 回報資源不存在時回傳 404 Not Found 補上 API 整合測試。
- [x] 為 `GET /api/daily-records` 已驗證 request 會通過授權 middleware、綁定查詢日期、呼叫 Service 並回傳 200 OK 補上 API 整合測試。
- [x] 為 `GET /api/daily-records` 成功 response 會包含可解析且代表同一 UTC 時間點的食用時間補上 API 整合測試。
- [x] 為 `GET /api/daily-records` 查詢成功但沒有資料時回傳 200 OK 與空陣列補上 API 整合測試。
- [x] 為 `GET /api/daily-records` 查詢日期格式無效時由 API model binding 回傳 400 並避免進入 Service 補上整合測試。
- [x] 為 `GET /api/daily-records` 缺少查詢日期時由 API model binding 回傳 400 並避免進入 Service 補上整合測試。
- [x] 為 `POST /api/daily-records` 已驗證 request 會通過授權 middleware、呼叫 Service 並回傳 204 No Content 補上 API 整合測試。
- [x] 為 `POST /api/daily-records` 食物識別碼為 0 時由 API model validation 回傳 400 並避免進入 Service 補上整合測試。
- [x] 為 `POST /api/daily-records` 食用數量為 0 時由 API model validation 回傳 400 並避免進入 Service 補上整合測試。
- [x] 為 `POST /api/daily-records` 食用數量為負數時由 API model validation 回傳 400 並避免進入 Service 補上整合測試。
- [x] 為 `POST /api/daily-records` 食用數量超過 API 可接受上限時由 model validation 回傳 400 並避免進入 Service 補上整合測試。
- [x] 為 `POST /api/daily-records` 食用數量等於業務最大合法值時可通過 API model validation 並呼叫 Service 補上整合測試。
- [x] 為 `POST /api/daily-records` 食用數量超過業務上限 `10000` 時由 API model validation 回傳 400 並避免進入 Service 補上整合測試。
- [x] 為 `POST /api/daily-records` 食用數量為 0 的 validation problem 回應包含 `Quantity` 欄位錯誤補上整合測試。
- [x] 為 `POST /api/daily-records` 食用數量為 0 的 `errors.Quantity` 回應為非空陣列補上整合測試。
- [ ] 為 Service 層補單元測試或接近整合測試的 EF Core InMemory 測試。
- [ ] 補食物查詢、分類篩選、營養素換算、每日飲食紀錄與使用者資料隔離測試。
- [ ] 補 API route、驗證與授權整合測試。
- [ ] 評估涉及 PostgreSQL 行為、Migration 或交易情境的 Testcontainers 測試。
- [ ] 逐步達到最低測試覆蓋率 70%。

### P3：部署、前端與維運

- [x] 建立 Docker Compose，編排 Web API 與 PostgreSQL 本機環境。
- [ ] 建立 CI/CD 流程，包含 restore、build、test、映像檔產生與部署。目前 GitHub Actions 已包含 restore、build、test、`docker compose config --quiet` 與 Docker image build 驗證；正式 image push 與部署仍待補。
- [ ] 規劃 React + TypeScript + Vite Web 前台。
- [ ] 規劃 React + TypeScript + Vite 管理後台。
- [ ] 規劃 Flutter App。
- [ ] 補充基礎資料匯入流程，例如食物、分類、營養素與多語系翻譯資料。

## 專案結構

```text
FoodLedger_BackEnd_By.Net/
├─ FoodLedger.slnx
├─ FoodLedger/
│  ├─ Controllers/
│  │  ├─ DailyRecordsController.cs
│  │  └─ UsersController.cs
│  ├─ Data/
│  │  ├─ ApplicationDbContext.cs
│  │  ├─ Configurations/
│  │  └─ Entities/
│  ├─ DTOs/
│  │  ├─ DailyRecords/
│  │  └─ Users/
│  ├─ Migrations/
│  ├─ Models/
│  ├─ Infrastructure/
│  │  └─ Mvc/
│  ├─ Security/
│  ├─ Services/
│  │  ├─ CurrentUserService.cs
│  │  ├─ DailyRecordService.cs
│  │  ├─ IDailyRecordService.cs
│  │  └─ ICurrentUserService.cs
│  ├─ Program.cs
│  └─ Dockerfile
├─ FoodLedger.AppHost/
│  └─ AppHost.cs
├─ FoodLedger.ServiceDefaults/
│  └─ Extensions.cs
├─ FoodLedger.Tests/
│  ├─ Controllers/
│  ├─ Data/
│  └─ Services/
├─ docker-compose.yml
├─ .env.example
├─ .dockerignore
├─ AGENTS.md
└─ README.md
```

## 規格文件

- [自訂 Auth API 與統一 API 錯誤回應規格](docs/specs/auth-and-api-error-response.md)
- [通用 DefinedCode 代碼架構規格](docs/specs/defined-codes.md)
- [主要 DailyRecord 飲食紀錄功能規格](docs/specs/daily-records.md)
- [Nutrition Summary 營養素統計規格](docs/specs/nutrition-summary.md)

## UML 圖表

- [主要使用者流程 UML Activity Diagram](docs/diagrams/main-user-flow.puml)
- [目前系統架構圖](docs/diagrams/system-architecture.puml)
- [CI/CD 流程圖](docs/diagrams/cicd-flow.puml)

## 技術棧

- .NET 10
- ASP.NET Core Web API
- ASP.NET Core Identity
- Entity Framework Core
- PostgreSQL
- Npgsql Entity Framework Core Provider
- Swagger / OpenAPI
- .NET Aspire AppHost
- .NET Aspire ServiceDefaults、OpenTelemetry、Health Checks
- Dockerfile
- Docker Compose

## 本機需求

- .NET 10 SDK
- PostgreSQL
- Git
- Visual Studio、Rider 或 VS Code
- Docker Desktop，若需要執行容器化環境

確認 .NET SDK：

```powershell
dotnet --version
```

## 還原與建置

```powershell
dotnet restore .\FoodLedger.slnx
dotnet build .\FoodLedger.slnx
```

## 資料庫連線

請使用 User Secrets、環境變數或部署環境設定提供連線字串，不要把真實密碼寫入 Git。

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=Foodledger;Username=postgres;Password=YOUR_SECRET_PASSWORD" --project .\FoodLedger\FoodLedger.csproj
```

建立本機 PostgreSQL 資料庫：

```sql
CREATE DATABASE "Foodledger";
```

套用 EF Core Migration：

```powershell
dotnet ef database update --project .\FoodLedger\FoodLedger.csproj --startup-project .\FoodLedger\FoodLedger.csproj
```

若尚未安裝 `dotnet-ef`：

```powershell
dotnet tool install --global dotnet-ef
```

## 啟動 Web API

```powershell
dotnet run --project .\FoodLedger\FoodLedger.csproj
```

Development 環境預設網址：

```text
http://localhost:5062
https://localhost:7041
```

Swagger UI 僅應在 Development 環境啟用：

```text
http://localhost:5062/swagger
https://localhost:7041/swagger
```

## 使用 Docker Compose 啟動本機環境

請先依照 `.env.example` 建立本機 `.env`，並調整只屬於本機的資料庫密碼；`.env` 已被 Git 忽略，不應提交。

```powershell
Copy-Item .\.env.example .\.env
```

驗證 Docker Compose 設定：

```powershell
docker compose config --quiet
```

建置 Web API image：

```powershell
docker build -f .\FoodLedger\Dockerfile -t foodledger-api:test .
```

啟動 Web API 與 PostgreSQL：

```powershell
docker compose up --build
```

預設 HTTP 入口由 `.env` 的 `FOODLEDGER_API_HTTP_PORT` 控制，範例值為 `5062`。若本機已經有 PostgreSQL 佔用 `5432`，可調整 `.env` 的 `POSTGRES_HOST_PORT`：

```text
http://localhost:5062/swagger
```

### 本機部署腳本

本機部署腳本會檢查 Docker CLI、Docker daemon、`.env` 與 `docker-compose.yml`，再以 detached mode 啟動 Web API 與 PostgreSQL。

```powershell
.\scripts\deploy-local.ps1
```

若只想重啟既有 image，不重新 build：

```powershell
.\scripts\deploy-local.ps1 -SkipBuild
```

停止本機容器：

```powershell
docker compose down
```

## Jenkins 本機 Pipeline

本機 Jenkins 可使用根目錄 `Jenkinsfile` 執行 CI 驗證，並在偵測到 Git 更新後自動觸發本機 Docker Compose 部署。

Jenkins 執行環境需可使用：

```powershell
git --version
dotnet --version
docker --version
docker compose version
```

若 Jenkins 以 Windows service 執行，服務帳號的 PATH 可能不同於目前登入使用者。`Jenkinsfile` 會透過 `DOCKER_CLI_BIN` 參數補入 Docker Desktop CLI 路徑；若仍出現 `docker : The term 'docker' is not recognized`，請確認 Docker Desktop 已安裝，並讓 Jenkins service 帳號可存取 `C:\Program Files\Docker\Docker\resources\bin` 或使用者安裝路徑 `C:\Users\<UserName>\AppData\Local\Programs\DockerDesktop\resources\bin`。調整 Jenkins service 環境變數後需要重新啟動 Jenkins。

Jenkins 與本機部署腳本會透過 `scripts\Invoke-DockerCompose.ps1` 執行 Compose 指令，優先使用 `docker-compose.exe`，若找不到才改用 `docker compose`。這可避免 Windows Jenkins service 找得到 `docker.exe` 但無法載入 Compose plugin 時，出現 `docker: unknown command: docker compose`。

Local Deploy stage 只負責執行 detached 模式的 Docker Compose 部署，啟動完成後 Jenkins job 應結束，後端監聽會交由 Docker container 持續執行。若 Local Deploy 超過 10 分鐘未結束，Jenkins 會將該次建置視為失敗，避免 pipeline 無限等待。

建立 Pipeline Job 時建議設定：

```text
Pipeline: Pipeline script from SCM
SCM: Git
Repository URL: https://github.com/zx32829601/FoodLedger_BackEnd_By.Net
Credentials: 使用 Jenkins UI 建立的 GitHub PAT credential
Branch: */chore/add-docker-cicd-flow
Script Path: Jenkinsfile
```

`Jenkinsfile` 會使用 `pollSCM('H/2 * * * *')` 讓 Jenkins 約每 2 分鐘檢查一次 Git 是否有新 commit。當偵測到目標分支更新時，Jenkins 會自動執行 restore、build、test、Compose 設定驗證與 Local Deploy。

Compose 驗證階段會使用 Jenkinsfile 內的 CI-only 範例環境變數，不會使用正式密碼，也不會提交 `.env`。

`RUN_LOCAL_DEPLOY` 預設為：

```text
RUN_LOCAL_DEPLOY=true
```

若只想執行 CI 驗證、不重新部署 Docker container，可在手動 Build with Parameters 時改成 `RUN_LOCAL_DEPLOY=false`。

`DOCKER_CLI_BIN` 預設為本機 Docker Desktop 使用者安裝路徑：

```text
C:\Users\zx328\AppData\Local\Programs\DockerDesktop\resources\bin
```

若 Docker Desktop 安裝在其他位置，請在 Build with Parameters 時改成實際包含 `docker.exe` 的資料夾。

執行 Local Deploy 前，Jenkins workspace 需要有 `.env`。`.env` 不應提交到 Git，請在 Jenkins 執行機器上依 `.env.example` 建立並調整本機值；Local Deploy stage 會交由 `scripts/deploy-local.ps1` 讀取 `.env`。

## 啟動 Aspire AppHost

```powershell
dotnet run --project .\FoodLedger.AppHost\FoodLedger.AppHost.csproj
```

AppHost 會啟動 `FoodLedger` API，並提供 Aspire dashboard 觀察 logs、health checks 與服務狀態。

## 身分驗證

專案使用 ASP.NET Core Identity：

```csharp
app.MapIdentityApi<ApplicationUser>();
```

帳號、密碼雜湊、登入驗證、角色儲存與授權流程統一交由 ASP.NET Core Identity 管理。早期自建帳號資料表 `user_account` 已在既有 migration 中移除，後續不再作為帳號來源。

Service 層若需要取得目前登入使用者，應透過 `ICurrentUserService` 取得 `UserId`、`UserName` 與登入狀態，不直接依賴 `HttpContext`。

Service 層若需要目前時間，應透過 .NET 內建 `TimeProvider` 取得 UTC 時間，讓時間相關商業規則可被固定時間測試。`DailyRecord` 代表已實際攝取的飲食紀錄，因此 `ConsumedAt` 可以等於目前 UTC 時間，但不可晚於目前 UTC 時間；API 可接受非 UTC offset 的 `DateTimeOffset`，Service 判斷時以實際時間點比較，寫入時統一正規化為 UTC。預先規劃可能會吃的餐點應另以 `PlannedMeal` 或 `MealPlan` 功能建模。

目前提供登入者資訊 API：

```http
GET /api/users/me
Authorization: Bearer {accessToken}
```

Swagger UI 已設定 Bearer token 驗證輸入。登入後可點選 Swagger 右上角 `Authorize`，貼上 `accessToken` 後測試需要登入的 API。

開發診斷用 Controller 需標示 `DevelopmentOnlyControllerAttribute`。Production 或其他非 Development 環境會透過 MVC feature provider 排除這類 Controller，避免本機測試端點成為正式 API 攻擊面。

管理員授權統一使用 `ApplicationRoles.Admin` 與 `AuthorizationPolicyNames.AdminOnly`。未來新增 `*AdminController` 時，必須套用 Admin role 或 Admin policy，避免管理功能只登入即可操作。

## 架構原則

- Controller 只負責 HTTP request / response、驗證與授權。
- 商業邏輯放在 Service 層。
- 資料存取透過 `ApplicationDbContext`。
- 資料庫 I/O 優先使用 EF Core async API。
- API response 使用 DTO / Response model，不直接暴露 Entity。
- 需要登入的 API 必須加上 `[Authorize]`。
- 管理員 API 必須加上 Admin role 或 Admin policy。
- 使用者只能操作自己的 `DailyRecord`，不得信任前端傳入的 `UserId`。
- 時間欄位統一使用 UTC。

## 常用檢查指令

檢查套件弱點：

```powershell
dotnet list .\FoodLedger.slnx package --vulnerable --include-transitive
```

檢查套件更新：

```powershell
dotnet list .\FoodLedger.slnx package --outdated --include-transitive
```

清理建置輸出：

```powershell
dotnet clean .\FoodLedger.slnx
```
