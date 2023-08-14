namespace TapExtensions.Interfaces.Gui
{
    public enum EInputButtons
    {
        StartCancel,
        OkCancel,
        YesNo
    }

    public interface IGui
    {
        bool ShowDialog();
    }
}