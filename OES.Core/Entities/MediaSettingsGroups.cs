using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class MediaSettingsGroups : BaseEntity<long>
    {
        public Guid OESGroupId { get; set; }

        public long MediaConfigurationId { get; set; }

        [ForeignKey(nameof(OESGroupId))]
        public virtual OESGroup OESGroup { get; set; }

        [ForeignKey(nameof(MediaConfigurationId))]
        public virtual MediaSetting MediaConfiguration { get; set; }
    }
}
