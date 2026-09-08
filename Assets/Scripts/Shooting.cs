using System.Collections;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using UnityEngine.Animations;

public class Shooting : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public CameraShake cameraShake;
    //public Hud hud;
    public InventoryManager inventoryManager;
    public PickableObjects playerInventory;

    [Header("Decals")]
    public ParticleSystem decalParticleStone;
    public ParticleSystem decalParticleMud;
    public ParticleSystem decalParticleMetal;
    public ParticleSystem decalParticleBody;

    [Header("Misc")]
    public AudioSource audioSource;
    public RotationConstraint rotationConstraint;
    public float smoothSpeedRotConst = 5f;
    public float targetWeightRotConst = 0f;
    public Vector3 aimRestingPosition = new Vector3(0, 45f, 0);
    public float aimPitchSendInterval = 0.05f;
    private float currentLayerWeight = 0f;
    private float targetLayerWeight = 1f;
    private bool isRecoiling = false;
    private float recoilReturnDuration = 0.2f;
    private float recoilOutDuration = 0.05f;
    private bool isShootingAuto = false;
    private Animator animator;
    private PlayerCamera playerCameraController;
    private float aimPitchSendTimer;
    private GameObject cachedWeaponObj = null;
    private Weapon cachedWeapon;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerCameraController = GetComponentInChildren<PlayerCamera>(true);
        inventoryManager.Initialize(); // Initialize first weapon 

    }

    void Update()
    {
        if (!isOwner)
        {
            SmoothWeightTransition();
            return;
        }
        
        HandleAimToggle();
        SendAimPitchIfNeeded();
        SmoothWeightTransition();
        inventoryManager.AddWeaponByIndex();

        // Reload Mechanic
        if (!animator.GetBool("isAiming"))
        {
            GameObject currentWeapon = inventoryManager.GetCurrentWeapon();
            if (currentWeapon != cachedWeaponObj)
            {
                cachedWeaponObj = currentWeapon;
                cachedWeapon = cachedWeaponObj.GetComponent<Weapon>();
            }
            GunBehaviourOnReload(cachedWeapon);
        }


        // Weapon switching logic
        if (!animator.GetBool("isAiming"))
        {
            if (Input.GetKeyDown(KeyCode.E)) inventoryManager.SwitchWeapon(+1);
            else if (Input.GetKeyDown(KeyCode.Q)) inventoryManager.SwitchWeapon(-1);
            if (Input.GetKeyDown(KeyCode.F)) inventoryManager.DropWeapon(transform);
        }
    }

    void ShootMechanic() // For shooting fixed projectiles at a time
    {
        if (isRecoiling) return;

        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null) return;

        if (audioSource != null) audioSource.PlayOneShot(currentWeapon.wep_sfx);

        RequestShootVisualsServerRpc();
        StartCoroutine(RecoilRoutine());
        HandleShootingType();

        DebugSpreadCone(currentWeapon.shoot_point.position, playerCamera.transform.forward, currentWeapon.wep_data.spreadAngle);
    }

    void ShootAutomaticMechanic() // For handling Automatic Weapons
    {
        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null || isRecoiling || isShootingAuto || currentWeapon.currentAmmo <= 0) return;

        isShootingAuto = true;
        StartCoroutine(ShootAuto());
        DebugSpreadCone(currentWeapon.shoot_point.position, playerCamera.transform.forward, currentWeapon.wep_data.spreadAngle);
    }

    IEnumerator ShootAuto() // Uses ShootSingle() to simulate automatic fire, called from its Mechanic function
    {
        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null || !isShootingAuto || currentWeapon.currentAmmo <= 0) yield break;

        float fireDelay = 1f / currentWeapon.wep_data.fireRate;

        while (isShootingAuto)
        {
            if (currentWeapon.currentAmmo <= 0)
            {
                isShootingAuto = false;
                yield break;
            }

            StartCoroutine(RecoilRoutine());

            if (audioSource != null)
                audioSource.PlayOneShot(currentWeapon.wep_sfx);

            RequestShootVisualsServerRpc();
            ShootSingle();
            yield return new WaitForSeconds(fireDelay);
        }

        isShootingAuto = false;
    }

    [ServerRpc]
    void RequestShootVisualsServerRpc()
    {
        PlayShootVisualsObserversRpc();
    }

    [ObserversRpc(excludeOwner: true)]
    void PlayShootVisualsObserversRpc()
    {
        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null) return;

        currentWeapon.TriggerMuzzleEffects();

        if (audioSource != null)
            audioSource.PlayOneShot(currentWeapon.wep_sfx);
    }

    void HandleShootingType() // Sets Weapon-Type specific behaviour of shooting, like shotgun, pistol, etc. (Auto weapons are not managed here)
    {
        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null) return;

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol: ShootSingle(); break;
            case WeaponType.Shotgun: ShootShotgun(6); break;
        }
    }

    void ShootSingle() // Responsible for shooting a single bullet
    {
        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null) return;

        Transform shootPoint = currentWeapon.shoot_point;
        Vector3 direction = playerCamera.transform.forward;

        Vector3 right = playerCamera.transform.right;
        Vector3 up = playerCamera.transform.up;

        // Random cone recoil
        float pitch = Random.Range(-currentWeapon.wep_data.spreadAngle, currentWeapon.wep_data.spreadAngle);
        float yaw = Random.Range(-currentWeapon.wep_data.spreadAngle, currentWeapon.wep_data.spreadAngle);

        direction = Quaternion.AngleAxis(pitch, right) * direction;
        direction = Quaternion.AngleAxis(yaw, up) * direction;

        Ray ray = new Ray(playerCamera.transform.position, direction);
        Vector3 hitPoint = new Vector3();

        // Action upon hit
        GunActionUponHit(ray, currentWeapon, out hitPoint);
        // Gun Behaviour
        GunBehaviourUponShoot(currentWeapon);



        Debug.DrawLine(shootPoint.position, hitPoint, Color.red, 2f);
        GameObject trailObj = Instantiate(currentWeapon.bulletTrail, shootPoint.position, Quaternion.identity);
        StartCoroutine(AnimateTrail(trailObj.GetComponent<TrailRenderer>(), hitPoint));
        //hud.RefreshWeaponAmmo(currentWeapon.gameObject);
    }

    void GunBehaviourUponShoot(Weapon currentWeapon)
    {
        if (currentWeapon.currentAmmo >= 0)
        {
            currentWeapon.currentAmmo--;
            currentWeapon.TriggerMuzzleEffects();
            currentWeapon.TriggerBulletEject();
            cameraShake.TriggerShake(0.2f);
        }
        Debug.Log($"Current: {currentWeapon.currentAmmo} | Max: {currentWeapon.maxAmmo} | Total: {currentWeapon.totalAmmo}");
    }

    void GunBehaviourOnReload(Weapon currentWeapon)
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentWeapon.currentAmmo >= currentWeapon.maxAmmo || currentWeapon.totalAmmo <= 0) return;

            int toAdd = currentWeapon.maxAmmo - currentWeapon.currentAmmo;
            int ammoUsed = Mathf.Min(toAdd, currentWeapon.totalAmmo);

            Debug.Log("Reloading...");
            currentWeapon.currentAmmo += ammoUsed;
            currentWeapon.totalAmmo -= ammoUsed;

            Debug.Log($"Current: {currentWeapon.currentAmmo} | Max: {currentWeapon.maxAmmo} | Total: {currentWeapon.totalAmmo}");
        }
    }

    void GunActionUponHit(Ray ray, Weapon currentWeapon, out Vector3 hitPoint)
    {
        if (Physics.Raycast(ray, out RaycastHit hitInfo, currentWeapon.wep_data.range, ~LayerMask.GetMask("Player")))
        {
            hitPoint = hitInfo.point;
            if (hitInfo.rigidbody != null)
                hitInfo.rigidbody.AddForce(ray.direction * 5f, ForceMode.Impulse);

            DeployDecal(hitInfo);
            Debug.Log($"Hit: {hitInfo.collider.name}");
        }
        else
        {
            hitPoint = ray.origin + ray.direction * currentWeapon.wep_data.range;
        }

    }
    void ShootShotgun(int pelletCount) // Shoots n number of bullets.
    {
        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null) return;

        Transform shootPoint = currentWeapon.shoot_point;
        float spread = currentWeapon.wep_data.spreadAngle;

        GunBehaviourUponShoot(currentWeapon);


        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 direction = playerCamera.transform.forward;

            Vector3 right = playerCamera.transform.right;
            Vector3 up = playerCamera.transform.up;

            // Random cone recoil
            float pitch = Random.Range(-spread, spread);
            float yaw = Random.Range(-spread, spread);

            direction = Quaternion.AngleAxis(pitch, right) * direction;
            direction = Quaternion.AngleAxis(yaw, up) * direction;

            Ray ray = new Ray(playerCamera.transform.position, direction);
            Vector3 hitPoint = new Vector3();

            GunActionUponHit(ray, currentWeapon, out hitPoint);

            Debug.DrawLine(shootPoint.position, hitPoint, Color.yellow, 1.5f);
            GameObject trailObj = Instantiate(currentWeapon.bulletTrail, shootPoint.position, Quaternion.identity);
            StartCoroutine(AnimateTrail(trailObj.GetComponent<TrailRenderer>(), hitPoint));
        }

        //hud.RefreshWeaponAmmo(currentWeapon.gameObject);
    }
    void HandleAimToggle() // For shoulder aiming
    {
        Weapon currentWeapon = inventoryManager.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (currentWeapon == null) return;

        bool isAiming = animator.GetBool("isAiming");

        animator.SetBool("isTwoHanded", currentWeapon.twoHanded);
        bool isAutomatic = currentWeapon.isAutomatic;
        bool hasAmmo = currentWeapon.currentAmmo > 0;

        if (isAiming && hasAmmo)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!isAutomatic)
                    ShootMechanic();
                else if (!isShootingAuto)
                    ShootAutomaticMechanic();
            }

            if (Input.GetMouseButtonUp(0))
                isShootingAuto = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (!isAiming)
            {
                ApplyAimState(true);
                RequestAimStateServerRpc(true);
            }
            else
            {
                isShootingAuto = false;
                ApplyAimState(false);
                RequestAimStateServerRpc(false);
            }
        }
    }

    void ApplyAimState(bool aiming)
    {
        targetWeightRotConst = aiming ? 1f : 0f;
        targetLayerWeight = aiming ? 0f : 1f;
        animator.SetBool("isAiming", aiming);

        if (aiming)
        {
            rotationConstraint.rotationOffset = aimRestingPosition;
            animator.SetTrigger("Aiming");
        }
    }

    [ServerRpc]
    void RequestAimStateServerRpc(bool aiming)
    {
        ApplyAimStateObserversRpc(aiming);
    }

    [ObserversRpc(excludeOwner: true)]
    void ApplyAimStateObserversRpc(bool aiming)
    {
        ApplyAimState(aiming);
    }

    void SendAimPitchIfNeeded()
    {
        if (!animator.GetBool("isAiming") || playerCameraController == null)
            return;

        aimPitchSendTimer -= Time.deltaTime;
        if (aimPitchSendTimer > 0f)
            return;

        aimPitchSendTimer = aimPitchSendInterval;
        RequestAimPitchServerRpc(
            playerCameraController.VerticalAimAngle,
            playerCameraController.HorizontalAimAngle);
    }

    [ServerRpc]
    void RequestAimPitchServerRpc(float pitch, float yaw)
    {
        ApplyAimPitchObserversRpc(pitch, yaw);
    }

    [ObserversRpc(excludeOwner: true)]
    void ApplyAimPitchObserversRpc(float pitch, float yaw)
    {
        if (playerCameraController == null) return;

        playerCameraController.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void SmoothWeightTransition() // Transitions Weight of IK, responsible for seamless transition into aiming mode
    {
        rotationConstraint.weight = Mathf.Lerp(rotationConstraint.weight, targetWeightRotConst, Time.deltaTime * smoothSpeedRotConst);

        if (animator.GetBool("isTwoHanded"))
            currentLayerWeight = Mathf.Lerp(currentLayerWeight, targetLayerWeight, Time.deltaTime * smoothSpeedRotConst * 2);
        else
            currentLayerWeight = Mathf.Lerp(currentLayerWeight, 0f, Time.deltaTime * smoothSpeedRotConst * 2);

        animator.SetLayerWeight(3, currentLayerWeight);
    }
    IEnumerator RecoilRoutine() // Simulates recoil in the IK (makes character arm go up) **This is not the actual recoil of the gun**
    {
        isRecoiling = true;

        Vector3 startOffset = rotationConstraint.rotationOffset;
        Vector3 recoilOffset = new Vector3(
            Random.Range(-5f, -10f), // Vertical offset
            Random.Range(45f, 50f), // Horizontal offset
            Random.Range(-5f, -10f) // Vertical offset
        );

        float elapsed = 0f;
        while (elapsed < recoilOutDuration)
        {
            rotationConstraint.rotationOffset = Vector3.Lerp(startOffset, recoilOffset, elapsed / recoilOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rotationConstraint.rotationOffset = recoilOffset;
        yield return new WaitForSeconds(0.01f); // A Slight pause at the peak of recoil

        Vector3 returnOffset = aimRestingPosition; // Default pos of Aiming
        elapsed = 0f;
        while (elapsed < recoilReturnDuration)
        {
            rotationConstraint.rotationOffset = Vector3.Lerp(recoilOffset, returnOffset, elapsed / recoilReturnDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rotationConstraint.rotationOffset = returnOffset;
        isRecoiling = false;
    }
    void DebugSpreadCone(Vector3 origin, Vector3 forward, float spreadAngle, int rays = 250, float length = 5f) // To Debug Recoil Cone
    {
        float angleRad = spreadAngle * Mathf.Deg2Rad;

        for (int i = 0; i < rays; i++)
        {
            float z = Mathf.Cos(angleRad);
            float theta = Random.Range(0f, 2f * Mathf.PI);
            float r = Mathf.Sqrt(1f - z * z);
            float x = r * Mathf.Cos(theta);
            float y = r * Mathf.Sin(theta);

            Vector3 localDirection = new Vector3(x, y, z);
            Vector3 worldDirection = Quaternion.LookRotation(forward) * localDirection;
            Debug.DrawRay(origin, worldDirection * length, Color.white, 1.5f);
        }
    }
    IEnumerator AnimateTrail(TrailRenderer trail, Vector3 hitPoint) // For the trail behind bullet
    {
        float speed = 500f;
        Vector3 start = trail.transform.position;
        float travelTime = Vector3.Distance(start, hitPoint) / speed;
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            if (trail == null) yield break;
            trail.transform.position = Vector3.Lerp(start, hitPoint, elapsed / travelTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = hitPoint;
        yield return new WaitForSeconds(trail.time);
        if (trail != null) Destroy(trail.gameObject);
    }
    void DeployDecal(RaycastHit hitInfo) // Instantiates a decal at hit point
    {
        if (hitInfo.collider.CompareTag("Untagged")) return;

        Vector3 forward = hitInfo.normal;
        Vector3 arbitrary = Mathf.Abs(forward.y) < 0.99f ? Vector3.up : Vector3.forward;
        Vector3 right = Vector3.Cross(arbitrary, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right);

        Quaternion decalRotation = Quaternion.LookRotation(forward, up);
        Vector3 spawnPos = hitInfo.point + hitInfo.normal * 0.01f;

        ParticleSystem selectedDecal = hitInfo.collider.tag switch
        {
            "Stone" => decalParticleStone,
            "Mud" => decalParticleMud,
            "Metal" => decalParticleMetal,
            "Flesh" => decalParticleBody,
            _ => null
        };

        if (selectedDecal != null)
            Instantiate(selectedDecal, spawnPos, decalRotation);
    }
    
}
