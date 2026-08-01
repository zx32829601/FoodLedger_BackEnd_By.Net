# 主要 DailyRecord 飲食紀錄功能規格

## 問題說明

FoodLedger 的核心功能是讓使用者記錄每日實際攝取的食物。目前 DailyRecord 已有新增、依 UTC 日期查詢與刪除能力，但第一版紀錄內容還不足以支援前端每日紀錄頁的分類顯示與日常補充描述。

前端每日紀錄頁需要根據餐別分類，例如早餐、午餐、晚餐與點心；使用者也需要輸入備註來補充特殊情境。

## 解決方案

擴充 DailyRecord，加入：

- `mealTypeCode`
- `note`

DailyRecord 第一版核心欄位：

- `foodId`
- `quantityInGrams`
- `consumedAt`
- `mealTypeCode`
- `note`

`mealTypeCode` 由前端 select 傳入，選項來源為 `GET /api/defined-codes/meal-types`。後端新增與修改 DailyRecord 時，透過 `DefinedCode` 驗證 `mealTypeCode` 是否存在且 active。

## 使用者故事

1. 作為使用者，我想記錄我吃了哪個食物，讓我能追蹤每日飲食。
2. 作為使用者，我想記錄吃了多少份量，讓系統未來可以計算營養攝取。
3. 作為使用者，我想記錄實際攝取時間，讓每日紀錄能依時間排序。
4. 作為使用者，我想選擇餐別，讓我的紀錄可以分成早餐、午餐、晚餐與點心。
5. 作為使用者，我想替飲食紀錄加入備註，讓我能補充當下情境。
6. 作為前端工程師，我想每日紀錄 API 回傳 `mealTypeCode`，讓前端可以依餐別分組。
7. 作為前端工程師，我想 DailyRecord response 保持 flat list，讓前端可以自由決定分組與呈現方式。
8. 作為使用者，我想修改自己的飲食紀錄，讓我可以修正食物、份量、時間、餐別或備註。
9. 作為使用者，我想刪除自己的飲食紀錄，讓錯誤紀錄可以被移除。
10. 作為重視隱私的使用者，我不希望別人能查詢、修改或刪除我的飲食紀錄。
11. 作為後端工程師，我希望刪除別人的紀錄與紀錄不存在都回 404，避免透露資料是否存在。
12. 作為前端工程師，我希望不存在或停用的餐別回 validation error，讓表單能提示使用者重選餐別。

## 實作決策

- DailyRecord 第一版核心欄位：
  - `foodId`
  - `quantityInGrams`
  - `consumedAt`
  - `mealTypeCode`
  - `note`
- `mealTypeCode` 必填。
- `mealTypeCode` 對應 `DefinedCode`：
  - `CodeType = MealType`
  - `Code = mealTypeCode`
  - `IsActive = true`
- 第一版使用 Service 驗證 `mealTypeCode`，不做 DB composite FK。
- `note` 選填。
- `note` 可不傳或傳 `null`。
- `note` 前後 trim。
- `note` trim 後若為空字串，視為 `null`。
- `note` 最大長度 500。
- `note` 不參與查詢與統計。
- `mealTypeCode` 由前端 select 傳入。
- 後端不根據 `consumedAt` 自動推斷餐別。
- 前端可以依時間預設選項，但使用者要能改。
- `GET /api/daily-records` 回 flat list。
- `GET /api/daily-records` response 包含：
  - `recordId`
  - `foodId`
  - `quantityInGrams`
  - `consumedAt`
  - `mealTypeCode`
  - `note`
- `GET /api/daily-records` 第一版不回 `mealTypeDisplayName`。
- 前端使用 `GET /api/defined-codes/meal-types` 取得 dictionary，再用 `mealTypeCode` 對應顯示名稱。
- 後端查詢排序維持：
  - `consumedAt`
  - `recordId`
- 新增 DailyRecord 時：
  - request 不收 `UserId`
  - 使用目前登入使用者作為擁有者
  - `foodId` 必須有效且食物存在
  - `quantityInGrams` 必須大於 0 且不超過 10000
  - `consumedAt` 不可晚於目前 UTC
  - `consumedAt` 儲存時正規化為 UTC
  - `mealTypeCode` 必須存在且 active
  - `note` 依規則 trim / null 化
- 既有資料 migration：
  - `mealTypeCode` 預設為 `Snack`
  - `note` 預設為 `null`
- 修改 API 納入規格：
  - `PUT /api/daily-records/{recordId}`
- 修改可更新：
  - `foodId`
  - `quantityInGrams`
  - `consumedAt`
  - `mealTypeCode`
  - `note`
- 修改規則同新增。
- 只能修改自己的紀錄。
- 不存在或不是自己的紀錄回 404。
- 刪除維持既有 API：
  - `DELETE /api/daily-records/{recordId}`
- 第一版刪除維持實體刪除。
- 不做 soft delete。
- 刪除成功回 204。
- 刪除不存在或別人的紀錄回 404。
- 未登入由授權機制回 401。

## 錯誤規格

- 不存在或已停用的 `mealTypeCode` 回 400 validation error。
- 欄位名稱：`mealTypeCode`
- 錯誤 code：`DailyRecord.InvalidMealType`
- 不回 404，因為這是 request 欄位值不合法。
- 刪除 / 修改不存在或非本人資料回 404。
- 未登入回 401。
- 錯誤 response 格式沿用「自訂 Auth API 與統一 API 錯誤回應規格」。

## 測試決策

第一階段實作順序：

1. 建立 `DefinedCode` entity / migration / MealType seed。
2. 建立 `GET /api/defined-codes/meal-types`。
3. DailyRecord 加上 `mealTypeCode` 與 `note` 欄位。
4. 新增 DailyRecord 時驗證 active `MealType`。
5. 查詢 DailyRecord 時回 `mealTypeCode` 與 `note`。
6. 補 `PUT /api/daily-records/{recordId}` 修改功能。
7. 刪除維持現有實體刪除規則。

測試策略：

- 每個 TDD loop 只新增一個紅燈測試。
- 測試優先驗證外部行為，不測內部 helper。
- Service 層測試驗證商業規則。
- Controller/API 測試驗證 HTTP 邊界、授權與 model validation。
- EF model 測試驗證欄位、索引、唯一鍵與必要約束。
- 第一個 DailyRecord 相關紅燈測試可在 DefinedCode API 完成後進行：
  - 新增 DailyRecord 時保存 `mealTypeCode` 與 trim 後的 `note`
  - 或新增 DailyRecord 時 invalid `mealTypeCode` 回 validation error
- 既有測試需隨欄位新增調整，避免舊 request 缺少必填 `mealTypeCode` 導致測試失焦。

## 不在本次範圍

- 食物名稱多語系顯示。
- DailyRecord response 回 `mealTypeDisplayName`。
- 依餐別 grouped response。
- DailyRecord soft delete。
- 照片上傳。
- 心情、地點、標籤。
- 預先規劃餐點 `PlannedMeal` / `MealPlan`。
- 營養統計彙總 API。
- DefinedCode 管理 API。
- DefinedCode 多語系 API。
- DB composite FK 強約束。

## 補充說明

DailyRecord 代表已實際攝取的飲食紀錄，不代表未來計畫餐點。未來若要記錄「預計會吃什麼」，應另建 `PlannedMeal` 或 `MealPlan` 功能。
