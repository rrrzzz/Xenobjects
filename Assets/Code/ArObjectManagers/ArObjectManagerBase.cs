using System.Collections.Generic;
using Code;
using UnityEngine;

public abstract class ArObjectManagerBase : MonoBehaviour
{
    protected bool IsInitialized;
    protected MovementInteractionProviderBase DataProvider;
    protected MovementPathVisualizer PathVisualizer;

    private readonly HashSet<string> _usedInteractables = new HashSet<string>();
    private bool _wasPathShown;

    /// <summary>
    /// How many distinct interactable elements this object exposes. The path
    /// visualization is offered once the visitor has discovered all of them.
    /// </summary>
    protected abstract int InteractableElementsTotal { get; }

    public virtual void Initialize(MovementInteractionProviderBase dataProvider, MovementPathVisualizer pathVisualizer)
    {
        DataProvider = dataProvider;
        PathVisualizer = pathVisualizer;
        IsInitialized = true;
    }

    /// <summary>
    /// Records that the visitor used one of the object's interactable elements.
    /// Idempotent — marking the same id twice counts once.
    /// </summary>
    protected void MarkInteractableUsed(string id)
    {
        _usedInteractables.Add(id);
    }

    /// <summary>
    /// Shows the movement path (once) after every interactable has been used.
    /// Call from Update at the point where showing the path is allowed.
    /// </summary>
    protected void TryShowPathWhenAllInteractablesUsed()
    {
        if (_wasPathShown)
        {
            return;
        }

        if (_usedInteractables.Count < InteractableElementsTotal)
        {
            return;
        }

        _wasPathShown = true;

        if (PathVisualizer)
        {
            PathVisualizer.StartCoroutine(PathVisualizer.ShowPathAfterDelay());
        }
    }
}
