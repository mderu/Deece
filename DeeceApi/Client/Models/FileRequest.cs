using System;
using System.Linq;
using System.Text;

namespace DeeceApi.Client.Models
{
    [Serializable]
    public class FileRequest : ISerializable<FileRequest>
    {
        /// <summary>
        /// The id of this request.
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// The file path on the original file system.
        /// </summary>
        public string OriginalFilePath { get; set; }

        public FileRequest FromBytes(byte[] bytes)
        {
            return new FileRequest
            {
                MessageId = BitConverter.ToInt32(bytes, 0),
                OriginalFilePath = Encoding.Unicode.GetString(bytes, 4, bytes.Length - 4),
            };
        }

        public byte[] ToBytes(FileRequest obj)
        {
            return BitConverter.GetBytes(MessageId)
                .Concat(Encoding.Unicode.GetBytes(OriginalFilePath))
                .ToArray();
        }
    }
}
