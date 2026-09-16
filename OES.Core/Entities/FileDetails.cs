using System.ComponentModel.DataAnnotations;

namespace OES.Core.Entities
{
    public class FileDetails : BaseEntity<long>
    {
        public string FileName { get; set; }
        [Required]
        public string FileType { get; set; }
        public string Authors { get; set; }
        public long? Pages { get; set; }
        public long? WordCount { get; set; }
        public long Size { get; set; }
        public string Title { get; set; }
        public string LastSavedBy { get; set; }
        public long RevisionNumber { get; set; }
        public string VersionNumber { get; set; }
        public long? CharacterCount { get; set; }
        public long? LineCount { get; set; }
        public long? ParagraphCount { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime? DateAccessed { get; set; }
    }
}
