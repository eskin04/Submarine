using System.Collections;
using System.Collections.Generic; // HashSet kullanmak için eklendi
using PurrNet.StateMachine;
using UnityEngine;
using PurrNet;

public class WaitForPlayersState : StateNode
{
    [SerializeField] private int minPlayersToStart = 2;

    // Sadece sunucuda (host) tutulacak olan "sahneyi yüklemiş hazır oyuncular" listesi
    private HashSet<PlayerID> readyPlayers = new HashSet<PlayerID>();

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (asServer)
        {
            // Host yeni sahneye girdiğinde listeyi sıfırlar ve kendini "hazır" olarak ekler
            readyPlayers.Clear();
            readyPlayers.Add(networkManager.localPlayer);

            machine.StartCoroutine(WaitForPlayers());
        }
        else
        {
            // Client sahneyi yükleyip bu metoda ulaştığında sunucuya "Ben de geldim" mesajı atar
            SendClientReadyServerRpc();
        }
    }

    // RequireOwnership = false önemlidir, çünkü bu obje direkt client'a ait olmayabilir (Level Manager vb.)
    [ServerRpc(requireOwnership: false)]
    private void SendClientReadyServerRpc(RPCInfo info = default)
    {
        // RPC'yi gönderen client'ın ID'sini hazır listesine ekle
        readyPlayers.Add(info.sender);
    }

    private IEnumerator WaitForPlayers()
    {
        if (isServer)
        {
            RpcShowLoadingScreen();
        }

        // Artık bağlantı sayısını değil, bu spesifik sahneye giriş yapmış hazır oyuncuları bekliyoruz
        while (readyPlayers.Count < minPlayersToStart)
        {
            yield return new WaitForSeconds(0.5f); // Daha hızlı kontrol etmesi için 1 yerine 0.5 yapıldı
        }

        if (isServer)
        {
            Debug.Log("Hiding Loading Screen");
            RpcHideLoadingScreen();
        }

        machine.Next();
    }

    [ObserversRpc]
    private void RpcShowLoadingScreen()
    {
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreen();
        }
    }

    [ObserversRpc]
    private void RpcHideLoadingScreen()
    {
        Debug.Log("RpcHideLoadingScreen called");
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.HideLoadingScreen();
        }
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (asServer)
        {
            readyPlayers.Clear(); // Güvenlik amacıyla çıkışta listeyi temizliyoruz
        }
    }
}