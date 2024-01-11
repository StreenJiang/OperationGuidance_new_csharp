using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OperationGuidance_service.Attributes;
using System.Reflection;

namespace OperationGuidance_service.Configurations {
    public class DependencyInjector {
        public static IServiceCollection Services {
            private set; get;
        }

        public static IServiceProvider Provider {
            private set; get;
        }

        public static void Initialize() {
            // Create dependency injection service
            Services = new ServiceCollection();

            // TODO: should log info here: Dependencies injection start...

            // Check and inject object
            AssemblyName[] assemblies = Assembly.GetCallingAssembly().GetReferencedAssemblies();
            foreach (AssemblyName assemblyName in assemblies) {
                Assembly assembly = Assembly.Load(assemblyName);
                Type[] types = assembly.GetTypes();
                foreach (Type type in types) {
                    if (type.IsInterface || type.IsAbstract) {
                        continue;
                    }
                    IEnumerable<Attribute> attributes = type.GetCustomAttributes();
                    foreach (Attribute attribute in attributes) {
                        if (attribute is ComponentAttribute) {
                            // TODO: should log info here
                            Services.TryAddSingleton(type);
                        }
                    }
                }
            }
            // Build denpendency injection service
            Provider = Services.BuildServiceProvider();

            // Create instances
            IEnumerator<ServiceDescriptor> enumerator = Services.GetEnumerator();
            while (enumerator.MoveNext()) {
                Type? currentType = enumerator.Current.ImplementationType;
                if (currentType == null) {
                    // TODO: may be need a warn log here
                    continue;
                }
                object? currentObj = Provider.GetService(currentType);
                FieldInfo[] fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields) {
                    IEnumerable<Attribute> fieldAttrs = field.GetCustomAttributes();
                    foreach (Attribute fieldAttr in fieldAttrs) {
                        if (fieldAttr is AutowiredAttribute) {
                            // TODO: should log info here
                            field.SetValue(currentObj, Provider.GetService(field.FieldType));
                            break;
                        }
                    }
                }
                PropertyInfo[] properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (PropertyInfo property in properties) {
                    IEnumerable<Attribute> fieldAttrs = property.GetCustomAttributes();
                    foreach (Attribute fieldAttr in fieldAttrs) {
                        if (fieldAttr is AutowiredAttribute) {
                            // TODO: should log info here
                            property.SetValue(currentObj, Provider.GetService(property.PropertyType));
                            break;
                        }
                    }
                }
            }

            // TODO: should log info here: Dependencies injection done...
        }
    }
}
