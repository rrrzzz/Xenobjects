
using Code;
using UnityEngine;

public abstract class ArObjectManagerBase : MonoBehaviour
{
    protected bool IsInitialized;
    protected MovementInteractionProviderBase DataProvider;
    
    public virtual void Initialize(MovementInteractionProviderBase dataProvider)
    {
        DataProvider = dataProvider;
        IsInitialized = true;
    }
}
