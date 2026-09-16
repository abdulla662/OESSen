namespace OES.Helper.General
{
    public sealed record CTRExamSettings
    {
        public string ExportFolderPath { get; init; }
        public string NetworkUsername { get; init; } 
        public string NetworkPassword { get; init; } 
        public string NetworkDomain { get; init; } 
    }
}
