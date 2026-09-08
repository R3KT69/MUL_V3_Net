using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using System.Collections;

public class EnemyAI_Shooter : MonoBehaviour
{
    [Header("AI Core")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask groundmask, playermask;
    public float health = 100f;
    public Transform aimRotationSource;
    public float sprintSpeed = 5f;
    public float walkSpeed = 2.5f;
    public float speedReduceFactor = 0.5f;

    [Header("AI Vision")]
    public float viewAngle = 30f;
    public float eyeHeight = 1.6f;
    private Vector3 lastSeenPosition;
    private bool hasSeenPlayer = false;
    public Transform target;
    public LayerMask obstructionMask;

    [Header("Combat")]
    public float timeBetweenAttacks = 1f;
    private bool alreadyAttacked;
    public float sightRange = 15f;
    public float attackRange = 10f;
    private float defaultAtkRange;
    public bool playerInSightRange, playerInAttackRange, targetInFOV;

    [Header("Patrol")]
    private Vector3 walkPoint;
    private bool walkPointSet;
    public float walkPointRange = 10f;
    private bool isPatrolling = false;


    [Header("Weapon System")]
    public InventoryManager inventoryManager;
    private GameObject cachedWeaponObj;
    private Weapon cachedWeapon;
    public AudioSource audioSource;

    [Header("Aiming & IK")]
    public RotationConstraint rotationConstraint;
    public float smoothSpeedRotConst = 5f;
    public float targetWeightRotConst = 0f;
    public Vector3 aimRestingPosition = new Vector3(0, 45f, 0);
    private float currentLayerWeight = 0f;
    private float targetLayerWeight = 1f;
    private Animator animator;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        target = player.transform;
        defaultAtkRange = attackRange;
    }

    void Start()
    {
        if (inventoryManager.GetCurrentWeapon() == null)
        {
            GameObject firstWeapon = inventoryManager.playerInventory.allWeapons[0];
            firstWeapon.SetActive(true);
            inventoryManager.activeInventory.Add(firstWeapon);
        }

        cachedWeaponObj = inventoryManager.GetCurrentWeapon();
        cachedWeaponObj.SetActive(true);
        cachedWeapon = cachedWeaponObj.GetComponent<Weapon>();
        animator.SetBool("isTwoHanded", cachedWeapon.twoHanded);
    }

