using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.modelss
{
    [Table("FT605")]
    public class FT605
    {
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// FG item code.
        /// </summary>
        public string c000 { get; set; }
        /// <summary>
        /// FT602 Id reference.
        /// </summary>
        public Guid? FT602Id { get; set; }

        public string? mesocomp { get; set; }
        public int? mesoyear { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedMachine { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }

    }
}
