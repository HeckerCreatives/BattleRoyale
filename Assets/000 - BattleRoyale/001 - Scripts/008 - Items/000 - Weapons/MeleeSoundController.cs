using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeSoundController : NetworkBehaviour
{
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip attackOneClip;
    [SerializeField] private AudioClip attackTwoClip;
    [SerializeField] private AudioClip hitClip;

    public void PlayAttackOne() => sfxAudioSource.PlayOneShot(attackOneClip);

    public void PlayAttackTwo() => sfxAudioSource.PlayOneShot(attackTwoClip);

    public void PlayHit() => sfxAudioSource.PlayOneShot(hitClip);
}
