using OES.Helper.Dtos.Question.WebSearchQuestionDtos;

namespace OES.Blazor.Pages.QuestionLayout.WebSearchLayout
{
    public static class WebSearchDummyData
    {
        public static readonly List<WebSearchResultDto> _dummyPool = new()
        {
            new("Facebook – Log in or Sign Up", "www.facebook.com", "Connect with friends and the world around you on Facebook.", false, 1, true),
            new("Wikipedia – The Free Encyclopedia", "en.wikipedia.org", "Wikipedia is a free online encyclopedia, created and edited by volunteers around the world.", false, 2, true),
            new("Financial Times – Business & Finance News", "www.ft.com", "Latest international business, finance, economic news, and analysis from the Financial Times.", false, 3, true),
            new("BBC News – Breaking News, World & UK News", "www.bbc.com/news", "Visit BBC News for up-to-the-minute news, breaking news, video, audio and feature stories.", false, 4, true),
            new("YouTube", "www.youtube.com", "Enjoy the videos and music you love, upload original content and share it all with friends.", false, 5, true),
            new("Amazon – Online Shopping for Electronics, Apparel & More", "www.amazon.com", "Free delivery on millions of items with Prime. Low prices across earth's biggest selection.", false, 6, true),
            new("X (formerly Twitter) – What's happening", "twitter.com", "From breaking news and entertainment to sports and politics, get the full story with all the live commentary.", false, 7, true),
            new("LinkedIn – Professional Network", "www.linkedin.com", "Manage your professional identity, build and engage with your professional network.", false, 8, true),
        };

        public static readonly List<WebSearchResultDto> _dummyPoolAr = new()
        {
            new("فيسبوك - تسجيل الدخول أو الاشتراك", "www.facebook.com", "قم بتسجيل الدخول إلى فيسبوك لبدء المشاركة والتواصل مع أصدقائك وعائلتك والأشخاص الذين تعرفهم.", false, 1, true),
            new("ويكيبيديا، الموسوعة الحرة", "ar.wikipedia.org", "ويكيبيديا هي مشروع موسوعة متعددة اللغات، مبنية على الويب، ذات محتوى حر، مشاع ومفتوح للجميع.", false, 2, true),
            new("الجزيرة.نت - أخبار، تحليلات، عاجل", "www.aljazeera.net", "شبكة الجزيرة الإعلامية، أخبار العالم، الشرق الأوسط، المغرب العربي، تحليلات سياسية واقتصادية.", false, 3, true),
            new("اليوم السابع - أخبار مصر والعالم", "www.youm7.com", "جريدة اليوم السابع تقدم أخبار مصر والعالم على مدار الساعة، وتغطية شاملة لجميع الأحداث.", false, 4, true),
            new("يلا كورة - أخبار الرياضة والمباريات", "www.yallakora.com", "يلا كورة الموقع الرياضي الأول في الشرق الأوسط، بث مباشر، نتائج المباريات، وأخبار الرياضة.", false, 5, true),
            new("في الجول - FilGoal | أخبار كرة القدم", "www.filgoal.com", "في الجول موقع رياضي مصري وعربي يقدم تغطية حية للمباريات، أخبار اللاعبين، والتحليلات.", false, 6, true),
            new("كووورة: الموقع الرياضي العربي الأول", "www.kooora.com", "موقع كووورة العربي الرياضي الأول، يغطي جميع البطولات العربية والعالمية.", false, 7, true),
            new("جوجل - محرك البحث العالمي", "www.google.com", "ابحث في معلومات العالم، بما في ذلك صفحات الويب والصور ومقاطع الفيديو والمزيد.", false, 8, true),
            new("يوتيوب - استمتع بمقاطع الفيديو والموسيقى", "www.youtube.com", "استمتع بمقاطع الفيديو والموسيقى التي تحبها، وحمّل المحتوى الأصلي وشاركه مع الجميع.", false, 9, true)
        };

        public static List<WebSearchResultDto> GetDummyPool(bool isArabic)
        {
            return isArabic ? _dummyPoolAr : _dummyPool;
        }
    }
}
