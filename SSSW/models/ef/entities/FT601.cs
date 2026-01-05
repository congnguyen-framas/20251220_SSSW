using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.Model
{
    [Table("FT601")]
    public class FT601 : EntityCommon
    {
        [Browsable(false)][Key] public Guid Id { get; set; }

        /// <summary>
        /// HydraOrderType.
        /// </summary>
        [DisplayName("Hydra Order Type")] public string? C000 { get; set; }

        /// <summary>
        /// Location
        /// </summary>
        [DisplayName("Location")] public EnumSampleLocation? C001 { get; set; }

        /// <summary>
        /// Size.
        /// </summary>
        [DisplayName("Size")] public string? C002 { get; set; }

        /// <summary>
        /// MainName.
        /// </summary>
        [DisplayName("Main Name")] public string? C003 { get; set; }

        /// <summary>
        /// Step Item Code.
        /// </summary>
        [DisplayName("Step Item Code")] public string? C004 { get; set; }

        /// <summary>
        /// Step Item Name.
        /// </summary>
        [DisplayName("Step Item Name")] public string? C005 { get; set; }

        /// <summary>
        /// Article.
        /// </summary>
        [DisplayName("Article")] public string? C006 { get; set; }

        /// <summary>
        /// FG Item Code.
        /// </summary>
        [DisplayName("FG Item Code")] public string? C007 { get; set; }

        /// <summary>
        /// "FG Item Name.
        /// </summary>
        [DisplayName("FG Item Name")] public string? C008 { get; set; }

        /// <summary>
        /// Step Index Hydra.
        /// </summary>
        [DisplayName("Step Index Hydra")] public string? C009 { get; set; }

        /// <summary>
        /// Step Index. From WL.
        /// </summary>
        [DisplayName("Step Index WL")] public int? C010 { get; set; }

        /// <summary>
        /// Color Code
        /// </summary>
        [DisplayName("Color Code")] public string? C011 { get; set; }

        /// <summary>
        /// Color Name.
        /// </summary>
        [DisplayName("Color Name")] public string? C012 { get; set; }

        /// <summary>
        /// ArticlePairsShot.
        /// </summary>
        [DisplayName("Article Pairs Shot")] public int? C013 { get; set; }

        /// <summary>
        /// Mold Paird Shot.
        /// </summary>
        [DisplayName("Mold Paird Shot")] public int? C014 { get; set; }

        /// <summary>
        /// Machine.
        /// </summary>
        [DisplayName("Machine")] public string? C015 { get; set; }     

        /// <summary>
        /// Machine Group.
        /// </summary>
        [DisplayName("Machine Group")] public string? C016 { get; set; }

        /// <summary>
        /// Status.
        /// </summary>
        [DisplayName("Status")] public bool? C017 { get; set; }

        /// <summary>
        /// Order Hydra Num.
        /// </summary>
        [DisplayName("Order Hydra Num")] public string? C018 { get; set; }

        /// <summary>
        /// Mold Id.
        /// </summary>
        [DisplayName("Mold Id")] public string? C019 { get; set; }

        /// <summary>
        /// Main Code.
        /// </summary>
        [DisplayName("Main Code")] public string? C020 { get; set; }
    }
}
