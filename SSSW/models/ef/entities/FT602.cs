using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    [Table("FT602")]
    public class FT602: EntityCommon
    {
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// FG item code.
        /// </summary>
        public string c000 { get; set; }
        /// <summary>
        /// FG item name.
        /// </summary>
        public string c002 { get; set; }
        /// <summary>
        /// Step item code.
        /// </summary>
        public string c003 { get; set; }
        /// <summary>
        /// Step item name.
        /// </summary>
        public string c004 { get; set; }
        /// <summary>
        /// The list model contains steps used for this step.
        /// It is StepConfigModel.
        /// </summary>
        public string c005 { get; set; }
        /// <summary>
        /// Recetacle/Outsoleboard.
        /// </summary>
        public bool c006 { get; set; }
    }
}
