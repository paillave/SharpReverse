using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Paillave.SharpReverse
{
    public class ReverseModel
    {
        public List<ClassModel> ClassModels { get; private set; } = new List<ClassModel>();
        public ReverseModel(string assemblyPath, string rootClassName, bool noOrphans)
            : this(Assembly.LoadFrom(assemblyPath), GetXmlDocumentation(assemblyPath), rootClassName, noOrphans)
        {
        }
        private static XDocument GetXmlDocumentation(string assemblyPath)
        {
            var xmlDocumentationPath = Path.ChangeExtension(assemblyPath, "xml");
            return File.Exists(xmlDocumentationPath) ? XDocument.Load(xmlDocumentationPath) : null;
        }

        public string GetEntityTypeText(ClassModelType classModelType)
        {
            switch (classModelType)
            {
                case ClassModelType.AbstractClass: return "-abstract- ";
                case ClassModelType.Enumeration: return "-enum- ";
                default: return null;
            }
        }
        public void WriteYuml(StreamWriter sw)
        {
            sw.WriteLine("// {type:class}");
            sw.WriteLine("// {direction:leftToRight}");
            sw.WriteLine("// {generate:true}");
            var labelDico = ClassModels.ToDictionary(cm => cm.Name, cm => $"{GetEntityTypeText(cm.Type)}{cm.Name}");
            foreach (var cm in ClassModels)
            {
                if (cm.Members.Count > 0)
                    sw.WriteLine($"[{labelDico[cm.Name]}|{string.Join(';', cm.Members.Select(i => string.IsNullOrWhiteSpace(i.Type) ? i.Name : $"{i.Name}:{i.Type}"))}]");
                else
                    sw.WriteLine($"[{labelDico[cm.Name]}]");
                if (!string.IsNullOrWhiteSpace(cm.Documentation))
                    sw.WriteLine($"[{labelDico[cm.Name]}]-[note:{cm.Documentation.Replace(Environment.NewLine, "").Trim()}{{bg:cornsilk}}]");
            }
            foreach (var cm in ClassModels.Where(i => !string.IsNullOrWhiteSpace(i.SubClass)))
            {
                sw.WriteLine($"[{labelDico[cm.SubClass]}]^[{labelDico[cm.Name]}]");
            }
            foreach (var r in ClassModels.Where(cm => cm.Relationships != null).SelectMany(cm => cm.Relationships.Select(r => new { ClassModel = cm, RelationShip = r })))
            {
                var relationShipName = (r.RelationShip.Name != r.RelationShip.Target) ? r.RelationShip.Name : null;
                if (r.RelationShip.Target.EndsWith('?'))
                {
                    if (labelDico.TryGetValue(r.RelationShip.Target.Substring(0, r.RelationShip.Target.Length - 1), out var val))
                        sw.WriteLine($"[{labelDico[r.ClassModel.Name]}]{relationShipName}-0..1>[{val}]");
                }
                else
                {
                    if (labelDico.TryGetValue(r.RelationShip.Target, out var val))
                        sw.WriteLine($"[{labelDico[r.ClassModel.Name]}]{relationShipName}->[{val}]");
                }
            }
            foreach (var r in ClassModels.Where(cm => cm.Aggregations != null).SelectMany(cm => cm.Aggregations.Select(r => new { ClassModel = cm, RelationShip = r })))
            {
                var relationShipName = (r.RelationShip.Name != $"{r.RelationShip.Target}s") ? r.RelationShip.Name : null;
                if (labelDico.TryGetValue(r.RelationShip.Target, out var val))
                    sw.WriteLine($"[{labelDico[r.ClassModel.Name]}]<>{relationShipName}->[{val}]");
            }
        }

        public ReverseModel(Assembly assembly, XDocument xmlDocumentation, string rootClassName, bool noOrphans)
        {
            var types = assembly
                .GetLoadableTypes()
                .Where(i => i.IsPublic)
                .Where(i => !i.IsStatic())
                .Where(i => !i.IsAnonymousType())
                .Where(i => !i.IsConfiguration())
                .Where(i => !i.IsValueConverter())
                .Where(i => !i.IsDbContext())
                .Where(i => !i.IsDbContextFactory())
                .Where(i => !i.IsMigration())
                .Where(i => !i.IsModelSnapshot())
                .Where(i => !i.IsMigrationOperation())
                .Where(i => !i.IsMigrationsSqlGenerator())
                .Where(i => !i.IsInterface)
                .ToList();
            if (!string.IsNullOrWhiteSpace(rootClassName))
            {
                var rootType = types.Single(i => i.Name.Equals(rootClassName));
                types = types.Where(i => rootType.IsAssignableFrom(i)).ToList();
            }

            var typesNamesHashSet = new HashSet<string>(types.Select(i => i.Name).Union(types.Select(i => $"{i.Name}?")));
            Dictionary<string, string> comments = new Dictionary<string, string>();
            if (xmlDocumentation != null)
            {
                var members = xmlDocumentation.Descendants().Elements("member");
                comments = members
                   .Where(i => i.Attribute("name").Value.StartsWith("T:"))
                   .Select(i => new
                   {
                       TypeName = i.Attribute("name").Value.Split('.').LastOrDefault(),
                       Comment = i.Element("summary").Value
                   })
                   .ToDictionary(i => i.TypeName, i => i.Comment);
            }
            foreach (var type in types)
            {
                ClassModel classModel = new ClassModel { Name = type.Name };
                comments.TryGetValue(type.Name, out string documentation);
                classModel.Documentation = documentation;
                if (type.IsEnum)
                {
                    classModel.Type = ClassModelType.Enumeration;
                    classModel.Members = type.GetEnumNames().Select(i => new ClassModelMember { Name = i }).ToList();
                }
                else
                {
                    if (type.BaseType != typeof(object))
                        classModel.SubClass = type.BaseType.Name;// types.FirstOrDefault(i => type.IsSubclassOf(i))?.Name;
                    classModel.Type = type.IsAbstract ? ClassModelType.AbstractClass : ClassModelType.Class;
                    var properties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                    classModel.Relationships = properties
                        .Where(i => typesNamesHashSet.Contains(GetTypeLabel(i.PropertyType)))
                        .Select(i => new ClassModelRelationShip { Name = i.Name, Target = GetTypeLabel(i.PropertyType) })
                        .ToList();
                    classModel.Members = properties
                        .Where(i => i.Name != "Id")
                        .Where(i => !classModel.Relationships.Any(r => $"{r.Name}Id" == i.Name))
                        .Where(i => !typesNamesHashSet.Contains(GetTypeLabel(i.PropertyType)) && (i.PropertyType == typeof(string) || !i.PropertyType.IsEnumerable()))
                        .Select(i => new ClassModelMember { Name = i.Name, Type = GetTypeLabel(i.PropertyType) })
                        .ToList();
                    classModel.Aggregations = properties
                        .Where(i => !typesNamesHashSet.Contains(GetTypeLabel(i.PropertyType)) && i.PropertyType != typeof(string) && i.PropertyType.IsEnumerable())
                        .Select(i => new ClassModelRelationShip { Name = i.Name, Target = i.PropertyType.GetEnumeratedType().Name })
                        .ToList();
                }
                ClassModels.Add(classModel);
            }
            if (noOrphans)
            {
                ClassModels = ClassModels.Where(cm =>
                {
                    var links = ((cm.Aggregations?.Count) ?? 0) + ((cm.Relationships?.Count) ?? 0) + ClassModels.Count(i => i.SubClass == cm.Name) + (string.IsNullOrWhiteSpace(cm.SubClass) ? 0 : 1);
                    links += ClassModels.SelectMany(i => i.Relationships ?? new List<ClassModelRelationShip>()).Count(i => i.Target == cm.Name);
                    links += ClassModels.SelectMany(i => i.Aggregations ?? new List<ClassModelRelationShip>()).Count(i => i.Target == cm.Name);
                    return links > 0;
                }).ToList();
            }
        }
        private static string GetTypeLabel(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return underlyingType == null ? type.Name : $"{underlyingType.Name}?";
        }
    }
}