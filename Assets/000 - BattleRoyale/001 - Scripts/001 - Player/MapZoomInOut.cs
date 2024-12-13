using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MapZoomInOut : NetworkBehaviour
{
    [SerializeField] private Transform playerTF;
    [SerializeField] private Camera mapCamera;

    [Space]
    [SerializeField] private Slider mapSlider;
    [SerializeField] private float mapZoomOutMin;

    [Space]
    [SerializeField] private Vector3 waitingAreaCenter;
    [SerializeField] private float waitingAreaMax;

    [Space]
    [SerializeField] private Vector3 battleAreaCenter;
    [SerializeField] private float battleAreaMax;

    [field: Space]
    [field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }

    public async override void Spawned()
    {
        while (!Runner) await Task.Delay(100);

        if (!HasInputAuthority) return;

        while(!ServerManager) await Task.Delay(100);

        mapCamera.transform.parent = null;

        ServerManager.OnCurrentStateChange += CheckPosition;

        UpdateZoomInOut();
    }

    private void OnDisable()
    {
        ServerManager.OnCurrentStateChange -= CheckPosition;
    }

    private void CheckPosition(object sender, EventArgs e)
    {
        if (ServerManager.CurrentGameState == GameState.WAITINGAREA)
        {
            mapCamera.orthographicSize = waitingAreaMax;

            transform.position = new Vector3(playerTF.position.x, mapCamera.transform.position.y, playerTF.position.z);
        }
        else
        {
            mapCamera.orthographicSize = battleAreaMax;

            transform.position = new Vector3(playerTF.position.x, mapCamera.transform.position.y, playerTF.position.z);
        }
    }

    public void UpdateZoomInOut()
    {
        if (ServerManager == null) return;

        if (ServerManager.CurrentGameState == GameState.WAITINGAREA)
        {
            mapCamera.orthographicSize = Mathf.Lerp(waitingAreaMax, mapZoomOutMin, mapSlider.value);

            if (mapSlider.value <= 0)
                mapCamera.transform.position = waitingAreaCenter;
            else
                mapCamera.transform.position = new Vector3(playerTF.position.x, mapCamera.transform.position.y, playerTF.position.z);
        }
        else
        {
            mapCamera.orthographicSize = Mathf.Lerp(battleAreaMax, mapZoomOutMin, mapSlider.value);

            if (mapSlider.value <= 0)
                mapCamera.transform.position = waitingAreaCenter;
            else
                mapCamera.transform.position = new Vector3(playerTF.position.x, mapCamera.transform.position.y, playerTF.position.z);
        }
    }

    public void ManualZoomInOut(bool isAdd)
    {
        if (isAdd)
        {
            if (mapSlider.value >= 1) return;

            mapSlider.value += 0.1f;
        }
        else
        {
            if (mapSlider.value <= 0) return;

            mapSlider.value -= 0.1f;
        }
    }
}
