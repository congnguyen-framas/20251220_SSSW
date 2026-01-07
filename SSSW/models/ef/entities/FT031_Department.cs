using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    /// <summary>
    /// Department.
    /// </summary>
    [Table("FT031")]
    public class FT031_Department : EntityCommon
    {
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// Department name.
        /// </summary>
        public string C000 { get; set; }
        /// <summary>
        /// Note free text.
        /// </summary>
        public string C001 { get; set; }
    }
}
