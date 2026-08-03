using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW
{
    public class ResultModel
    {
        public bool? Result { get; set; } = false;
        public string? Message { get; set; } = string.Empty;
        public string UpdatedItems { get; set; } = string.Empty;
        public string NotFoundItems { get; set; } = string.Empty;
    }
}
