using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
    private static readonly Dictionary<Type, Func<object>> factories = new Dictionary<Type, Func<object>>();

    // Events
    public static event Action<Type, object> ServiceRegistered;
    public static event Action<Type> ServiceUnregistered;

    // Register a service instance
    public static void Register<T>(T service) where T : class
    {
        var type = typeof(T);
        services[type] = service;
        ServiceRegistered?.Invoke(type, service);
    }

    // Register a factory for lazy instantiation
    public static void RegisterFactory<T>(Func<T> factory) where T : class
    {
        var type = typeof(T);
        factories[type] = () => factory();
        ServiceRegistered?.Invoke(type, null);
    }

    // Register interface to implementation mapping (auto-instantiates on resolve)
    public static void RegisterType<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        factories[typeof(TInterface)] = () => AutoWire<TImplementation>();
        ServiceRegistered?.Invoke(typeof(TInterface), null);
    }

    // Resolve a service instance (with auto-wiring and factory support)
    public static T Resolve<T>() where T : class
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
            return service as T;
        if (factories.TryGetValue(type, out var factory))
        {
            var instance = factory();
            services[type] = instance;
            return instance as T;
        }
        // Try auto-wiring if type is a class with public constructor
        if (!type.IsAbstract && !type.IsInterface)
        {
            var instance = AutoWire<T>();
            services[type] = instance;
            return instance;
        }
        throw new Exception($"Service of type {type} not registered.");
    }

    // TryResolve: returns null if not registered
    public static T TryResolve<T>() where T : class
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
            return service as T;
        if (factories.TryGetValue(type, out var factory))
        {
            var instance = factory();
            services[type] = instance;
            return instance as T;
        }
        if (!type.IsAbstract && !type.IsInterface)
        {
            var instance = AutoWire<T>();
            services[type] = instance;
            return instance;
        }
        return null;
    }

    // Unregister a service
    public static void Unregister<T>() where T : class
    {
        var type = typeof(T);
        services.Remove(type);
        factories.Remove(type);
        ServiceUnregistered?.Invoke(type);
    }

    // Clear all services (optional)
    public static void Clear()
    {
        services.Clear();
        factories.Clear();
    }

    // Auto-wire constructor dependencies (basic DI)
    private static T AutoWire<T>() where T : class
    {
        var type = typeof(T);
        var ctor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        if (ctor == null)
            throw new Exception($"No public constructor found for {type}");
        var parameters = ctor.GetParameters()
            .Select(p => typeof(ServiceLocator).GetMethod("Resolve").MakeGenericMethod(p.ParameterType).Invoke(null, null))
            .ToArray();
        return (T)ctor.Invoke(parameters);
    }
}
