using PK.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ContainerMM
{
    public class ContainerMM : IContainer
    {
        private IDictionary<Type, Type> dictionary;
        private IDictionary<Type, object> factory;

        public ContainerMM()
        {
            dictionary = new Dictionary<Type, Type>();
            factory = new Dictionary<Type, object>();
        }


        /// <summary>
        /// Registering object providing implementation of interface T.
        /// Resolve method will execute this instance of provider to obtain interface T implementation.
        /// </summary>
        /// <typeparam name="T">implementing interface</typeparam>
        /// <param name="provider">provider instance</param>
        public void Register<T>(Func<T> provider) where T : class
        {
            Type type = provider().GetType();

            RegisterHelper(type);
        }

        /// <summary>
        /// Registering object implementing interface T as provider of singleton.
        /// Resolve method will return exact this single instance for required interface T.
        /// </summary>
        /// <typeparam name="T">implementing interface</typeparam>
        /// <param name="impl">singleton instance</param>
        public void Register<T>(T impl) where T : class
        {
            Type type = impl.GetType();

            var interfaces = type.GetInterfaces();

            foreach (var item in interfaces)
            {
                factory.Add(item, impl);
            }
        }

        /// <summary>
        /// Registering class as provider of new instance of all implementing interfaces.
        /// Resolve method will return new instance of this class for each implemented interface.
        /// </summary>
        /// <param name="type">type of interface implementation to register</param>
        public void Register(Type type)
        {
            RegisterHelper(type);
        }

        /// <summary>
        /// Registering all interface implementations in assembly as providers of new instances.
        /// Resolve method will return new instance for each registered interface implementations.
        /// </summary>
        /// <param name="assembly">assembly with interface implementations</param>
        public void Register(System.Reflection.Assembly assembly)
        {
            var classes = assembly.GetTypes().Where(t => t.IsClass && t.IsPublic && !t.IsAbstract);

            foreach (var @class in classes)
            {
                RegisterHelper(@class);
            }
        }

        /// <summary>
        /// Resolving instance of object implementing required interface
        /// throws UnresolvedDependenciesException in case of not resolved dependencies for required component
        /// </summary>
        /// <typeparam name="T">required interface</typeparam>
        /// <returns>object implementing required interface T or null if no implementation registered</returns>
        public object Resolve(Type type)
        {
            return ResolveHelper(type);
        }

        /// <summary>
        /// Resolving instance of object implementing required interface
        /// throws UnresolvedDependenciesException in case of not resolved dependencies for required component
        /// </summary>
        /// <param name="type">required interface</param>
        /// <returns>object implementing required interface T or null if no implementation registered</returns>
        public T Resolve<T>() where T : class
        {
            Type type = typeof(T);

            return ResolveHelper(type) as T;
        }

        #region Helpers

        private object ResolveHelper(Type type, bool isFromConstructor = false)
        {
            if (dictionary.Keys.Any(k => k == type))
            {
                var constructors = dictionary[type].GetConstructors();
                var parameters = GetParametersForConstructor(constructors);

                return Activator.CreateInstance(dictionary[type], parameters);
            }
            else if (factory.Keys.Any(k => k == type))
            {
                return factory[type];
            }
            else
            {
                if (isFromConstructor)
                {
                    throw new UnresolvedDependenciesException();
                }

                return null;
            }
        }

        private void RegisterHelper(Type type)
        {
            var interfaces = type.GetInterfaces();

            foreach (var item in interfaces)
            {
                if (dictionary.Keys.Any(k => k == item))
                    dictionary.Remove(item);

                dictionary.Add(item, type);
            }
        }

        private object[] GetParametersForConstructor(ConstructorInfo[] constructors)
        {
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                if (parameters.All(p => p.ParameterType.IsInterface))
                {
                    var arguments = new List<object>();

                    foreach (var argument in parameters)
                    {
                        arguments.Add(ResolveHelper(argument.ParameterType, true));
                    }
                    return arguments.ToArray();
                }
            }

            return null;
        }

        #endregion
    }
}
