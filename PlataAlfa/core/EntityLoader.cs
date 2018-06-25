using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PlataAlfa.core
{
    public static class EntityLoader
    {
        public static IEnumerable<Type> LoadEntities()
        {
            Assembly assembly = null;
            var library = DependencyContext.Default.RuntimeLibraries.Where(x => x.Name == "PlataAlfa").FirstOrDefault();
            assembly = Assembly.Load(new AssemblyName(library.Name));
            var Entities = assembly.GetTypes().Where(x => x.BaseType == typeof(Entity) || x.BaseType == typeof(DataSteward));
            return Entities;

        }
    }
}
