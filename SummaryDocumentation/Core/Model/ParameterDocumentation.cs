namespace SummaryDocumentation.Core.Model
{
    public sealed class ParameterDocumentation
    {
        public string Name { get; }
        public string Description { get; }

        public ParameterDocumentation(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
