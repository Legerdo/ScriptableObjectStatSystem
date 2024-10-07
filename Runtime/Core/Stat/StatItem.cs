using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using ScriptableObjectArchitecture;
using UnityEngine;

namespace ScriptableStatSystem
{
    /// <summary>
    /// 스탯 아이템 클래스. 캐릭터의 스탯에 효과를 적용합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "New Stat Item", menuName = "Character Stats/Stat/Stat Item")]
    public class StatItem : ScriptableObjectPlayMode, IStatModifierSource
    {
        #region Fields

        [Header("Item Details")]
        [Tooltip("아이템의 이름을 지정합니다.")]
        public string itemName;

        [Tooltip("이 아이템이 영향을 미치는 스탯의 유형을 지정합니다.")]
        public StatType affectedStat;

        public ItemType itemType;

        [Header("Modifier Details")]
        [Tooltip("아이템이 제공하는 수정자 값입니다.")]
        public float modifierValue;

        [Tooltip("수정자의 유형을 지정합니다 (예: Additive, Multiplicative 등).")]
        public StatModifier.ModifierType modifierType;

        [Tooltip("수정자가 영구적인지 여부를 설정합니다.")]
        public bool isPermanent; // 영구적 효과

        [Tooltip("수정자가 일시적인 경우, 효과의 지속 시간을 설정합니다 (초 단위).")]
        public float duration; // 일시적 효과의 지속 시간 (초)

        [Tooltip("중복 처리를 어떻게 할지 설정합니다 ( 허용, 방지, 스택, 갱신 ).")]
        public DuplicateHandling duplicateHandling;

        [Tooltip("틱 효과의 간격을 설정합니다 (초 단위).")]
        public float tickInterval; // 데미지 간격 (초)

        [Tooltip("총 틱 효과 횟수를 설정합니다.")]
        public int tickCount; // 총 데미지 횟수

        private int currentTickCount = 0;

        public int CurrentTickCount
        {
            get => currentTickCount;
            private set => currentTickCount = value;
        }

        [Header("Stack Details")]
        [Tooltip("아이템의 최대 스택 수를 설정합니다.")]
        public int maxStacks = 1; // 최대 스택 수

        [Tooltip("스택당 효과 증가 비율을 설정합니다.")]
        public float stackMultiplier = 0f; // 스택당 효과 증가 비율

        private int currentStacks = 0; // 현재 스택 수

        private CancellationTokenSource tickEffectCts;

        private CancellationTokenSource effectCts;

        #endregion

        #region Public Methods

        /// <summary>
        /// 스탯에 효과를 적용합니다.
        /// </summary>
        /// <param name="stat">효과를 적용할 수학적 스탯.</param>
        public async UniTask ApplyEffect(INumericStat stat)
        {
            switch (duplicateHandling)
            {
                case DuplicateHandling.Allow:
                    await ApplyModifier(stat, CreateModifier());
                    break;
                case DuplicateHandling.Prevent:
                    if (tickInterval > 0)
                    {
                        tickEffectCts = new CancellationTokenSource();
                        await ApplyTickEffect(stat, CreateModifier(), tickEffectCts.Token);
                    }
                    else if (!stat.HasModifierFromSource(this))
                    {
                        await ApplyModifier(stat, CreateModifier());
                    }
                    else
                    {
                        Debug.Log($"{itemName}의 효과가 이미 적용되어 있어 중복 적용되지 않았습니다.");
                    }
                    break;
                case DuplicateHandling.Stack:
                    if (currentStacks < maxStacks)
                    {
                        currentStacks++;
                    }
                    else
                    {
                        Debug.Log($"{itemName}의 최대 스택 수에 도달했습니다.");
                    }

                    // 스택된 수정자 생성
                    StatModifier stackedModifier = CreateModifier(currentStacks);

                    if (tickInterval > 0)
                    {
                        // 기존 틱 효과 취소
                        CancelAndDisposeTickEffect();

                        // 새로운 틱 효과 적용
                        tickEffectCts = new CancellationTokenSource();
                        await ApplyTickEffect(stat, stackedModifier, tickEffectCts.Token);
                    }
                    else
                    {
                        // 기존 수정자 제거
                        stat.RemoveAllModifiersFromSource(this);

                        // 수정자 적용
                        await ApplyModifier(stat, stackedModifier);
                    }

                    Debug.Log($"{itemName}의 현재 스택 수: {currentStacks}/{maxStacks}");
                    break;
                case DuplicateHandling.Refresh:
                    RemoveEffect(stat);

                    if (tickInterval > 0)
                    {
                        tickEffectCts = new CancellationTokenSource();
                        await ApplyTickEffect(stat, CreateModifier(), tickEffectCts.Token);
                    }
                    else
                    {
                        await ApplyModifier(stat, CreateModifier());
                    }
                    break;
            }
        }

