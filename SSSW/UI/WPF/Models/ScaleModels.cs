// ============================================================================
//  ScaleModels.cs
//  Shared model types for the Shot Weight MVVM layer
//  Namespace : SSSW.UI.WPF.Models
// ============================================================================
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SSSW.UI.WPF.Models
{
    // ─── Tolerance category ───────────────────────────────────────────────────
    public enum ToleranceCategory { Idle, Ok, Warn, Err }

    // ─── ReferenceRow ─────────────────────────────────────────────────────────
    /// <summary>
    /// Một dòng hiển thị trong bảng REFERENCE VALUES (STD / Actual / Delta).
    /// </summary>
    public class ReferenceRow : INotifyPropertyChanged
    {
        private int     _no;
        private string  _fieldName = "";
        private string  _unit      = "";
        private double? _std;
        private double? _actual;

        public int No
        {
            get => _no;
            set { _no = value; OnPropertyChanged(); }
        }

        public string FieldName
        {
            get => _fieldName;
            set { _fieldName = value; OnPropertyChanged(); }
        }

        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        public double? Std
        {
            get => _std;
            set
            {
                _std = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StdDisplay));
                OnPropertyChanged(nameof(DeltaDisplay));
                OnPropertyChanged(nameof(Tolerance));
            }
        }

        public double? Actual
        {
            get => _actual;
            set
            {
                _actual = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActualDisplay));
                OnPropertyChanged(nameof(DeltaDisplay));
                OnPropertyChanged(nameof(Tolerance));
            }
        }

        // ── Computed display strings ─────────────────────────────────────────
        public string StdDisplay    => Std.HasValue    ? Std.Value.ToString("F2")    : "—";
        public string ActualDisplay => (Actual.HasValue && Actual > 0)
                                           ? Actual.Value.ToString("F2") : "—";
        public string DeltaDisplay  => Delta.HasValue
                                           ? ((Delta >= 0 ? "+" : "") + Delta.Value.ToString("F2") + " (" + DeltaPct.Value.ToString("F2")+" %)") 
                                           : "—";

        public double? Delta => (Actual.HasValue && Std.HasValue && Std.Value != 0)
            ? Math.Round(Actual.Value - Std.Value, 3) : (double?)null;

        public double? DeltaPct => (Actual.HasValue && Std.HasValue && Std.Value != 0)
            ? (Actual.Value - Std.Value) / Std.Value * 100.0 : (double?)null;

        public ToleranceCategory Tolerance
        {
            get
            {
                if (DeltaPct == null) return ToleranceCategory.Idle;
                var abs = Math.Abs(DeltaPct.Value);
                return abs <= GlobalVariable.ConfigSystem.DeltaLevel1 ? ToleranceCategory.Ok
                     : abs <= GlobalVariable.ConfigSystem.DeltaLevel2 ? ToleranceCategory.Warn
                     : ToleranceCategory.Err;
            }
        }

        // ── INotifyPropertyChanged ───────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
