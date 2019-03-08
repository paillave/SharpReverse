using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Paillave.SharpReverse
{
    public static class AssemblyEx
    {
        public static Boolean IsAnonymousType(this Type type)
        {
            var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Count() > 0;
            var nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
            return hasCompilerGeneratedAttribute && nameContainsAnonymousType;
        }
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
        static Type entityTypeConfigurationType = typeof(IEntityTypeConfiguration<>);
        static Type queryTypeConfigurationType = typeof(IQueryTypeConfiguration<>);
        static Type enumerableType = typeof(IEnumerable<>);
        static Type migrationType = typeof(Migration);
        static Type migrationsSqlGeneratorType = typeof(IMigrationsSqlGenerator);
        static Type migrationOperationType = typeof(MigrationOperation);
        static Type modelSnapshotType = typeof(ModelSnapshot);
        static Type dbContextType = typeof(DbContext);
        static Type valueConverterType = typeof(ValueConverter);
        static Type designTimeDbContextFactoryType = typeof(IDesignTimeDbContextFactory<>);
        public static bool IsConfiguration(this Type type)
            => type.GetInterfaces()
                .Where(i => i.IsGenericType && (i.GetGenericTypeDefinition() == entityTypeConfigurationType || i.GetGenericTypeDefinition() == queryTypeConfigurationType))
                .Any();
        public static bool IsDbContextFactory(this Type type)
            => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == designTimeDbContextFactoryType)
                .Any();
        public static bool IsValueConverter(this Type type)
            => valueConverterType.IsAssignableFrom(type);
        public static bool IsMigrationsSqlGenerator(this Type type)
            => migrationsSqlGeneratorType.IsAssignableFrom(type);
        public static bool IsMigrationOperation(this Type type)
            => migrationOperationType.IsAssignableFrom(type);
        public static bool IsDbContext(this Type type)
            => dbContextType.IsAssignableFrom(type);
        public static bool IsMigration(this Type type)
            => migrationType.IsAssignableFrom(type);
        public static bool IsModelSnapshot(this Type type)
            => modelSnapshotType.IsAssignableFrom(type);
        public static bool IsStatic(this Type type)
            => type.IsAbstract && type.IsSealed;
        public static bool IsEnumerable(this Type type)
            => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == enumerableType)
                .Any();
        public static Type GetEnumeratedType(this Type type)
            => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == enumerableType)
                .Select(i => i.GetGenericArguments()[0])
                .FirstOrDefault();
    }
}