# 食物明細與餐別時間解耦規格

## Problem Statement

FoodLedger 的食物查詢目前只顯示名稱、穩定代碼與每 100 克熱量。使用者無法查看食物說明、分類、完整營養資料，也不能先輸入實際食用克數確認換算結果後再建立飲食紀錄。

搜尋 response 目前為每筆食物附帶完整營養素，而 Flutter 會一次抓完所有分頁。資料量增加後，列表會下載大量當下不會顯示的內容；副標題顯示技術代碼，對一般使用者也沒有辨識價值。

飲食紀錄流程另有跨功能問題：新增紀錄會以餐別的固定時刻產生 `consumedAt`，前端 domain model 在缺少餐別時也會從時間推斷餐別。每個人的早餐、午餐、晚餐與點心時間不同，餐別與食用時間不應互相改寫。這項規則必須適用食物明細新增、既有飲食紀錄新增、編輯與未來所有紀錄入口。

## Solution

提供登入限定的食物明細 API 與 Flutter 明細頁。搜尋列表改成輕量、雙語、明確分頁的摘要；點選食物後，以食物 ID 載入名稱、英文副名稱、說明、分類與完整動態營養素。

使用者輸入克數後，前端即時換算熱量、蛋白質、脂肪、碳水化合物與其他營養素，再進入第二步確認日期時間、餐別與備註。前端預覽不成為可信輸入，正式紀錄仍由後端依目前每 100 克資料計算。

營養素增加全域 `displayOrder`，讓鈉等較重要項目能在所有 client 中穩定排序。所有 Daily Record 表單將 `consumedAt` 與 `mealTypeCode` 視為完全獨立的欄位；新表單只在初始化時依初始時間預選餐別，之後兩者不再連動。

## User Stories

1. 作為使用者，我想從搜尋列表開啟食物明細，讓我能在記錄前確認完整資訊。
2. 作為使用者，我想看到符合 App 語系的食物名稱，讓內容容易理解。
3. 作為非英文語系使用者，我想看到英文副名稱，讓我能辨識包裝上的英文名稱。
4. 作為英文語系使用者，我不想看到重複的英文副名稱。
5. 作為使用者，我不想在列表看到內部 food code。
6. 作為使用者，我想用目前語系或英文名稱搜尋同一食物。
7. 作為使用者，我想讓完整符合與開頭符合優先於部分符合。
8. 作為使用者，我想以明確頁碼瀏覽搜尋結果。
9. 作為使用者，我想在返回搜尋列表時保留關鍵字、頁碼與捲動位置。
10. 作為使用者，我想看到食物分類與說明。
11. 作為使用者，我想在說明或分類缺少時隱藏空白區塊。
12. 作為使用者，我想知道營養資料以每 100 克為基準。
13. 作為使用者，我想輸入實際食用克數並立即看到換算結果。
14. 作為使用者，我想看到醒目的熱量以及固定的蛋白質、脂肪與碳水化合物摘要。
15. 作為使用者，我想在資料缺少時看到破折號而不是零。
16. 作為使用者，我想展開其他動態營養資料。
17. 作為使用者，我想讓鈉等較重要營養素排在前面。
18. 作為使用者，我想先確認食物與克數，再設定日期時間、餐別與備註。
19. 作為使用者，我想在送出前返回明細修改克數。
20. 作為使用者，我想自由選擇食用日期與時間，並阻擋未來時間。
21. 作為使用者，我想在任何時間選擇早餐、午餐、晚餐或點心。
22. 作為使用者，我想切換餐別而不改變食用時間。
23. 作為使用者，我想修改食用時間而不改變餐別。
24. 作為使用者，我想在新表單第一次開啟時取得合理的餐別預選。
25. 作為使用者，我想編輯既有紀錄時保留原本的時間與餐別。
26. 作為使用者，我想在新增成功後選擇繼續新增或查看飲食紀錄。
27. 作為使用者，我想在繼續新增時返回原搜尋狀態。
28. 作為使用者，我想在查看紀錄時前往剛新增的本地日期。
29. 作為使用者，我想在明細載入失敗時重試，而不是用過期摘要建立紀錄。
30. 作為使用者，我想在新增失敗時保留確認表單內容。
31. 作為使用者，我不希望新增 request 被自動重送。
32. 作為前端工程師，我想使用輕量搜尋 response。
33. 作為前端工程師，我想依 food ID 獨立載入明細。
34. 作為前端工程師，我想取得每個本地化欄位實際採用的 `langCode`。
35. 作為前端工程師，我想取得營養素 `displayOrder`。
36. 作為後端工程師，我想讓搜尋與明細使用 DTO projection。
37. 作為後端工程師，我想分別驗證及儲存 `consumedAt` 與 `mealTypeCode`。
38. 作為測試維護者，我想使用固定 clock、IANA timezone 與正式 HTTP seam 驗證行為。
39. 作為資料維護者，我想集中設定營養素顯示順位。
40. 作為資料維護者，我想另行追蹤缺少的翻譯、分類與營養資料。

