BEGIN;

-- FoodLedger 基本營養素。
-- amount_per_100g 的數值單位由 nutrient.unit_code 決定。
INSERT INTO nutrient (
    nutrient_code,
    unit_code,
    display_order,
    created_at,
    created_by,
    modified_at,
    modified_by
)
VALUES
    ('Calories',       'kcal', 10,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Protein',        'g',    20,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Carbohydrates',  'g',    30,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Fat',            'g',    40,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Sodium',         'mg',   50,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('SaturatedFat',   'g',    60,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('DietaryFiber',   'g',    70,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Sugar',          'g',    80,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Cholesterol',    'mg',   90,  CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Potassium',      'mg',   100, CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Calcium',        'mg',   110, CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('Iron',           'mg',   120, CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('VitaminA',       'ug',   130, CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients'),
    ('VitaminC',       'mg',   140, CURRENT_TIMESTAMP, 'Seed.BasicNutrients', CURRENT_TIMESTAMP, 'Seed.BasicNutrients')
ON CONFLICT (nutrient_code) DO UPDATE
SET
    unit_code = EXCLUDED.unit_code,
    display_order = EXCLUDED.display_order,
    modified_at = CURRENT_TIMESTAMP,
    modified_by = 'Seed.BasicNutrients';

WITH translations(nutrient_code, lang_code, nutrient_name) AS (
    VALUES
        ('Calories',      'zh-TW', '熱量'),
        ('Calories',      'en-US', 'Calories'),
        ('Protein',       'zh-TW', '蛋白質'),
        ('Protein',       'en-US', 'Protein'),
        ('Carbohydrates', 'zh-TW', '碳水化合物'),
        ('Carbohydrates', 'en-US', 'Carbohydrates'),
        ('Fat',           'zh-TW', '脂肪'),
        ('Fat',           'en-US', 'Fat'),
        ('SaturatedFat',  'zh-TW', '飽和脂肪'),
        ('SaturatedFat',  'en-US', 'Saturated Fat'),
        ('Sugar',         'zh-TW', '糖'),
        ('Sugar',         'en-US', 'Sugar'),
        ('DietaryFiber',  'zh-TW', '膳食纖維'),
        ('DietaryFiber',  'en-US', 'Dietary Fiber'),
        ('Sodium',        'zh-TW', '鈉'),
        ('Sodium',        'en-US', 'Sodium'),
        ('Potassium',     'zh-TW', '鉀'),
        ('Potassium',     'en-US', 'Potassium'),
        ('Calcium',       'zh-TW', '鈣'),
        ('Calcium',       'en-US', 'Calcium'),
        ('Iron',          'zh-TW', '鐵'),
        ('Iron',          'en-US', 'Iron'),
        ('Cholesterol',   'zh-TW', '膽固醇'),
        ('Cholesterol',   'en-US', 'Cholesterol'),
        ('VitaminA',      'zh-TW', '維生素 A'),
        ('VitaminA',      'en-US', 'Vitamin A'),
        ('VitaminC',      'zh-TW', '維生素 C'),
        ('VitaminC',      'en-US', 'Vitamin C')
)
INSERT INTO nutrient_translation (
    nutrient_id,
    lang_code,
    nutrient_name,
    created_at,
    created_by,
    modified_at,
    modified_by
)
SELECT
    nutrient.nutrient_id,
    translations.lang_code,
    translations.nutrient_name,
    CURRENT_TIMESTAMP,
    'Seed.BasicNutrients',
    CURRENT_TIMESTAMP,
    'Seed.BasicNutrients'
FROM translations
JOIN nutrient
    ON nutrient.nutrient_code = translations.nutrient_code
ON CONFLICT (nutrient_id, lang_code) DO UPDATE
SET
    nutrient_name = EXCLUDED.nutrient_name,
    modified_at = CURRENT_TIMESTAMP,
    modified_by = 'Seed.BasicNutrients';

COMMIT;

-- 執行結果確認。
SELECT
    nutrient.nutrient_id,
    nutrient.nutrient_code,
    nutrient.unit_code,
    nutrient.display_order,
    translation.lang_code,
    translation.nutrient_name
FROM nutrient
JOIN nutrient_translation AS translation
    ON translation.nutrient_id = nutrient.nutrient_id
WHERE nutrient.nutrient_code IN (
    'Calories',
    'Protein',
    'Carbohydrates',
    'Fat',
    'SaturatedFat',
    'Sugar',
    'DietaryFiber',
    'Sodium',
    'Potassium',
    'Calcium',
    'Iron',
    'Cholesterol',
    'VitaminA',
    'VitaminC'
)
ORDER BY nutrient.nutrient_id, translation.lang_code;
