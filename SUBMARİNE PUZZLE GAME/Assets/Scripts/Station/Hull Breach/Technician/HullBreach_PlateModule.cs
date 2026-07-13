using UnityEngine;
using DG.Tweening;

public class HullBreach_PlateModule : MonoBehaviour
{
    [Header("References")]
    public Transform drillVisual; // Sadece modüldeyken aktif olan 3D matkap modeli

    [Header("Settings")]
    public float drillFollowSpeed = 15f;
    public Vector3 angledRotation = new Vector3(45f, 0f, 45f); // Beklerkenki çapraz duruş
    public Vector3 straightRotation = Vector3.zero;            // Vidalarkenki dik duruş
    public float animSpeed = 0.15f;

    private Camera mainCam;
    private bool isInteracting = false;
    private Plane platePlane;
    private bool isDrilling = false;

    // ModuleInteraction'ın OnEnter / OnExit eventlerine (Inspector'dan) bağlanacak
    public void OnModuleEnter()
    {
        isInteracting = true;
        mainCam = Camera.main;

        // Plakanın yüzeyini temsil eden matematiksel bir düzlem oluştur (Fare takibi için)
        platePlane = new Plane(transform.forward, transform.position);

        drillVisual.gameObject.SetActive(true);
        drillVisual.localRotation = Quaternion.Euler(angledRotation);

        // TODO: Oyuncunun elindeki GERÇEK matkabı görünmez yap (InventoryManager üzerinden)
    }

    public void OnModuleExit()
    {
        isInteracting = false;
        drillVisual.gameObject.SetActive(false);

        // TODO: Oyuncunun elindeki GERÇEK matkabı tekrar görünür yap
    }

    private void Update()
    {
        if (!isInteracting || mainCam == null) return;

        // 1. FARE TAKİBİ
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (platePlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            // Matkabı farenin olduğu yere pürüzsüzce götür
            drillVisual.position = Vector3.Lerp(drillVisual.position, hitPoint, Time.deltaTime * drillFollowSpeed);
        }

        // 2. TIKLAMA VE VİDALAMA MANTIĞI
        if (Input.GetMouseButton(0))
        {
            CheckAndDrill(ray);
        }
        else if (isDrilling)
        {
            // Tıklama bırakıldıysa veya boşa tıklandıysa çapraz konuma (Angled) geri dön
            isDrilling = false;
            drillVisual.DOKill(); // Mevcut animasyonu durdur
            drillVisual.DOLocalRotate(angledRotation, animSpeed);

            // TODO: FMOD Matkap sesini durdur
        }
    }

    private void CheckAndDrill(Ray ray)
    {
        // Fareden ileriye doğru Raycast atıp vidayı (WeldPoint) arıyoruz
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            HullBreach_WeldPoint wp = hit.collider.GetComponent<HullBreach_WeldPoint>();

            if (wp != null && !wp.isWelded)
            {
                if (!isDrilling)
                {
                    // Yeni vidalamaya başlandı, matkabı dik konuma getir!
                    isDrilling = true;
                    drillVisual.DOKill();
                    drillVisual.DOLocalRotate(straightRotation, animSpeed);

                    // TODO: FMOD Matkap sesini oynat
                }

                // Vidayı sık
                wp.ApplyWeld(Time.deltaTime);
                return;
            }
        }

        // Eğer butona basılı tutuluyor ama vida üzerinde değilse çapraza dön
        if (isDrilling)
        {
            isDrilling = false;
            drillVisual.DOKill();
            drillVisual.DOLocalRotate(angledRotation, animSpeed);
        }
    }
}