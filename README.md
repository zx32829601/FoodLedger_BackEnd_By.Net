# FoodLedger

FoodLedger 是一個飲食紀錄與營養管理系統，目標是讓使用者記錄每日飲食、查詢食物營養資訊，並統計每日營養攝取狀況。

目前專案仍在早期階段，已完成後端 Web API 骨架、PostgreSQL 資料模型、EF Core Migration，以及 .NET Aspire 啟動架構。前端、行動 App、正式 API 分層、測試與 CI/CD 尚未完成。

## 目前定位

這個專案未來會拆成三個主要使用介面：

- Flutter App：一般使用者日常記錄飲食、查詢食物、查看每日營養摘要。
- React Web 前台：提供網頁版飲食紀錄與查詢功能。
- React Web 後台：提供管理員維護食物、分類、營養素與基礎資料。

後端會作為共用 API，統一處理帳號、食物資料、每日飲食紀錄與營養統計。

## 技術棧

### 後端

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- ASP.NET Core Identity，使用 `IdentityUser<long>`
- PostgreSQL
- Npgsql Entity Framework Core Provider
- Swagger / OpenAPI
- .NET Aspire AppHost
- OpenTelemetry / Health Checks

### 規劃中的前端與工具

- Flutter
- React
- TypeScript
- Vite
- NUnit
- Docker / Docker Compose
- CI/CD，例如 GitHub Actions

## 專案結構

```text
FoodLedger/
├─ FoodLedger.slnx
├─ FoodLedger/
│  ├─ Controllers/
│  ├─ Data/
│  │  ├─ ApplicationDbContext.cs
│  │  └─ Entities/
│  ├─ DTOs/
│  ├─ Migrations/
│  ├─ Models/
│  ├─ Services/
│  ├─ Program.cs
│  └─ Dockerfile
├─ FoodLedger.AppHost/
│  └─ AppHost.cs
├─ FoodLedger.ServiceDefaults/
│  └─ Extensions.cs
└─ AGENTS.md
```

## 核心資料模型

目前已建立的主要 Entity：

- `UserAccount`：早期建立的使用者帳號資料；後續導入 ASP.NET Core Identity 時，需評估移除、改為 profile table，或與 `IdentityUser` 整合。
- `DailyRecord`：使用者每日飲食紀錄，包含食物、數量與食用時間。
- `SimpleFood`：食物主檔。
- `SimpleFoodTranslation`：食物多語系名稱與描述。
- `FoodCategory`：食物分類。
- `FoodCategoryTranslation`：分類多語系名稱。
- `SimpleFoodCategory`：食物與分類的關聯。
- `Nutrient`：營養素主檔。
- `NutrientTranslation`：營養素多語系名稱。
- `FoodNutrient`：食物與營養素含量關聯。

## 本機執行

### 前置需求

- .NET 10 SDK
- PostgreSQL
- Visual Studio 2026、Rider 或 VS Code
- Docker Desktop，若要測試容器化流程

### 資料庫連線

目前 `FoodLedger/appsettings.json` 內的 connection string 使用 placeholder 密碼：

```json
"DefaultConnection": "Host=localhost;Database=Foodledger;Username=postgres;Password=YOUR_SECRET_PASSWORD"
```

正式開發時應改用 User Secrets 或環境變數保存真實密碼，避免將敏感資訊 commit 到 Git。

### 執行 Web API

```bash
dotnet run --project FoodLedger/FoodLedger.csproj
```

預設 HTTP 位址：

```text
http://localhost:5062
```

Development 環境下可開啟 Swagger UI：

```text
http://localhost:5062/swagger
```

### 使用 Aspire AppHost 執行

```bash
dotnet run --project FoodLedger.AppHost/FoodLedger.AppHost.csproj
```

AppHost 會啟動 Aspire dashboard 與 `FoodLedger` API 專案。

## 目前狀態

目前已完成：

- 建立 `.NET 10` solution。
- 建立 ASP.NET Core Web API 專案。
- 建立 .NET Aspire AppHost 與 ServiceDefaults。
- 設定 EF Core + PostgreSQL。
- 初步導入 ASP.NET Core Identity 與 bearer token endpoint。
- 建立初版資料模型與 Migration。
- 產生 ASP.NET Core Identity migration。
- 建立食物、分類、營養素、多語系翻譯、使用者帳號與每日飲食紀錄資料表。
- 建立 Swagger / OpenAPI 開發環境。
- 建立測試資料庫連線用 API。

目前尚未完成：

- 正式 Service 層。
- Request / Response DTO。
- 食物查詢 API。
- 每日飲食紀錄 CRUD API。
- 營養統計 API。
- 套用最新 Migration 到開發資料庫。
- 登入、註冊與授權流程的整合測試。
- Flutter App。
- React Web 前台。
- React Web 後台。
- 獨立測試專案。
- Docker Compose。
- CI/CD 流程。

## 開發建議順序

建議先完成一條最小可用流程：

```text
登入或指定測試使用者
→ 搜尋食物
→ 新增今日飲食紀錄
→ 查詢今日飲食清單
→ 統計今日熱量與主要營養素
```

這條流程穩定後，再擴充後台管理、Flutter App、React Web、Docker 與 CI/CD。

## 專案規劃進度

| 階段 | 狀態 | 說明 |
| --- | --- | --- |
| 後端基礎專案 | 已完成 | Web API、Swagger、Aspire、ServiceDefaults 已建立 |
| 資料庫模型 | 已完成初版 | 已建立食物、分類、營養素、翻譯、使用者與每日紀錄模型 |
| Migration | 已完成初版 | 已產生初始 Migration |
| 帳號與授權設計 | 已開始 | 採用 ASP.NET Core Identity 與 `IdentityUser<long>`；下一步需套用 Migration 並補登入流程測試 |
| Service / DTO 分層 | 未開始 | 需建立商業邏輯與 API 輸入輸出模型 |
| 飲食紀錄 API | 未開始 | 建議優先實作每日紀錄 CRUD |
| 營養統計 API | 未開始 | 依每日紀錄計算熱量、蛋白質、脂肪、碳水等 |
| 單元測試 | 未開始 | 建議新增 `FoodLedger.Tests` |
| Docker Compose | 未開始 | 建議加入 API + PostgreSQL 本機編排 |
| Flutter App | 未開始 | 作為使用者主要行動端 |
| React Web 前台 | 未開始 | 作為使用者網頁端 |
| React Web 後台 | 未開始 | 作為資料維護與管理端 |
| CI/CD | 未開始 | 建議先加入 build 與 test，再加入 Docker image 與部署 |
