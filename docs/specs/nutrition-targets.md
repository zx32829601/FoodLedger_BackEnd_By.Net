# 建議攝取量與身體測量規格

## Problem Statement

FoodLedger 已能記錄每日飲食並統計實際攝取的營養素，但使用者仍缺少一組可依個人身體狀況、活動程度與健身目標產生的每日參考目標。因此，Nutrition Summary 目前只能回答「吃了多少」，無法回答「與自己的目標相比還差多少」。

使用者也需要保存身體測量歷史，並了解每次建議攝取量是根據哪些資料與公式產生。若只保存最新結果，日後公式或身體資料改變時，過去結果將無法解釋；若在每次資料異動時暗中重算，又會讓使用者無法判斷何時建立了新目標。

本功能提供一般性的估算值，不作為醫療診斷、疾病飲食處方或專業營養建議。

## Solution

建立由 Body Profile、Body Measurement 與 Nutrition Target Snapshot 組成的建議攝取量模組。

使用者先建立身體基本資料與身體測量，再明確要求系統計算每日基礎代謝量、維持熱量、目標熱量，以及蛋白質、脂肪與碳水化合物建議量。每次有效計算會保存完整有效輸入、實際係數、公式代碼與政策版本，讓目前結果與歷史結果都可追蹤及顯示。

Profile 或新測量異動不會暗中建立 Snapshot。前端可提供預先勾選的「儲存後立即計算」，但計算仍是使用者明確選擇。修改已被 Snapshot 引用的測量則視為修正原始紀錄，系統會在單一交易內逐筆重算相關歷史結果。

Nutrition Summary 依查詢日期選擇當日最後生效的 Nutrition Target，向使用者呈現實際攝取量與當時建議值的差異。

## User Stories

1. As a user, I want to create my body profile, so that the system can calculate a personalized nutrition target.
2. As a user, I want to enter my birth date, biological sex, and height, so that the system can estimate my resting calories.
3. As a user, I want to select my current activity level, so that the system can estimate my maintenance calories.
4. As a user, I want to select fat loss, maintenance, or muscle gain, so that the system can adjust my target calories.
5. As a user, I want my device timezone to be prefilled and editable, so that dates and age are evaluated in my local calendar.
6. As a user, I want an empty-state hint and a create-now action when no body profile exists, so that I know how to begin.
7. As a user, I want body profile setup on a dedicated page, so that I can understand and validate all required fields.
8. As a user, I want to add my current weight, so that the target reflects my latest body state.
9. As a user, I want to optionally record body fat percentage and muscle mass, so that I can preserve more complete measurement history.
10. As a user, I want measurement time to be recorded automatically, so that I do not accidentally create future measurements.
11. As a user, I want to view paged body measurement history, so that I can understand how my body data changed.
12. As a user, I want measurement history filtered by my local date range, so that results match the date rules used elsewhere in FoodLedger.
13. As a user, I want to correct an existing measurement, so that targets derived from incorrect data can be recalculated.
14. As a user, I want to preview the impact of deleting a measurement, so that I know how many target snapshots will also be deleted.
15. As a user, I want deletion to stop when the impact changes after preview, so that the system never deletes more data than I confirmed.
16. As a user, I want the newest effective measurement to be selected deterministically, so that repeated calculations use the same source data.
17. As a user, I want body fat percentage to enable a fat-free-mass formula, so that the system can use the more relevant available input.
18. As a user, I want a fallback formula when body fat percentage is absent, so that I can still receive an estimate.
19. As a user, I want activity and fitness-goal coefficients to be versioned, so that policy changes do not make past results ambiguous.
20. As a user, I want to explicitly recalculate my target, so that a new target is not created without my intent.
21. As a user, I want an optional save-and-calculate action after profile or measurement creation, so that common setup remains convenient.
22. As a user, I want duplicate recalculation requests to return the existing result, so that retries do not clutter my history.
23. As a user, I want to see resting, maintenance, and target calories, so that I understand each stage of the estimate.
24. As a user, I want to see protein, fat, and carbohydrate targets, so that I can plan daily food choices.
25. As a user, I want to see the full inputs used for a target, so that I can understand why the result was produced.
26. As a user, I want muscle mass preserved in target history even when V1 does not use it, so that the historical body state remains useful.
27. As a user, I want to know which formula and policy version were used, so that a result remains explainable after policy changes.
28. As a user, I want calculation and effective times separated, so that correcting a result does not move it to another historical day.
29. As a user, I want to view paged target history and target details, so that I can review how recommendations changed.
30. As a user, I want an old target to remain visible when my current data changes, so that the page does not lose useful information.
31. As a user, I want stale targets clearly marked with actionable reasons, so that I know when recalculation is appropriate.
32. As a user, I do not want a target to become stale merely because a new calendar day began, so that unchanged inputs remain usable.
33. As a user, I want my target to become stale when my age changes on my birthday, so that age-dependent calculations can be refreshed.
34. As a user, I want disabled activity or goal options to remain readable in history, so that old results preserve their meaning.
35. As a user, I want a clear not-calculated state when my inputs are complete but no target exists, so that I can calculate immediately.
36. As a user, I want precise missing-field guidance when calculation inputs are incomplete, so that I can fix the correct data.
37. As a user, I want a failed combined save-and-calculate operation to preserve valid new profile or measurement data, so that I do not re-enter it.
38. As a user, I want a correction of an already-used measurement to roll back entirely when historical recalculation fails, so that source data and derived history cannot diverge.
39. As a user, I want Nutrition Summary to compare actual intake with the target effective on that date, so that history does not use my current target retroactively.
40. As a user, I want target results labeled as estimates, so that I do not mistake them for medical advice.
41. As a user, I want localized fitness goals and activity descriptions, so that the options are understandable in my selected language.
42. As a user, I want confirmation before deleting a Daily Record, Body Measurement, or admin-managed food, so that accidental destructive actions are less likely.
43. As a user, I want confirmation before logout, so that I do not end my session accidentally.
44. As a user, I want confirmation dialogs localized consistently, so that destructive actions use the same language as the rest of the app.
45. As a user, I want concurrent edits detected, so that another device cannot silently overwrite newer profile or measurement data.
46. As a backend maintainer, I want formulas outside controllers, so that calculation policy remains testable and versioned.
47. As a backend maintainer, I want immutable target identity and explicit correction semantics, so that ordinary edits cannot silently rewrite historical meaning.
48. As a frontend maintainer, I want one reusable confirmation dialog boundary, so that delete and logout interactions remain consistent.
49. As a frontend maintainer, I want server-provided deletion impact for cascading deletes, so that the client does not reproduce relationship rules.
50. As an API consumer, I want stable error codes, field errors, missing fields, and trace IDs, so that failures can be presented and diagnosed consistently.

