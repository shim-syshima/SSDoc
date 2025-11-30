namespace SSDoc.Services
{
    /// <summary>
    /// Represents the <see cref="CaretInfo"/> struct.
    /// </summary>
    public struct CaretInfo
    {
        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets or sets the line.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// Gets or sets the column.
        /// </summary>
        public int Column { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CaretInfo"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="line">The line.</param>
        /// <param name="column">The column.</param>
        public CaretInfo(string filePath, int line, int column)
        {
            FilePath = filePath;
            Line = line;
            Column = column;
        }
    }
}