        /// <summary>
        /// 스탯에 적용된 효과를 제거합니다.
        /// </summary>
        /// <param name="stat">효과를 제거할 수학적 스탯.</param>
        public void RemoveEffect(INumericStat stat)
        {
            CancelAndDisposeTickEffect();

            // effectCts 취소 및 해제
            if (effectCts != null)
            {
                if (!effectCts.IsCancellationRequested)
                {
                    effectCts.Cancel();
                }
                effectCts.Dispose();
                effectCts = null;
            }

            stat.RemoveAllModifiersFromSource(this);

            Reset();
        }

        public void ResetTickCount()
        {
            currentTickCount = 0;
        }

        public void Reset()
        {
            currentStacks = 0;
            currentTickCount = 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 수정자를 생성합니다.
        /// </summary>
        /// <param name="stacks">적용할 스택 수.</param>
        /// <returns>생성된 StatModifier.</returns>
        private StatModifier CreateModifier(int stacks = 1)
        {
            float stackedValue = modifierValue * (1 + stackMultiplier * (stacks - 1));

            return new StatModifier(
                value: stackedValue,
                type: modifierType,
                source: this
            );
        }

        /// <summary>
        /// 수정자를 스탯에 적용합니다.
        /// </summary>
        /// <param name="stat">수정자를 적용할 스탯.</param>
        /// <param name="modifier">적용할 수정자.</param>
        private async UniTask ApplyModifier(INumericStat stat, StatModifier modifier)
        {
            if (isPermanent)
            {
                stat.AddModifier(modifier);
            }
            else
            {
                await stat.AddTimedModifier(modifier, duration);
            }
        }

        /// <summary>
        /// 틱 효과를 적용합니다.
        /// </summary>
        /// <param name="stat">틱 효과를 적용할 스탯.</param>
        /// <param name="modifier">틱 효과에 사용할 수정자.</param>
        /// <param name="cancellationToken">취소 토큰.</param>
        private async UniTask ApplyTickEffect(INumericStat stat, StatModifier modifier, CancellationToken cancellationToken)
        {
            try
            {
                while ((isPermanent || currentTickCount < tickCount) && !cancellationToken.IsCancellationRequested)
                {
                    await ApplySingleTickEffect(stat, modifier);
                }
            }
            catch (OperationCanceledException)
            {
                // 작업이 취소되었을 때 처리
                Debug.Log($"{itemName}의 틱 효과가 취소되었습니다.");
            }
        }

        private async UniTask ApplySingleTickEffect(INumericStat stat, StatModifier modifier)
        {
            await stat.AddTimedModifier(modifier, tickInterval);
            currentTickCount++;
            Debug.Log($"{itemName}의 틱 효과 적용: {currentTickCount}");
        }

        /// <summary>
        /// 틱 효과를 취소하고 토큰을 해제합니다.
        /// </summary>
        private void CancelAndDisposeTickEffect()
        {
            if (tickEffectCts != null)
            {
                if (!tickEffectCts.IsCancellationRequested)
                {
                    tickEffectCts.Cancel();
                }
                tickEffectCts.Dispose();
                tickEffectCts = null;
            }
        }

        #endregion

        #region Unity Callbacks

        protected override void OnEnable()
        {
            base.OnEnable();
            Reset();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CancelAndDisposeTickEffect();
            Reset();
        }

        #endregion
    }
}