using UnityEngine;

public class Weapon_WorldModel : MonoBehaviour
{
    [SerializeField] private int weaponIndex; // Set this in Inspector
    private GameObject weaponPrefab;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Shooting shooting = player.GetComponent<Shooting>();
            if (shooting != null && shooting.playerInventory != null)
            {
                PickableObjects playerInventory = shooting.playerInventory;

                if (weaponIndex >= 0 && weaponIndex < playerInventory.allWeapons.Count)
                {
                    weaponPrefab = playerInventory.allWeapons[weaponIndex];
                }
                else
                {
                    Debug.LogWarning($"Weapon index {weaponIndex} is out of range in {gameObject.name}.");
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Shooting shooting = other.GetComponent<Shooting>();
            if (shooting != null && weaponPrefab != null && shooting.inventoryManager != null)
            {
                shooting.inventoryManager.AddWeapon(weaponPrefab);
                Destroy(gameObject);
            }
        }
    }
}
