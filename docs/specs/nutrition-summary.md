# Nutrition Summary 營養素統計規格

## 問題說明

FoodLedger 的核心價值不只是記錄使用者吃了什麼，也需要讓使用者理解自己攝取了哪些營養素。目前 DailyRecord 可以記錄食物、份量、時間、餐別與備註，但使用者還無法從紀錄中看到每日或每週的營養攝取分析。

前端需要支援：

- 每日營養素總量。
- 每日依餐別拆分的營養素統計。
- 每週營養素總量。
- 每週每天 breakdown，用於圖表與左右滑切換週期。
- 每筆 DailyRecord 明細附帶食物名稱與該筆紀錄的營養素貢獻。

## 解決方案

建立 Nutrition Summary 功能，根據目前登入使用者的 DailyRecord 與 FoodNutrient 即時計算營養素攝取量。

第一版不建立營養素快照，不在新增 DailyRecord 時儲存計算後的營養素結果。統計查詢時即時計算，避免資料同步複雜度，也讓食物營養資料修正後能自然反映在統計結果中。

第一版提供：

```http
GET /api/nutrition-summary/daily?date=2026-07-26&timeZone=Asia%2FTaipei&langCode=zh-TW
GET /api/nutrition-summary/weekly?date=2026-07-26&timeZone=Asia%2FTaipei&langCode=zh-TW
GET /api/nutrients?langCode=zh-TW
```

並調整：

```http
GET /api/daily-records?date=2026-07-26&timeZone=Asia%2FTaipei&langCode=zh-TW
```

讓每筆 DailyRecord 明細包含 `foodName` 與計算後的 `nutrients`。

## 使用者故事

1. 作為使用者，我想查看今天攝取了哪些營養素，讓我知道今天飲食是否均衡。
2. 作為使用者，我想查看今天總共攝取多少熱量、蛋白質、脂肪、碳水與其他營養素，讓我能掌握每日攝取狀況。
3. 作為使用者，我想查看每個餐別的營養素攝取，讓我知道早餐、午餐、晚餐或點心各自貢獻多少。
4. 作為使用者，我想查看本週營養素總量，讓我能從一週角度理解飲食習慣。
5. 作為使用者，我想左右滑切換上一週或下一週，讓我能快速比較不同週的飲食狀況。
6. 作為前端工程師，我想 weekly summary 固定以週一到週日為範圍，讓週圖表規則穩定。
7. 作為前端工程師，我想 weekly summary 回傳 7 天 breakdown，讓我不需要自己補日期。
8. 作為前端工程師，我想 daily records 每筆包含食物名稱，讓每日紀錄頁不需要再額外查食物名稱。
9. 作為前端工程師，我想 daily records 每筆包含營養素貢獻，讓使用者可以看出每個食物提供了哪些營養。
10. 作為後端工程師，我想營養素統計只使用目前登入使用者的 DailyRecord，讓使用者資料保持隔離。
11. 作為後端工程師，我想營養素統計即時計算，讓第一版避免 snapshot 同步問題。
12. 作為後端工程師，我想使用 decimal 計算營養素，避免 double 浮點誤差。
13. 作為前端工程師，我想 API 回傳所有有資料的 nutrient，讓前端可自行決定顯示哪些核心營養素。
14. 作為使用者，我不希望缺少資料的 nutrient 被誤當成 0，避免營養分析誤導。
15. 作為前端工程師，我想沒有紀錄時仍收到 200 與空統計，讓空狀態 UI 容易處理。

## 實作決策

- 第一版採用即時計算，不建立營養素快照。
- 不在 DailyRecord table 儲存 calories、protein、fat、carbs 或其他營養素結果。
- DailyRecord 明細 API 可以附帶計算後的營養素，但資料庫不保存該結果。
- 營養素計算依據：
  - DailyRecord
  - SimpleFood
  - FoodNutrient
  - Nutrient
- 營養素計算只使用目前登入使用者的 DailyRecord。
- API 不接受前端傳 `userId`。
- Service 透過 `ICurrentUserService` 取得目前登入使用者。
- 第一版支援單日統計與週統計。
- 第一版不支援月統計。
- Daily summary API：
  - `GET /api/nutrition-summary/daily?date=yyyy-MM-dd&timeZone={IANA}&langCode={BCP47}`
- Weekly summary API：
  - `GET /api/nutrition-summary/weekly?date=yyyy-MM-dd&timeZone={IANA}&langCode={BCP47}`
- 建立與編輯食物所需的營養素目錄 API：
  - `GET /api/nutrients?langCode={BCP47}`
- DailyRecord 與 Nutrition Summary 使用相同的本地日期、IANA timezone 與語系參數。
- 翻譯先使用指定語系，再 fallback 到 `en-US`；營養素兩者皆缺少時保留穩定 code，不捨棄數值。
- Weekly summary 的 `date` 代表焦點日期。
- 後端依 `date` 計算該日期所在週。
- 週期固定為週一到週日。
- Weekly response 回傳：
  - `startDate`
  - `endDate`
  - `totals`
  - `days`
- Weekly summary 回週總量與每日 breakdown。
- Weekly summary 第一版不回餐別 breakdown。
- Weekly summary 永遠回 7 個 day item。
- 沒資料的 day item 回 `totals = []`。
- 整週沒有資料時回 `200 OK`、`totals = []`，但 `days` 仍包含 7 天。
- Daily summary response 回傳：
  - `date`
  - `totals`
  - `mealTypes`
