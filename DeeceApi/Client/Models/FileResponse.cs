using System;

namespace DeeceApi.Client.Models
{
    [Serializable]
    public class FileResponse
    {
        /// <summary>
        /// The id of the <see cref="FileRequest"/> this response is for.
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// The file's hash, used for storing it in the CAS.
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// The name of the file on the local machine.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The contents of the file.
        /// This value may not be set if <see cref="JobRequest.DirectTransfer"/> is false.
        /// </summary>
        public byte[] Contents { get; set; }
    }
}
