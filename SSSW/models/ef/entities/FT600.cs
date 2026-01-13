using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSW.models
{
    [Table("FT600_Test")]
    public partial class FT600 : EntityCommon
    {
        [Browsable(false)][Key] public Guid id { get; set; }

        /// <summary>
        /// Machine.
        /// </summary>
        [DisplayName("Machine")]
        public string? C004 { get; set; }

        /// <summary>
        /// Main item name.
        /// </summary>
        [DisplayName("Main Item Name")]
        public string? C027 { get; set; }

        /// <summary>
        /// Step Index.
        /// </summary>
        [DisplayName("Sequence Step")]
        public int? C015 { get; set; }

        /// <summary>
        /// Step item code.
        /// </summary>
        [DisplayName("Step Code")]
        public string? C002 { get; set; }

        /// <summary>
        /// Size name.
        /// </summary>
        [DisplayName("Size Name")]
        public string? C008 { get; set; }

        //phần cân

        /// <summary>
        /// Part Weight (g) of step.
        /// </summary>
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:#,##0.##}")]
        [DisplayName("Part Weight (g/prs)")] public double? C021 { get; set; }
        /// <summary>
        /// Runner weight (g) of step.
        /// </summary>
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:#,##0.##}")]
        [DisplayName("Runner Weight (g/prs)")] public double? C022 { get; set; }

        /// <summary>
        /// Total scale value of part weight (include these previous step), scale value.
        /// </summary>
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:#,##0.##}")]
        [DisplayName("Total PW (g/prs)")] public double? C023 { get; set; }

        /// <summary>
        /// Total weight of step injection (include runner + part), Scale value.
        /// This is the weight of Non injection materials (Receptacle).
        /// EnumScaleType
        /// </summary>
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:#,##0.##}")]
        [DisplayName("Total W_injection (g)")] public double? C024 { get; set; }

        /// <summary>
        /// Số lượng. Dùng cho cân Recetacle/outsoleboard/Stud/Logo để quy đinh số lượng sử dụng trong bước.
        /// </summary>
        [DisplayName("Quantity")] public double? C025 { get; set; } = 0;

        /// <summary>
        /// Article pairs shot.
        /// </summary>
        [DisplayName("Article Pairs Shot")]
        public int? C017 { get; set; }

        /// <summary>
        /// Mold pairs shot.
        /// </summary>
        [DisplayName("Mold Pairs Shot")]
        public int? C018 { get; set; }

        /// <summary>
        /// Step item name.
        /// </summary>
        [DisplayName("Step Name")]
        public string? C003 { get; set; }

        /// <summary>
        /// Article pairs shot finaly.
        /// </summary>
        [Column("c028")]
        [DisplayName("Article Pairs Shot Final")]
        public int? C028 { get; set; }

        //------------------------------
        /// <summary>
        /// Main item code.
        /// </summary>
        [DisplayName("Main item code")]
        public string? C026 { get; set; }

        /// <summary>
        /// FG item code.
        /// </summary>
        [DisplayName("FG Code")]
        public string? C013 { get; set; }
        /// <summary>
        /// FG item name.
        /// </summary>
        [DisplayName("FG Name")]
        public string? C014 { get; set; }

        /// <summary>
        /// Machine groups.
        /// </summary>
        [DisplayName("Machine Group")]
        public string? C019 { get; set; }

        /// <summary>
        /// Mold Id.
        /// </summary>
        [DisplayName("Mold Id")]
        public string? C020 { get; set; }

        /// <summary>
        /// HydraOrderNumber.
        /// </summary>
        [DisplayName("Hydra Order")]
        public string? C000 { get; set; }

        /// <summary>
        /// Vị trí lấy mẫu.
        /// Production/Sample.
        /// EnumSampleLocation.
        /// </summary>
        [DisplayName("Location")]
        public EnumSampleLocation? C001 { get; set; }

        /// <summary>
        /// Article.
        /// </summary>
        [DisplayName("Article")]
        public string? C005 { get; set; }

        /// <summary>
        /// ColorCode.
        /// </summary>
        [DisplayName("Color Code")]
        public string? C006 { get; set; }
        /// <summary>
        /// Color name.
        /// </summary>
        [DisplayName("Color Name")] 
        public string? C007 { get; set; }
        
        /// <summary>
        /// Số mẫu cần lấy.
        /// Mặc định 1.
        /// </summary>
        [Browsable(false)]
        [DisplayName("Sample num")] 
        public int? C009 { get; set; } = 1;

        /// <summary>
        /// Employee code.
        /// </summary>
        [DisplayName("Operator Code")]
        public string? C010 { get; set; }
        /// <summary>
        /// Employee name.
        /// </summary>
        [DisplayName("Operator Name")]
        public string? C011 { get; set; }
        /// <summary>
        /// QR generate.
        /// format:ItemStepCode|Machine|Id.
        /// </summary>
        [Browsable(false)]
        [DisplayName("QR Code")]
        public string? C012 { get; set; }

        /// <summary>
        /// Remarks.
        /// </summary>
        [DisplayName("Remarks")]
        public string? C016 { get; set; }

        /// <summary>
        /// Id label. FT606.Id.
        /// </summary>
        [DisplayName("FT606.Id")]
        public Guid? C029 { get; set; } = null;

        /// <summary>
        /// Return thesample to production status. 0-notYet; 1- returned,
        /// </summary>
        [DisplayName("Return Status")]
        public bool? C030 { get; set; } = false;

        /// <summary>
        /// The quantity pairs return to production.
        /// </summary>
        [DisplayName("Return Qty (prs)")]
        public int? C031 { get; set; } = 0;

        /// <summary>
        /// FT600.Id
        /// </summary>
        [DisplayName("FT600.Id")]
        public Guid? C032 { get; set; }

        /// <summary>
        /// Categorry Code.
        /// </summary>
        [DisplayName("Category Code")]
        public int? C033 { get; set; }

        /// <summary>
        /// Category Name.
        /// </summary>
        [DisplayName("Category Name")]
        public string? C034 { get; set; }

        /// <summary>
        /// Percent of usage (%).
        /// </summary>
        [DisplayName("Percent of Usage (%)")]
        public double? C035 { get; set; }
    }
}
