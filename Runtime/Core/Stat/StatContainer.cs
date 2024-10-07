using System.Collections.Generic;
using UnityEngine;

namespace ScriptableStatSystem
{
    /// <summary>
    /// 스탯 컨테이너 클래스. 캐릭터별로 다른 스탯을 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "New Stat Container", menuName = "Character Stats/Stat Container")]
    public class StatContainer : ScriptableObject
    {
        [Header("Container Details")]
        public string containerName;

        [Tooltip("수학적 계산이 가능한 스탯 정의 리스트.")]
        public List<NumericStatDefinition<float>> floatStatDefinitions;

        [Tooltip("비수학적 스탯 정의 리스트.")]
        public List<NonNumericStatDefinition<string>> stringStatDefinitions;
    }
}
