namespace OES.Helper.Dtos.Disabilities
{
    public class AddOrUpdateDisabilityDto
    {
        public long? Id { get; set; } = default;
        public string Name { get; set; }
        public string Description { get; set; }
        public float ExtraTimePercentage { get; set; }
    }
}
