using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public List<GameObject> activeInventory = new();
    public int currentIndex = 0;
    public PickableObjects playerInventory;

    public GameObject GetCurrentWeapon()
    {
        if (activeInventory.Count > 0)
        {
            return activeInventory[currentIndex];
        }
        else
        {
            return null;
        }
    }  

    public int GetCurrentWeaponIndex()
    {
        return playerInventory.allWeapons.IndexOf(GetCurrentWeapon());
    }

    public Weapon EquipWeaponByInventoryIndex(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= playerInventory.allWeapons.Count)
            return null;

        GameObject weapon = playerInventory.allWeapons[weaponIndex];
        if (!activeInventory.Contains(weapon))
            activeInventory.Add(weapon);

        currentIndex = activeInventory.IndexOf(weapon);
        UpdateWeaponActivation();
        return weapon.GetComponent<Weapon>();
    }

    public void Initialize()
    {
        activeInventory.Clear();
        currentIndex = 0;

        foreach (GameObject weapon in playerInventory.allWeapons)
            weapon.SetActive(false);

        if (playerInventory.allWeapons.Count > 0)
        {
            GameObject firstWeapon = playerInventory.allWeapons[0];
            activeInventory.Add(firstWeapon);
            UpdateWeaponActivation();
            //hud.CreateWeaponUIElement(firstWeapon, firstWeapon.GetComponent<Weapon>().wep_data.name, 0);
        }
    }

    public void AddWeaponByIndex()
    {
        for (int i = 0; i < playerInventory.allWeapons.Count && i < 8; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                GameObject weaponToAdd = playerInventory.allWeapons[i];

                if (!activeInventory.Contains(weaponToAdd))
                {
                    activeInventory.Add(weaponToAdd);
                    currentIndex = activeInventory.Count - 1;
                    UpdateWeaponActivation();
                    //hud.CreateWeaponUIElement(weaponToAdd, weaponToAdd.GetComponent<Weapon>().wep_data.name, index);
                    //hud.RefreshWeaponUILabels(activeInventory);
                    Debug.Log($"[Index Pickup] Added {weaponToAdd.name} to inventory.");
                }
            }
        }
    }


    public void SwitchWeapon(int delta)
    {
        if (activeInventory.Count <= 1) return;

        currentIndex = (currentIndex + delta + activeInventory.Count) % activeInventory.Count;
        UpdateWeaponActivation();
        //hud.RefreshWeaponUILabels(activeInventory);
    }

    public void AddWeapon(GameObject weapon)
    {
        if (!activeInventory.Contains(weapon))
        {
            activeInventory.Add(weapon);
            currentIndex = activeInventory.Count - 1;
            UpdateWeaponActivation();
            //hud.CreateWeaponUIElement(weapon, weapon.GetComponent<Weapon>().wep_data.name, index);
            //hud.RefreshWeaponUILabels(activeInventory);
            Debug.Log($"[Ground Pickup] Added {weapon.name} to inventory");
        }
    }

    public void DropWeapon(Transform playerTransform)
    {
        if (activeInventory.Count <= 1) return;

        GameObject weapon = activeInventory[currentIndex];
        Weapon weaponComponent = weapon.GetComponent<Weapon>();

        if (weaponComponent.world_model != null)
        {
            GameObject dropped = Instantiate(
                weaponComponent.world_model,
                playerTransform.position + playerTransform.forward + Vector3.up * 0.5f,
                weaponComponent.world_model.transform.rotation
            );

            if (dropped.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddForce((playerTransform.forward + Vector3.up) * 2f, ForceMode.Impulse);
            }
        }

        weapon.SetActive(false);
        activeInventory.RemoveAt(currentIndex);
        //hud.RemoveWeaponUIElement(weapon);
        //hud.RefreshWeaponUILabels(activeInventory);

        currentIndex = Mathf.Clamp(currentIndex, 0, activeInventory.Count - 1);
        UpdateWeaponActivation();
        Debug.Log($"Dropped {weapon.name} from inventory");
    }

    private void UpdateWeaponActivation()
    {
        for (int i = 0; i < activeInventory.Count; i++)
        {
            activeInventory[i].SetActive(i == currentIndex);
        }
    }
}

