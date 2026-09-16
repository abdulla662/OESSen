namespace OES.Helper.Dtos.User
{
    public class BlazUserDTO : IEquatable<BlazUserDTO>
    {
        public Guid ID { get; set; }

        public string Name { get; set; }

        public bool Equals(BlazUserDTO other) => other is not null && ID == other.ID;

        public override bool Equals(object obj) => Equals(obj as BlazUserDTO);

        public override int GetHashCode() => ID.GetHashCode();
    }
}
