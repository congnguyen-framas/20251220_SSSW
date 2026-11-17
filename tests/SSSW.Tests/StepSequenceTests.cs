using SSSW.models;
using SSSW.UI.WPF.Services;

namespace SSSW.Tests;

public class StepSequenceTests
{
    private static BomWinlineModel S(string code, int seq) => new() { ItemStepCode = code, ParallelSequence = seq };

    private static readonly List<BomWinlineModel> Fg = new()
    {
        S("1-FC992601-SG03-F00L", 1), S("2-FC992601-SG04-F00L", 2),
        S("3-FC992601-SG05-F00L", 3), S("4-FC992601-SG11-F00L", 4),
        S("REX00172", 9),   // REX không tính vào step cuối
    };

    [Fact]
    public void LastStep_IsNotTrimmed_EvenWhenHydraSeqIsSmaller()
    {
        // FT601.C010 = 3 (lệch BOM) nhưng step 4 là step cuối theo BOM → không phải "không cuối"
        Assert.False(StepSequence.IsNotLastStep(Fg, "4-FC992601-SG11-F00L", fallbackSeq: 3));
    }

    [Fact]
    public void MiddleStep_IsNotLast_EvenWhenHydraSeqIsLarger()
    {
        Assert.True(StepSequence.IsNotLastStep(Fg, "3-FC992601-SG05-F00L", fallbackSeq: 4));
    }

    [Fact]
    public void UnknownStep_FallsBackToProvidedSequence()
    {
        Assert.True(StepSequence.IsNotLastStep(Fg, "9-XX", fallbackSeq: 2));
        Assert.False(StepSequence.IsNotLastStep(Fg, "9-XX", fallbackSeq: null));
    }

    private static FT600 R(string code, string size) => new() { C002 = code, C008 = size };

    [Fact]
    public void PickPerComponent_DifferentComponentsSameSeq_AreAllKept()
    {
        // Studs (1-…) và Rim (2-…) cùng Step No. 1 → cả hai đều là previous step
        var picked = StepSequence.PickPerComponent(new[]
        {
            R("1-VB322617-NOC1-2658", "6.5-10"),
            R("2-VB322617-NPY7-2952", "9.5-10"),
        }, "9.5-10");

        Assert.Equal(2, picked.Count);
    }

    [Fact]
    public void PickPerComponent_SameComponentManySizes_PicksSameSize()
    {
        var picked = StepSequence.PickPerComponent(new[]
        {
            R("3-VB322617-NOB1-2951", "9-9.5"),
            R("3-VB322617-NOB1-2952", "9.5-10"),
        }, "9.5-10");

        var one = Assert.Single(picked);
        Assert.Equal("3-VB322617-NOB1-2952", one.C002);
    }

    [Fact]
    public void EmptyBom_IsNotLastStep_False()
        => Assert.False(StepSequence.IsNotLastStep(new List<BomWinlineModel>(), "x", 1));
}
