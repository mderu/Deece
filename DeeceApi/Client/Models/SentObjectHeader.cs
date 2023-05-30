using System;

namespace DeeceApi.Client.Models
{
    public struct SentObjectHeader
    {
        public int SizeInBytes { get; set; }
        public int ModelId { get; set; }

        public static int GetSize()
        {
            unsafe
            {
                return sizeof(SentObjectHeader);
            }
        }
    }

    public enum ModelId
    {
        JobRequest = 0,
        JobResponse = 1,
        FileRequest = 2,
        FileResponse = 3,
    }
}
