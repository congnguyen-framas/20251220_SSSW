using SSSW.models;
using SSSW.UI.WPF.Services;

namespace SSSW.Tests;

// Số liệu BOM lấy theo FG 6112042313-A478-2801 (Level 2 của từng step)
public class PartWeightCalculatorTests
{
    private readonly PartWeightCalculator _calc = new();

    private static BomWinlineModel Row(string code, double qty, string unit)
        => new() { ItemStepCode = code, Quantity = qty, Unit = unit };

    // c021: Part Weight (g/prs); pieces: C039 (số chiếc / 1 đôi); c036: g/chiếc đã lưu (REX hoặc đã tính khi cân)
    private static FT600 Weighed(string code, double? c021 = null, double? pieces = null, double? c036 = null)
        => new() { C002 = code, C021 = c021, C039 = pieces, C036 = c036 };

    private static Func<string, FT600?> Lookup(params FT600[] rows)
        => code => rows.FirstOrDefault(r => r.C002 == code);

    // 3-VB042313-A123-2801 Base_FF = self + Studs x2 + Inlay x2 + REX00018 x8
    private static readonly BomWinlineModel[] BaseFF =
    {
        Row("3-VB042313-A123-0000", 1, "G"),
        Row("1-VB042313-A123-5065", 2, "PRS"),
        Row("2-VB042313-A123-2801", 2, "PRS"),
        Row("REX00018", 8, "PCS"),
    };

    [Fact]
    public void BaseFF_SubtractsStudsInlayAndRex_ByPiecesFromLevel2()
    {
        var r = _calc.Calculate(100, "3-VB042313-A123-2801", BaseFF, Lookup(
            Weighed("1-VB042313-A123-5065", c021: 22.10, pieces: 10),   // 2.21 g/pcs x2 = 4.42
            Weighed("2-VB042313-A123-2801", c021: 20, pieces: 2),       // 10 g/pcs   x2 = 20
            Weighed("REX00018", c036: 1.5)));                           // 1.5 x8 = 12

        Assert.True(r.IsComplete);
        Assert.Equal(36.42, r.TotalDeduction, 6);
        Assert.Equal(63.58, r.PartWeight!.Value, 6);
    }

    [Fact]
    public void BaseHF_OnlyUsesItsOwnChildren_NotStudsOrInlay()
    {
        var level2 = new[]
        {
            Row("4-VB042313-A123-0000", 1, "G"),
            Row("0-VB042313-A123-2706", 2, "PRS"),
            Row("REX00018", 4, "PCS"),
            Row("3-VB042313-A123-2801", 2, "PRS"),
        };

        // Studs/Inlay đã cân nhưng KHÔNG nằm trong L2 của Base_HF → không bị trừ
        var r = _calc.Calculate(200, "4-VB042313-A123-2801", level2, Lookup(
            Weighed("0-VB042313-A123-2706", c021: 6, pieces: 2),        // 3 g/pcs x2 = 6
            Weighed("REX00018", c036: 1.5),                             // 1.5 x4 = 6
            Weighed("3-VB042313-A123-2801", c021: 60, pieces: 2),       // 30 g/pcs x2 = 60
            Weighed("1-VB042313-A123-5065", c021: 999, pieces: 10)));

        Assert.Equal(128, r.PartWeight!.Value, 6);
    }

    [Fact]
    public void StepWithOnlySelfRow_PartWeightEqualsTotal()
    {
        var level2 = new[] { Row("0-VB042313-A123-0000", 1, "G") };
        var r = _calc.Calculate(12.5, "0-VB042313-A123-2706", level2, Lookup());
        Assert.Equal(12.5, r.PartWeight!.Value, 6);
    }

    [Fact]
    public void MissingChild_IsReported_AndNoPartWeightReturned()
    {
        var r = _calc.Calculate(100, "3-VB042313-A123-2801", BaseFF, Lookup(
            Weighed("1-VB042313-A123-5065", c021: 22.10, pieces: 10)));

        Assert.False(r.IsComplete);
        Assert.Null(r.PartWeight);
        Assert.Equal(new[] { "2-VB042313-A123-2801", "REX00018" }, r.MissingChildren);
    }

