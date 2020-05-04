﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Onbox.Di.V6
{
    /// <summary>
    /// Onbox's IOC container contract
    /// </summary>
    public interface IContainer
    {
        /// <summary>
        /// Adds an implementation as a singleton on the container.
        /// </summary>
        void AddSingleton<TImplementation>() where TImplementation : class;

        /// <summary>
        /// Adds an instance as a singleton on the container
        /// </summary>
        void AddSingleton<TImplementation>(TImplementation instance) where TImplementation : class;

        /// <summary>
        /// Adds an implementation to a contract as a singleton on the container
        /// </summary>
        void AddSingleton<TContract, TImplementation>() where TImplementation : TContract;

        /// <summary>
        /// Adds an instance as a singleton on the container
        /// </summary>
        void AddSingleton<TContract, TImplementation>(TImplementation instance) where TImplementation : TContract;

        /// <summary>
        /// Adds an implementation as a transient on the container.
        /// </summary>
        void AddTransient<TImplementation>() where TImplementation : class;

        /// <summary>
        /// Adds an implementation to a contract as transient on the container
        /// </summary>
        void AddTransient<TContract, TImplementation>() where TImplementation : TContract;

        /// <summary>
        /// Resets the entire container
        /// </summary>
        void Clear();

        /// <summary>
        /// Asks the container for a new instance of a type
        /// </summary>
        T Resolve<T>();
    }

    /// <summary>
    /// Onbox's IOC container implementation
    /// </summary>
    public class Container : IContainer, IDisposable
    {
        private IDictionary<Type, Type> types = new Dictionary<Type, Type>();
        private IDictionary<Type, object> instances = new Dictionary<Type, object>();

        private IDictionary<Type, Type> singletonCache = new Dictionary<Type, Type>();

        private List<Type> currentTypes = new List<Type>();
        private Type currentType;

        /// <summary>
        /// Adds an implementation as a singleton on the container.
        /// </summary>
        /// <remarks>It can not be an abstract or interface type</remarks>
        /// <typeparam name="TImplementation"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown when using a abstract class or an interface</exception>
        public void AddSingleton<TImplementation>() where TImplementation : class
        {
            var type = typeof(TImplementation);
            EnsureNonAbstractClass(type);
            this.singletonCache[type] = type;
        }

        /// <summary>
        /// Adds an instance as a singleton on the container
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="instance"></param>
        public void AddSingleton<TImplementation>(TImplementation instance) where TImplementation : class
        {
            this.instances[typeof(TImplementation)] = instance;
        }

        /// <summary>
        /// Adds an implementation to a contract as a singleton on the container
        /// </summary>
        /// <remarks>It can not be an abstract or interface type</remarks>
        /// <typeparam name="TContract"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown when using a abstract class or an interface</exception>
        public void AddSingleton<TContract, TImplementation>() where TImplementation : TContract
        {
            var type = typeof(TImplementation);
            EnsureNonAbstractClass(type);
            this.singletonCache[typeof(TContract)] = type;
        }

        /// <summary>
        /// Adds an instance as a singleton on the container
        /// </summary>
        /// <typeparam name="TContract"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        public void AddSingleton<TContract, TImplementation>(TImplementation instance) where TImplementation : TContract
        {
            this.instances[typeof(TContract)] = instance;
        }


        /// <summary>
        /// Adds an implementation as a transient on the container.
        /// </summary>
        /// <remarks>It can not be an abstract or interface type</remarks>
        /// <typeparam name="TImplementation"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown when using a abstract class or an interface</exception>
        public void AddTransient<TImplementation>() where TImplementation : class
        {
            var type = typeof(TImplementation);
            EnsureNonAbstractClass(type);
            this.types[type] = type;
        }

        /// <summary>
        /// Adds an implementation to a contract as transient on the container
        /// </summary>
        /// <remarks>It can not be an abstract or interface type</remarks>
        /// <typeparam name="TContract"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <exception cref="InvalidOperationException">Thrown when using a abstract class or an interface</exception>
        public void AddTransient<TContract, TImplementation>() where TImplementation : TContract
        {
            var type = typeof(TImplementation);
            EnsureNonAbstractClass(type);
            this.types[typeof(TContract)] = type;
        }


        /// <summary>
        /// Asks the container for a new instance of a type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            var type = (T)this.Resolve(typeof(T));
            this.currentTypes.Clear();
            return type;
        }

        private object Resolve(Type contract)
        {
            Console.WriteLine("Onbox Container request: " + contract.ToString());

            // Always prioritize instances
            if (this.instances.ContainsKey(contract))
            {
                return this.instances[contract];
            }
            else if (this.singletonCache.ContainsKey(contract))
            {
                var instance = this.Resolve(contract, this.singletonCache);
                this.instances[contract] = instance;
                this.singletonCache.Remove(contract);
                return instance;
            }
            else if (!contract.IsAbstract || this.types.ContainsKey(contract))
            {
                var instance = this.Resolve(contract, this.types);
                return instance;
            }
            else
            {
                throw new KeyNotFoundException($"{contract.Name} not registered on Onbox Container.");
            }
        }


        private object Resolve(Type contract, IDictionary<Type, Type> dic)
        {
            // Check for Ciruclar dependencies
            if (this.currentTypes.Contains(contract))
            {
                string error;
                if (currentType == contract)
                {
                    error = $"Onbox Container found circular dependency on {currentType.Name} trying to inject itself.";
                    Console.WriteLine(error);
                    throw new InvalidOperationException(error);
                }

                if (currentType != null)
                {
                    error = $"Onbox Container found circular dependency between {currentType.Name} and {contract.Name}.";
                    Console.WriteLine(error);
                    throw new InvalidOperationException(error);
                }

                error = $"Onbox Container found circular dependency on: {contract.Name}.";
                Console.WriteLine(error);
                throw new InvalidOperationException(error);
            }
            this.currentTypes.Add(contract);

            // If this is a concrete type just instantiate it, if not, get the concrete type on the dictionary
            Type implementation = contract;
            if (implementation.IsAbstract)
            {
                implementation = dic[contract];
            }

            // Get the first available contructor
            var constructors = implementation.GetConstructors();
            if (constructors.Length < 1)
            {
                throw new InvalidOperationException($"Onbox Container: {implementation.Name} has no available constructors. The container can not instantiate it.");
            }

            ConstructorInfo constructor = constructors[0];

            ParameterInfo[] constructorParameters = constructor.GetParameters();
            if (constructorParameters.Length == 0)
            {
                Console.WriteLine("Onbox Container instantiated " + implementation.ToString());
                currentTypes.Remove(implementation);
                return Activator.CreateInstance(implementation);
            }
            List<object> parameters = new List<object>(constructorParameters.Length);
            foreach (ParameterInfo parameterInfo in constructorParameters)
            {
                var type = parameterInfo.ParameterType;
                currentType = implementation;
                parameters.Add(this.Resolve(type));
            }

            Console.WriteLine("Onbox Container instantiated " + implementation.ToString());
            currentTypes.Remove(implementation);
            return constructor.Invoke(parameters.ToArray());
        }

        /// <summary>
        /// Resets the entire container
        /// </summary>
        public void Clear()
        {
            this.types.Clear();
            this.instances.Clear();
            this.singletonCache.Clear();
            this.currentType = null;
        }

        /// <summary>
        /// Creates the default container implementation and adds it to itself as a abstract singleton of type <see cref="IContainer"/>
        /// </summary>
        /// <returns></returns>
        public static Container Default()
        {
            var container = new Container();
            container.AddSingleton<IContainer>(container);

            return container;
        }

        private static void EnsureNonAbstractClass(Type type)
        {
            if (type.IsInterface)
            {
                throw new InvalidOperationException($"Can not add {type.Name} to Onbox Container because it is an interface. You need to provide a concrete type or provide an instance.");
            }
            if (type.IsAbstract)
            {
                throw new InvalidOperationException($"Can not add {type.Name} to Onbox Container because because it is an abstract type. You need to provide a concrete type or provide an instance.");
            }
        }

        /// <summary>
        /// Clears and releases resources from the container
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("Onbox Container disposing... ");

            this.types?.Clear();
            this.types = null;

            this.instances?.Clear();
            this.instances = null;

            this.singletonCache?.Clear();
            this.singletonCache = null;

            this.currentTypes?.Clear();
            this.currentTypes = null;
           
            this.currentType = null;
        }
    }
}
