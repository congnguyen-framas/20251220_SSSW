using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW
{
    public class GetShotWeightWL_DTO
    {
        public string? ItemCode_C016 { get; set; }

        /// <summary>
        /// FG--Kg; step--Gr.
        /// </summary>
        public double? PartWeight_c063 { get; set; } = 0;
        /// <summary>
        /// FG--Kg; step--Gr.
        /// </summary>
        public double? RunnerWeight_c064 { get; set; } = 0;

        /// <summary>
        /// FG--Kg; step--Gr.
        /// </summary>
        public double? ShotWeight { get; set; } = 0;
    }
}
