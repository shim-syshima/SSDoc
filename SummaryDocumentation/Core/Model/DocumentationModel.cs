using System.Collections.Generic;

namespace SummaryDocumentation.Core.Model
{
    /// <summary>
    /// Represents the DocumentationModel class.
    /// </summary>
    public sealed class DocumentationModel
    {
        /// <summary>
        /// Gets or sets the Summary.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets the Parameters.
        /// </summary>
        public IList<ParameterDocumentation> Parameters { get; } =
            new List<ParameterDocumentation>();

        /// <summary>
        /// Gets the TypeParameters.
        /// </summary>
        public IList<TypeParameterDocumentation> TypeParameters { get; } =
            new List<TypeParameterDocumentation>();

        public string Returns { get; set; } = string.Empty;

        
        /// <summary>
        /// Performs the HasAnyContent operation.
        /// </summary>
        /// <returns>True if the operation succeeds; otherwise, false.</returns>
        public bool HasAnyContent()
        {
            return !string.IsNullOrWhiteSpace(Summary)
                   || Parameters.Count > 0
                   || TypeParameters.Count > 0
                   || !string.IsNullOrWhiteSpace(Returns);
        }
    }
}
