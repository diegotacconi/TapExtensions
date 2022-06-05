namespace TapExtensions.Interfaces.Gui
{
    public enum EInputButtons
    {
        StartCancel,
        OkCancel,
        YesNo
    }

    public enum EBorderStyle
    {
        None,
        Green,
        Yellow,
        Orange,
        Red,
        Blue,
        Black,
        Gray,
        White,
    }

    public interface IGui
    {
        bool ShowDialog();
    }
}