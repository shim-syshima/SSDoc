namespace SummaryDocumentation.Core.Model
{
    public sealed class TypeParameterDocumentation
    {
        public string Name { get; }
        public string Description { get; }

        public TypeParameterDocumentation(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
