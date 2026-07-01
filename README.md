# FoodLedger 開發環境啟動指南

FoodLedger 是一個飲食紀錄與營養管理系統，後端以 ASP.NET Core Web API、Entity Framework Core、PostgreSQL、ASP.NET Core Identity 與 .NET Aspire 建立。

這份 README 主要說明如何在本機開啟專案、設定資料庫連線、套用 Migration、啟動 API 與確認 Swagger 是否可用。

## 專案內容

目前主要專案如下：

```text
FoodLedger/
├─ FoodLedger.slnx
├─ FoodLedger/
│  ├─ Controllers/
│  ├─ Data/
│  │  ├─ ApplicationDbContext.cs
│  │  ├─ Configurations/
│  │  └─ Entities/
│  ├─ Migrations/
│  ├─ Program.cs
│  └─ Dockerfile
├─ FoodLedger.AppHost/
│  └─ AppHost.cs
├─ FoodLedger.ServiceDefaults/
│  └─ Extensions.cs
└─ AGENTS.md
```

## 前置需求

開啟專案前，請先確認本機已安裝：

- .NET 10 SDK
- PostgreSQL
- Visual Studio、Rider 或 VS Code
- Git
- Docker Desktop，只有在需要測試 Dockerfile 或後續 Docker Compose 時才需要

可用以下指令確認 .NET SDK：

```powershell
dotnet --version
```

## 取得專案

```powershell
git clone https://github.com/zx32829601/FoodLedger_BackEnd_By.Net.git
cd FoodLedger_BackEnd_By.Net
```

如果已經有本機專案，只需要進入專案根目錄：

```powershell
cd C:\Users\zx328\OneDrive\桌面\SideProject\FoodLedger
```

## 開啟方案

### Visual Studio

1. 開啟 Visual Studio。
2. 選擇「Open a project or solution」。
3. 選取 `FoodLedger.slnx`。
4. 將啟動專案設為 `FoodLedger` 或 `FoodLedger.AppHost`。

### Rider

1. 開啟 Rider。
2. 選擇「Open」。
3. 選取專案根目錄或 `FoodLedger.slnx`。
4. 等待 NuGet restore 完成。

### VS Code

```powershell
code .
```

建議安裝 C# Dev Kit，並在 VS Code 中選擇 `FoodLedger.slnx` 作為方案。

## 還原套件與建置

第一次開啟專案後，先還原 NuGet 套件：

```powershell
dotnet restore .\FoodLedger.slnx
```

確認專案可正常建置：

```powershell
dotnet build .\FoodLedger.slnx
```

## 設定資料庫連線

專案預設讀取 `DefaultConnection`：

```json
"DefaultConnection": "Host=localhost;Database=Foodledger;Username=postgres;Password=YOUR_SECRET_PASSWORD"
```

不要把真實密碼寫進 Git。建議使用 User Secrets 設定本機連線字串：

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=Foodledger;Username=postgres;Password=你的本機密碼" --project .\FoodLedger\FoodLedger.csproj
```

確認 PostgreSQL 中已建立資料庫：

```sql
CREATE DATABASE "Foodledger";
```

## 套用 Migration

在資料庫連線設定完成後，執行 EF Core Migration：

```powershell
dotnet ef database update --project .\FoodLedger\FoodLedger.csproj --startup-project .\FoodLedger\FoodLedger.csproj
```

如果本機尚未安裝 `dotnet-ef` 工具，可先安裝：

```powershell
dotnet tool install --global dotnet-ef
```

若已安裝但版本過舊，可更新：

```powershell
dotnet tool update --global dotnet-ef
```

## 啟動 Web API

直接啟動 Web API：

```powershell
dotnet run --project .\FoodLedger\FoodLedger.csproj
```

Development 環境預設網址：

```text
http://localhost:5062
https://localhost:7041
```

Swagger UI：

```text
http://localhost:5062/swagger
https://localhost:7041/swagger
```

## 使用 Aspire AppHost 啟動

也可以透過 Aspire AppHost 啟動專案：

```powershell
dotnet run --project .\FoodLedger.AppHost\FoodLedger.AppHost.csproj
```

AppHost 會啟動 `FoodLedger` API，並提供 Aspire dashboard 觀察服務狀態、logs 與健康檢查。

## 身分驗證端點

目前專案已使用 ASP.NET Core Identity API endpoints：

```csharp
app.MapIdentityApi<ApplicationUser>();
```

啟動後可透過 Swagger 或 HTTP client 測試註冊、登入與 bearer token 流程。實際端點會依 ASP.NET Core Identity API endpoints 的預設路由產生。

## 常用檢查指令

檢查 NuGet 套件弱點：

```powershell
dotnet list .\FoodLedger.slnx package --vulnerable --include-transitive
```

檢查過期套件：

```powershell
dotnet list .\FoodLedger.slnx package --outdated --include-transitive
```

清除建置輸出：

```powershell
dotnet clean .\FoodLedger.slnx
```

## 目前開發狀態

已完成：

- ASP.NET Core Web API 專案骨架
- .NET Aspire AppHost 與 ServiceDefaults
- EF Core + PostgreSQL 設定
- ASP.NET Core Identity 初步導入
- 食物、分類、營養素、多語系翻譯與每日飲食紀錄資料模型
- EF Core Migration
- Swagger / OpenAPI 開發文件

尚未完成：

- Service 層商業邏輯
- Request / Response DTO
- 食物查詢 API
- 每日飲食紀錄 CRUD API
- 營養統計 API
- 獨立測試專案
- Docker Compose
- CI/CD
- Flutter App
- React Web 前台與後台

## 疑難排解

### 無法連線 PostgreSQL

請確認：

- PostgreSQL 服務已啟動。
- `Foodledger` 資料庫已建立。
- User Secrets 中的帳號、密碼與連線埠正確。
- 防火牆或本機安全軟體沒有阻擋 PostgreSQL port。

### 找不到 `dotnet ef`

請安裝或更新 EF Core CLI：

```powershell
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
```

### Swagger 無法開啟

請確認目前環境是 Development，並檢查 `Properties/launchSettings.json` 中的 `ASPNETCORE_ENVIRONMENT` 是否為 `Development`。

## 開發規範

詳細開發規範請閱讀 `AGENTS.md`。新增功能時請遵守：

- Controller 只處理 HTTP request / response、驗證與授權。
- 商業邏輯放在 Service 層。
- 資料存取透過 `ApplicationDbContext`。
- API response 不直接回傳 Entity。
- 需要登入的 API 必須加上 `[Authorize]`。
- Commit message 的標題可維持英文格式，commit body 與 PR 詳細內容需使用繁體中文。