    void Update()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, playermask);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, playermask);

        if (player != null)
        {
            Vector3 dir = (player.position + Vector3.up * 1.5f) - aimRotationSource.position;
            if (dir.sqrMagnitude > 0.001f)
                aimRotationSource.rotation = Quaternion.LookRotation(dir);
        }

        //Debug.Log(isPatrolling);

        targetInFOV = IsTargetInFOV();
        if (targetInFOV && playerInSightRange)
        {
            lastSeenPosition = target.position + Vector3.up * 1.5f;
            hasSeenPlayer = true;
            Debug.Log("[FOV] Target is in field of view.");
        }

        HandleAIMovementAnimation();
        SmoothWeightTransition();

        Debug.Log($"[AI] InSight: {playerInSightRange}, InFOV: {targetInFOV}, InAtk: {playerInAttackRange}");
        Debug.Log($"[AI] seen: {hasSeenPlayer}, seenPos: {lastSeenPosition}, currentPlayerPos {target.position}, currentAIpos {transform.position}");
        

        if (!playerInSightRange || !targetInFOV)
        {
            if (hasSeenPlayer)
            {
                GoToLastSeenPosition();

            } else
            {
                attackRange = defaultAtkRange;
                Patrolling();
            }
            
        }
        else if (playerInAttackRange && targetInFOV)
        {
            LookAtPlayer();
            AttackPlayer();
        }
        else if (!playerInAttackRange && targetInFOV)
        {
            ChasePlayer();
        }
        else
        {
            Debug.LogWarning("[AI] No state matched, forcing patrol.");
            Patrolling();
        }
                    

    }

    void SmoothWeightTransition()
    {
        rotationConstraint.weight = Mathf.Lerp(rotationConstraint.weight, targetWeightRotConst, Time.deltaTime * smoothSpeedRotConst);

        float targetWeight = animator.GetBool("isTwoHanded") ? targetLayerWeight : 0f;
        currentLayerWeight = Mathf.Lerp(currentLayerWeight, targetWeight, Time.deltaTime * smoothSpeedRotConst * 2);

        animator.SetLayerWeight(3, currentLayerWeight);
    }

    void HandleAIMovementAnimation()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);

        float acceleration = localVelocity.z;
        float horizontal = localVelocity.x;

        if (Mathf.Abs(horizontal) > 0.01f && Mathf.Abs(acceleration) > 0.01f)
        {
            float mag = Mathf.Sqrt(acceleration * acceleration + horizontal * horizontal);
            acceleration /= mag;
            horizontal /= mag;
        }

        acceleration = Mathf.Clamp(acceleration, -1f, 1f);
        horizontal = Mathf.Clamp(horizontal, -1f, 1f);

        if (animator.GetBool("isAiming") || isPatrolling)
        {
            acceleration *= speedReduceFactor;
            horizontal *= speedReduceFactor;
        }

        animator.SetFloat("Acceleration", acceleration, 0.1f, Time.deltaTime);
        animator.SetFloat("Horizontal", horizontal, 0.1f, Time.deltaTime);
    }

    void Patrolling()
    {
        Debug.Log("[AI] Patrolling");
        ExitAimingMode();
        isPatrolling = true;

        if (!walkPointSet) SearchWalkPoint();
        if (walkPointSet) agent.SetDestination(walkPoint);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                walkPointSet = false;
            }
        }
                
    }

    void SearchWalkPoint()
    {
        for (int i = 0; i < 10; i++) // Try up to 10 times
        {
            Vector3 randomDirection = Random.insideUnitSphere * walkPointRange;
            randomDirection.y = 0;

            Vector3 potentialPoint = transform.position + randomDirection;

            if (NavMesh.SamplePosition(potentialPoint, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                walkPoint = hit.position;
                walkPointSet = true;
                return;
            }
        }

        walkPointSet = false;
    }

    void ChasePlayer()
    {
        Debug.Log("[AI] Chasing player");
        ExitAimingMode();
        isPatrolling = false;
        agent.speed = sprintSpeed;
        agent.SetDestination(player.position);
    }

    void ExitAimingMode()
    {
        attackRange = defaultAtkRange;
        animator.SetBool("isAiming", false);
        targetWeightRotConst = 0f;
        targetLayerWeight = 1f;
        rotationConstraint.rotationOffset = aimRestingPosition;
        agent.speed = walkSpeed;
    }

    bool EnterAimingMode()
    {
        agent.speed = walkSpeed;
        animator.SetBool("isTwoHanded", cachedWeapon.twoHanded);
        animator.SetBool("isAiming", true);
        animator.SetTrigger("Aiming");
        targetWeightRotConst = 1f;
        targetLayerWeight = 0f;
        rotationConstraint.rotationOffset = aimRestingPosition;
        return true;
    }

    void LookAtPlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;

        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 8f);
        }
    }

    void AttackPlayer()
    {
        Debug.Log("[AI] Attacking player");
        isPatrolling = false;
        agent.SetDestination(transform.position);
        attackRange = sightRange;

        LookAtPlayer();

        if (!alreadyAttacked && cachedWeapon != null && cachedWeapon.currentAmmo > 0)
        {
            EnterAimingMode();

            if (rotationConstraint.weight >= 0.95f)
            {
                Transform shootPoint = cachedWeapon.shoot_point;
                Vector3 targetPoint = player.position + Vector3.up * 1.5f;
                Vector3 direction = (targetPoint - shootPoint.position).normalized;

                if (Physics.Raycast(shootPoint.position, direction, out RaycastHit hit, cachedWeapon.wep_data.range, ~LayerMask.GetMask("Enemy")))
                {
                    Debug.DrawRay(shootPoint.position, direction * cachedWeapon.wep_data.range, Color.red, 1.5f);

                    if (hit.transform == player)
                        Debug.Log("[AI] Hit player");

                    DeployTrail(shootPoint.position, hit.point);
                    cachedWeapon.currentAmmo--;
                    cachedWeapon.TriggerMuzzleEffects();
                    cachedWeapon.TriggerBulletEject();

                    if (audioSource) audioSource.PlayOneShot(cachedWeapon.wep_sfx);
                }

                alreadyAttacked = true;
                Invoke(nameof(ResetAttack), timeBetweenAttacks);
            }
        }
        else if (cachedWeapon.currentAmmo <= 0)
        {
            Reload();
        }
    }

    void GoToLastSeenPosition()
    {
        Debug.Log("[AI] Moving to last seen position");
        ExitAimingMode();
        isPatrolling = false;
        agent.speed = sprintSpeed;
        agent.SetDestination(lastSeenPosition);

        Vector3 flatAIPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatLastSeen = new Vector3(lastSeenPosition.x, 0f, lastSeenPosition.z);

        if (Vector3.Distance(flatAIPos, flatLastSeen) < 1.5f)
        {
            Debug.Log("[AI] Reached last seen position. Resuming patrol.");
            hasSeenPlayer = false;
        }

    }



    void DeployTrail(Vector3 startPos, Vector3 endPos)
    {
        if (cachedWeapon.bulletTrail != null)
        {
            GameObject trailObj = Instantiate(cachedWeapon.bulletTrail, startPos, Quaternion.identity);
            StartCoroutine(AnimateTrail(trailObj.GetComponent<TrailRenderer>(), endPos));
        }
    }

    IEnumerator AnimateTrail(TrailRenderer trail, Vector3 hitPoint)
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

    void Reload()
    {
        int toAdd = cachedWeapon.maxAmmo - cachedWeapon.currentAmmo;
        int ammoUsed = Mathf.Min(toAdd, cachedWeapon.totalAmmo);
        cachedWeapon.currentAmmo += ammoUsed;
        cachedWeapon.totalAmmo -= ammoUsed;
        Debug.Log("[AI] Reloaded.");
    }

    void ResetAttack() => alreadyAttacked = false;

    public void TakeDamage(int damage)
    {
        health = Mathf.Clamp(health - damage, 0, 100);
        if (health <= 0) Destroy(gameObject);
    }

    private bool IsTargetInFOV()
    {
        if (target == null) return false;

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetCenter = target.position + Vector3.up * 1.5f;

        Vector3 direction = (targetCenter - eyePosition);
        float distance = direction.magnitude;

        // Always allow if very close (optional)
        if (distance < 1.5f)
            return true;

        Vector3 dirToTarget = direction.normalized;
        float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);

        if (angleToTarget < viewAngle / 2f)
        {
            // Use SphereCast for more realistic visibility
            float radius = 0.25f;

            if (Physics.SphereCast(eyePosition, radius, dirToTarget, out RaycastHit hit, distance, obstructionMask))
            {
                if (hit.transform != target)
                {
                    Debug.DrawRay(eyePosition, dirToTarget * distance, Color.red, 1f);
                    return false; // Blocked by something else
                }
            }

            Debug.DrawRay(eyePosition, dirToTarget * distance, Color.green, 1f);
            return true;
        }

        return false;
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 forward = transform.forward;

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * forward;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(eyePosition, eyePosition + leftBoundary * sightRange);
        Gizmos.DrawLine(eyePosition, eyePosition + rightBoundary * sightRange);

        if (target != null)
        {
            Vector3 targetCenter = target.position + Vector3.up * 1.5f;
            Vector3 dirToTarget = (targetCenter - eyePosition).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget);
            float distToTarget = Vector3.Distance(eyePosition, targetCenter);
            bool unobstructed = !Physics.Raycast(eyePosition, dirToTarget, distToTarget, obstructionMask);
            bool inFOV = angleToTarget < viewAngle / 2f && distToTarget <= sightRange && unobstructed;

            Gizmos.color = inFOV ? Color.green : Color.red;
            Gizmos.DrawLine(eyePosition, targetCenter);
        }

    }
}