# 通用 DefinedCode 多語系規格

## 功能目標

DefinedCode 是跨功能共用的固定代碼來源。前端不得自行維護代碼、顯示名稱、說明或排序；後端以穩定的 `CodeType + Code` 保存資料語意，並依要求語系提供可顯示文字。

本版支援餐別、健身目標與活動程度，並將 `DisplayName` 與 `Note` 移至多語系翻譯資料。

## 資料模型

### DefinedCode

- 複合主鍵：`CodeType + Code`
- 欄位：`CodeType`、`Code`、`SortOrder`、`IsActive` 與共用稽核欄位
- 不保存語系文字
- `IsActive = true` 才能出現在新資料的選項 API
- 所有代碼都不得實體刪除；資料庫 trigger 會拒絕 `DELETE`，停用時只能將 `IsActive` 設為 `false`

### DefinedCodeTranslation

- 複合主鍵：`CodeType + Code + LangCode`
- 複合外鍵指向 `DefinedCode`，刪除行為為 `Restrict`
- 欄位：
  - `LangCode`：合法 BCP 47 語系代碼
  - `DisplayName`：必要，最大 100 字元
  - `Note`：選填，最大 500 字元
  - 共用稽核欄位
- `Note` 只提供使用者理解選項，不得由計算邏輯解析或作為係數來源

## 語系規則

- API query `langCode` 預設為 `zh-TW`
- `langCode` 必須符合系統接受的 BCP 47 格式
- 查詢順序：
  1. 不分大小寫比對要求語系
  2. 找不到時 fallback 至 `en-US`
  3. 仍找不到時以原始 `Code` 作為 `displayName`，`langCode` 與 `note` 回 `null`
- Response 的 `langCode` 是實際採用的翻譯語系，因此發生 fallback 時會回 `en-US`

## 代碼種類與 seed

### MealType

| Code | sortOrder | zh-TW | en-US |
| --- | ---: | --- | --- |
| `Breakfast` | 1 | 早餐 | Breakfast |
| `Lunch` | 2 | 午餐 | Lunch |
| `Dinner` | 3 | 晚餐 | Dinner |
| `Snack` | 4 | 點心 | Snack |

### FITNESS_GOAL

| Code | sortOrder | zh-TW | en-US |
| --- | ---: | --- | --- |
| `FAT_LOSS` | 1 | 減脂 | Fat loss |
| `MAINTAIN` | 2 | 維持體重 | Maintain |
| `MUSCLE_GAIN` | 3 | 增肌 | Muscle gain |

### ACTIVITY_LEVEL

| Code | sortOrder | zh-TW | en-US |
| --- | ---: | --- | --- |
| `SEDENTARY` | 1 | 久坐 | Sedentary |
| `LIGHT` | 2 | 輕度活動 | Lightly active |
| `MODERATE` | 3 | 中度活動 | Moderately active |
| `HIGH` | 4 | 高度活動 | Highly active |
| `VERY_HIGH` | 5 | 極高活動 | Very highly active |

每筆 seed 都必須提供 `zh-TW`、`en-US` 的 `DisplayName` 與非空白 `Note`。

## HTTP API

三個端點皆不需登入，只回 active code，依 `sortOrder`、`code` 排序：

```http
GET /api/defined-codes/meal-types?langCode=zh-TW
GET /api/defined-codes/fitness-goals?langCode=zh-TW
GET /api/defined-codes/activity-levels?langCode=zh-TW
```

Response item：

```json
{
  "code": "FAT_LOSS",
  "displayName": "減脂",
  "langCode": "zh-TW",
  "note": "以降低體脂為目標，建議熱量設定低於維持需求。",
  "sortOrder": 1
}
```

不合法的 `langCode` 回 `400 Bad Request`，欄位錯誤碼使用 `DefinedCode.InvalidLangCode`。

## Migration 規則

- 升級時將既有 `defined_code.display_name` 保存為 `zh-TW` 翻譯後，才移除原欄位
- 已有 seed 翻譯優先，搬移資料遇到相同複合鍵時不得覆寫
- rollback 時優先取 `zh-TW`、其次 `en-US`、最後原始 code 還原 `display_name`
- migration 建立資料庫層級刪除 trigger；rollback 會先移除 trigger，再還原舊 schema

## 測試決策

- 驗證指定語系、`en-US` fallback 與實際 `langCode`
- 驗證 localized `Note`
- 驗證只回 active code 且排序穩定
- 驗證 Fitness Goal 與 Activity Level 的完整 seed
- 驗證翻譯複合鍵及 `Restrict` 外鍵
- 驗證不合法語系回 400
- 驗證 migration 不遺失既有顯示名稱

## 不在本次範圍

- DefinedCode 後台管理 API 與管理畫面
- 自訂計算係數
- 由 `Note` 推導熱量或營養素公式
- 實體刪除已使用的代碼
