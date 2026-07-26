# 通用 DefinedCode 代碼架構規格

## 問題說明

FoodLedger 後續會有多種固定代碼需求，例如 DailyRecord 的餐別 `MealType`，以及 Auth / Account 的帳號狀態 `AccountStatus`。如果每個功能都各自 hardcode code 與顯示名稱，前端與後端會逐漸出現重複定義、排序不一致、停用規則不一致與多語系擴充困難。

目前 DailyRecord 第一版需要餐別 select，前端不應自行維護餐別 code/displayName，而應由後端提供統一代碼來源。

## 解決方案

建立通用 `DefinedCode` 架構，用一張代碼表支援多種代碼類型。第一階段先支援 `MealType`，未來可擴充到 `AccountStatus` 與其他代碼。

`DefinedCode` 使用 `CodeType + Code` 區分代碼：

- `CodeType = MealType`
- `Code = Breakfast`

第一版提供讀取 API 給前端取得餐別選項：

```http
GET /api/defined-codes/meal-types
```

## 使用者故事

1. 作為前端工程師，我想從後端取得餐別選項，讓前端 select 不需要 hardcode 餐別定義。
2. 作為使用者，我想在新增飲食紀錄時選擇早餐、午餐、晚餐或點心，讓我的紀錄可以被清楚分類。
3. 作為後端工程師，我想用同一套代碼架構管理餐別與未來帳號狀態，讓代碼規則一致。
4. 作為前端工程師，我想取得餐別顯示順序，讓 UI 能依後端規則排序。
5. 作為系統管理者，我希望未來能停用某些代碼，讓新資料不可再使用，但舊資料仍保留語意。
6. 作為後端工程師，我希望代碼不被實體刪除，避免歷史資料失去可讀性。
7. 作為未來多語系使用者，我希望代碼架構能擴充翻譯資料，讓不同語系可顯示不同名稱。

## 實作決策

- Entity 名稱使用 `DefinedCode`。
- DbSet 名稱使用 `DefinedCodes`。
- `DefinedCode` 第一版欄位：
  - `CodeType`
  - `Code`
  - `DisplayName`
  - `SortOrder`
  - `IsActive`
- `CodeType + Code` 形成唯一鍵。
- 第一版 `DisplayName` 放預設中文名稱。
- 未來若需要多語系，再新增 `DefinedCodeTranslation`。
- 不在 `DefinedCode` 本表加入 `DisplayNameEn`、`DisplayNameZhTw` 這種欄位。
- 第一版不做 DefinedCode 管理 API。
- 第一版只做 seed / migration 與讀取 API。
- 不做實體刪除，使用 `IsActive` 控制是否可用。
- `IsActive = true` 的代碼可被新資料使用。
- `IsActive = false` 的代碼不可被新資料使用，但舊資料仍可保留該 code。
- 第一版 seed `MealType`：
  - `Breakfast` / 早餐 / sortOrder 1
  - `Lunch` / 午餐 / sortOrder 2
  - `Dinner` / 晚餐 / sortOrder 3
  - `Snack` / 點心 / sortOrder 4
- 第一版 API：
  - `GET /api/defined-codes/meal-types`
- API response：
  - `code`
  - `displayName`
  - `sortOrder`
- `GET /api/defined-codes/meal-types`：
  - 只回 `CodeType = MealType`
  - 只回 `IsActive = true`
  - 依 `SortOrder` 由小到大排序
  - 第一版不支援語系參數
  - 不要求登入
- 未來規劃：
  - DefinedCode 管理 API
  - Admin 授權
  - 新增代碼
  - 修改顯示名稱
  - 調整排序
  - 停用代碼
  - `DefinedCodeTranslation`
  - `AccountStatus`

## 測試決策

- 第一個 TDD loop 從餐別讀取 API 開始。
- 第一個紅燈測試：
  - `DefinedCodesController.GetMealTypes_WhenActiveMealTypesExist_ReturnsActiveCodesOrderedBySortOrder`
- 測試應驗證外部行為：
  - 只回 active meal types。
  - 排除 inactive code。
  - 依 `SortOrder` 排序。
  - 回傳 `code`、`displayName`、`sortOrder`。
- 後續測試再補：
  - `DefinedCode` EF model 唯一鍵。
  - `DefinedCode` seed data。
  - 非 MealType code 不會出現在 meal types API。
  - API 不需登入即可取得餐別選項。
- 測試使用 NUnit。
- 每個 TDD loop 只新增一個紅燈測試。
- 測試方法加繁體中文 XML summary。

## 不在本次範圍

- DefinedCode 管理 API。
- DefinedCode 多語系 API。
- DefinedCodeTranslation。
- 後台維護畫面。
- AccountStatus 實作。
- DB composite FK 強約束。
- 實體刪除 DefinedCode。

## 補充說明

`DefinedCode` 是跨功能基礎架構，不只是 DailyRecord 的餐別附屬功能。DailyRecord 使用 `MealType`，Auth 未來可使用 `AccountStatus`。
