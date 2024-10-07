using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace ScriptableStatSystem
{
    /// <summary>
    /// 캐릭터의 스탯을 관리하는 매니저 클래스.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterStatsManager : MonoBehaviour
    {
        #region Fields

        [Header("Debug Settings")]
        [Tooltip("모든 스탯의 값을 디버깅 출력합니다.")]
        public bool isDebugStatsUpdate = false;

        public TextMeshProUGUI allStatsText;

        public string charName = "TEST";

        [Header("Stat Container")]
        [Tooltip("캐릭터의 스탯 컨테이너.")]
        public StatContainer statContainer;

        // 모든 스탯을 저장하는 딕셔너리 (StatType을 키로 사용)
        private Dictionary<StatType, Stat> stats = new Dictionary<StatType, Stat>();

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            InitializeStats();
        }

        private void Update()
        {
            if (isDebugStatsUpdate)
            {
                PrintAllStats();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 스탯을 초기화합니다.
        /// </summary>
        private void InitializeStats()
        {
            if (statContainer == null)
            {
                Debug.LogError("StatContainer가 할당되지 않았습니다.");
                return;
            }

            // 수학적 스탯 초기화
            foreach (var definition in statContainer.floatStatDefinitions)
            {
                if (definition == null || definition.statType == null)
                {
                    Debug.LogWarning("StatContainer의 numericStatDefinitions에 null 참조가 있습니다.");
                    continue;
                }

                if (stats.ContainsKey(definition.statType))
                {
                    Debug.LogWarning($"StatType '{definition.statType.StatName}'은 이미 초기화되었습니다.");
                    continue;
                }

                var numericStat = new NumericStat((FloatStatDefinition)definition);
                numericStat.OnValueChanged += OnStatValueChanged;
                stats[definition.statType] = numericStat;
            }

            // 비수학적 스탯 초기화
            foreach (var definition in statContainer.stringStatDefinitions)
            {
                if (definition == null || definition.statType == null)
                {
                    Debug.LogWarning("StatContainer의 stringStatDefinitions에 null 참조가 있습니다.");
                    continue;
                }

                if (stats.ContainsKey(definition.statType))
                {
                    Debug.LogWarning($"StatType '{definition.statType.StatName}'은 이미 초기화되었습니다.");
                    continue;
                }

                var stringStat = new NonNumericStat<string>(definition.baseValue);
                stringStat.OnValueChanged += OnStrengthStatChanged;
                stats[definition.statType] = stringStat;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 특정 스탯을 가져옵니다.
        /// </summary>
        /// <param name="statType">가져올 스탯의 StatType 객체.</param>
        /// <returns>해당 스탯 객체. 존재하지 않으면 null.</returns>
        public Stat GetCharacterStat(StatType statType)
        {
            if (statType == null)
            {
                Debug.LogWarning("Provided StatType is null.");
                return null;
            }

            if (stats.TryGetValue(statType, out Stat stat))
            {
                return stat;
            }

            Debug.LogWarning($"Stat '{statType.StatName}' not found.");
            return null;
        }

        /// <summary>
        /// 모든 스탯의 값을 출력합니다.
        /// </summary>
        public void PrintAllStats()
        {
            // Add title at the beginning.
            allStatsText.text = charName + " : Displaying all stats : \n";

            foreach (var statPair in stats)
            {
                var statType = statPair.Key;
                var stat = statPair.Value;

                if (stat is INumericStat numericStat)
                {
                    allStatsText.text += $"{statType.StatName}: {numericStat.Value}\n";
                }
                else if (stat is NonNumericStat<string> stringStat)
                {
                    allStatsText.text += $"{statType.StatName}: {stringStat.Value}\n";
                }
                else
                {
                    allStatsText.text += $"{statType.StatName}: Unknown stat type.\n";
                }
            }
        }

        private void OnStatValueChanged(float newValue)
        {
            Debug.Log($"OnStatValueChanged Stat has been updated to: {newValue}");
            // 추가적인 로직 구현
        }

        private void OnStrengthStatChanged(String newValue)
        {
            string strengthValue = newValue;
            Debug.Log($"OnStrengthStatChanged Stat has been updated to: {newValue}");
            // 추가적인 로직 구현
        }

        #endregion
    }
}
