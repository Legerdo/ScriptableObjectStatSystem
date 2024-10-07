using System;
using UnityEngine;

namespace ScriptableStatSystem
{
    /// <summary>
    /// 비수학적 스탯을 나타내는 클래스.
    /// </summary>
    /// <typeparam name="T">스탯의 데이터 타입.</typeparam>
    [System.Serializable]
    public class NonNumericStat<T> : Stat
    {
        [SerializeField]
        private T _value;

        /// <summary>
        /// 스탯의 현재 값.
        /// 값이 변경될 때마다 OnValueChanged 이벤트가 발생합니다.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value;
                    NotifyValueChanged(_value);
                }
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="initialValue">초기 값</param>
        public NonNumericStat(T initialValue)
        {
            _value = initialValue;
        }

        #region Events

        /// <summary>
        /// 스탯 값이 변경될 때 발생하는 이벤트.
        /// </summary>
        public event Action<T> OnValueChanged;

        /// <summary>
        /// 스탯 값이 변경되었음을 알립니다.
        /// </summary>
        protected void NotifyValueChanged(T newValue)
        {
            OnValueChanged?.Invoke(newValue);
        }

        #endregion        
    }
}
