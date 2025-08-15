using OpenTap;

namespace TapExtensions.Interfaces.Uart
{
    public interface IUartDut : IUart, IDut
    {
    }

    public interface IUartInstrument : IUart, IInstrument
    {
    }

    public interface IUart
    {
        void Login(string expectedUsernamePrompt, string expectedPasswordPrompt, string expectedShellPrompt, int timeout);

        bool Expect(string expectedResponse, int timeout);

        string Query(string command, string expectedEndOfMessage, int timeout);

        void Write(string command);
    }
}