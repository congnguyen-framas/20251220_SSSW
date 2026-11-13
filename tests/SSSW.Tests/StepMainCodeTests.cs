using SSSW.models;
using SSSW.UI.WPF.Services;

namespace SSSW.Tests;

// Level 2 trả child chỉ có mã chính (bỏ màu & size): "1-VB042313", REX giữ nguyên
public class StepMainCodeTests
{
    [Theory]
    [InlineData("1-VB042313-A123-5065", "1-VB042313")]
    [InlineData("1-VB042313", "1-VB042313")]
    [InlineData("2-VB042313-A123-0000", "2-VB042313")]
    [InlineData("REX00018", "REX00018")]
    [InlineData("rex-00018-x", "rex-00018-x")]
    public void StepMainCode_StripsColorAndSize_KeepsRex(string code, string expected)
        => Assert.Equal(expected, PartWeightCalculator.StepMainCode(code));

    [Fact]
    public void IsSelfRow_DetectsSelfByMainCode_OrSuffixOrUnit()
    {
        BomWinlineModel R(string c, string u) => new() { ItemStepCode = c, Unit = u, Quantity = 1 };
        Assert.True(PartWeightCalculator.IsSelfRow(R("2-VB042313-A123-0000", "G"), "2-VB042313-A123-2801"));
        Assert.True(PartWeightCalculator.IsSelfRow(R("2-VB042313", "PRS"), "2-VB042313-A123-2801"));
        Assert.False(PartWeightCalculator.IsSelfRow(R("1-VB042313", "PRS"), "2-VB042313-A123-2801"));
    }

    [Fact]
    public void Calculate_MatchesChildByMainCode_Inlay()
    {
        var level2 = new[]
        {
            new BomWinlineModel { ItemStepCode = "2-VB042313-A123-0000", Quantity = 1, Unit = "G" },
            new BomWinlineModel { ItemStepCode = "1-VB042313", Quantity = 8, Unit = "PRS" },
        };
        var studs = new FT600 { C002 = "1-VB042313-A123-5065", C021 = 22.10, C039 = 10 };

        var r = new PartWeightCalculator().Calculate(50, "2-VB042313-A123-2801", level2,
            c => PartWeightCalculator.SameStep(studs.C002, c) ? studs : null);

        Assert.True(r.IsComplete);
        Assert.Equal(50 - 8 * 2.21, r.PartWeight!.Value, 6);
    }

    [Fact]
    public void Aggregate_SumsByMainCode_Studs10()
    {
        var byStep = new Dictionary<string, IReadOnlyList<BomWinlineModel>>
        {
            ["2-VB042313-A123-2801"] = new[]
            {
                new BomWinlineModel { ItemStepCode = "2-VB042313-A123-0000", Quantity = 1, Unit = "G" },
                new BomWinlineModel { ItemStepCode = "1-VB042313", Quantity = 8, Unit = "PRS" },
            },
            ["3-VB042313-A123-2801"] = new[]
            {
                new BomWinlineModel { ItemStepCode = "3-VB042313-A123-0000", Quantity = 1, Unit = "G" },
                new BomWinlineModel { ItemStepCode = "1-VB042313", Quantity = 2, Unit = "PRS" },
                new BomWinlineModel { ItemStepCode = "REX00018", Quantity = 8, Unit = "PCS" },
            },
        };

        var total = ComponentQuantityAggregator.Aggregate(byStep);

        Assert.Equal(10, total["1-VB042313"]);
        Assert.Equal(8, total["REX00018"]);
        Assert.Equal(10, ComponentQuantityAggregator.PiecesFor("1-VB042313-A123-5065", total, null, null));
    }
}
