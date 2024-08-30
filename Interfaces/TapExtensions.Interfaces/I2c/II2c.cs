using OpenTap;

namespace TapExtensions.Interfaces.I2c
{
    public interface II2C : IInstrument
    {
        void Write(ushort slaveAddress, ushort numOfBytes, byte[] dataOut);

        void Write(ushort slaveAddress, byte[] registerAddress, ushort numOfBytes, byte[] dataOut);

        byte[] Read(ushort slaveAddress, ushort numOfBytes);

        byte[] Read(ushort slaveAddress, ushort numOfBytes, byte[] regAddress);

        void SetBitRate(uint bitRateKhz);

        void SetBusTimeOutInMs(ushort timeOutMs);

        void SlaveEnable(byte slaveAddress, ushort maxTxBytes, ushort maxRxBytes);

        void SlaveDisable();

        byte[] SlaveRead(byte slaveAddress, ushort numOfBytesMax, out int numOfBytesRead);
    }
}