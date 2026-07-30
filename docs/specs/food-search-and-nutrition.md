# 食物搜尋、飲食紀錄契約與營養摘要規格

## Problem Statement

FoodLedger 已具備飲食紀錄 CRUD、餐別與備註，但目前 DailyRecord API 只提供 `foodId`，前端無法透過正式 API 顯示食物名稱、搜尋可記錄的食物或取得營養資料。既有 `FoodNutrient` 也未明確表達營養素單位與計算基準，因此無法建立可靠、可跨 Flutter 與未來 React 共用的營養統計。

此外，現有日期查詢以 UTC 日界切分，與使用者在本地日曆選擇的日期不一致；`quantity` 也沒有明確單位，容易被解讀為克、份或其他單位。

## Solution

建立食物搜尋與動態營養資料 API，將第一版飲食份量統一定義為克數，並由後端負責每日營養換算與加總。DailyRecord response 直接嵌入目前版本的食物顯示資料，讓 client 能以單一 request 呈現紀錄。

日期查詢改用使用者選擇的本地日期與 IANA timezone，後端將當地日界轉換為 UTC 區間。食物翻譯採指定語系、`en-US` fallback 與無翻譯排除規則。

## User Stories

1. 作為使用者，我想搜尋食物名稱，讓我能選擇實際吃下的食物。
2. 作為使用者，我想看到食物的營養資料，讓我能了解食物內容。
3. 作為使用者，我想以克數記錄份量，讓營養換算具有一致基準。
4. 作為使用者，我想查看本地日期的飲食紀錄，讓凌晨紀錄不會被歸到錯誤日期。
5. 作為使用者，我想查看每日營養摘要，讓我能掌握當日攝取量。
6. 作為使用者，我想自由選擇早餐、午餐、晚餐或點心，讓餐別符合真實情境而非固定時段。
7. 作為使用者，我想修改食物、克數、攝取時間、餐別與備註，讓我能修正錯誤紀錄。
8. 作為使用者，我不希望其他使用者讀取或修改我的紀錄與營養摘要。
9. 作為前端工程師，我想取得分頁食物搜尋結果，讓大量食物不會造成過大 response。
10. 作為前端工程師，我想取得動態營養素清單，讓新增營養素時不必修改 API 固定欄位。
11. 作為前端工程師，我想從 DailyRecord response 直接取得食物顯示資料，避免逐筆查詢造成 N+1 requests。
12. 作為前端工程師，我想知道實際採用的翻譯語系，讓 UI 能辨識 fallback。
13. 作為後端工程師，我想用穩定 nutrient code 與 unit code 表達營養資料，讓多個 client 使用相同契約。
14. 作為後端工程師，我想集中營養換算與加總規則，讓 Flutter 與 React 不會得到不同結果。
15. 作為系統維護者，我想保留動態營養素擴充能力，讓未來可加入糖、鈉、纖維及其他項目。
16. 作為系統維護者，我想用 IANA timezone 計算日界，讓支援日光節約時間的地區也能正確查詢。
17. 作為 API 使用者，我想在傳入無效語系、時區、頁碼或份量時收到欄位 validation error，讓 client 能提供可恢復操作。
18. 作為重視隱私的使用者，我希望不存在與不屬於我的紀錄維持相同 404 語意，避免資料存在性外洩。
19. 作為未來食物詳情頁的使用者，我希望目前契約不阻礙未來建立獨立詳情功能。
20. 作為弱網使用者，我希望系統未來能避免 create request 重送造成重複資料。

## Implementation Decisions

