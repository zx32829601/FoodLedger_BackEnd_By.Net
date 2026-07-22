# 個人身分
2 年經驗 .NET 軟體開發工程師，具備 OOP 與系統分析基礎。

--- project-doc ---

# 專案概述
FoodLedger 是一個飲食紀錄與營養管理系統，目標是協助使用者記錄每日飲食、查詢食物營養資訊，並依據飲食紀錄統計每日營養攝取狀況。

目前專案處於後端資料模型與 API 骨架階段。現有程式碼已建立 ASP.NET Core Web API、EF Core、PostgreSQL、.NET Aspire 啟動架構，以及食物、分類、營養素、多語系翻譯、使用者帳號與每日飲食紀錄等資料表模型。

未來規劃包含：
- Flutter App：提供一般使用者記錄飲食、查詢食物與查看營養統計。
- React Web 前台：提供使用者網頁版飲食紀錄與查詢功能。
- React Web 後台：提供管理員維護食物、分類、營養素與基礎資料。
- Docker / Docker Compose：建立可重現的本機與部署環境。
- CI/CD：加入建置、測試、映像檔產生與部署流程。

## 技術棧使用

### 目前已使用
- .NET: 10.x
- ASP.NET Core Web API: 10.x
- ASP.NET Core Identity：使用 `IdentityUser<long>` 作為使用者模型，讓使用者主鍵可與 `DailyRecord.UserId` 對齊
- Entity Framework Core: 10.x
- Database: PostgreSQL
- PostgreSQL Provider: `Npgsql.EntityFrameworkCore.PostgreSQL`
- API 文件: Swagger / OpenAPI，使用 `Microsoft.AspNetCore.OpenApi` 與 `Swashbuckle.AspNetCore`
- Application Host: .NET Aspire AppHost
- Observability / Service Defaults: .NET Aspire ServiceDefaults、OpenTelemetry、Health Checks
- Container: 已有 Web API Dockerfile

### 規劃使用
- Test Framework: NUnit 4.x
- Test Project: 獨立建立 `FoodLedger.Tests`
- Test Database: Service 層可使用 EF Core InMemory；涉及 PostgreSQL 行為、關聯查詢或 Migration 的測試，優先考慮整合測試或 Testcontainers
- Mobile App: Flutter
- Web Frontend: React + TypeScript + Vite
- CI/CD: GitHub Actions 或其他等價流程
- Local Orchestration: Docker Compose

### 身分驗證與授權決策
- 已決定採用 ASP.NET Core Identity 實作註冊、登入、角色與授權流程。
- 目前既有的 `UserAccount` entity 屬於早期自建帳號模型；導入 Identity 時需評估移除、改為使用者 profile table，或改成繼承 / 關聯 `IdentityUser` 的擴充模型。
- 不得自行實作密碼雜湊、密碼驗證、角色儲存或 token 安全細節。

## 禁止使用的套件
目前沒有特別禁用的套件。

新增套件前請先確認是否符合既有技術棧，並避免引入與 ASP.NET Core、EF Core、PostgreSQL、Identity、Flutter、React 或既有前端工具高度重疊的套件。

## 專案結構

### 目前存在
- `/FoodLedger.slnx` - .NET solution 檔案
- `/FoodLedger` - ASP.NET Core Web API 主專案
- `/FoodLedger/Program.cs` - Web API 啟動設定、Swagger、DbContext、Controller 註冊
- `/FoodLedger/Controllers` - API Controller，目前包含範本與測試 DB 連線用 Controller
- `/FoodLedger/Data/ApplicationDbContext.cs` - EF Core DbContext 與資料表關聯設定
- `/FoodLedger/Data/Entities` - EF Core Entity
- `/FoodLedger/Migrations` - EF Core Migration
- `/FoodLedger/DTOs` - 規劃放置 API Request / Response DTO，目前尚未實作
- `/FoodLedger/Models` - 規劃放置非 Entity 的 domain / view model，目前尚未實作
- `/FoodLedger/Services` - 規劃放置商業邏輯 Service，目前尚未實作
- `/FoodLedger.AppHost` - .NET Aspire AppHost 專案
- `/FoodLedger.ServiceDefaults` - Aspire 共用服務設定、健康檢查、OpenTelemetry、Service Discovery

