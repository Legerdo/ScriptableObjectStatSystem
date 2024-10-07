using UnityEngine;

namespace ScriptableStatSystem
{
    #region Enumerations

    /// <summary>
    /// 중복 처리 방식 열거형.
    /// </summary>
    public enum DuplicateHandling
    {
        Allow,          // 중복 적용 허용
        Prevent,        // 중복 적용 방지
        Stack,          // 중복 시 효과 누적
        Refresh         // 중복 시 지속 시간 갱신
    }

    #endregion
}