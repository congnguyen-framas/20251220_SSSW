using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW
{
    public class WeightUpdateModel
    {
        public string ItemCode { get; set; }
        public double? PartWeight_C201 { get; set; } = 0;
        public double? RunnerWeight_C022 { get; set; } = 0;

        public string? MachineGroup_C019 { get; set; } = string.Empty;
        public string? MoldId_C020 { get; set; } = string.Empty;
    }
}
