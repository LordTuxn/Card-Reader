using System;
using System.Collections.Generic;
using System.Text;

namespace CardReaderOne.Inputs.HID {

    public class HIDOptions {
        public string COMPort { get; set; }

        public int COMPaud { get; set; }

        public string USBPID { get; set; }

        public string USBVID { get; set; }
    }
}