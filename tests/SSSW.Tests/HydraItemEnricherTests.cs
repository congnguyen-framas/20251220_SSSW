using SSSW.models;
using SSSW.UI.WPF.Services;

namespace SSSW.Tests;

public class HydraItemEnricherTests
{
    private static HydraItemDetailModel Item(string? fg, string step, string? main = null, string? size = null) => new()
    {
        FGItemCode = fg, StepItemCode = step, MainCode = main, MainName = main, Size = size,
        FGItemName = "fgname", StepItemName = "stepname", StepIndex = 1
    };

    [Fact]
    public void NeedsInfo_WhenSizeOrMainMissing()
    {
        Assert.True(HydraItemEnricher.NeedsInfo(Item("6813992601-SG10-F00L", "3-X", main: "6813992601", size: null)));
        Assert.False(HydraItemEnricher.NeedsInfo(Item("6813992601-SG10-F00L", "3-X", main: "6813992601", size: "35.5")));
    }

    [Fact]
    public void FillMissingInfo_ByFg_FillsEmptyOnly_NeverOverwrites()
    {
        var it = Item("6813992601-SG10-F00L", "3-X", main: "KEEP", size: null);
        it.FGItemName = "keep-name";
        var rows = new List<ProductInfoStoreModel>
        {
            new() { ProductItemCode = "6813992601-SG10-F00L", ProductName = "from-sp", MainCode = "6813992601", MainName = "m", SizeName = "35.5 (Left)", StepFG = "3-X", StepName = "Central", Sequence = 3 }
        };

        HydraItemEnricher.FillMissingInfo(new[] { it }, rows);

        Assert.Equal("keep-name", it.FGItemName);   // đã có → giữ
        Assert.Equal("KEEP", it.MainCode);          // đã có → giữ
        Assert.Equal("35.5 (Left)", it.Size);       // trống → điền
    }

    [Fact]
    public void FillMissingInfo_NoFg_TakesFirstProductByStep_AndSetsFg()
    {
        var it = Item(null, "3-X");
        var rows = new List<ProductInfoStoreModel>
        {
            new() { ProductItemCode = "B-2", StepFG = "3-X", MainCode = "m2", SizeName = "s2" },
            new() { ProductItemCode = "A-1", StepFG = "3-X", MainCode = "m1", SizeName = "s1" },
        };

        HydraItemEnricher.FillMissingInfo(new[] { it }, rows);

        Assert.Equal("A-1", it.FGItemCode);   // sản phẩm đầu theo thứ tự mã → kết quả ổn định
        Assert.Equal("m1", it.MainCode);
        Assert.Equal("s1", it.Size);
    }

    [Fact]
    public void Filters_DropNoFg_AndIncomplete()
    {
        var items = new List<HydraItemDetailModel>
        {
            Item("NOHYPHEN", "1", "m", "s"),
            Item(null, "2", "m", "s"),
            Item("A-1", "3", "m", null),      // thiếu size
            Item("A-1", "4", "m", "s"),
        };

        var withFg = HydraItemEnricher.KeepWithFg(items);
        Assert.Equal(2, withFg.Count);
        Assert.Equal(new[] { "A-1" }, HydraItemEnricher.FgMissingInfo(withFg));

        var complete = HydraItemEnricher.KeepComplete(withFg);
        Assert.Equal("4", Assert.Single(complete).StepItemCode);
    }

    [Fact]
    public void LookupCodes_UsesFgElseStep_Distinct()
    {
        var codes = HydraItemEnricher.LookupCodes(new[]
        {
            Item("A-1", "s1"), Item("A-1", "s2"), Item(null, "s3"), Item(null, ""),
        });
        Assert.Equal(new[] { "A-1", "s3" }, codes);
    }
}
