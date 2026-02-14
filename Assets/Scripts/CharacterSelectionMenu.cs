using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown characterDropdown;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject uiPanel; // The panel to hide after selection

    [Header("Character Configuration")]
    [Tooltip("List of character prefabs. Index must match Dropdown options.")]
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        // Ensure references are assigned
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        else
        {
            Debug.LogError("CharacterSelectionMenu: Confirm Button is not assigned!");
        }
    }

    private void OnConfirmClicked()
    {
        if (characterDropdown == null)
        {
            Debug.LogError("CharacterSelectionMenu: Dropdown is not assigned!");
            return;
        }

        int selectedIndex = characterDropdown.value;

        // Validation
        if (characterPrefabs == null || characterPrefabs.Length == 0)
        {
            Debug.LogError("CharacterSelectionMenu: No Character Prefabs assigned!");
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= characterPrefabs.Length)
        {
            Debug.LogError($"CharacterSelectionMenu: Selected index {selectedIndex} is out of bounds (Prefabs count: {characterPrefabs.Length})");
            return;
        }

        SpawnCharacter(selectedIndex);

        // Hide UI
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
        
        // Optional: Disable this script or destroy the menu object if no longer needed
        // Destroy(gameObject); 
    }

    private void SpawnCharacter(int index)
    {
        GameObject prefabToSpawn = characterPrefabs[index];
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        Instantiate(prefabToSpawn, spawnPos, spawnRot);
        Debug.Log($"CharacterSelectionMenu: Spawned character index {index} ({prefabToSpawn.name})");
    }
}
