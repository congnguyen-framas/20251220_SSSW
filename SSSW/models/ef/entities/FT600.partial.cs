using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.Model
{
    public partial class FT600
    {
        [NotMapped]
        [Browsable(false)]
        public bool AllowScale { get; set; } = false;
    }
}