### 規劃新增
- `/FoodLedger.Tests` - NUnit 測試專案
- `/FoodLedger.Tests/Services` - Service 層測試
- `/FoodLedger.Tests/Controllers` - API 行為或整合測試
- `/foodledger-app` - Flutter App，實際命名可依建立時調整
- `/foodledger-web` - React + TypeScript + Vite Web 前台 / 後台，實際命名可依建立時調整
- `/docker-compose.yml` - 本機 API + PostgreSQL 等服務編排

# 程式碼風格

## 命名慣例
- C# 檔案名稱：PascalCase，原則上與主要類別名稱一致（例：`FoodService.cs`、`DailyRecordService.cs`）。
- C# 類別名稱：PascalCase（例：`FoodsController`、`DailyRecordService`）。
- C# 介面名稱：以 `I` 開頭並使用 PascalCase（例：`IFoodService`、`IDailyRecordService`）。
- C# 方法名稱：PascalCase（例：`GetFoodsAsync`、`CreateDailyRecordAsync`）。
- C# 非同步方法：方法名稱以 `Async` 結尾，回傳 `Task` 或 `Task<T>`。
- C# 私有欄位：使用 `_camelCase`（例：`_foodService`、`_dbContext`）。
- C# 區域變數與參數：camelCase（例：`foodId`、`currentUserId`）。
- C# 常數：PascalCase（例：`DefaultPageSize`）。
- Controller：類別名稱以 `Controller` 結尾（例：`FoodsController`、`DailyRecordsController`）。
- Service：類別名稱以 `Service` 結尾（例：`FoodService`、`NutritionSummaryService`）。
- DTO：依用途使用 `Request`、`Response`、`Dto` 結尾（例：`CreateDailyRecordRequest`、`FoodSearchResponse`）。
- Entity：使用單數名詞（例：`SimpleFood`、`DailyRecord`、`Nutrient`）。
- DbSet：使用複數名詞（例：`SimpleFoods`、`DailyRecords`、`Nutrients`）。
- TypeScript / React 檔案名稱：依前端既有 Vite / React 慣例；Component 使用 PascalCase，工具函式可使用 camelCase 或 kebab-case，但同一資料夾內需保持一致。
- Dart / Flutter 檔案名稱：優先使用 lower_snake_case；Widget / class 使用 PascalCase；變數與方法使用 lowerCamelCase。

## C# / ASP.NET Core 要求
- 啟用 nullable reference types，避免使用不必要的 nullable。
- Controller 只負責 HTTP request / response、驗證與授權，不放商業邏輯。
- 商業邏輯放在 Service 層，資料存取透過 `ApplicationDbContext`。
- 不要在 Service 直接依賴 `HttpContext`；若需要目前登入者，應透過可測試的抽象，例如 `ICurrentUserService`。
- 所有資料庫 I/O 優先使用 EF Core async API，例如 `ToListAsync`、`FirstOrDefaultAsync`、`SaveChangesAsync`。
- 查詢資料時優先使用 DTO / Response model 回傳，不直接把 Entity 暴露給 API response。
- Request model 使用 Data Annotations 做基本驗證，例如 `[Required]`、`[MaxLength]`、`[Range]`。
- 不自行實作密碼雜湊、登入驗證或角色儲存。帳號、登入與角色授權流程統一以 ASP.NET Core Identity 為主要方案。
- 時間欄位統一使用 UTC，資料庫預設值使用 PostgreSQL `CURRENT_TIMESTAMP` 或應用程式端 `DateTimeOffset.UtcNow`。
- Swagger UI 僅應在 Development 環境啟用。

## 抽象與重構原則
- 不要為了抽象而抽象；只有在能降低重複、降低複雜度、隔離明確責任或提升可讀性時才抽方法、helper 或類別。
- 若原始語法本身已經清楚，例如 `string.IsNullOrWhiteSpace(value)`，且只使用一次，優先保留直接寫法。
- 新增 helper 前先確認呼叫端是否因此更容易理解；若只是讓讀者多跳一層，應避免抽出。
- Refactor 階段可以嘗試整理，但如果整理後沒有讓程式更清楚，應收回變更並保留較簡單的寫法。
- 共用 helper 應在多處重複、測試資料建立明顯冗長，或有穩定業務語意時再抽出。

