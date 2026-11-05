namespace SSSW.models
{
    /// <summary>
    /// Kết quả của stored procedure <c>sp_sssw_getGroupSize @ItemCode</c>: trọng lượng ép theo từng size của nhóm item.
    /// Script tạo SP: docs/sql/sp_sssw_getGroupSize.sql.
    /// Gọi: <c>db.Database.SqlQueryRaw&lt;GroupSizeModel&gt;("EXEC sp_sssw_getGroupSize @ItemCode = {0}", prefix)</c>
    /// </summary>
    public class GroupSizeModel
    {
        public string? ItemCode { get; set; }

        public string? ItemName { get; set; }

        public string? SizeCode { get; set; }

        public string? SizeName { get; set; }

        public double? C063 { get; set; }

        public double? C064 { get; set; }

        /// <summary>C063 + C064.</summary>
        public double? ShotWeight { get; set; }
    }
}
