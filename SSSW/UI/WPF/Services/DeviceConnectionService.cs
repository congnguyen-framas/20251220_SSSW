// ============================================================================
//  DeviceConnectionService.cs
//  Singleton owning the ScanAndScale.Core hardware drivers (Barcode/RFID/Scale)
//  so that ShotWeightWindow and ShotWeightFGWindow share ONE connection each
//  instead of each window creating (and disposing) its own — avoiding
//  duplicate event subscriptions and TCP/IP scale conflicts.
//
//  Registered as a DI singleton in Program.cs. Windows subscribe to the
//  re-broadcast events below and call EnsureInitialized() in their OnLoaded;
//  Shutdown() is called exactly once from Program.cs after the WPF message
//  loop exits — NOT from any window's OnClosing.
//  Namespace : SSSW.UI.WPF.Services
// ============================================================================
using ScanAndScale.Core.Drivers;
using ScanAndScale.Core.Models;
using SSSW.modelss;

namespace SSSW.UI.WPF.Services
{
    public class DeviceConnectionService
    {
        // ── Shared driver instances ─────────────────────────────────────────
        public BarcodeDriver Barcode => BarcodeDriver.Instance;
        public RfidDriver Rfid => RfidDriver.Instance;
        public ScaleDriver? Scale { get; private set; }

        // ── Re-broadcast events (windows subscribe to these, not the drivers directly) ──
        public event EventHandler<DataValueChangedEventArgs>? BarcodeChanged;
        public event EventHandler<DataValueChangedEventArgs>? RfidChanged;
        public event EventHandler<DataValueChangedEventArgs>? ScaleChanged;

        // ── Last-known status (so a window that subscribes AFTER the drivers are
        //    already connected — e.g. ShotWeightFGWindow opened on top of an
        //    already-running main window — can read the current state immediately
        //    instead of showing DriverStatus.Unknown until the next real event) ──
        public DriverStatus BarcodeStatus { get; private set; } = DriverStatus.Unknown;
        public DriverStatus RfidStatus { get; private set; } = DriverStatus.Unknown;
        public DriverStatus ScaleStatus { get; private set; } = DriverStatus.Unknown;

        private readonly object _gate = new();
        private bool _initialized;

        /// <summary>
        /// Idempotent: the first window to load actually initializes the drivers
        /// from GlobalVariable.ConfigSystem; whichever window loads afterwards
        /// (main or FG) just reuses the already-connected drivers.
        /// </summary>
        public void EnsureInitialized()
        {
            lock (_gate)
            {
                if (_initialized) return;
                _initialized = true;

                Barcode.DataValueChanged += (s, e) => { BarcodeStatus = e.NewValue.DriverStatus; BarcodeChanged?.Invoke(s, e); };
                Rfid.DataValueChanged += (s, e) => { RfidStatus = e.NewValue.DriverStatus; RfidChanged?.Invoke(s, e); };

                ApplyHardwareConfig();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  HARDWARE CONFIG
        //  Moved verbatim from ShotWeightWindow.xaml.cs's ApplyHardwareConfig():
        //  Map ScanAndScale.Driver config (GlobalVariable.ConfigSystem) →
        //  ScanAndScale.Core config → Initialize.
        // ════════════════════════════════════════════════════════════════════
        private void ApplyHardwareConfig()
        {
            var cfg = GlobalVariable.ConfigSystem;

            // ── Barcode (Zebra CoreScanner SDK) ──────────────────────────────
            var barcodeCfg = new BarcodeConfig
            {
                Enable = cfg.Scanner.Enable,
                ReadOnly = cfg.Scanner.ReadOnly
            };

            if (barcodeCfg.Enable)
            {
                bool ok = Barcode.Initialize(barcodeCfg);
                BarcodeStatus = ok ? DriverStatus.Connected : DriverStatus.Disconnected;
            }
            else
                BarcodeStatus = DriverStatus.Disconnected;

            // ── RFID (SerialPort / COM) ───────────────────────────────────────
            //  Map: Rfid_Com → ComPort, Rfid_AutoFindCom → AutoFindCom,
            //       Rfid_Caption → DeviceCaption, Rfid_Manufact → DeviceManufacturer
            var rfidCfg = new RfidConfig
            {
                Enable = cfg.RFID.Enable,
                AutoFindCom = cfg.RFID.Rfid_AutoFindCom,
                ComPort = cfg.RFID.Rfid_Com,
                DeviceCaption = cfg.RFID.Rfid_Caption,
                DeviceManufacturer = cfg.RFID.Rfid_Manufact
            };

            if (rfidCfg.Enable)
            {
                bool ok = Rfid.Initialize(rfidCfg);
                RfidStatus = ok ? DriverStatus.Connected : DriverStatus.Disconnected;
            }
            else
                RfidStatus = DriverStatus.Disconnected;

            // ── Scale (TCP/IP) ────────────────────────────────────────────────
            //  Map: TimeScan → TimeScanMs (tên property khác nhau giữa hai thư viện)
            var scaleCfg = new ScaleConfig
            {
                Enable = cfg.Scale.Enable == true,
                ReadOnly = cfg.Scale.ReadOnly == true,
                IP = cfg.Scale.IP,
                Port = cfg.Scale.Port,
                TimeScanMs = cfg.Scale.TimeScan,
                CalibZero = cfg.Scale.CalibZero,
                CalibGain = cfg.Scale.CalibGain,
                DecimalNum = cfg.Scale.DecimalNum,
                ModelName = cfg.Scale.ModelName,
                CheckStable = cfg.Scale.CheckStable == true,
                CheckTare = cfg.Scale.CheckTare == true,
            };

            bool enableScale = scaleCfg.Enable && (cfg.EnableReadScale ?? false);
            if (enableScale)
            {
                Scale = new ScaleDriver();
                Scale.DataValueChanged += (s, e) => { ScaleStatus = e.NewValue.DriverStatus; ScaleChanged?.Invoke(s, e); };
                Scale.Initialize(scaleCfg);
                // Connected/Disconnected status is reflected as soon as the first event fires.
            }
            else
                ScaleStatus = DriverStatus.Disconnected;
        }

        /// <summary>Called once, from Program.cs on host shutdown — NOT from window Closing.</summary>
        public void Shutdown()
        {
            lock (_gate)
            {
                Barcode.Dispose();
                Rfid.Dispose();
                Scale?.Dispose();
                Scale = null;
            }
        }
    }
}
