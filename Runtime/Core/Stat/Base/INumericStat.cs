using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ScriptableStatSystem
{
    /// <summary>
    /// 수학적 계산이 가능한 스탯을 위한 인터페이스.
    /// </summary>
    public interface INumericStat
    {
        float Value { get; }
        void AddModifier(StatModifier mod);
        UniTask AddTimedModifier(StatModifier mod, float duration);
        bool RemoveAllModifiersFromSource(object source);
        bool HasModifierFromSource(object source);
    }
}