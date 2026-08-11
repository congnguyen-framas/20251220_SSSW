using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    [Table("FT606")]
    public class FT606_Label : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// FT601.Id
        /// </summary>
        public Guid? C000 { get; set; }

        /// <summary>
        /// QR code.
        /// </summary>

        public string? C001 { get; set; }
    }
}
