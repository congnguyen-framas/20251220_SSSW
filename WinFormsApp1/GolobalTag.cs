using ScanAndScale.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public static class GolobalTag
    {

        public static BarcodeConfig barcodeConfig = new BarcodeConfig()
        {
            Enable = true,
            ReadOnly = false
        };

        public static RfidConfig rfidConfig = new RfidConfig()
        {
            Enable = true,
            ReadOnly = false,
            Rfid_AutoFindCom = true,
            Rfid_Caption = "Pongee",
            Rfid_Manufact = "Prolific",
            Rfid_Com = "COM1"
        };


        public static ScaleConfig scaleConfig = new ScaleConfig()
        {
            Enable = true,
            ReadOnly = false,
            IP = "192.168.80.237",
            Port = 23,
            ModelName = "Scale_Vibra_HAW30",
            CalibGain = 1.0,

        };
    }
}
