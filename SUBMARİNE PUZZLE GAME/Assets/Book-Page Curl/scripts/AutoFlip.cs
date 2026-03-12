using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Book))]
public class AutoFlip : MonoBehaviour
{
    public Handbook handbook;
    public FlipMode Mode;
    public float PageFlipTime = 0.5f;
    public Book ControledBook;

    bool isFlipping = false;

    private void OnEnable()
    {
        ControledBook.OnFlip.AddListener(PageFlipped);
        isFlipping = false;
    }

    void OnDisable()
    {
        ControledBook.OnFlip.RemoveListener(PageFlipped);
    }

    void PageFlipped()
    {
        isFlipping = false;
    }

    void Update()
    {
        if (isFlipping) return;

        // Kendi sisteminle entegrasyon
        if (handbook != null && !handbook.IsOperate && !handbook.IsOperating) return;

        float scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta > 0)
        {
            FlipRightPage();
        }
        else if (scrollDelta < 0)
        {
            FlipLeftPage();
        }
    }

    public void FlipRightPage()
    {
        if (isFlipping) return;
        if (ControledBook.currentPage >= ControledBook.TotalPageCount) return;
        isFlipping = true;

        float xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
        float xl = ((ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2) * 0.9f;
        float h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;

        float startX = xc + xl;
        float endX = xc - xl;

        ControledBook.DragRightPageToPoint(new Vector3(startX, GetParabolaY(startX, xc, xl, h), 0));

        DOVirtual.Float(startX, endX, PageFlipTime, (x) =>
        {
            float y = GetParabolaY(x, xc, xl, h);
            ControledBook.UpdateBookRTLToPoint(new Vector3(x, y, 0));
        })
        .SetEase(Ease.InOutSine)
        .OnComplete(() =>
        {
            ControledBook.ReleasePage();
        });
    }

    public void FlipLeftPage()
    {
        if (isFlipping) return;
        if (ControledBook.currentPage <= 0) return;
        isFlipping = true;

        float xc = (ControledBook.EndBottomRight.x + ControledBook.EndBottomLeft.x) / 2;
        float xl = ((ControledBook.EndBottomRight.x - ControledBook.EndBottomLeft.x) / 2) * 0.9f;
        float h = Mathf.Abs(ControledBook.EndBottomRight.y) * 0.9f;

        float startX = xc - xl;
        float endX = xc + xl;

        ControledBook.DragLeftPageToPoint(new Vector3(startX, GetParabolaY(startX, xc, xl, h), 0));

        DOVirtual.Float(startX, endX, PageFlipTime, (x) =>
        {
            float y = GetParabolaY(x, xc, xl, h);
            ControledBook.UpdateBookLTRToPoint(new Vector3(x, y, 0));
        })
        .SetEase(Ease.InOutSine)
        .OnComplete(() =>
        {
            ControledBook.ReleasePage();
        });
    }

    private float GetParabolaY(float x, float xc, float xl, float h)
    {
        return (-h / (xl * xl)) * (x - xc) * (x - xc);
    }
}