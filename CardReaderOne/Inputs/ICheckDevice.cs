using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace CardReaderOne.Inputs {

    public interface ICheckDevice {

        event EventHandler<CheckDeviceArgs> DeviceAttached;

        event EventHandler<CheckDeviceArgs> DeviceRemoved;
    }
}