    [Fact]
    public void ChildWithZeroWeight_CountsAsMissing()
    {
        var r = _calc.Calculate(100, "3-VB042313-A123-2801", BaseFF, Lookup(
            Weighed("1-VB042313-A123-5065", c021: 0, pieces: 10),
            Weighed("2-VB042313-A123-2801", c021: 20, pieces: 2),
            Weighed("REX00018", c036: 1.5)));

        Assert.Contains("1-VB042313-A123-5065", r.MissingChildren);
    }

    [Fact]
    public void UnitWeightPerPiece_UsesStoredC036_ElseC021DividedByPieces_ElseNull()
    {
        Assert.Equal(2.21, PartWeightCalculator.UnitWeightPerPiece(Weighed("1-X-Y-5065", c021: 22.10, pieces: 10, c036: 2.21)));
        Assert.Equal(2.21, PartWeightCalculator.UnitWeightPerPiece(Weighed("1-X-Y-5065", c021: 22.10, pieces: 10))!.Value, 6);
        Assert.Equal(1.5, PartWeightCalculator.UnitWeightPerPiece(Weighed("REX00018", c036: 1.5)));
        Assert.Null(PartWeightCalculator.UnitWeightPerPiece(Weighed("REX00018", c021: 10, pieces: 4))); // REX không suy từ C021
        Assert.Null(PartWeightCalculator.UnitWeightPerPiece(Weighed("1-X-Y-5065")));
    }

    // ── REX / RPU tiêu hao tính theo gram (đơn vị G) của step PU, vd. 4-FC992601-SG11-F05L ──
    private static readonly BomWinlineModel[] PuStep =
    {
        Row("4-FC992601-SG11-0000", 1, "G"),
        Row("2-FC992601", 1, "PCS"),
        Row("REX00172", 5.715, "G"),
        Row("RPU00001", 118.59, "G"),
    };

    [Fact]
    public void IsSelfRow_RexAndRpuWithUnitG_AreNotSelf_ButStepRowStillIs()
    {
        Assert.False(PartWeightCalculator.IsSelfRow(Row("REX00172", 5.715, "G"), "4-FC992601-SG11-F05L"));
        Assert.False(PartWeightCalculator.IsSelfRow(Row("RPU00001", 118.59, "G"), "4-FC992601-SG11-F05L"));
        Assert.True(PartWeightCalculator.IsSelfRow(Row("4-FC992601-SG11-0000", 1, "G"), "4-FC992601-SG11-F05L"));
        Assert.True(PartWeightCalculator.IsSelfRow(Row("2-VB042313-A123-0000", 1, "G"), "3-VB042313-A123-2801"));
    }

    [Fact]
    public void GramMaterials_NotWeighed_DeductBomQuantity()
    {
        var r = _calc.Calculate(200, "4-FC992601-SG11-F05L", PuStep,
            Lookup(Weighed("2-FC992601", c021: 30, pieces: 1)));

        Assert.True(r.IsComplete);
        Assert.Equal(30 + 5.715 + 118.59, r.TotalDeduction, 6);
        Assert.Equal(200 - (30 + 5.715 + 118.59), r.PartWeight!.Value, 6);
    }

    [Fact]
    public void GramMaterials_Weighed_DeductWeighedUsage()
    {
        var r = _calc.Calculate(200, "4-FC992601-SG11-F05L", PuStep, Lookup(
            Weighed("2-FC992601", c021: 30, pieces: 1),
            Weighed("REX00172", c021: 6.0, c036: 3),
            Weighed("RPU00001", c021: 120.5, c036: 60)));

        Assert.Equal(30 + 6.0 + 120.5, r.TotalDeduction, 6);
    }

    [Fact]
    public void NonGramComponents_StillNeedWeighing()
    {
        var r = _calc.Calculate(100, "3-VB042313-A123-2801", BaseFF, Lookup());
        Assert.False(r.IsComplete);
        Assert.Contains("REX00018", r.MissingChildren);
    }
}

public class ComponentQuantityAggregatorTests
{
    private static BomWinlineModel Row(string code, double qty, string unit)
        => new() { ItemStepCode = code, Quantity = qty, Unit = unit };

