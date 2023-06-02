using System;
using System.Collections.Generic;

namespace DeeceApi.Client.Models
{
    [Serializable]
    public class JobResponse
    {
        Dictionary<string, string> OriginalPathToHash { get; set; }
    }
}