## Implementation Decisions

### Domain boundaries

- Body Profile stores the current personal inputs used by future calculations: birth date, biological sex code, height, fitness goal code, activity level code, and IANA timezone ID.
- There is at most one Body Profile per user.
- `GET /api/me/body-profile` returns `404 BodyProfile.NotFound` when no profile exists.
- `PUT /api/me/body-profile` creates or updates the current user's profile.
- Profile updates affect only future Snapshots and never rewrite historical Snapshots.
- Body Measurement stores weight, optional body fat percentage, optional muscle mass, server-generated measurement time, and concurrency version.
- Measurement time is generated by the backend as the current UTC instant. V1 does not accept backdated or user-edited measurement times.
- Recalculation selects the latest measurement whose measurement time is not later than the calculation instant, ordered by measurement time, creation time, and ID descending.
- Nutrition Target Snapshot stores the calculation result and the complete effective inputs needed to understand it.
- Snapshot input includes weight, optional body fat percentage, optional muscle mass, height, age at calculation, biological sex code, timezone ID, local calculation date, goal code, activity code, and all applied coefficients.
- Snapshot does not duplicate birth date; age at calculation is the formula input.
- Muscle mass is preserved for history but is explicitly not used by V1 calculations.
- Snapshot stores the source measurement ID, resting formula code, calculation policy version, effective time, latest calculation time, result values, and an input fingerprint.
- Effective time is assigned when the Snapshot is first created and does not change when a correction causes recalculation.
- Latest calculation time changes whenever a Snapshot is recalculated.
- Snapshot records are not directly editable or individually deletable by users.

### Supported inputs and validation

- V1 supports adult users from 18 through 120 years old, inclusive.
- Age is calculated from the user's local date in the configured IANA timezone.
- V1 biological sex codes are `MALE` and `FEMALE`.
- Height is required and must be from 100 through 250 centimeters.
- Weight is required and must be from 20 through 400 kilograms.
- Body fat percentage is optional and must be from 2 through 70 percent when supplied.
- Muscle mass is optional, must be greater than zero when supplied, and must not exceed weight.
- Boundaries are inclusive.
- Validation messages describe the product-supported range and do not claim medical normality.
- Timezone IDs must be valid IANA identifiers; invalid values do not silently fall back to UTC.
- Fitness goal and activity level must reference active DefinedCodes when creating or updating current data.

### DefinedCode and localization

