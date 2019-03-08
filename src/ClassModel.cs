using System.Collections.Generic;

namespace Paillave.SharpReverse
{
    public class ClassModel
    {
        public string SubClass { get; set; }
        public string Documentation { get; set; }
        public string Name { get; set; }
        public string InheritsFrom { get; set; }
        public ClassModelType Type { get; set; }
        public List<ClassModelMember> Members { get; set; }
        public List<ClassModelRelationShip> Relationships { get; set; }
        public List<ClassModelRelationShip> Aggregations { get; set; }
        public override string ToString() => Name;
    }
    public class ClassModelMember{
        public string Name { get; set; }
        public string Type { get; set; }
    }
    public class ClassModelRelationShip
    {
        public string Name { get; set; }
        public string Target { get; set; }
        public override string ToString() => $"{Name}->{Target}";
    }
}