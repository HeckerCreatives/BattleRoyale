using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunSoundController : NetworkBehaviour
{
    [SerializeField] private AudioSource gunSource;
    [SerializeField] private AudioSource reloadSource;
    [SerializeField] private AudioClip gunClip;
    [SerializeField] private AudioClip shootCooldownClip;
    [SerializeField] private AudioClip reloadClip;

    public void PlayGunshot() => gunSource.PlayOneShot(gunClip);

    public void PlayShootCooldown() => reloadSource.PlayOneShot(shootCooldownClip);

    public void PlayReload() => reloadSource.PlayOneShot(reloadClip);
}
