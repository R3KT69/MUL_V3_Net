using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RagdollManager : MonoBehaviour
{
    public List<Rigidbody> rg_rigidbodies;

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private EnemyAI_Shooter enemyAI_Shooter;

    void Awake()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        enemyAI_Shooter = GetComponent<EnemyAI_Shooter>();

        rg_rigidbodies = new List<Rigidbody>();

        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject != gameObject)
                rg_rigidbodies.Add(rb);
        }

        SetRagdollState(false); // Start in animated state
    }

    public void SetRagdollState(bool isRagdoll)
    {
        if (animator != null)
            animator.enabled = !isRagdoll;

        if (navMeshAgent != null)
            navMeshAgent.enabled = !isRagdoll;

        if (enemyAI_Shooter != null)
            enemyAI_Shooter.inventoryManager.GetCurrentWeapon().SetActive(false);
            enemyAI_Shooter.enabled = !isRagdoll;

        foreach (Rigidbody rb in rg_rigidbodies)
        {
            if (rb == null) continue;

            rb.isKinematic = !isRagdoll;
            rb.useGravity = isRagdoll;

            // Mass shouldn't be too low or high
            rb.mass = 3f;

            // Clamp spinning
            rb.linearDamping = isRagdoll ? 1.5f : 0f;
            rb.angularDamping = isRagdoll ? 4.0f : 0.05f;

            if (isRagdoll)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.maxAngularVelocity = 1f;
            }
        }


    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetRagdollState(true);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            SetRagdollState(false);
        }
    }
}
