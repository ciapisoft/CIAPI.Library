using System.Collections.Generic;

namespace CIAPI.Helpers
{
    public class BlazorResponseBase
    {
        public bool Success { get; set; }
        public Dictionary<string, string> ErrorsKeyValue { get; set; }
    }
}
