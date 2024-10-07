using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace ScriptableStatSystem
{
    /// <summary>
    /// 수학적 계산이 가능한 스탯 클래스.
    /// </summary>
    [System.Serializable]
    public class NumericStat : Stat, INumericStat
    {
        #region Fields

        /// <summary>
        /// 스탯의 정의 및 설정 정보를 담고 있는 객체.
        /// </summary>
        [SerializeField] private FloatStatDefinition definition;

        /// <summary>
        /// 스탯의 기본 값으로, 수정자들이 적용되기 전의 값.
        /// </summary>
        [SerializeField] private float baseValue;

        /// <summary>
        /// 마지막으로 기록된 기본 값으로, 변경 여부를 감지하기 위해 사용됨.
        /// </summary>
        private float lastBaseValue;

        /// <summary>
        /// 스탯 값이 변경되어 다시 계산이 필요한지를 나타내는 플래그.
        /// </summary>
        private bool isDirty = true;

        /// <summary>
        /// 최종 계산된 스탯 값의 캐시.
        /// </summary>
        private float _value;

        /// <summary>
        /// 최소 값.
        /// </summary>
        private float min;

        /// <summary>
        /// 최대 값.
        /// </summary>
        private float max;

        /// <summary>
        /// 현재 스탯에 적용된 모든 수정자들의 리스트.
        /// </summary>
        private List<StatModifier> statModifiers = new List<StatModifier>();

        /// <summary>
        /// 시간 제한이 있는 수정자들과 그에 대응하는 작업 완료 소스를 저장하는 딕셔너리.
        /// </summary>
        private Dictionary<StatModifier, UniTaskCompletionSource> timedModifiers = new Dictionary<StatModifier, UniTaskCompletionSource>();

        /// <summary>
        /// 영구적으로 틱을 발생시키는 수정자들과 틱 간격을 저장하는 리스트.
        /// </summary>
        private List<(StatModifier modifier, float interval)> permanentTickModifiers = new List<(StatModifier, float)>();

        /// <summary>
        /// 임시 수정자로 인해 누적된 값.
        /// </summary>
        private float accumulatedValue = 0;

        /// <summary>
        /// 영구적인 수정자로 인한 총 수정 값.
        /// </summary>
        private float permanentModification = 0;

        /// <summary>
        /// 영구적인 수정자로 인한 총 수정 값의 프로퍼티.
        /// </summary>
        private float PermanentModification
        {
            get => permanentModification;
            set
            {
                if (permanentModification != value)
                {
                    permanentModification = value;
                    NotifyValueChanged(Value);
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// 스탯 값이 변경될 때 발생하는 이벤트.
        /// </summary>
        public event Action<float> OnValueChanged;

        /// <summary>
        /// 스탯 값이 변경되었음을 알립니다.
        /// </summary>
        protected void NotifyValueChanged(float newValue)
        {
            OnValueChanged?.Invoke(newValue);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 스탯의 최종 값.
        /// </summary>
        public float Value
        {
            get
            {
                if (isDirty || lastBaseValue != baseValue)
                {
                    lastBaseValue = baseValue;
                    _value = CalculateFinalValue();
                    isDirty = false;
                }
                
                float finalValue = _value + accumulatedValue + permanentModification;
                return Mathf.Clamp(finalValue, min, max);
            }
        }

        public int GetIntValue()
        {
            return (int)Math.Round(Value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 새로운 NumericStat 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="definition">스탯 정의.</param>
        public NumericStat(FloatStatDefinition definition)
        {
            this.definition = definition;
            this.baseValue = definition.baseValue;

            this.min =  definition.GetMin();
            this.max = definition.GetMax();
        }

        #endregion

        #region Modifier Management

        /// <summary>
        /// 스탯에 수정자를 추가합니다.
        /// </summary>
        /// <param name="mod">추가할 수정자.</param>
        public void AddModifier(StatModifier mod)
        {
            isDirty = true;
            statModifiers.Add(mod);
        }

        /// <summary>
        /// 일정 시간 동안 지속되는 수정자를 추가합니다.
        /// </summary>
        /// <param name="mod">추가할 수정자.</param>
        /// <param name="duration">수정자의 지속 시간(초).</param>
        public async UniTask AddTimedModifier(StatModifier mod, float duration)
        {
            isDirty = true;
            float modValue = CalculateModifierValue(mod);
            accumulatedValue += modValue;

            var cts = new UniTaskCompletionSource();
            timedModifiers[mod] = cts;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(duration));
                accumulatedValue -= modValue;
                PermanentModification += modValue; // 영구적인 수정 적용
            }
            finally
            {
                timedModifiers.Remove(mod);
                cts.TrySetResult();
                isDirty = true;
            }
        }

        /// <summary>
        /// 영구적인 틱 수정자를 제거합니다.
        /// </summary>
        /// <param name="source">수정자의 소스 식별자.</param>
        public void RemovePermanentTickModifier(object source)
        {
            var modifiersToRemove = permanentTickModifiers.Where(x => x.modifier.Source == source).ToList();
            foreach (var (modifier, _) in modifiersToRemove)
            {
                float modValue = CalculateModifierValue(modifier);
                PermanentModification -= modValue;
                permanentTickModifiers.RemoveAll(x => x.modifier == modifier);
            }
            isDirty = true;
        }

        /// <summary>
        /// 특정 소스에서 발생한 모든 수정자를 제거합니다.
        /// </summary>
        /// <param name="source">소스 식별자.</param>
        /// <returns>수정자가 제거되었는지 여부.</returns>
        public bool RemoveAllModifiersFromSource(object source)
        {
            bool removed = false;

            // 정적 수정자 제거
            for (int i = statModifiers.Count - 1; i >= 0; i--)
            {
                if (statModifiers[i].Source == source)
                {
                    statModifiers.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                isDirty = true;
            }

            // 영구적인 틱 수정자 제거
            RemovePermanentTickModifier(source);

            // 시간 제한 수정자 제거
            var timedModifiersToRemove = timedModifiers.Where(kvp => kvp.Key.Source == source).ToList();
            foreach (var kvp in timedModifiersToRemove)
            {
                timedModifiers.Remove(kvp.Key);
                kvp.Value.TrySetCanceled();
            }

            return removed || timedModifiersToRemove.Any() || permanentTickModifiers.Any(x => x.modifier.Source == source);
        }

        /// <summary>
        /// 특정 소스에서 발생한 수정자가 있는지 확인합니다.
        /// </summary>
        /// <param name="source">소스 식별자.</param>
        /// <returns>수정자가 있는지 여부.</returns>
        public bool HasModifierFromSource(object source)
        {
            return statModifiers.Exists(mod => mod.Source == source);
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// 두 수정자의 순서를 비교합니다.
        /// </summary>
        /// <param name="a">첫 번째 수정자.</param>
        /// <param name="b">두 번째 수정자.</param>
        /// <returns>비교 결과.</returns>
        private int CompareModifierOrder(StatModifier a, StatModifier b)
        {
            return a.Order.CompareTo(b.Order);
        }

        /// <summary>
        /// 모든 수정자를 적용한 후 최종 값을 계산합니다.
        /// </summary>
        /// <returns>계산된 최종 값.</returns>
        private float CalculateFinalValue()
        {
            float finalValue = baseValue;
            float sumPercentAdd = 0;

            // 수정자를 순서대로 정렬
            statModifiers.Sort(CompareModifierOrder);

            foreach (var mod in statModifiers)
            {
                switch (mod.Type)
                {
                    case StatModifier.ModifierType.Flat:
                        finalValue += mod.Value;
                        break;

                    case StatModifier.ModifierType.PercentAdd:
                        sumPercentAdd += mod.Value;
                        break;

                    case StatModifier.ModifierType.PercentMult:
                        finalValue *= 1 + mod.Value;
                        break;
                }

                // PercentAdd 수정자를 누적하여 한번에 적용
                if (mod.Type != StatModifier.ModifierType.PercentAdd || statModifiers.Last() == mod)
                {
                    finalValue *= 1 + sumPercentAdd;
                    sumPercentAdd = 0;
                }
            }

            return (float)Math.Round(finalValue, 4);
        }

        /// <summary>
        /// 특정 수정자의 값을 계산합니다.
        /// </summary>
        /// <param name="mod">수정자.</param>
        /// <returns>계산된 수정자 값.</returns>
        private float CalculateModifierValue(StatModifier mod)
        {
            float baseForCalculation = baseValue + accumulatedValue;
            return mod.Type switch
            {
                StatModifier.ModifierType.Flat => mod.Value,
                StatModifier.ModifierType.PercentAdd => baseForCalculation * mod.Value,
                StatModifier.ModifierType.PercentMult => baseForCalculation * mod.Value,
                _ => 0,
            };
        }

        #endregion
    }
}
