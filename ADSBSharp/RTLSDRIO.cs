using System;
using System.Windows.Forms;

namespace ADSBSharp
{
    public unsafe delegate void SamplesReadyDelegate(object sender, Complex *data, int length);

    public unsafe class RtlSdrIO : IDisposable
    {       
        private RtlDevice _rtlDevice;
        private uint _frequency = 1090000000;
        private SamplesReadyDelegate _callback;

        ~RtlSdrIO()
        {
            Dispose();
        }

        public void Dispose()
        {        
            GC.SuppressFinalize(this);
        }

        public void SelectDevice(uint index)
        {
            Close();
            _rtlDevice = new RtlDevice(index);
            _rtlDevice.SamplesAvailable += rtlDevice_SamplesAvailable;
            _rtlDevice.Frequency = _frequency;         
        }

        public RtlDevice Device
        {
            get { return _rtlDevice; }
        }

        public void Open()
        {
            var devices = DeviceDisplay.GetActiveDevices();
            foreach (var device in devices)
            {
                try
                {
                    SelectDevice(device.Index);
                    return;
                }
                catch (ApplicationException)
                {                    
                    // Just ignore it
                }
            }
            if (devices.Length > 0)
            {
                throw new ApplicationException(devices.Length + " 兼容设备已经被发现但是正忙");
            }
            throw new ApplicationException("没有兼容设备被发现");
        }

        public void Close()
        {
            if (_rtlDevice != null)
            {
                _rtlDevice.Stop();
                _rtlDevice.SamplesAvailable -= rtlDevice_SamplesAvailable;
                _rtlDevice.Dispose();
                _rtlDevice = null;
            }
        }

        public void Start(SamplesReadyDelegate callback)
        {         
            if (_rtlDevice == null)
            {
                throw new ApplicationException("没有设备被选择");
            }
            _callback = callback;
            try
            {
                _rtlDevice.Start();
            }
            catch
            {
                Open();
                _rtlDevice.Start();
            }
        }

        public void Stop()
        {
            _rtlDevice.Stop();
        }
           
        public double Samplerate
        {
            get { return _rtlDevice == null ? 0.0 : _rtlDevice.Samplerate; }            
        }

        public long Frequency
        {
            get { return _frequency; }
            set
            {
                _frequency = (uint) value;
                if (_rtlDevice != null)
                {
                    _rtlDevice.Frequency = _frequency;
                }
            }
        }

        private void rtlDevice_SamplesAvailable(object sender, SamplesAvailableEventArgs e)
        {
            _callback(this, e.Buffer, e.Length);
        }
    }
}
