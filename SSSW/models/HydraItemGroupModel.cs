using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW.models
{
    public  class HydraItemGroupModel
    {
        public string StepItemCode { get; set; }
        public string StepItemName { get; set; }
        public string Size { get; set; }
        //public string Machine { get; set; }
        public int? ArticlePairShot { get; set; }
        public int? MoldPairShot { get; set; }
        public string MachineGroup { get; set; }
        public string Artikel { get; set; }
        public string FGItemCode { get; set; }
        public string FGItemName { get; set; }
        public List<HydraItemDetailModel> Details { get; set; }
    }
}
