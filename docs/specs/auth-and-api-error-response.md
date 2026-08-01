# 自訂 Auth API 與統一 API 錯誤回應規格

## 問題說明

FoodLedger 後端目前已導入 ASP.NET Core Identity，也已建立目前登入使用者抽象、DailyRecord Service、授權規則與測試。不過目前註冊登入仍主要依賴 Identity 內建端點，尚未完全符合前端與產品需求。

目前需要補齊兩個共同規格：

1. API 錯誤回應格式尚未統一，前端難以穩定判斷錯誤類型、欄位錯誤與多語系文案。
2. 註冊登入 API 尚未支援 FoodLedger 自己的帳號欄位與回應格式，例如 `UserAccount`、`DisplayName`、token 與使用者基本資料。

## 解決方案

建立 FoodLedger 自訂 Auth API 與統一錯誤回應規格。

正式前端只串接：

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/users/me`

後端仍使用 ASP.NET Core Identity 處理密碼雜湊、使用者儲存、密碼驗證與 token 安全細節，不自行實作底層安全機制。

API 錯誤格式採用 code-first 設計，`code` 是前後端真正契約，`message` 只作為 fallback。未來前端可依 `code` 做多語系轉換。

## 使用者故事

1. 作為新使用者，我想用帳號、顯示名稱、Email 與密碼註冊，讓我可以建立 FoodLedger 帳號。
2. 作為新使用者，我想註冊成功後直接取得 token，讓我可以立即開始使用 App。
3. 作為使用者，我想用帳號與密碼登入，讓我不用只能依賴 Email。
4. 作為使用者，我想用 Email 與密碼登入，讓我忘記帳號時仍可登入。
5. 作為前端工程師，我想登入與註冊回應使用固定 DTO，讓前端不用依賴 Identity 內建端點格式。
6. 作為前端工程師，我想登入與註冊成功時同時取得使用者基本資料，讓畫面可以立即更新登入狀態。
7. 作為使用者，我想顯示名稱與登入帳號分開，讓我可以自由設定公開顯示名稱。
8. 作為使用者，我想顯示名稱可使用中文、英文、數字、空白與常見符號，讓名稱顯示更自然。
9. 作為使用者，我希望帳號與 Email 都是唯一的，讓登入識別不會混淆。
10. 作為前端工程師，我希望註冊失敗時可知道帳號或 Email 哪個欄位重複，讓我能顯示正確表單錯誤。
11. 作為重視安全的使用者，我希望登入失敗時不要透露帳號或 Email 是否存在，降低帳號枚舉風險。
12. 作為前端工程師，我希望 API 錯誤包含穩定的錯誤代碼，讓前端能對應多語系文案。
13. 作為前端工程師，我希望驗證錯誤包含欄位層級錯誤，讓表單能精準顯示錯誤。
14. 作為後端工程師，我希望錯誤回應包含 traceId，讓我能對應 log 追查問題。
15. 作為後端工程師，我希望 Service 不直接回 HTTP response，讓商業邏輯不綁死 API 傳輸層。
16. 作為後端工程師，我希望 `/api/users/me` 回傳與登入註冊相同的使用者 DTO，讓使用者資料契約一致。

## 實作決策

- 正式前端使用自訂 Auth API，不使用 Identity 內建 `/register`、`/login` 作為正式契約。
- 註冊 API：`POST /api/auth/register`
- 登入 API：`POST /api/auth/login`
- 目前使用者 API：`GET /api/users/me`
- 註冊 request 包含：
  - `userAccount`
  - `displayName`
  - `email`
  - `password`
- 登入 request 包含：
  - `loginId`
  - `password`
- `loginId` 支援 UserAccount 與 Email。
- `loginId` 包含 `@` 時視為 Email 登入。
- `loginId` 不包含 `@` 時視為 UserAccount 登入。
- `UserAccount` 對應 Identity 的 `ApplicationUser.UserName`。
- 不另外新增 `ApplicationUser.UserAccount`。
- 不重新命名繼承自 Identity 的 `ApplicationUser.UserName`。
- `DisplayName` 是使用者對外顯示名稱。
- `DisplayName` 可重複。
- `UserAccount` 必須唯一。
- `Email` 必須唯一。
- `UserAccount` 規則：
  - 長度 4 到 30。
  - 允許英文字母、數字、底線 `_`、連字號 `-`。
  - 不允許 `@`。
  - 不允許空白。
  - 唯一性不分大小寫。
- `DisplayName` 規則：
  - 長度 1 到 30。
  - 允許中文、英文、數字、空白與常見符號。
  - 前後空白會 trim。
  - 不允許空字串或全空白。
  - 不參與登入。
  - 不需要唯一。
- Email 規則：
  - 必填。
  - 必須符合 Email 格式。
  - 必須唯一。
  - 註冊成功後不要求立即完成 Email 驗證。
  - `EmailConfirmed` 初期可維持 `false`。
- Password 規則沿用目前 Identity 設定：
  - 最小長度 8。
  - 必須包含數字。
  - 必須包含小寫英文。
  - 必須包含大寫英文。
  - 不強制特殊符號。
- 註冊成功回傳 token 與使用者基本資料。
- 登入成功回傳 token 與使用者基本資料。
- Auth response 使用自訂 DTO：
  - `accessToken`
  - `refreshToken`
  - `expiresIn`
  - `user`
- 使用者基本資料 DTO 包含：
  - `userId`
  - `userAccount`
  - `displayName`
  - `email`
- `/api/users/me` 回傳相同的使用者基本資料 DTO。
- response 可保留 `refreshToken`，完整 refresh 流程可在下一階段實作。
- 不回傳 password hash、security stamp 或 Identity 內部安全欄位。
- token 產生與驗證仍依賴 ASP.NET Core Identity / bearer token 基礎能力。

## API 錯誤回應規格

一般錯誤格式：

```json
{
  "code": "DailyRecord.NotFound",
  "message": "找不到指定的飲食紀錄。",
  "traceId": "...",
  "parameters": {
    "recordId": "123"
  }
}
```

欄位驗證錯誤格式：

```json
{
  "code": "Validation.Failed",
  "message": "請確認輸入資料是否正確。",
  "traceId": "...",
  "errors": {
    "quantityInGrams": [
      {
        "code": "DailyRecord.QuantityMustBeGreaterThanZero",
        "message": "數量必須大於 0。",
        "parameters": {
          "min": 0
        }
      }
    ]
  }
}
```

錯誤規則：

- `code` 是前後端真正契約。
- `message` 只是 fallback，不是前端最終多語系來源。
- `parameters` 保留給未來多語系插值。
- `traceId` 用於後端 log 追查。
- validation error 使用欄位層級錯誤集合。
- 錯誤 code 命名格式為 `<Domain>.<Reason>`。
- 命名使用 PascalCase 並以 dot 分段。
- 範例：
  - `Auth.InvalidCredentials`
  - `Auth.UserAccountAlreadyExists`
  - `Auth.EmailAlreadyExists`
  - `Validation.Failed`
  - `DailyRecord.NotFound`
  - `System.UnexpectedError`
- 登入失敗統一回 `Auth.InvalidCredentials`。
- 註冊重複錯誤可明確指出：
  - `Auth.UserAccountAlreadyExists`
  - `Auth.EmailAlreadyExists`
- Service 不直接建立 HTTP response。
- API 層負責把 Service / application result 轉成 HTTP status code 與錯誤 DTO。
- 未來 `ServiceResult<T>` 應以 error code 與 parameters 為主，不以使用者文案作為主要契約。

## 測試決策

- Auth 第一輪 TDD 從 Auth Service 註冊成功路徑開始。
- 第一個紅燈測試：
  - `AuthService.RegisterAsync_WhenRequestIsValid_CreatesUserAndReturnsTokenWithUser`
- 第一個測試驗證：
  - 建立 Identity 使用者。
  - `request.UserAccount` 正確映射到 `ApplicationUser.UserName`。
  - 正確保存 `DisplayName`。
  - 正確保存 `Email`。
  - 成功回傳 `accessToken`。
  - 成功回傳 user 基本資料。
- 每個 TDD loop 只新增一個紅燈測試。
- 測試使用 NUnit。
- 測試維持 Arrange / Act / Assert。
- 測試方法補上繁體中文 XML summary。
- 後續測試循環再逐步補：
  - UserAccount 重複。
  - Email 重複。
  - UserAccount 格式錯誤。
  - DisplayName 格式錯誤。
  - 使用 UserAccount 登入。
  - 使用 Email 登入。
  - 登入失敗統一回 `Auth.InvalidCredentials`。
  - `/api/users/me` 回傳統一 user DTO。
  - Identity 內建 register/login 端點不作為正式前端契約。
- API 錯誤格式測試優先從 Controller / API 行為切入。
- 第一個錯誤格式 TDD loop 可針對：
  - DailyRecord 找不到時回 `404`，且 body 使用統一錯誤格式，`code = DailyRecord.NotFound`。

## 不在本次範圍

- 完整 Email 驗證流程。
- 寄信服務。
- 忘記密碼。
- 雙因素驗證。
- 第三方登入。
- 角色管理 UI。
- 完整 refresh token 更新流程。
- 頭像上傳。
- 年齡、體重、身高、性別、營養目標等進階個人資料。
- 管理員帳號管理。
- 雲端部署調整。
- 取代 ASP.NET Core Identity。
- 自行實作密碼雜湊或底層 token 安全邏輯。

## 補充說明

- `UserAccount` 是產品語言。
- `UserName` 是 Identity 框架語言。
- 兩者的 mapping 應集中在 Auth 相關程式碼。
- 前端 DTO 不應出現 `UserName`。
- `DisplayName` 不用於登入或唯一識別。
- 專案仍維持單體 Web API 架構。
- Controller 負責 HTTP、授權、驗證邊界與 status code。
- Service 負責商業規則與 application result。
- Data 層負責 EF Core persistence。
