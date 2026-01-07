using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    /// <summary>
    /// Operator.
    /// </summary>
    [Table("FT029")]
    public class FT029_Operator_RFID : EntityCommon
    {
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// Employee code.
        /// </summary>
        public string? C000 { get; set; }
        /// <summary>
        /// Employee name. 
        /// </summary>
        public string? C001 { get; set; }
        /// <summary>
        /// Depatment Id. FT031_Department.Id.
        /// </summary>
        public Guid C002 { get; set; }
        /// <summary>
        /// Phone number.
        /// </summary>
        public string? C003 { get; set; }
        /// <summary>
        /// Email.
        /// </summary>
        public string? C004 { get; set; }
        /// <summary>
        /// RFID Code.
        /// </summary>
        public string? C005 { get; set; }
        /// <summary>
        /// Approved cho cac yeu cau can confirm. 1-co quyen; 0-ko co quyen.
        /// </summary>
        public bool? C006 { get; set; }

        [NotMapped]
        public FT031_Department? DepartmentInfor { get; set; }
    }
}
