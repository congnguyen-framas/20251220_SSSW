using ScanAndScale.Driver;

namespace SSSW.modelss
{
    public class ConfigModel
    {
        public BarcodeConfig Scanner { get; set; } = new BarcodeConfig() { Enable = true, ReadOnly = false, };
        public RfidConfig RFID { get; set; } = new RfidConfig()
        {
            Enable = true,
            ReadOnly = false,
            Rfid_AutoFindCom = true,
            Rfid_Caption = "Pongee",
            Rfid_Manufact = "Prolific",
            Rfid_Com = "COM1"
        };

        public ScaleConfig Scale { get; set; } = new ScaleConfig()
        {
            Enable = true,
            ReadOnly = false,
            IP = "192.168.80.237",
            Port = 23,
            ModelName = "Scale_Vibra_HAW30",
            CalibGain = 1.0,
            DecimalNum = 0,
        };
    }
}
