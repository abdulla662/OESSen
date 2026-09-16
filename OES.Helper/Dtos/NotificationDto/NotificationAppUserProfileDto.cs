using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.NotificationDto
{
    public class NotificationAppUserProfileDto
    {
        public long NotificationId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreationDate { get; set; }
        public NotificationDto Notification { get; set; } = new();
    }
}