## TypeScript / React 要求
- 避免使用 `any`，除非有明確原因並加上說明。
- API response / request 應定義明確型別。
- React Component 使用 PascalCase。
- Hook 命名以 `use` 開頭（例：`useDailyRecords`）。
- 優先使用 async/await，避免不必要的 `.then()` 鏈。
- 前端狀態與 API 呼叫邏輯應與 UI component 適度分離，避免單一 component 過大。

## Flutter / Dart 要求
- Widget 應依功能拆分，避免單一畫面檔案過大。
- API client、資料模型、狀態管理與 UI 元件應適度分離。
- DTO / API response 應建立明確型別，不直接在畫面中處理鬆散 JSON。
- 飲食紀錄、營養統計與登入狀態應有清楚的狀態管理邊界。

## 錯誤處理
- Controller 應回傳合適的 HTTP status code，例如 `400 BadRequest`、`401 Unauthorized`、`403 Forbid`、`404 NotFound`。
- 驗證錯誤優先回傳 `ValidationProblem` 或一致的錯誤格式。
- Service 不應吞掉例外；能處理才處理，不能處理就讓上層統一處理。
- 錯誤訊息需包含足夠上下文，但不得洩漏密碼、連線字串、token 或個資。
- 對外 API 不直接回傳內部 exception detail。

## 測試風格
- 測試類別名稱以被測目標命名（例：`FoodServiceTests`、`DailyRecordServiceTests`）。
- 測試方法名稱描述被測方法、測試狀態與預期結果（例：`CreateDailyRecordAsync_WhenFoodDoesNotExist_ReturnsNotFound`）。
- 測試內容使用 Arrange / Act / Assert 結構。
- Service 測試可使用 EF Core InMemory；若測試依賴 PostgreSQL 特性、Migration、複雜關聯或交易行為，應使用更接近真實資料庫的整合測試。
- 測試應聚焦單一行為，避免一個測試同時驗證過多規則。

# 測試規範

## 測試涵蓋要求
- 優先採用 TDD 方式開發：先寫失敗測試，再實作最小功能讓測試通過，最後重構。
- 所有新功能都應有對應測試。
- Service 層商業邏輯必須有單元測試或接近整合測試的資料庫測試。
- API 路由與授權行為應補整合測試。
- 食物查詢、分類篩選、營養素換算、每日飲食紀錄、使用者資料隔離與營養統計流程必須有測試。
- 工具函數與共用 helper 必須有單元測試。
- 最低測試覆蓋率目標：70%。
- 效能測試只針對核心查詢、大量資料情境或明確高風險流程，不要求每個測試都包含效能測試。

## 測試框架
- NUnit 4.x。
- Microsoft.NET.Test.Sdk。
- NUnit3TestAdapter。
- EF Core InMemory 可用於 Service 層資料邏輯測試。
- API 整合測試可使用 `WebApplicationFactory`。
- PostgreSQL 整合測試可評估 Testcontainers。
- 測試檔案命名：`*Tests.cs`。
- 測試類別命名：`<TargetClassName>Tests`（例：`FoodServiceTests`）。

## 測試原則
- 每個測試只驗證一件事。
- 使用描述性的測試名稱，建議格式：`MethodName_StateUnderTest_ExpectedBehavior`。
- 測試要能獨立執行，不依賴其他測試的執行順序。
- 每個測試應建立自己的測試資料，避免共用可變狀態。
- 測試資料庫名稱應保持唯一，避免 EF Core InMemory 測試互相污染。
- 測試應包含正常流程、邊界條件與失敗情境。
- 測試不應連線到正式資料庫或依賴真實外部服務。

# 安全規範

## 絕對禁止
- 在程式碼、設定檔或 Git commit 中寫死 API key、密碼、token、真實 connection string。
- 使用 `eval()` 或類似方式執行動態程式碼。
- 使用字串拼接直接組 SQL 查詢，避免 SQL Injection 風險。
- 未驗證的使用者輸入不得直接寫入資料庫或用於查詢條件。
- 不得將 `.env`、User Secrets、私鑰、憑證、資料庫備份檔 commit 到 Git。
- 不得自行實作密碼雜湊、密碼驗證或 token 安全細節。

