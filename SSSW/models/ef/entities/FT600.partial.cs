using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    public partial class FT600
    {
        [NotMapped]
        [Browsable(false)]
        public bool AllowScale { get; set; } = false;

        [NotMapped]
        public string? StatusText { get; set; } =string.Empty;
        [NotMapped]
        public string? StatusDotColor { get; set; } = string.Empty;
        [NotMapped]
        public string? StatusBarColor { get; set; } = string.Empty;
    }
}
