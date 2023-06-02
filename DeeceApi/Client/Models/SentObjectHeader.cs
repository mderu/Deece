using System;

namespace DeeceApi.Client.Models
{
    [Serializable]
    public struct SentObjectHeader
    {
        public int SizeInBytes { get; set; }
        public int ModelId { get; set; }
    }

    [Serializable]
    public enum ModelId
    {
        JobRequest = 0,
        JobResponse = 1,
        FileRequest = 2,
        FileResponse = 3,
    }
}
