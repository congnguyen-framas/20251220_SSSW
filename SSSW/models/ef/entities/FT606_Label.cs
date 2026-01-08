using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    [Table("FT606")]
    public class FT606_Label : EntityCommon
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// FT601.Id
        /// </summary>
        public Guid? c000 { get; set; }

        /// <summary>
        /// QR code.
        /// </summary>

        public string? c001 { get; set; }
    }
}
