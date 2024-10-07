using UnityEngine;

#if USE_SCRIPTABLE_OBJECT_ARCHITECTURE
using ScriptableObjectArchitecture;
#endif

namespace ScriptableStatSystem
{
    public abstract class StatDefinition : ScriptableObject
    {
        public StatType statType;    
    }
}