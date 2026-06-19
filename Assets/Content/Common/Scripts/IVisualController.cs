using Cysharp.Threading.Tasks;

public interface IVisualController
{
    /// <summary>
    /// Waits until the user presses a key.
    /// Returns true when Space is pressed, false when Escape is pressed.
    /// </summary>
    UniTask<bool> WaitNextClicked();
}
