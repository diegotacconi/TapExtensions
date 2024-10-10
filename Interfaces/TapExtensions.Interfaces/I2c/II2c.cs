namespace TapExtensions.Interfaces.I2c
{
    public interface II2C
    {
        byte[] Read(ushort slaveAddress, ushort numOfBytes);

        byte[] Read(ushort slaveAddress, ushort numOfBytes, byte[] regAddress);

        void SetBitRate(uint bitRateKhz);

        void SetBusTimeOutInMs(ushort timeOutMs);

        void SlaveDisable();

        void SlaveEnable(byte slaveAddress, ushort maxTxBytes, ushort maxRxBytes);

        byte[] SlaveRead(byte slaveAddress, ushort numOfBytesMax, out int numOfBytesRead);

        void Write(ushort slaveAddress, byte[] command);

        void Write(ushort slaveAddress, byte[] regAddress, byte[] command);
    }
}