    // Level 2 của FG 6112042313-A478-2801
    private static IReadOnlyDictionary<string, IReadOnlyList<BomWinlineModel>> Fg() =>
        new Dictionary<string, IReadOnlyList<BomWinlineModel>>
        {
            ["0-VB042313-A123-2706"] = new[] { Row("0-VB042313-A123-0000", 1, "G") },
            ["1-VB042313-A123-5065"] = new[] { Row("1-VB042313-A123-0000", 1, "G") },
            ["2-VB042313-A123-2801"] = new[]
            {
                Row("2-VB042313-A123-0000", 1, "G"),
                Row("1-VB042313-A123-5065", 8, "PRS"),
            },
            ["3-VB042313-A123-2801"] = new[]
            {
                Row("3-VB042313-A123-0000", 1, "G"),
                Row("1-VB042313-A123-5065", 2, "PRS"),
                Row("2-VB042313-A123-2801", 2, "PRS"),
                Row("REX00018", 8, "PCS"),
            },
            ["4-VB042313-A123-2801"] = new[]
            {
                Row("4-VB042313-A123-0000", 1, "G"),
                Row("0-VB042313-A123-2706", 2, "PRS"),
                Row("REX00018", 4, "PCS"),
                Row("3-VB042313-A123-2801", 2, "PRS"),
            },
        };

    [Fact]
    public void Aggregate_SumsQuantityOverAllParents()
    {
        var total = ComponentQuantityAggregator.Aggregate(Fg());

        Assert.Equal(2, total["0-VB042313"]);   // Logo: 2 (Base_HF)
        Assert.Equal(10, total["1-VB042313"]);  // Studs: 8 (Inlay) + 2 (Base_FF)
        Assert.Equal(2, total["2-VB042313"]);   // Inlay: 2 (Base_FF)
        Assert.Equal(2, total["3-VB042313"]);   // Base_FF: 2 (Base_HF)
        Assert.Equal(12, total["REX00018"]);              // REX: 8 + 4
        Assert.False(total.ContainsKey("4-VB042313")); // Base_HF không là chi tiết của step nào
    }

    [Fact]
    public void PiecesFor_FinalStep_UsesLevel1QuantityConvertedToPieces()
    {
        var total = ComponentQuantityAggregator.Aggregate(Fg());

        Assert.Equal(10, ComponentQuantityAggregator.PiecesFor("1-VB042313-A123-5065", total, 1, "PRS"));
        Assert.Equal(2, ComponentQuantityAggregator.PiecesFor("4-VB042313-A123-2801", total, 1, "PRS")); // 1 PRS = 2 PCS
        Assert.Equal(3, ComponentQuantityAggregator.PiecesFor("REX00099", total, 3, "PCS"));
        Assert.Equal(1, ComponentQuantityAggregator.PiecesFor(null, total, null, null));
    }

    [Fact]
    public void WeightPerPiece_StudsExample()
    {
        var total = ComponentQuantityAggregator.Aggregate(Fg());
        var perPiece = 22.10 / total["1-VB042313"];

        Assert.Equal(2.21, perPiece, 6);
        Assert.Equal(17.68, 8 * perPiece, 6);  // trừ ở Inlay
        Assert.Equal(4.42, 2 * perPiece, 6);   // trừ ở Base_FF
    }

    [Fact]
    public void GramMaterials_AreAggregatedIntoPieces()
    {
        var puStep = new[]
        {
            new BomWinlineModel { ItemStepCode = "4-FC992601-SG11-0000", Quantity = 1, Unit = "G" },
            new BomWinlineModel { ItemStepCode = "2-FC992601", Quantity = 1, Unit = "PCS" },
            new BomWinlineModel { ItemStepCode = "REX00172", Quantity = 5.715, Unit = "G" },
            new BomWinlineModel { ItemStepCode = "RPU00001", Quantity = 118.59, Unit = "G" },
        };
        var map = new Dictionary<string, IReadOnlyList<BomWinlineModel>> { ["4-FC992601-SG11-F05L"] = puStep };
        var total = ComponentQuantityAggregator.Aggregate(map);

        Assert.Equal(5.715, total["REX00172"], 6);
        Assert.Equal(118.59, total["RPU00001"], 6);
    }
}
