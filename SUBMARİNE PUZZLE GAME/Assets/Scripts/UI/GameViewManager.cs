using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using DG.Tweening;

public class GameViewManager : MonoBehaviour
{
    [SerializeField] private List<View> views = new List<View>();
    [SerializeField] private View defaultView;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);

        foreach (var view in views)
        {
            HideViewInternal(view);
        }
        ShowViewInternal(defaultView);
    }

    private void OnDestroy()
    {

        InstanceHandler.UnregisterInstance<GameViewManager>();
    }

    public void ShowView<T>(bool hideOthers = true) where T : View
    {
        foreach (var view in views)
        {
            if (view.GetType() == typeof(T))
            {
                ShowViewInternal(view);
            }
            else if (hideOthers)
            {
                HideViewInternal(view);
            }
        }

    }

    public void HideView<T>() where T : View
    {
        foreach (var view in views)
        {
            if (view.GetType() == typeof(T))
            {
                HideViewInternal(view);
            }
        }
    }

    private void ShowViewInternal(View view)
    {
        view.canvasGroup.DOFade(1f, 0.2f);
        view.OnShow();
    }

    private void HideViewInternal(View view)
    {
        view.canvasGroup.DOFade(0f, 0.2f);
        view.OnHide();
    }
}

public abstract class View : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public abstract void OnShow();
    public abstract void OnHide();
}
