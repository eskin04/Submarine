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

    public bool IsViewActive<T>() where T : View
    {
        foreach (var view in views)
        {
            if (view.GetType() == typeof(T))
            {
                return view.canvasGroup.alpha > 0f;
            }
        }
        return false;
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
        if (view == null)
        {
            Debug.LogError("ShowViewInternal hatası: View atanmamış!");
            return;
        }

        if (view.canvasGroup == null)
        {
            Debug.LogError($"ShowViewInternal hatası: {view.gameObject.name} objesinde CanvasGroup referansı eksik!");
            return;
        }

        view.canvasGroup.DOFade(1f, 0.2f);
        view.OnShow();
    }

    private void HideViewInternal(View view)
    {
        if (view == null) return;

        if (view.canvasGroup == null)
        {
            Debug.LogError($"HideViewInternal hatası: {view.gameObject.name} objesinde CanvasGroup referansı eksik!");
            return;
        }

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
