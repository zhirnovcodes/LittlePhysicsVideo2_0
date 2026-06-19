using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

public class VisualController : IVisualController
{
    private readonly InputAction NextAction = new InputAction(binding: "<Keyboard>/space");
    private readonly InputAction AbortAction = new InputAction(binding: "<Keyboard>/escape");

    public VisualController()
    {
        NextAction.Enable();
        AbortAction.Enable();
    }

    public async UniTask<bool> WaitNextClicked()
    {
        while (true)
        {
            await UniTask.Yield();

            if (NextAction.WasPressedThisFrame())
            {
                return true;
            }

            if (AbortAction.WasPressedThisFrame())
            {
                return false;
            }
        }
    }
}
