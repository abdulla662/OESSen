using OES.Helper.Enums;

namespace OES.Core.Entities
{
    public class MediaSetting : BaseEntity<long>
    {
        // Properties

        public MediaCategory MediaCategory { get; set; }

        public int MaxSizeInKB { get; set; }


        // Domain Methods

        public MediaSetting() { }

        public MediaSetting(MediaCategory mediaCategory, int maxSizeInKB)
        {
            MediaCategory = mediaCategory;
            MaxSizeInKB = maxSizeInKB;
        }
    }
}
