using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class VisualSceneBootstrapper : MonoBehaviour
{
    [SerializeField] private List<VisualBase> Visuals;
    [SerializeField] private bool RunParallel = false;

    private VisualController Controller;

    private void Start()
    {
        Controller = new VisualController();

        foreach (var visual in Visuals)
        {
            visual.Init();
        }

        if (RunParallel)
        {
            RunAllParallel().Forget();
        }
        else
        {
            RunSequence().Forget();
        }
    }

    private async UniTask RunSequence()
    {
        foreach (var visual in Visuals)
        {
            await visual.Run(Controller);
        }
    }

    private async UniTask RunAllParallel()
    {
        await UniTask.WhenAll(Visuals.Select(visual => visual.Run(Controller)));
    }
}
