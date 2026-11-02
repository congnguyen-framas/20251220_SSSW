using SSSW.models;
using SSSW.UI.WPF.Services;

namespace SSSW.Tests;

// Số liệu BOM lấy theo FG 6112042313-A478-2801 (Level 2 của từng step)
public class PartWeightCalculatorTests
{
    private readonly PartWeightCalculator _calc = new();

    private static BomWinlineModel Row(string code, double qty, string unit)
        => new() { ItemStepCode = code, Quantity = qty, Unit = unit };

    private static FT600 Weighed(string code, double? c021 = null, double? c036 = null, string unit = "PRS")
        => new() { C002 = code, C021 = c021, C036 = c036, C037 = unit };

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
    public void BaseFF_SubtractsStudsInlayAndRex_PerPiece()
    {
        var r = _calc.Calculate(100, "3-VB042313-A123-2801", BaseFF, Lookup(
            Weighed("1-VB042313-A123-5065", c021: 10),           // 5 g/piece x2 = 10
            Weighed("2-VB042313-A123-2801", c021: 20),           // 10 g/piece x2 = 20
            Weighed("REX00018", c036: 1.5, unit: "PCS")));       // 1.5 x8 = 12

        Assert.True(r.IsComplete);
        Assert.Equal(42, r.TotalDeduction, 6);
        Assert.Equal(58, r.PartWeight!.Value, 6);
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
            Weighed("0-VB042313-A123-2706", c021: 6),            // 3 x2 = 6
            Weighed("REX00018", c036: 1.5, unit: "PCS"),         // 1.5 x4 = 6
            Weighed("3-VB042313-A123-2801", c021: 60),           // 30 x2 = 60
            Weighed("1-VB042313-A123-5065", c021: 999)));

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
            Weighed("1-VB042313-A123-5065", c021: 10)));

        Assert.False(r.IsComplete);
        Assert.Null(r.PartWeight);
        Assert.Equal(new[] { "2-VB042313-A123-2801", "REX00018" }, r.MissingChildren);
    }

    [Fact]
    public void ChildWithZeroWeight_CountsAsMissing()
    {
        var r = _calc.Calculate(100, "3-VB042313-A123-2801", BaseFF, Lookup(
            Weighed("1-VB042313-A123-5065", c021: 0),
            Weighed("2-VB042313-A123-2801", c021: 20),
            Weighed("REX00018", c036: 1.5, unit: "PCS")));

        Assert.Contains("1-VB042313-A123-5065", r.MissingChildren);
    }

    [Fact]
    public void UnitWeightPerPiece_PrsIsHalved_PcsIsNot()
    {
        Assert.Equal(5, PartWeightCalculator.UnitWeightPerPiece(Weighed("1-X-Y-5065", c021: 10)));
        Assert.Equal(1.5, PartWeightCalculator.UnitWeightPerPiece(Weighed("REX00018", c036: 1.5, unit: "PCS")));
        Assert.Null(PartWeightCalculator.UnitWeightPerPiece(Weighed("1-X-Y-5065")));
    }
}
