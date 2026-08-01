using FoodLedger.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodLedger.Data.Configurations;

internal sealed class DefinedCodeTranslationConfiguration
    : IEntityTypeConfiguration<DefinedCodeTranslation>
{
    private static readonly DateTimeOffset SeededAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<DefinedCodeTranslation> entity)
    {
        entity.HasKey(translation => new
        {
            translation.CodeType,
            translation.Code,
            translation.LangCode,
        });

        entity.HasOne(translation => translation.DefinedCode)
            .WithMany(code => code.Translations)
            .HasForeignKey(translation => new
            {
                translation.CodeType,
                translation.Code,
            })
            .OnDelete(DeleteBehavior.Restrict);

        entity.ConfigureBaseEntity();

        entity.HasData(
            CreateMealType(
                "Breakfast",
                "zh-TW",
                "早餐",
                "通常於早晨或起床後食用的第一餐。"),
            CreateMealType(
                "Breakfast",
                "en-US",
                "Breakfast",
                "The first meal of the day, typically eaten in the morning."),
            CreateMealType(
                "Lunch",
                "zh-TW",
                "午餐",
                "通常於中午時段食用的正餐。"),
            CreateMealType(
                "Lunch",
                "en-US",
                "Lunch",
                "A meal typically eaten around midday."),
            CreateMealType(
                "Dinner",
                "zh-TW",
                "晚餐",
                "通常於傍晚或晚間食用的正餐。"),
            CreateMealType(
                "Dinner",
                "en-US",
                "Dinner",
                "A meal typically eaten in the evening."),
            CreateMealType(
                "Snack",
                "zh-TW",
                "點心",
                "在正餐之間食用的少量餐食。"),
            CreateMealType(
                "Snack",
                "en-US",
                "Snack",
                "A smaller meal or food eaten between main meals."),
            CreateTranslation(
                DefinedCodeTypes.FitnessGoal,
                "FAT_LOSS",
                "zh-TW",
                "減脂",
                "以降低體脂為目標，建議熱量設定低於維持需求。"),
            CreateTranslation(
                DefinedCodeTypes.FitnessGoal,
                "FAT_LOSS",
                "en-US",
                "Fat loss",
                "Targets body fat reduction with calories set below estimated maintenance."),
            CreateTranslation(
                DefinedCodeTypes.FitnessGoal,
                "MAINTAIN",
                "zh-TW",
                "維持體重",
                "以維持目前體重與身體組成為目標。"),
            CreateTranslation(
                DefinedCodeTypes.FitnessGoal,
                "MAINTAIN",
                "en-US",
                "Maintain",
                "Targets maintaining current body weight and composition."),
            CreateTranslation(
                DefinedCodeTypes.FitnessGoal,
                "MUSCLE_GAIN",
                "zh-TW",
                "增肌",
                "以增加肌肉量為目標，建議熱量設定高於維持需求。"),
            CreateTranslation(
                DefinedCodeTypes.FitnessGoal,
                "MUSCLE_GAIN",
                "en-US",
                "Muscle gain",
                "Targets muscle growth with calories set above estimated maintenance."),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "SEDENTARY",
                "zh-TW",
                "久坐",
                "日常以坐姿活動為主，幾乎沒有規律運動。"),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "SEDENTARY",
                "en-US",
                "Sedentary",
                "Mostly seated daily activity with little or no regular exercise."),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "LIGHT",
                "zh-TW",
                "輕度活動",
                "每週約一至三天輕度運動或日常活動量偏低。"),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "LIGHT",
                "en-US",
                "Lightly active",
                "Light exercise or activity about one to three days per week."),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "MODERATE",
                "zh-TW",
                "中度活動",
                "每週約三至五天中等強度運動。"),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "MODERATE",
                "en-US",
                "Moderately active",
                "Moderate exercise about three to five days per week."),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "HIGH",
                "zh-TW",
                "高度活動",
                "每週約六至七天高強度運動或體力活動。"),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "HIGH",
                "en-US",
                "Highly active",
                "Hard exercise or physical activity about six to seven days per week."),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "VERY_HIGH",
                "zh-TW",
                "極高活動",
                "每日進行高強度訓練，或從事高度體力需求的工作。"),
            CreateTranslation(
                DefinedCodeTypes.ActivityLevel,
                "VERY_HIGH",
                "en-US",
                "Very highly active",
                "Very hard daily training or a physically demanding occupation."));
    }

    private static DefinedCodeTranslation CreateMealType(
        string code,
        string langCode,
        string displayName,
        string note)
    {
        return CreateTranslation(
            DefinedCodeTypes.MealType,
            code,
            langCode,
            displayName,
            note);
    }

    private static DefinedCodeTranslation CreateTranslation(
        string codeType,
        string code,
        string langCode,
        string displayName,
        string note)
    {
        return new DefinedCodeTranslation
        {
            CodeType = codeType,
            Code = code,
            LangCode = langCode,
            DisplayName = displayName,
            Note = note,
            CreatedAt = SeededAt,
            CreatedBy = "Migration",
            ModifiedAt = SeededAt,
        };
    }
}