## Implementation Decisions

### Food Search

- 本功能同時包含 Flutter 食物搜尋／明細／紀錄表單、後端 Food Search／Food Detail／Daily Record 契約，以及 Nutrient schema migration。
- 不需要相容仍依賴舊搜尋 item 完整 nutrients 的舊版 App。
- 直接修改既有 `GET /api/foods`，不新增另一個 search route。
- `GET /api/foods` 維持登入限定，接收選填 `query`、`langCode`、`page` 與 `pageSize`。
- Response 維持 `items`、`page`、`pageSize`、`totalCount` 的分頁外形。
- Flutter 每頁固定請求 20 筆，使用上一頁、下一頁與頁碼選擇器。
- 不使用無限捲動，也不預先下載其他頁。
- 搜尋停止輸入約 350ms 後執行；鍵盤搜尋鍵可立即觸發。
- 新 query 回第 1 頁；空白 query 回傳全部可顯示食物的第 1 頁。
- 舊 request 不得覆蓋較新的 query 或 page state。
- 返回列表需保留 query、page、目前結果與捲動位置。
- 輕量 item 包含 food ID、food code、本地化 display name、實際 lang code、nullable English name，以及 nullable 每 100 克熱量摘要。
- 搜尋 item 不再附帶完整 nutrients。
- food code 保留於 API 與內部識別，但不顯示給一般使用者。
- 非英文語系同時比對主要語系名稱與 `en-US` 名稱；英文語系只搜尋英文顯示名稱。
- 同一食物只能回傳一次。
- Query 先 trim；英文字母不分大小寫並支援部分包含。
- 排序依序為主要語系完整符合、主要語系開頭符合、主要語系部分符合、英文完整符合、英文開頭符合、英文部分符合，再以主要名稱與 food ID 維持穩定順序。
- MVP 不實作錯字容忍、拼音、同義詞或模糊搜尋。
- 主名稱實際採用的 BCP 47 主要語言子標籤不是 `en` 時，才顯示不同且非空的 `en-US` 副名稱。
- 主名稱已是任何英文體系、英文翻譯缺少或兩者相同時不顯示副名稱。

### Food Detail and localization

- 新增登入限定的 `GET /api/foods/{foodId}?langCode=...`。
- 明細 response 包含 food ID、food code、display name、實際 lang code、nullable English name、description、categories 與完整動態 nutrients。
- Category item 包含穩定 code、display name 與實際 lang code。
- Nutrient item 包含穩定 code、display name、實際 lang code、`amountPer100Grams`、`unitCode` 與 `displayOrder`。
- 文字先使用指定語系，再 fallback 至 `en-US`。
- 食物名稱兩種語系皆缺少時，搜尋排除該食物，明細回 404。
- Description 使用實際採用的食物翻譯；空白時隱藏說明區塊。
- 分類缺少可用翻譯時隱藏該分類。
- 營養素缺少指定語系與英文翻譯時仍保留數值，以 nutrient code 顯示並回傳 null lang code。
- 明細載入時可先以搜尋摘要顯示名稱，但正式明細成功前禁止進入確認步驟。
- 一般錯誤提供重試；404 顯示食物目前無法使用；401 交由統一 authentication 流程處理。

### Nutrient display order

- Nutrient 增加必要的全域 display order 欄位；資料庫欄位為 `display_order`。
- 未指定的預設值為 1000，數值越小越優先。
- `displayOrder` 公開於所有營養素相關 response。
- 後端依 `displayOrder`、nutrient code 排序，client 以相同條件做防禦性排序。
- 初始順位如下：
  - Calories 10
  - Protein 20
  - Carbohydrates 30
  - Fat 40
  - Sodium 50
  - SaturatedFat 60
  - DietaryFiber 70
  - Sugar 80
  - Cholesterol 90
  - Potassium 100
  - Calcium 110
  - Iron 120
  - VitaminA 130
  - VitaminC 140
