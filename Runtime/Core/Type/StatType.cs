using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableStatSystem
{
    [CreateAssetMenu(fileName = "New Stat Type", menuName = "Character Stats/Type/Stat Type")]
    public class StatType : ScriptableObject
    {
        [SerializeField] private string statName;
        
        public string StatName => statName;
    }
}