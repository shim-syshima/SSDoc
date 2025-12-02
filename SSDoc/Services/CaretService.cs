using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace SSDoc.Services
{
    /// <summary>
    /// Represents the <see cref="CaretService"/> class.
    /// </summary>
    public sealed class CaretService
    {
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="caret service"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        public CaretService(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Gets the active caret position.
        /// </summary>
        /// <returns>The nullable result.</returns>
        public CaretInfo? GetActiveCaretPosition()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textMgr = _services.GetService(typeof(SVsTextManager)) as IVsTextManager;
            if (textMgr == null)
                return null;

            textMgr.GetActiveView(1, null, out var view);
            if (view == null)
                return null;

            if (view.GetCaretPos(out var line, out var column) != VSConstants.S_OK)
                return null;

            if (view.GetBuffer(out var lines) != VSConstants.S_OK)
                return null;

            var persistFile = lines as IPersistFileFormat;
            if (persistFile == null)
                return null;

            persistFile.GetCurFile(out var filePath, out _);
            if (string.IsNullOrEmpty(filePath))
                return null;

            return new CaretInfo(filePath, line, column);
        }
    }
}