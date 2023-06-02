using System;

namespace DeeceApi.Client.Models
{
    [Serializable]
    public class JobRequest
    {
        /// <summary>
        /// The id of this request.
        /// </summary>
        public int MessageId { get; set; }

        /// <summary>
        /// The path on the client's machine that the execution should take place within.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// The path of the executable on the original filesystem.
        /// </summary>
        // TODO: Replace this with a more optimal object. Making job requests this way will
        //       always result in a follow-up FileRequest.
        public string OriginalExecutablePath { get; set; }

        /// <summary>
        /// The full command line, including the executable name.
        /// </summary>
        public string Commandline { get; set; }

        /// <summary>
        /// If true, the contents of files are passed along with FileRequestResults.
        /// 
        /// If true, the client does not need to upload the file to the CAS. The worker
        /// will inevitiably upload the file to the CAS itself.
        /// 
        /// If false, it is expected that the client will upload the file to the CAS, and
        /// worker will request the contents from the CAS.
        /// </summary>
        public bool DirectTransfer { get; set; }

        // TODO: Environment Variable dictionary?
    }
}
