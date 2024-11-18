using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerOwnObjectEnabler : NetworkBehaviour
{
    [SerializeField] private GameObject canvasPlayer;
    [SerializeField] private GameObject playerVcam;
    [SerializeField] private GameObject playerAimVCam;
    [SerializeField] private GameObject playerMapIcon;
    [SerializeField] private GameObject playerMinimapCam;
    [SerializeField] private GameObject playerSpawnLocCam;

    async public override void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        if (!HasInputAuthority) return;

        canvasPlayer.SetActive(true);
        playerVcam.SetActive(true);
        playerAimVCam.SetActive(true);
        playerMapIcon.SetActive(true);
        playerMinimapCam.SetActive(true);
        playerSpawnLocCam.SetActive(true);
    }
}
