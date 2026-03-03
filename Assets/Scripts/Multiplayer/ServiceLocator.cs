using System;
using System.Collections.Generic;

namespace GolfGame.Multiplayer
{
    /// <summary>
    /// Simple service locator for dependency injection.
    /// Register services at startup, retrieve them anywhere.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// Register a service implementation for an interface type.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        /// <summary>
        /// Get a registered service. Returns null if not registered.
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }
            return null;
        }

        /// <summary>
        /// Clear all registered services. Useful for testing.
        /// </summary>
        public static void Clear()
        {
            services.Clear();
        }
    }
}
