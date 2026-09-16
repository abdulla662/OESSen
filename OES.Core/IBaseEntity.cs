namespace OES.Core
{
    public interface IBaseEntity<T> where T : struct
    {
        public T Id { get; set; }
        public string CreationUser { get; set; }
        public string? ModeficationUser { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime? ModeficationDate { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
