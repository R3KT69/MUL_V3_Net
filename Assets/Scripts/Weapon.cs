using UnityEngine;
using System.Collections;

public enum WeaponType
{
    Shotgun, Pistol, Automatic
}

public class Weapon : MonoBehaviour
{
    [Header("Attributes")]
    public GameObject wep_light;
    public GameObject world_model;
    public ParticleSystem wep_flash;
    private ShellEjector shellEjector;
    public Transform ejection_point;
    public WeaponData wep_data;
    public AudioClip wep_sfx;
    public Transform shoot_point;
    public GameObject bulletTrail;
    public WeaponType weaponType;
    public float lightDuration = 0.1f; // Flash duration
    public bool twoHanded = false;
    public bool isAutomatic = false;
    private Coroutine lightRoutine;
    public int currentAmmo;
    public int maxAmmo;
    public int totalAmmo = 500;

    public void Init()
    {
        maxAmmo = wep_data.maxAmmo;
        currentAmmo = wep_data.maxAmmo;
    }

    void Awake()
    {
        Init();
        if (shellEjector == null)
        shellEjector = GetComponentInParent<ShellEjector>();
    }

    public void TriggerBulletEject()
    {
        if (shellEjector != null && ejection_point != null)
        {
            shellEjector.EmitShell(ejection_point.position, ejection_point.rotation);
        }
    }

    public void TriggerMuzzleEffects()
    {
        if (wep_flash != null)
        {
            wep_flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            wep_flash.Play();
        }

        if (wep_light != null)
        {
            wep_light.SetActive(true);

            // Restart light toggle coroutine
            if (lightRoutine != null)
                StopCoroutine(lightRoutine);

            lightRoutine = StartCoroutine(AutoDisableLight());
        }


    }

    private IEnumerator AutoDisableLight()
    {
        yield return new WaitForSeconds(lightDuration);
        if (wep_light != null)
        {
            wep_light.SetActive(false);
        }
    }
}
