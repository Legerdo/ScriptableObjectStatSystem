using UnityEngine;
using System.Collections.Generic;

#if USE_SCRIPTABLE_OBJECT_ARCHITECTURE
using ScriptableObjectArchitecture;
#endif

namespace ScriptableStatSystem
{
    public class NonNumericStatDefinition<T> : StatDefinition
    {
#if USE_SCRIPTABLE_OBJECT_ARCHITECTURE
        public GenericReference<T, GenericVariable<T>> baseValue;
#else
    public T baseValue;
#endif
    }
}