- Migration 與基本營養素 seed 寫入順位。
- MVP 不新增營養素排序管理 API 或後台。

### Food Detail UI and calculation

- 明細標題區沿用搜尋的英文副名稱規則，並顯示可用分類與說明。
- 明細清楚標示營養資料基準為每 100 克。
- 克數預設 100，允許 0.1 至 10000 克且最多一位小數。
- 欄位標籤為「食用份量」、輸入 hint 為「例如 150」、單位為「克」。
- 常駐提示為「請輸入實際食用重量，營養數值會依此克數換算。」
- 窄螢幕時提示可排列到輸入框下方。
- 克數無效時立即顯示欄位錯誤並停用加入按鈕。
- 第一版只使用數字欄位，不加入 slider、步進按鈕或非克數單位。
- 前端以 `amountPer100Grams × quantityGrams ÷ 100` 即時產生預覽，不在輸入過程呼叫 API。
- 建立紀錄只提交 food ID 與克數，正式營養結果由後端依目前資料計算。
- 熱量為最醒目的主要數字。
- 摘要固定顯示 Protein、Fat、Carbohydrates，不依各食物含量動態替換。
- 缺少項目顯示破折號。
- 其餘營養素放在預設收合的「詳細營養資料（項目數）」。
- 詳細區排除 Calories、Protein、Fat、Carbohydrates；沒有剩餘項目時不顯示。
- kcal 與 g 最多顯示一位小數，mg 與 ug／µg 最多兩位小數，並移除尾端零。
- 計算保留原精度，只在顯示時四捨五入。

### Record confirmation and independent meal time

- 新增採兩步流程：第一步為明細與克數試算；第二步確認 food、quantityGrams、consumedAt、mealTypeCode 與 note。
- 確認步驟中的 food 與 quantityGrams 為唯讀。
- 使用者可返回保留狀態的明細頁修改克數，但不能在確認步驟更換食物或改克數。
- 確認步驟允許獨立選擇本地日期與時間，提交前依單一 IANA timezone 轉成 UTC。
- 未來 `consumedAt` 必須在前後端阻擋。
- `consumedAt` 與 `mealTypeCode` 是所有新增、編輯與未來 Daily Record 入口的獨立欄位。
- 改變任一欄位不得自動修改另一欄位。
- 後端不得依兩者組合拒絕早餐、午餐、晚餐或點心。
- 新表單只在初始化時依初始本地 consumedAt 建議餐別；初始化完成後不再連動。
- 編輯表單分別使用既有 consumedAt 與 mealTypeCode，不重新推斷。
- Daily Record domain model 以有效 mealTypeCode 作為餐別來源。
- 缺少或無效 code 應成為資料／驗證錯誤，不得 fallback 至從 consumedAt 推斷。
- 時間推斷只能作為新表單初始化建議。
- Daily Record 列表與 Nutrition Summary 依已儲存的 mealTypeCode 分組，不依 consumedAt 重新分類。
- 所有新增／編輯入口共用相同的日期時間、餐別獨立狀態與 validation 規則。
- 送出期間停用重複操作。
- POST、PUT、DELETE 不自動 retry。
- 失敗時保留日期時間、餐別與備註；欄位錯誤顯示於對應欄位，一般錯誤提供人工重新送出。
- 食物在確認期間失效時，關閉確認步驟、重新載入明細並禁止再次新增。
- 建立成功後顯示「飲食紀錄已新增」Dialog。
- Dialog 提供「繼續新增」與「查看飲食紀錄」。
- 繼續新增返回並保留原搜尋狀態。
- 查看紀錄前往剛建立紀錄的本地日期並刷新 Daily Record 與 Nutrition Summary。
- 本功能不大量補寫正式食物說明、分類或翻譯內容；已有資料就顯示，缺少時套用隱藏與 fallback 規則。

## Testing Decisions

