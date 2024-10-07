using System;
using UnityEngine;

namespace ScriptableStatSystem
{
    #region Interfaces

    /// <summary>
    /// StatModifier의 출처를 나타내는 인터페이스.
    /// </summary>
    public interface IStatModifierSource
    {
        // 출처 관련 메서드나 속성을 정의할 수 있습니다.
    }

    #endregion

    #region StatModifier Class

    /// <summary>
    /// 스탯 수정자 클래스. 스탯에 다양한 효과를 적용하는 데 사용됩니다.
    /// </summary>
    [System.Serializable]
    public class StatModifier
    {
        #region Enumerations

        /// <summary>
        /// 스탯 수정자의 유형을 정의합니다.
        /// </summary>
        public enum ModifierType
        {
            Flat = 100,        // 고정 값 추가
            PercentAdd = 200,  // 비율 추가
            PercentMult = 300  // 비율 곱셈
        }

        #endregion

        #region Fields

        [SerializeField] private readonly float value;
        [SerializeField] private readonly ModifierType type;
        [SerializeField] private readonly int order;
        [SerializeField] private readonly IStatModifierSource source;

        #endregion

        #region Properties

        /// <summary>
        /// 수정자의 값.
        /// </summary>
        public float Value => value;

        /// <summary>
        /// 수정자의 유형.
        /// </summary>
        public ModifierType Type => type;

        /// <summary>
        /// 수정자의 적용 순서.
        /// </summary>
        public int Order => order;

        /// <summary>
        /// 수정자의 출처.
        /// </summary>
        public IStatModifierSource Source => source;

        #endregion

        #region Constructors

        /// <summary>
        /// 새로운 StatModifier 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="value">수정자의 값.</param>
        /// <param name="type">수정자의 유형.</param>
        /// <param name="source">수정자의 출처.</param>
        /// <param name="order">수정자의 적용 순서. 기본값은 ModifierType의 정수 값입니다.</param>
        public StatModifier(float value, ModifierType type, IStatModifierSource source, int order = -1)
        {
            this.value = value;
            this.type = type;
            this.source = source;
            this.order = order == -1 ? (int)type : order;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 수정자의 정보를 문자열로 반환합니다.
        /// </summary>
        /// <returns>수정자 정보 문자열.</returns>
        public override string ToString()
        {
            return $"Type: {Type}, Value: {Value}, Order: {Order}, Source: {Source?.GetType().Name ?? "None"}";
        }

        #endregion
    }

    #endregion
}