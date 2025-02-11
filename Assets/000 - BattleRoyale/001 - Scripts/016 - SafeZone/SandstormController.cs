using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SandstormController : MonoBehaviour
{
    [SerializeField] private Material sandStorm;
    [SerializeField] private Material sandStormParticle;
    [SerializeField] private float offset;
    [SerializeField] private float particleOffset;

    [Header("DEBUGGER")]
    [SerializeField] private GameObject zoneObj;
    [SerializeField] private SafeZoneController zoneController;
    [SerializeField] private float radius;

    private async void Awake()
    {
        while (zoneController == null)
        {
            zoneObj = GameObject.FindGameObjectWithTag("BattleArea");

            if (zoneObj != null)
                zoneController = zoneObj.GetComponent<SafeZoneController>();

            await Task.Yield();
        }
    }

    private void Update()
    {
        SandstormControll();
    }

    private void SandstormControll()
    {
        if (zoneController == null) return;

        float radius = zoneController.transform.localScale.x / 2f;

        sandStorm.SetVector("_Center", zoneController.SpawnPosition);
        sandStorm.SetFloat("_Radius", radius - offset);

        sandStormParticle.SetVector("_Center", zoneController.SpawnPosition);
        sandStormParticle.SetFloat("_Radius", radius - particleOffset);
    }
}
