using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    [Table("FT608")]
    public class FT608_Config: EntityCommon
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// PC name.
        /// </summary>
        public string? c000 { get; set; }

        /// <summary>
        /// config json.
        /// configModel.
        /// </summary>

        public string? c001 { get; set; }
    }
}