- Add `FITNESS_GOAL` codes `FAT_LOSS`, `MAINTAIN`, and `MUSCLE_GAIN`.
- Add `ACTIVITY_LEVEL` codes `SEDENTARY`, `LIGHT`, `MODERATE`, `HIGH`, and `VERY_HIGH`.
- DefinedCode display name and Note move to a translation child model keyed uniquely by DefinedCode and BCP 47 language code.
- V1 seeds `zh-TW` and `en-US` translations.
- DefinedCode APIs accept `langCode`, prefer the requested language, and fall back to `en-US` using the existing localization rules.
- Note contains localized explanatory text for users only.
- Calculation logic never parses Note or display text.
- DefinedCodes that have been used cannot be physically deleted; they can only be deactivated.
- Inactive codes remain readable for Profile and Snapshot history, but cannot be used for new current selections or recalculation.

### Calculation policy V1

- When body fat percentage is available, resting calories use the Katch–McArdle calculation:
  - Fat-free mass equals weight multiplied by one minus body fat percentage divided by 100.
  - Resting calories equal 370 plus 21.6 multiplied by fat-free mass.
- Without body fat percentage, resting calories use Mifflin–St Jeor.
- For `MALE`, resting calories equal 10 times weight plus 6.25 times height minus 5 times age plus 5.
- For `FEMALE`, resting calories equal 10 times weight plus 6.25 times height minus 5 times age minus 161.
- Activity factors are `SEDENTARY = 1.20`, `LIGHT = 1.375`, `MODERATE = 1.55`, `HIGH = 1.725`, and `VERY_HIGH = 1.90`.
- Maintenance calories equal resting calories multiplied by the activity factor.
- Goal factors are `FAT_LOSS = 0.85`, `MAINTAIN = 1.00`, and `MUSCLE_GAIN = 1.08`.
- Target calories equal maintenance calories multiplied by the goal factor.
- Protein factors in grams per kilogram are `FAT_LOSS = 1.8`, `MAINTAIN = 1.4`, and `MUSCLE_GAIN = 1.7`.
- Protein grams equal weight multiplied by the protein factor.
- Fat calories equal target calories multiplied by 0.25; fat grams equal fat calories divided by 9.
- Protein calories equal protein grams multiplied by 4.
- Carbohydrate calories equal target calories minus protein calories minus fat calories; carbohydrate grams equal carbohydrate calories divided by 4.
- All calculation values use decimal arithmetic.
- Intermediate calculations are not rounded.
- Final calories and macronutrient grams are rounded to two decimal places with midpoint values rounded away from zero.
- If any final nutrient amount is negative, calculation fails with `NutritionTarget.Uncalculable` and no new Snapshot is created.
- `RestingFormulaCode` distinguishes `KATCH_MCARDLE` and `MIFFLIN_ST_JEOR`.
- `CalculationPolicyVersion` identifies the complete coefficient set and calculation order, beginning with `NUTRITION_TARGET_V1`.
- Any coefficient or calculation-order change creates a new policy version rather than changing V1.
- Historical policy implementations remain available when a corrected measurement requires recalculating a historical Snapshot.

### Snapshot lifecycle and stale state

- `POST /api/me/nutrition-target/recalculate` is the explicit recalculation command.
- Profile changes and newly created measurements do not independently create a Snapshot.
- The UI may submit an explicit save-and-calculate choice after Profile or Measurement creation.
- A valid Profile or new Measurement remains saved when the optional follow-up calculation fails.
- Repeated recalculation with the same effective inputs, coefficients, and policy version returns the existing latest Snapshot with `created = false`.
- Recalculation supports an Idempotency-Key so transport retries cannot create duplicate Snapshots.
- Snapshot history is ordered by effective time and ID descending.
- The current target is the remaining Snapshot with the latest effective time.
- Stale state is derived from current effective input, active-code state, current age, latest eligible measurement, and current calculation policy.
- Stale state does not preserve past events and does not become true merely because the local date changed.
- A birthday that changes age makes the current Snapshot stale with `AGE_CHANGED`.
- Profile input, newer measurement, inactive code, or policy changes produce stable, actionable stale reason codes.
- If the latest Snapshot is cascade-deleted, the next remaining Snapshot becomes current and is evaluated normally against current inputs.
- Current-target responses include the Snapshot, `isStale`, and `staleReasons`.

### Measurement correction and deletion

