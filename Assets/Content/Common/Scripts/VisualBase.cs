using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class VisualBase : MonoBehaviour
{
    public abstract void Init();
    public abstract UniTask Run(IVisualController controller);
}