- Daily summary 回當日總量與餐別 breakdown。
- Daily summary 不回 records 明細，避免與 DailyRecords API 重複。
- DailyRecords API 每筆紀錄附帶：
  - `foodName`
  - `nutrients`
- DailyRecords API 負責紀錄明細。
- NutritionSummary API 負責統計聚合。
- `foodName` 與營養素名稱依 `langCode` 回傳，缺少指定語系時 fallback 到 `en-US`。
- 第一版 `quantityInGrams` 明確代表克數。
- 對外 API 使用 `quantityInGrams`，取代語意不明確的 `quantity`。
- 營養素資料假設以每 100g 為基準。
- 計算公式：
  - `攝取營養量 = FoodNutrient.AmountPer100g * QuantityInGrams / 100`
- Service 使用 `decimal` 計算。
- 不使用 `double`。
- 後端不在計算過程過早四捨五入。
- 顯示格式與小數位數交給前端。
- API 回傳所有有 FoodNutrient 資料的 nutrient。
- 缺少營養素資料時不補 0。
- 不把 missing data 視為 0。
- Nutrient response 包含：
  - `nutrientId`
  - `code`
  - `displayName`
  - `langCode`
  - `amount`
  - `unitCode`

## API 契約草稿

Daily records response 每筆紀錄：

```json
{
  "recordId": 1,
  "foodId": 10,
  "foodName": "雞胸肉",
  "quantityInGrams": 150,
  "consumedAt": "2026-07-26T12:30:00Z",
  "mealTypeCode": "Lunch",
  "note": "今天吃比較少",
  "nutrients": [
    {
      "nutrientId": 1,
      "code": "Protein",
      "displayName": "蛋白質",
      "amount": 46.5,
      "unitCode": "g"
    }
  ]
}
```

Daily summary response：

```json
{
  "date": "2026-07-26",
  "timeZone": "Asia/Taipei",
  "totals": [
    {
      "nutrientId": 1,
      "code": "Protein",
      "displayName": "蛋白質",
      "amount": 95.2,
      "unitCode": "g"
    }
  ],
  "mealTypes": [
    {
      "mealTypeCode": "Lunch",
      "totals": [
        {
          "nutrientId": 1,
          "code": "Protein",
          "displayName": "蛋白質",
          "amount": 46.5,
          "unitCode": "g"
        }
      ]
    }
  ]
}
```

Weekly summary response：

```json
{
  "startDate": "2026-07-20",
  "endDate": "2026-07-26",
  "timeZone": "Asia/Taipei",
  "totals": [
    {
      "nutrientId": 1,
      "code": "Protein",
      "displayName": "蛋白質",
      "amount": 520.3,
      "unitCode": "g"
    }
  ],
  "days": [
    {
      "date": "2026-07-20",
      "totals": []
    },
    {
      "date": "2026-07-21",
      "totals": [
        {
          "nutrientId": 1,
          "code": "Protein",
          "displayName": "蛋白質",
          "amount": 74.3,
          "unitCode": "g"
        }
      ]
    }
  ]
}
```

## 測試決策

- 第一個 TDD loop 從 Service 層 daily summary 成功計算開始。
- 第一個紅燈測試：
  - `NutritionSummaryService.GetDailySummaryAsync_WhenUserHasDailyRecord_ReturnsCalculatedNutrientTotals`
- 第一個測試只驗證：
  - 目前登入使用者有一筆 DailyRecord。
  - 食物有 FoodNutrient。
  - Service 使用 `quantityInGrams / 100 * amountPer100g` 計算攝取量。
  - 回傳 daily summary totals。
- 第一輪不處理：
  - weekly
  - mealType breakdown
  - records nutrient detail
  - controller/API
  - empty summary
  - missing nutrient
  - foodName
- 後續測試循環再補：
  - 只統計目前登入使用者資料。
  - 未登入拒絕。
  - daily summary 無紀錄回空統計。
  - daily summary 依 `mealTypeCode` breakdown。
  - weekly summary 計算週一到週日。
  - weekly summary 永遠回 7 天。
  - weekly summary 沒資料仍回 7 天空資料。
  - 缺少 FoodNutrient 不補 0。
  - DailyRecords response 每筆包含 nutrients。
  - DailyRecords response 每筆包含 foodName。
- 測試使用 NUnit。
- 每個 TDD loop 只新增一個紅燈測試。
- 測試方法補繁體中文 XML summary。
- Service 測試可使用 EF Core InMemory。
- 若未來涉及 PostgreSQL 特有行為，再評估 Testcontainers。

## 不在本次範圍

- 營養素快照。
- 月統計。
- 年統計。
- 趨勢圖專用 API。
- 目標攝取量比較。
- 使用者營養目標。
- 缺少營養資料的品質提示。
- 前端顯示格式規則。
- 依餐別的 weekly breakdown。
- 快取或統計預計算。
- 大量資料效能優化。
- 匯出報表。

## 補充說明

Nutrition Summary 是統計聚合功能，不應改變 DailyRecord 的資料擁有權規則。DailyRecord 仍是使用者實際攝取紀錄；Nutrition Summary 只根據既有紀錄與食物營養資料即時計算結果。
