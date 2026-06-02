namespace NpcMemoryService.Core.Storage
{
    public sealed class JsonFileStoreConfig
    {
        /// <summary>Directory where NPC JSON files are stored. Created on first save if absent.</summary>
        public required string Directory { get; init; }
    }
}
