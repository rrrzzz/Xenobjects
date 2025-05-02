
using Code;
using UnityEngine;

public abstract class ArObjectManagerBase : MonoBehaviour
{
    protected bool IsInitialized;
    protected MovementInteractionProviderBase DataProvider;
    protected MovementPathVisualizer PathVisualizer;
    
    public virtual void Initialize(MovementInteractionProviderBase dataProvider, MovementPathVisualizer pathVisualizer)
    {
        DataProvider = dataProvider;
        IsInitialized = true;
        PathVisualizer = pathVisualizer;
    }
}
