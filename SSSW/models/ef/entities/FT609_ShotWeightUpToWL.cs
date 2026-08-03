using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSSW.models
{
    [Table("FT609")]
    public class FT609_ShotWeightUpToWL : BaseEntity
    {
        [Browsable(false)]
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Item Code. Step component or FG
        /// </summary>
        [StringLength(100)]
        public string? C000 { get; set; } = string.Empty;

        /// <summary>
        /// Item Name
        /// </summary>
        [StringLength(500)]
        public string? C001 { get; set; } = string.Empty;

        /// <summary>
        /// Main Code
        /// </summary>
        [StringLength(100)]
        public string? C002 { get; set; } = string.Empty;

        /// <summary>
        /// Main Name
        /// </summary>
        [StringLength(500)]
        public string? C003 { get; set; } = string.Empty;

        /// <summary>
        /// Part weight (g).
        /// FT600.c021.
        /// </summary>
        public double? C004 { get; set; } = 0;

        /// <summary>
        /// Runner weight (g).
        /// FT600.c022.
        /// </summary>
        public double? C005 { get; set; } = 0;

        /// <summary>
        /// Shot weight (g) = Part Weight + Runner Weight
        /// </summary>
        public double? C006 { get; set; } = 0;

        /// <summary>
        /// Latest Shot Weight flag.
        /// true = latest record.
        /// false = historical record.
        /// </summary>
        public bool? C007 { get; set; } = true;

        /// <summary>
        /// Machine group code. (Optional).
        /// FT600.c019.
        /// </summary>
        public string? C008 { get; set; } = string.Empty;

        /// <summary>
        /// Mold ID. (Optional).
        /// FT600.c020.
        /// </summary>
        public string? C009 { get; set; } = string.Empty;

        /// <summary>
        /// The Id of the Scale section that this shot weight record belongs to. (Optional).
        /// FT600.Id string.join. Ex: "FT600.Id1,FT600.Id2,FT600.Id3".
        /// </summary>
        public string? C010 { get; set; } = string.Empty;
    }
}
