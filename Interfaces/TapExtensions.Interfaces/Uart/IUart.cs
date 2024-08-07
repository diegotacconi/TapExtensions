﻿using OpenTap;

namespace TapExtensions.Interfaces.Uart
{
    public interface IUart : IInstrument, IDut
    {
        string PortName { get; }

        bool Expect(string expectedResponse, int timeout);

        string Query(string command, string expectedEndOfMessage, int timeout);

        void Write(string command);
    }
}