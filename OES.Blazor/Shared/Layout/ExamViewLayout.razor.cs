using MudBlazor;
using MudBlazor.Utilities;
using SharedHelper.General;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Blazored.LocalStorage;

namespace OES.Blazor.Shared.Layout
{
    public partial class ExamViewLayout
    {
        [Inject] private ILocalStorageService LocalStorage { get; set; }

        private static bool RightToLeft => CultureInfo.CurrentCulture.Name == LanguageCode.ARABIC_CODE;

        protected override async Task OnInitializedAsync()
        {
            var savedTheme = await LocalStorage.GetItemAsStringAsync("OEStheme");
            if (!string.IsNullOrEmpty(savedTheme))
            {
                SharedTheme.IsDarkMode = savedTheme == "Dark";
            }
        }
    }

    public static class SharedTheme
    {
        public static bool IsDarkMode { get; set; }


        // Light Mode Parameters
        private static readonly MudColor CesPrimary = new("#039d8f");
        private static readonly MudColor CesSecondary = new("#7785AC");
        private static readonly MudColor CesTertiary = new("#108a00");
        private static readonly MudColor CesWarning = new("#ec6313");
        private static readonly MudColor CesError = new("#f44336");
        private static readonly MudColor CesBarBackground = new("#f3ecf9");
        private static readonly MudColor CesSuccessText = new("#478c5c");
        private static readonly MudColor CesDrawerBackground = new("#f3ecf9");
        private static readonly MudColor CesBackground = new("#ffffff");
        private static readonly double CesHoverOpacity = 0.06;
        private static readonly MudColor CesFoorterBackground = new("#f5f5f5");


        // Dark Mode Parameters
        private static readonly MudColor CesPrimaryDark = new("#039d8f");
        private static readonly MudColor CesSecondaryDark = new("#9BA8CC");
        private static readonly MudColor CesTertiaryDark = new("#4CAF50");
        private static readonly MudColor CesWarningDark = new("#FF8A50");
        private static readonly MudColor CesErrorDark = new("#FF6B6B");
        private static readonly MudColor CesBarBackgroundDark = new("#1E1E2E");
        private static readonly MudColor CesSuccessTextDark = new("#81C995");
        private static readonly MudColor CesDrawerBackgroundDark = new("#252538");
        private static readonly MudColor CesBackgroundDark = new("#2D2D45");
        private static readonly double CesHoverOpacityDark = 0.08;
        private static readonly MudColor CesTextParagraphDark = new("#E0E0E0");
        private static readonly MudColor CesTextSecondaryDark = new("#EBF1FF");
        private static readonly MudColor CesFoorterBackgroundDark = new("#181825");


        // Common Parameters
        private static readonly MudColor CesLightText = new("#f5f5f5");


        // Single theme with both light and dark palettes
        public static readonly MudTheme OesTheme = new()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = CesPrimary,
                Secondary = CesSecondary,
                Tertiary = CesTertiary,
                Success = CesSuccessText,
                Error = CesError,
                Warning = CesWarning,
                TextPrimary = CesPrimary,
                TextSecondary = CesSecondary,
                AppbarBackground = CesBarBackground,
                DrawerBackground = CesDrawerBackground,
                DrawerText = CesSecondary,
                HoverOpacity = CesHoverOpacity,
                Background = CesBackground,
                Surface = CesFoorterBackground,
                AppbarText = CesLightText
            },
            PaletteDark = new PaletteDark()
            {
                Primary = CesPrimaryDark,
                Secondary = CesSecondaryDark,
                Tertiary = CesTertiaryDark,
                Success = CesSuccessTextDark,
                Error = CesErrorDark,
                Warning = CesWarningDark,
                TextPrimary = CesTextParagraphDark,
                TextSecondary = CesTextSecondaryDark,
                AppbarBackground = CesBarBackgroundDark,
                DrawerBackground = CesDrawerBackgroundDark,
                DrawerText = CesTextParagraphDark,
                HoverOpacity = CesHoverOpacityDark,
                Background = CesBackgroundDark,
                Surface = CesFoorterBackgroundDark,
                AppbarText = CesLightText
            },
            LayoutProperties = new LayoutProperties()
            {
                DrawerWidthLeft = "260px",
                DrawerWidthRight = "300px"
            }
        };

        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }
    }
}
