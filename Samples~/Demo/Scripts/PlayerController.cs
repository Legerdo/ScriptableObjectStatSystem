using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace ScriptableStatSystem
{
    /// <summary>
    /// 플레이어의 컨트롤러 클래스. 인벤토리 관리 및 아이템 사용을 담당합니다.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        #region Fields
        
        [Header("Stat Items")]
        [Tooltip("사용할 독 아이템.")]
        [SerializeField] private StatItem poison;

        [Tooltip("사용할 번 아이템.")]
        [SerializeField] private StatItem burn;

        [Tooltip("장착할 스트렝스 링.")]
        [SerializeField] private StatItem strengthRing;

        [Tooltip("장착할 두 번째 스트렝스 링.")]
        [SerializeField] private StatItem strengthRing2;

        [Header("UI Settings")]
        [SerializeField] private Transform uiCanvas;
        [SerializeField] private GameObject buttonPrefab;

        private Dictionary<string, Button> uiButtons = new Dictionary<string, Button>();

        // 인벤토리 및 스탯 매니저 참조
        private InventoryManager inventory;
        private CharacterStatsManager statsManager;

        // 복제된 아이템 인스턴스
        private StatItem poisonInstance;
        private StatItem burnInstance;

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            // InventoryManager와 CharacterStatsManager 컴포넌트를 가져옵니다.
            inventory = GetComponent<InventoryManager>();
            statsManager = GetComponent<CharacterStatsManager>();

            if (inventory == null)
            {
                Debug.LogError("InventoryManager 컴포넌트를 찾을 수 없습니다.");
            }

            if (statsManager == null)
            {
                Debug.LogError("CharacterStatsManager 컴포넌트를 찾을 수 없습니다.");
            }

            // 아이템 인스턴스 복제
            poisonInstance = Instantiate(poison);
            burnInstance = Instantiate(burn);

            CreateDynamicButtons();
        }

        private void CreateDynamicButtons()
        {
            CreateButton("UsePoison", "Use Poison", () => UsePoison().Forget());
            CreateButton("RemovePoison", "Remove Poison", () => RemoveItem(poisonInstance));
            CreateButton("UseBurn", "Use Burn", () => UseBurn().Forget());
            CreateButton("RemoveBurn", "Remove Burn", () => RemoveItem(burnInstance));
            CreateButton("EquipStrengthRing", "Equip Strength Ring", () => EquipStrengthRing().Forget());
            CreateButton("UnequipStrengthRing", "Unequip Strength Ring", () => RemoveItem(strengthRing));
            CreateButton("EquipStrengthRing2", "Equip Strength Ring 2", () => EquipStrengthRing2().Forget());
            CreateButton("UnequipStrengthRing2", "Unequip Strength Ring 2", () => RemoveItem(strengthRing2));
        }

        private void CreateButton(string buttonName, string buttonText, UnityAction onClick)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, uiCanvas.transform);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonTextComponent = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonTextComponent != null)
            {
                buttonTextComponent.text = buttonText;
            }

            button.onClick.AddListener(onClick);
            uiButtons[buttonName] = button;
        }

        private void OnDestroy()
        {
            foreach (var button in uiButtons.Values)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 독 아이템을 사용하여 해당 효과를 적용합니다.
        /// </summary>
        public async UniTaskVoid UsePoison()
        {
            if (poisonInstance == null)
            {
                Debug.LogWarning("Poison 아이템이 할당되지 않았습니다.");
                return;
            }

            Debug.Log("UsePoison Call");

            await inventory.UseItem(poisonInstance);
        }

        /// <summary>
        /// 번 아이템을 사용하여 해당 효과를 적용합니다.
        /// </summary>
        public async UniTaskVoid UseBurn()
        {
            if (burnInstance == null)
            {
                Debug.LogWarning("Burn 아이템이 할당되지 않았습니다.");
                return;
            }

            Debug.Log("UseBurn Call");

            await inventory.UseItem(burnInstance);
        }

        /// <summary>
        /// 스트렝스 링을 장착하여 해당 효과를 적용합니다.
        /// </summary>
        public async UniTaskVoid EquipStrengthRing()
        {
            if (strengthRing == null)
            {
                Debug.LogWarning("Strength Ring 아이템이 할당되지 않았습니다.");
                return;
            }

            await inventory.UseItem(strengthRing);
        }

        /// <summary>
        /// 두 번째 스트렝스 링을 장착하여 해당 효과를 적용합니다.
        /// </summary>
        public async UniTaskVoid EquipStrengthRing2()
        {
            if (strengthRing2 == null)
            {
                Debug.LogWarning("Strength Ring2 아이템이 할당되지 않았습니다.");
                return;
            }

            await inventory.UseItem(strengthRing2);
        }

        /// <summary>
        /// 아이템을 제거하여 해당 효과를 해제합니다.
        /// </summary>
        /// <param name="item">제거할 StatItem.</param>
        public void RemoveItem(StatItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("RemoveItem 호출 시 전달된 아이템이 null입니다.");
                return;
            }

            // 대상 스탯 가져오기
            Stat targetStat = statsManager.GetCharacterStat(item.affectedStat);

            if (targetStat == null)
            {
                Debug.LogWarning($"Stat '{item.affectedStat.StatName}' not found. Cannot remove item '{item.itemName}'.");
                return;
            }

            // 대상 스탯이 INumericStat을 구현하는지 확인
            if (!(targetStat is INumericStat numericStat))
            {
                Debug.LogWarning($"Stat '{item.affectedStat.StatName}'은(는) 수학적 스탯이 아닙니다. 아이템 '{item.itemName}'의 효과를 제거할 수 없습니다.");
                return;
            }

            inventory.RemoveItem(item, numericStat);
        }

        #endregion
    }
}