## 必須遵守
- 敏感資訊必須從 User Secrets、環境變數或安全的部署環境設定讀取。
- 資料庫操作優先使用 EF Core LINQ 查詢或參數化查詢。
- 所有 API request model 都要做輸入驗證，例如 Data Annotations 或自訂 validation。
- 需要登入的 API 必須加上 `[Authorize]`。
- 管理員 API 必須加上 role-based authorization，例如 `[Authorize(Roles = "Admin")]`。
- 使用者只能操作自己的 `DailyRecord`，不得信任前端傳入的 UserId。
- Controller / Service 不得回傳密碼、token、connection string 或內部 exception detail。
- 開發用 Swagger UI 僅應在 Development 環境啟用。

# 文件註解指令

---
description: Add documentation to code
argument-hint: file path, class name, method name, API action name
---

請為指定的檔案、類別、方法或 API action 加上完整註解，包含：

1. 函數或類別用途說明。
2. 參數說明，包含型別、預期值與必要限制。
3. 回傳值說明。
4. 公開 API 或 Controller action 的使用範例。
5. 注意事項，例如授權需求、狀態限制、資料驗證規則或可能的例外情境。

## C# 註解規則
- 公開類別、介面、DTO、Service 方法與 Controller action 使用 XML documentation comments。
- 使用 `/// <summary>` 說明用途。
- 使用 `/// <param name="...">` 說明參數。
- 使用 `/// <returns>` 說明回傳值。
- 需要補充限制或注意事項時，使用 `/// <remarks>`。
- 公開 API 可視需要使用 `/// <example>` 補充 request / response 範例。
- 不要為顯而易見的私有欄位或簡單 getter / setter 加上冗餘註解。
- 複雜商業規則可在程式區塊前加簡短註解，但不得用註解取代清楚的命名。

## TypeScript / React 註解規則
- 匯出的 component、hook、API client function 或共用 helper 可使用 JSDoc。
- 說明 props、參數、回傳資料與副作用。
- 不要為簡單 JSX 或明顯變數加上冗餘註解。

## Dart / Flutter 註解規則
- 匯出的 model、service、repository、state notifier 或複雜 widget 可使用 Dart doc comment。
- 註解應說明用途、參數限制、狀態副作用與錯誤情境。
- 不要為簡單 UI 排版或明顯變數加上冗餘註解。

## 限制
- 不可以修改程式碼邏輯。
- 不可以修改 method signature、route、DTO 欄位、資料庫 schema 或測試期待結果。
- 不可以為了加註解而重新排版大量無關程式碼。
- 註解內容必須符合目前專案使用的 ASP.NET Core、EF Core、PostgreSQL、NUnit、Flutter 與 React 慣例。
- 若現有註解風格與本規範衝突，優先遵循同一檔案內既有風格。

# 註解與魔法值規範
- 程式碼註解、XML documentation comments、JSDoc、Dart doc comment 與測試註解一律使用繁體中文。
- 公開類別、介面、DTO、Service 方法與 Controller action 應使用 XML documentation comments。
- 註解必須說明設計意圖、參數限制、回傳值或特殊行為，不要重複描述程式碼表面語法。
- 魔法數字與魔法字串應優先抽成具名常數，並用中文註解說明該固定值的用途。
- 若魔法值是業務規則，常數名稱需表達業務意義；若魔法值只用於測試，註解需明確標示為測試用固定值。
- 不得為了加註解改變程式邏輯、方法簽章、route、DTO 欄位、資料庫 schema 或測試期待結果。

# Git Branch & Commit Naming Conventions

## Branch Naming Rule
Format: `<type>/<description>`

All branch names must be lowercase, using hyphens to separate words.

Allowed types:
- `feat`: New features
- `fix`: Bug fixes
- `refactor`: Refactoring code
- `docs`: Documentation updates
- `chore`: Maintenance tasks

Example: `feat/add-daily-record`

## Commit Message Rule
建議使用簡潔明確的 commit message，格式如下：

`<type>: <summary>`

Commit message 的標題維持 `<type>: <summary>` 格式即可，summary 可使用英文。

Commit body 使用繁體中文撰寫，內容保持精簡並以條列式描述即可。原則上說明：
- 新增或調整了哪些功能、類別、方法或設定。
- 新增或調整了哪些測試。
- 已執行的驗證指令；若未執行，需簡短說明原因。

Pull Request 的詳細內容也需使用繁體中文撰寫，讓團隊能直接理解變更目的、影響範圍與驗證結果。

Examples:
- `docs: update project overview`
- `feat: add daily record api`
- `test: add food service tests`
