using System;

namespace DeeceApi.Client.Models
{
    public struct SentObjectHeader
    {
        public int SizeInBytes { get; set; }
        public int ModelId { get; set; }
    }

    public class SentObject<T>
    {
        public int SizeInBytes { get; set; }
        public int ModelId { get; set; }
        public T Payload { get; set; }
    }

    public enum ModelId
    {
        JobRequest = 0,
        JobResponse = 1,
        FileRequest = 2,
        FileResponse = 3,
    }
}
