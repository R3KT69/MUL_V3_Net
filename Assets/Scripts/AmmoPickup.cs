using UnityEngine;


public class AmmoPickup : MonoBehaviour
{
    public WeaponType ammoType;
    private Weapon current_weapon;

    void OnTriggerEnter(Collider other)
    {

        current_weapon = other.gameObject.GetComponent<InventoryManager>()?.GetCurrentWeapon()?.GetComponent<Weapon>();
        if (current_weapon != null)
        {
            if (ammoType == current_weapon.weaponType)
            {
                current_weapon.totalAmmo += 50;
                Debug.Log($"Current: {current_weapon.currentAmmo} | Max: {current_weapon.maxAmmo} | Total: {current_weapon.totalAmmo}");
                Destroy(gameObject);
            } else
            {
                Debug.Log("Ammo type mismatch");
            }
            
        }
        
    }

    
    
}