- Updating an unused Measurement changes only that Measurement.
- Updating a Measurement referenced by Snapshots is an explicit correction operation.
- In one database transaction, the Measurement is updated and every referencing Snapshot is recalculated with its original Profile inputs, goal, activity, formula policy, and effective time, but with the corrected measurement values.
- Corrected Snapshots keep their identity and effective time, replace the previous derived values, and update latest calculation time.
- V1 does not preserve the superseded derived values as revision history.
- If any historical recalculation fails, the entire correction transaction rolls back.
- Deleting a Measurement physically deletes it and all referencing Snapshots.
- `GET /api/me/body-measurements/{id}/deletion-impact` returns the owned Measurement's concurrency version, Snapshot count, whether the current target is affected, and a short-lived impact token.
- Delete requires the current concurrency version and impact token.
- The backend re-evaluates ownership, version, and cascade impact before deletion.
- If the impact differs from the confirmed preview, delete returns `409 Conflict` and nothing is deleted.
- Deletion is completed in one database transaction.
- Querying, updating, previewing, or deleting another user's Measurement returns Not Found.

### API surface

- Body Profile:
  - `GET /api/me/body-profile`
  - `PUT /api/me/body-profile`
- Body Measurements:
  - `GET /api/me/body-measurements`
  - `POST /api/me/body-measurements`
  - `PUT /api/me/body-measurements/{id}`
  - `GET /api/me/body-measurements/{id}/deletion-impact`
  - `DELETE /api/me/body-measurements/{id}`
- Nutrition Targets:
  - `GET /api/me/nutrition-target`
  - `POST /api/me/nutrition-target/recalculate`
  - `GET /api/me/nutrition-targets`
  - `GET /api/me/nutrition-targets/{snapshotId}`
- History APIs use the existing page and page-size contract: page defaults to 1, page size defaults to 20, and page size is at most 100.
- Optional `fromDate` and `toDate` values are interpreted in the Profile's IANA timezone.
- Date filtering uses the same local-date-to-UTC half-open interval behavior as DailyRecord and Nutrition Summary.
- History users cannot supply an arbitrary timezone that changes Profile date ownership.
- All endpoints derive user ownership from the authenticated principal and never accept a user ID from the client.
- Data format and range errors return `400`.
- Missing Profile fields, missing Measurement, inactive current codes, or otherwise insufficient calculation state return `422`.
- A complete state with no previously calculated target returns `404 NutritionTarget.NotCalculated` with `canRecalculate = true`.
- Empty history queries return `200` with an empty page.
- Version or impact-token conflicts return `409`.
- Errors follow the existing code-first response with stable code, fallback message, field errors, parameters, missing fields when relevant, and trace ID.

### Nutrition Summary integration

- Nutrition Summary remains responsible for actual intake aggregation; Nutrition Target remains responsible for recommended values.
- The two modules interact through a query boundary and do not directly share calculation services.
- For a past local date, Nutrition Summary selects the latest Snapshot effective before that local day's exclusive end.
- For the current local date, it selects the latest Snapshot effective no later than the query instant.
- A date before the user's first Snapshot returns actual intake without a target.
- Current Profile values are never applied retroactively to dates before their Snapshot effective time.
- Nutrition Summary may expose actual amount, target amount, and difference for matching stable nutrient codes without recalculating either side in the client.

### Frontend interaction

- A missing Profile produces an explanatory empty state with a create-now button.
- The create-now button navigates to a dedicated Body Profile setup page.
- Device IANA timezone is prefilled when available and remains editable.
- After Profile creation, the target page prompts for the first Body Measurement when none exists.
- New Measurement and Profile edit flows offer a preselected save-and-calculate choice when calculation prerequisites exist.
- A failed optional calculation keeps valid saved input and presents the calculation failure separately.
- Current target, stale warning, stale reasons, estimate label, and recalculate action are visually distinct.
- Target history and Measurement history support paging and local date filters.
- Snapshot detail shows all stored effective inputs, coefficients, formula codes, versions, effective time, and latest calculation time.
- Estimate views include a localized non-medical disclaimer without requiring repeated consent.
- A reusable localized confirmation dialog is used for permanent deletes and logout.
- Daily Record delete confirmation identifies the food and time.
- Measurement delete confirmation displays server-provided cascade impact.
- Admin Food delete uses impact preview when related data can be removed or invalidated.
- Logout uses session-ending language rather than deletion language.
- Confirmation actions cannot be submitted twice and remain cancel-first interactions.
- Frontend uses server-provided localized DefinedCode display names and Notes rather than hardcoded labels.

### Concurrency

- Body Profile and Body Measurement expose a version value.
- Update and delete commands require the version last read by the client.
- Conflicting writes return `409 Conflict` and require refresh.
- Combined Measurement correction and historical recalculation use one transaction.
- Impact tokens are scoped to the authenticated user, target Measurement, expected version, expected impact, and a short expiry.