- 測試外部可觀察行為，不測 private helper、Widget 樹細節或 EF 查詢實作。
- 後端最高 seam 使用正式 HTTP API，涵蓋 Food Search、Food Detail 與 Daily Record。
- Schema 與 PostgreSQL 特有行為使用 model／migration integration seam。
- Flutter 以 Repository contract、Riverpod controller 與關鍵 Widget interaction 作為主要 seam。
- 核心 integration seam 為「登入 → 雙語搜尋 → 選頁 → 開啟明細 → 修改克數 → 確認日期時間與餐別 → 建立 → 繼續新增或查看紀錄」。
- Food Search API 測試涵蓋登入、空白 query、指定語系、英文 fallback、雙語命中、大小寫、相關性順位、去重、穩定排序、分頁與輕量 response。
- Food Detail API 測試涵蓋登入、成功、404、無效 langCode、翻譯 fallback、英文副名稱去重、空 description、分類缺少、營養素 fallback、動態 nutrients 與 display order。
- Migration／model 測試驗證 displayOrder 必要性、預設 1000、基本 seed 順位與舊資料 upgrade。
- 所有營養素相關 API 測試驗證 displayOrder 與穩定排序。
- Flutter 搜尋測試涵蓋 debounce、鍵盤立即搜尋、舊 response 防護、空 query、頁碼、上／下一頁、直接選頁與返回狀態保存。
- 搜尋項目 Widget 測試涵蓋英文副標題去重、缺少英文名稱、food code 不顯示及缺少 Calories。
- 明細 Widget 測試涵蓋 loading、success、retry、404、authentication redirect、區塊隱藏、固定三大營養素、詳細資料與格式化。
- 克數測試涵蓋預設值、上下邊界、零、負數、超過上限、超過一位小數、即時換算及按鈕停用。
- 換算 expected value 使用獨立算例，不在 assertion 中複製 production algorithm。
- 確認流程測試驗證 food／quantityGrams 唯讀、返回修改、日期時間與餐別獨立、未來時間阻擋及成功 Dialog 分支。
- 所有 Daily Record 新增與編輯入口都加入回歸測試：切換餐別不改 consumedAt、修改 consumedAt 不改 mealTypeCode、凌晨可選晚餐、晚上可選早餐。
- 新表單初始化測試使用固定 clock 與 IANA timezone，驗證餐別只預選一次。
- 編輯測試驗證既有 consumedAt 與 mealTypeCode 分別初始化。
- 後端測試驗證任意合法 consumedAt 與啟用 mealTypeCode 組合都可建立／修改，未來時間仍被拒絕。
- Mutation 測試驗證 submitting 防重複、無自動 retry、失敗保留表單及食物失效處理。
- 時間測試使用固定 clock，至少涵蓋 Asia/Taipei 與具有 DST 轉換的 IANA timezone。
- 實作完成後執行後端完整測試，以及 Flutter format、analyze、test；影響 Web 畫面時執行 Flutter Web build。

## Out of Scope

- 食物圖片上傳、儲存與顯示。
- FoodPortion、份、碗、杯、顆、毫升、密度或其他非克數換算。
- 收藏、最近使用、食物比較與健康宣稱。
- 匿名食物查詢、公開分享、SEO 或公開 food detail contract。
- 正式食物說明、分類、翻譯與營養數值的大量補寫。
- 營養素、分類或食物內容管理後台。
- 營養素 display order 的管理 API 或 UI。
- 舊搜尋 response 的向後相容 endpoint 或版本化。
- 錯字容忍、拼音、同義詞、全文索引或模糊搜尋。
- 離線快取、離線 mutation queue、跨裝置衝突解決。
- Create idempotency key、自動 mutation retry 或 optimistic record creation。
- 歷史食物名稱與營養快照。

## Further Notes

- 本規格延伸前端 issue #8；該 issue 原先將 Food Detail 與完整動態營養 UI 列為範圍外，本規格正式將它們納入。
- 本規格覆寫 issue #8 的無限捲動決策，改為固定每頁 20 筆、上／下一頁與頁碼選擇器。
- 空白 query 應載入全部食物，不停止查詢。
- Issue #8 已描述日期、時間與餐別應獨立，但目前實作仍以餐別固定時刻建立 consumedAt；本規格將修正提升為所有 Daily Record 新增／編輯入口的必要驗收條件。
- 資料維護另列工作：盤點缺少的 `zh-TW`／`en-US` 名稱與說明、補齊英文翻譯、建立分類與翻譯、補齊食物分類關聯、檢查四個核心營養素、驗證 amountPer100Grams 與 unitCode、設定所有 displayOrder、檢查重複 code／翻譯、記錄資料來源、建立可重複匯入程序，以及正式更新前的備份、驗證與回復方式。
- 資料維護不阻擋功能實作；API 與 UI 必須能正確處理缺少說明、分類或個別翻譯的資料。
- 前端與後端 repository 必須同步本規格，避免 API 與 Flutter 決策漂移。
