using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T> where T : IEvent
{
    private static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();

    public static void Subscribe(IEventBinding<T> binding)
    {
        bindings.Add(binding);
    }

    public static void Unsubscribe(IEventBinding<T> binding)
    {
        bindings.Remove(binding);
    }

    public static void Publish(T eventData)
    {
        foreach (var binding in bindings)
        {
            binding.OnEvent?.Invoke(eventData);
            binding.OnEventNoArgs?.Invoke();
        }
    }

    private static void Clear()
    {
        bindings.Clear();
        Debug.Log("Clearing " + typeof(T).Name + " EventBus");
    }


}
