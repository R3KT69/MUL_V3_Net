using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class Hud : MonoBehaviour
{
    public GameObject layout, weaponElement;
    private Dictionary<GameObject, GameObject> weaponUIMap = new Dictionary<GameObject, GameObject>();


    public GameObject CreateWeaponUIElement(GameObject weapon, string name, int index)
    {
        if (layout == null || weaponElement == null)
        {
            Debug.LogWarning("HUD: Layout or WeaponElement is not assigned.");
            return null;
        }

        Weapon weaponComp = weapon.GetComponent<Weapon>();
        weaponComp.Init();

        GameObject newElement = Instantiate(weaponElement, layout.transform);
        newElement.transform.localPosition = Vector3.zero;
        newElement.transform.localRotation = Quaternion.identity;
        newElement.transform.localScale = Vector3.one;

        TextMeshProUGUI[] texts = newElement.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 3)
        {
            texts[0].text = $"#{index + 1}";
            texts[1].text = name;
            texts[2].text = $"{weaponComp.currentAmmo}/{weaponComp.wep_data.maxAmmo}";
        }

        weaponUIMap[weapon] = newElement;
        return newElement;
    }


    public void RemoveWeaponUIElement(GameObject weapon)
    {
        if (weaponUIMap.ContainsKey(weapon))
        {
            Destroy(weaponUIMap[weapon]);
            weaponUIMap.Remove(weapon);
        }
    }

    public void RefreshWeaponUILabels(List<GameObject> currentInventory)
    {
        for (int i = 0; i < currentInventory.Count; i++)
        {
            GameObject weapon = currentInventory[i];

            if (weaponUIMap.TryGetValue(weapon, out GameObject uiElement))
            {
                TextMeshProUGUI[] texts = uiElement.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = $"#{i + 1}";
                }
            }
        }
    }
    
    public void RefreshWeaponAmmo(GameObject weapon)
    {
        if (weaponUIMap.TryGetValue(weapon, out GameObject uiElement))
        {
            Weapon weaponComp = weapon.GetComponent<Weapon>();
            TextMeshProUGUI[] texts = uiElement.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 3)
            {
                texts[2].text = $"{weaponComp.currentAmmo}/{weaponComp.wep_data.maxAmmo}";
            }
        }
    }



}
