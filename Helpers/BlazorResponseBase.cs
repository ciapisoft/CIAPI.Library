using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIAPI.Helpers
{
    public class BlazorResponseBase
    {
        public bool Success { get; set; }
        public Dictionary<string,string> ErrorsKeyValue { get; set; }
    }
}
