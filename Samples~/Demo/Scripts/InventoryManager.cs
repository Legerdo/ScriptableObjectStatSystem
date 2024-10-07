using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace ScriptableStatSystem
{
    #region InventoryManager Class

    /// <summary>
    /// 캐릭터의 인벤토리를 관리하고, 아이템을 사용하여 스탯에 효과를 적용하는 매니저 클래스.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        #region Fields

        [Header("Debug Settings")]
        [Tooltip("모든 적용 중인 아이템을 디버깅 로그로 출력합니다.")]
        public bool isDebug = false;
        public TextMeshProUGUI activeItemsText;

        // 캐릭터의 스탯 매니저 참조
        private CharacterStatsManager statsManager;

        // 현재 활성화된 아이템 리스트
        private List<StatItem> activeItems = new List<StatItem>();

        // 각 아이템의 타이머를 관리하는 딕셔너리 (아이템을 키로, CancellationTokenSource를 값으로)
        private Dictionary<StatItem, CancellationTokenSource> itemTimers = new Dictionary<StatItem, CancellationTokenSource>();

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            // CharacterStatsManager 컴포넌트를 가져옵니다.
            statsManager = GetComponent<CharacterStatsManager>();
            if (statsManager == null)
            {
                Debug.LogError("CharacterStatsManager 컴포넌트를 찾을 수 없습니다. InventoryManager를 정상적으로 작동시키려면 CharacterStatsManager가 필요합니다.");
            }
        }

        private void Update()
        {
            if (isDebug)
            {
                PrintActiveItems();
            }
        }

        private void OnDestroy()
        {
            // 모든 아이템 타이머를 취소하고 정리합니다.
            foreach (var cts in itemTimers.Values)
            {
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }
                cts.Dispose();
            }

            itemTimers.Clear();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 아이템을 사용하여 해당 아이템의 효과를 적용합니다.
        /// </summary>
        /// <param name="item">사용할 StatItem.</param>
        public async UniTask UseItem(StatItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("UseItem 호출 시 전달된 아이템이 null입니다.");
                return;
            }

            // 대상 스탯 가져오기
            Stat targetStat = statsManager.GetCharacterStat(item.affectedStat);

            if (targetStat == null)
            {
                Debug.LogWarning($"Stat '{item.affectedStat.StatName}'을(를) 찾을 수 없습니다. 아이템 '{item.itemName}'의 효과를 적용할 수 없습니다.");
                return;
            }

            // 대상 스탯이 INumericStat을 구현하는지 확인
            if (!(targetStat is INumericStat numericStat))
            {
                Debug.LogWarning($"Stat '{item.affectedStat.StatName}'은(는) 수학적 스탯이 아닙니다. 아이템 '{item.itemName}'의 효과를 적용할 수 없습니다.");
                return;
            }

            // 중복 처리 방식에 따라 행동 결정
            switch (item.duplicateHandling)
            {
                case DuplicateHandling.Allow:
                    await ApplyItemEffect(item, numericStat);
                    break;

                case DuplicateHandling.Prevent:
                    if (activeItems.Contains(item))
                    {
                        Debug.LogWarning($"아이템 '{item.itemName}'은(는) 이미 사용 중이므로 중복 적용되지 않았습니다.");
                        return;
                    }
                    await ApplyItemEffect(item, numericStat);
                    break;

                case DuplicateHandling.Stack:
                    await ApplyItemEffect(item, numericStat);
                    break;

                case DuplicateHandling.Refresh:
                    if (activeItems.Contains(item))
                    {
                        Debug.Log($"아이템 '{item.itemName}'의 지속 시간이 갱신되었습니다.");

                        item.ResetTickCount();
                    }
                    else
                    {
                        await ApplyItemEffect(item, numericStat);

                        if (item.tickInterval <= 0)
                        {
                            StartItemTimer(item, numericStat);
                        }
                    }

                    break;
            }

            // 영구적이지 않고 지속 시간이 있는 아이템의 경우 타이머를 시작합니다.
            if (!item.isPermanent && item.duration > 0)
            {
                if (item.CurrentTickCount != 0)
                {
                    RemoveItem(item, numericStat);
                }
            }
        }

        #endregion

        #region Private Methods        

        /// <summary>
        /// 아이템의 효과를 적용합니다.
        /// </summary>
        /// <param name="item">적용할 StatItem.</param>
        /// <param name="numericStat">효과를 적용할 INumericStat.</param>
        private async UniTask ApplyItemEffect(StatItem item, INumericStat numericStat)
        {
            Debug.Log($"아이템 '{item.itemName}'을(를) 사용했습니다.");
            activeItems.Add(item);
            await item.ApplyEffect(numericStat);
        }

        /// <summary>
        /// 아이템의 지속 시간을 관리하는 타이머를 시작합니다.
        /// </summary>
        /// <param name="item">타이머를 시작할 StatItem.</param>
        /// <param name="numericStat">효과를 적용할 INumericStat.</param>
        private void StartItemTimer(StatItem item, INumericStat numericStat)
        {
            // 기존 타이머가 있다면 취소하고 제거합니다.
            if (itemTimers.TryGetValue(item, out var existingCts))
            {
                if (!existingCts.IsCancellationRequested)
                {
                    existingCts.Cancel();
                    Debug.Log($"타이머가 취소되었습니다. 아이템: {item.itemName}");
                }
                existingCts.Dispose();
                itemTimers.Remove(item);
                Debug.Log($"기존 타이머가 제거되었습니다. 아이템: {item.itemName}");
            }

            // 새로운 타이머를 생성하고 딕셔너리에 추가합니다.
            var cts = new CancellationTokenSource();
            itemTimers[item] = cts;

            Debug.Log($"새로운 타이머가 시작되었습니다. 아이템: {item.itemName}, 지속 시간: {item.duration}초");

            // 아이템의 지속 시간 후에 아이템을 제거합니다.
            UniTask.Delay(TimeSpan.FromSeconds(item.duration), cancellationToken: cts.Token)
            .ContinueWith(() =>
            {
                RemoveItem(item, numericStat);
                Debug.Log($"타이머 완료. 아이템 제거 시도: {item.itemName}");
            }).Forget();
        }


        /// <summary>
        /// 아이템을 제거하고 관련 리소스를 정리합니다.
        /// </summary>
        /// <param name="item">제거할 StatItem.</param>
        /// <param name="numericStat">효과를 적용할 INumericStat.</param>
        public void RemoveItem(StatItem item, INumericStat numericStat)
        {
            if (activeItems.Remove(item))
            {
                // 관련된 타이머가 존재하면 취소하고 제거합니다.
                if (itemTimers.TryGetValue(item, out var cts))
                {
                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                        Debug.Log($"타이머가 취소되었습니다. 아이템: {item.itemName}");
                    }
                    cts.Dispose();
                    itemTimers.Remove(item);
                    Debug.Log($"타이머가 제거되었습니다. 아이템: {item.itemName}");
                }

                // 아이템의 효과를 제거합니다.
                item.RemoveEffect(numericStat);

                Debug.Log($"아이템의 효과가 제거되었습니다. 아이템: {item.itemName}");

                if (isDebug)
                {
                    Debug.Log($"아이템 '{item.itemName}'이(가) 제거되었습니다.");
                }
            }
            else
            {
                Debug.LogWarning($"아이템 '{item.itemName}'을(를) 제거하려 했으나, 활성 아이템 목록에 존재하지 않습니다.");
            }
        }


        #endregion

        #region Debug Methods

        /// <summary>
        /// 모든 활성화된 아이템의 목록을 디버깅 로그로 출력합니다.
        /// </summary>
        public void PrintActiveItems()
        {
            activeItemsText.text = "Currently Active Items:\n";

            foreach (var item in activeItems)
            {
                activeItemsText.text += $"- {item.itemName}\n";
            }
        }

        #endregion
    }

    #endregion
}
