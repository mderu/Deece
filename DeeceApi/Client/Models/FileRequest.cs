namespace DeeceApi.Client.Models
{
    public class FileRequest
    {
        /// <summary>
        /// The id of this request.
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// The file path on the original file system.
        /// </summary>
        public string OriginalFilePath { get; set; }
    }
}