## Testing Decisions

- Tests verify externally observable behavior and avoid asserting private helper structure.
- The primary feature seam is the authenticated HTTP API covering Profile, Measurement, deletion impact, recalculation, current target, target history, localization, errors, ownership, and concurrency.
- A focused pure calculation-policy seam verifies deterministic formulas and versioned coefficient behavior without HTTP or persistence.
- PostgreSQL integration tests cover transactions, cascade deletion, optimistic concurrency, unique constraints, impact-token race protection, decimal precision, and historical correction across multiple Snapshots.
- Formula tests cover both resting formulas, all activity factors, all goal factors, all protein factors, exact macro conversion, final rounding, determinism, and negative-result rejection.
- Age tests use an injected clock and IANA timezone and cover the day before, on, and after a birthday.
- Timezone tests reuse the existing local date range behavior and cover Asia/Taipei plus a DST-changing IANA timezone.
- Validation tests cover every inclusive boundary and values immediately outside each supported range.
- API tests cover unauthenticated access, per-user isolation, Not Found masking, `400`, `404`, `409`, `422`, empty history, stable ordering, and trace ID presence.
- Idempotency tests cover repeated same-payload requests, concurrent requests, response-loss retries, and key reuse with different inputs.
- Stale-state tests cover unchanged input, birthday change, Profile change, newer Measurement, inactive DefinedCode, policy change, deletion fallback, and cross-midnight stability.
- Measurement correction tests cover multiple linked Snapshots, original effective-time preservation, original policy reuse, successful atomic replacement, and full rollback when one historical calculation fails.
- Deletion tests verify preview counts, current-target impact, changed-impact conflict, physical cascade, and isolation from other users.
- DefinedCode tests cover requested language, `en-US` fallback, localized Note, active filtering for current selection, inactive historical readability, and prohibition of physical deletion.
- Nutrition Summary integration tests cover dates before the first target, past-day target selection, current-day cutoff, stale target representation, and no retroactive use of current Profile.
- Frontend repository and state tests use fake API boundaries to verify DTO mapping, error mapping, paging, stale state, impact preview, conflict refresh, and save-and-calculate partial success.
- Widget tests cover Profile and Measurement empty states, dedicated setup navigation, validation, estimate disclaimer, stale warning, target history, deletion impact confirmation, Daily Record deletion confirmation, Admin Food confirmation, and logout confirmation.
- The highest end-to-end seam is: authenticate, create Profile, create Measurement, explicitly calculate, view current target, view Nutrition Summary comparison, edit Measurement and observe historical recalculation, preview deletion, confirm deletion, and observe target fallback.
- Existing Auth API, DailyRecord API, Nutrition Summary API, localization, local date range, repository, provider, and widget tests are prior art and should be extended rather than duplicated.

## Out of Scope

- Medical diagnosis or treatment advice.
- Disease-specific calculations.
- Pregnancy, breastfeeding, pediatric, geriatric-specialist, or clinical nutrition formulas.
- Automatic meal-plan generation.
- AI-generated recommendations or automatic coefficient changes.
- Wearable-device integration.
- Real-time exercise calorie adjustment.
- Manual backdating or editing of Measurement time.
- Body Measurement import.
- Soft deletion of Measurements.
- Direct editing or individual deletion of Nutrition Target Snapshots.
- Revision history for derived values replaced by a Measurement correction.
- User-configurable formula coefficients.
- DefinedCode management UI beyond the localization and lifecycle requirements needed by this feature.
- Monthly or yearly target trend APIs.
- Offline mutation queues or conflict merging.
- Repeated legal-consent capture for the estimate disclaimer.

## Further Notes

- Nutrition Target Snapshot is distinct from Nutrition Summary's live nutrient aggregation. The first records versioned recommendations; the second calculates actual intake from Daily Records.
- DefinedCode Note is explanatory content only and must never become an untyped configuration channel.
- When policy V2 is introduced, V1 must remain executable for correction of V1 Snapshots.
- Recommended delivery order within this specification:
  1. Expand localized DefinedCode and establish the shared confirmation-dialog contract.
  2. Deliver Body Profile as an authenticated vertical slice.
  3. Deliver Body Measurement history, correction, concurrency, and deletion impact as a vertical slice.
  4. Deliver the calculation policy, Snapshot lifecycle, current target, and target history.
  5. Integrate Nutrition Summary actual-versus-target comparison.
- The localized DefinedCode and shared confirmation foundations should also close the remaining hardcoded MealType, Daily Record delete-confirmation, Admin Food confirmation consistency, and logout-confirmation gaps in the current Flutter client.