- 本規格在既有 DailyRecord 與 DefinedCode 功能合併後，以獨立後端 PR 實作。
- 第一版公開份量欄位使用 `quantityInGrams`，明確表示克數；資料庫欄位可維持既有名稱，但 Entity、DTO、文件與測試必須表達克數語意。
- 第一版只支援克數，不支援碗、杯、顆、份或毫升。
- 未來其他份量單位應透過獨立 FoodPortion 模型換算成克數，不改變營養計算核心。
- Nutrient 增加穩定 `unitCode`；第一版至少支援 `kcal`、`g`、`mg`、`ug`。
- 營養素單位屬於 Nutrient，不由每筆 FoodNutrient 自行決定。
- FoodNutrient amount 統一表示每 100 克含量。
- 既有 `perUnit` 不再作為 API 或營養計算依據；migration 應明確選擇移除或保留但停用其業務語意。
- 食物搜尋 API 使用 GET，支援 `query`、`langCode`、`page` 與 `pageSize`。
- `query` 為選填；空白時回傳指定語系可顯示的全部食物，非空白時以 trim 後的文字篩選。
- `page` 預設 1；`pageSize` 預設 20，上限 100。
- 搜尋結果依實際採用翻譯的 food name 排序，再以 food ID 維持穩定順序。
- 搜尋 response 包含 items、page、pageSize 與 totalCount。
- 食物 item 包含 food ID、food code、display name、實際採用 lang code 與動態 nutrients。
- Nutrient item 包含穩定 code、display name、amountPer100Grams 與 unitCode。
- `langCode` 使用 BCP 47 格式，第一版預設 `zh-TW`。
- 翻譯先查指定語系，缺少時 fallback 到 `en-US`；兩者皆缺少時排除該食物。
- 搜尋只比對實際採用語系的 food name。
- 第一版不建立 food detail API；食物詳情未來以獨立功能實作。
- DailyRecord response 將 `foodId` 改為嵌入專用 food summary DTO，並包含動態營養素。
- 嵌入資料代表目前版本的食物名稱與營養資料，不保存建立紀錄當下的歷史快照。
- DailyRecord create、update、get response 的公開份量欄位統一使用 `quantityInGrams`。
- `consumedAt` 仍使用 UTC 傳輸與儲存，且不可晚於伺服器目前時間。
- 餐別不限制時段；後端不得依 `consumedAt` 推斷或拒絕 Breakfast、Lunch、Dinner、Snack。
- DailyRecord 與 Nutrition Summary 日期查詢接收本地 `date` 與 IANA `timeZone`。
- 後端將指定 timezone 的當地日界轉成 UTC 半開區間後查詢。
- 無效 IANA timezone 回欄位 validation error，不 fallback 到 UTC。
- Nutrition Summary API 接收 date、timeZone 與 langCode。
- Nutrition Summary response 使用動態 nutrient list，包含 code、displayName、amount 與 unitCode。
- 後端使用 decimal 計算 `amountPer100Grams × quantityInGrams ÷ 100`。
- 每筆紀錄換算時不先格式化或四捨五入；全部加總後仍回傳數值型別。
- 使用者資料隔離一律由目前登入身分決定，不接受前端 UserId。
- DailyRecord、Food Search 與 Nutrition Summary 使用 DTO projection，不直接暴露 Entity。
- 既有統一 API error response、field errors、stable error code 與 traceId 契約持續適用。
- Mutation 不自動 retry。

## Testing Decisions

- 測試外部可觀察行為，不測 private helper 或重現實作內部演算法。
- 主要 seam 是正式 HTTP API：Food Search、DailyRecord 與 Nutrition Summary。
- API 測試涵蓋成功、授權、欄位 validation、語系 fallback、分頁、排序、timezone 日界與 response shape。
- Service 測試涵蓋每 100 克換算、跨多筆紀錄加總、缺少營養素、不同 unit code 與使用者資料隔離。
- EF model／migration test 驗證 Nutrient unit code、FoodNutrient 基準語意與 schema 約束。
- 時間測試使用固定 TimeProvider，不依賴目前系統時間。
- timezone 測試至少涵蓋 Asia/Taipei 日界，以及具有 DST 轉換的 IANA timezone。
- 翻譯測試涵蓋指定語系、`en-US` fallback 與兩者皆缺少。
- 分頁測試涵蓋預設值、上限、空結果、總筆數與穩定排序。
- DailyRecord response 測試驗證嵌入 food summary，不產生額外 client request 需求。
- Nutrition Summary expected values 使用獨立算例，不在 assertion 內複製 production 演算法。
- PostgreSQL 特定 migration、精度或關聯行為使用整合測試或 Testcontainers，不以 InMemory 取代。

## Out of Scope

- 食物詳細資訊 API 與詳細頁。
- FoodPortion、份、碗、杯、顆、毫升及密度換算。
- 歷史食物名稱或營養快照。
- DefinedCode 管理 API。
- 營養素管理後台。
- 離線同步與 mutation queue。
- Create request idempotency key 的實作。
- 前端 API 串接與 UI 修改。

## Further Notes

- 前端整合規格見 FoodLedger Frontend 的 `daily-record-api-integration.md`。
- 未來 idempotency key 用於避免 create request 因逾時、斷線或重送而建立重複資料。
- 未來建議以「目前使用者＋idempotency key」建立唯一性邊界；相同 key 與相同 payload 回傳原始結果，相同 key 與不同 payload 必須拒絕。
- Idempotency key 需有保存期限與清理策略，不得被當成 authentication、authorization 或資料主鍵。
- 未來實作 idempotency 時需測試併發 request、回應遺失後重送、相同 payload 重送與不同 payload 重用 key。
