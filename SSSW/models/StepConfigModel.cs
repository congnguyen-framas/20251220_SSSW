using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW.models
{
    public class StepConfigModel
    {
        /// <summary>
        /// Unit is gram.
        /// </summary>
        public double LowLevel { get; set; } = 1;
        /// <summary>
        /// Unit is gram.
        /// </summary>
        public double HighLevel { get; set; } = 500;
        public List<UsedSteps> StepsUsing { get; set; } = new List<UsedSteps>();
    }

    /// <summary>
    /// Model 
    /// </summary>
    public class UsedSteps
    {
        /// <summary>
        /// Báo là FG này có dùng receptacle.
        /// </summary>
        public bool IsReceptacle { get; set; } = false;
        /// <summary>
        /// Nếu là hàng receptacle và oulsoboard thì để trống.
        /// </summary>
        public string? StepItemCode { get; set; }= null;
        public string? StepItemName { get; set; }=null;
        /// <summary>
        /// Chỉ định bước sẽ dùng bước này.
        /// </summary>
        public string UsingForStepItemCode { get; set; }
        public string UsingForStepItemName { get; set; }
        /// <summary>
        /// Sẽ mặc định là 1 đôi khi cân các bước inlay, Base FF.
        /// Khi cân Receptacle/Outsoleboard/stud/logo/ring, thì phải nhập số lượng dùng cho 1 đôi (>1).
        /// </summary>
        public int UsingQuantity { get; set; } = 1;
    }
}
