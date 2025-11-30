using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SSDoc.Services
{
    /// <summary>
    /// Represents the <see cref="VsShellNotifier"/> class.
    /// </summary>
    public sealed class VsShellNotifier
    {
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="VsShellNotifier"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        public VsShellNotifier(IServiceProvider services)
        {
            _services = services;
        }

        /// <summary>
        /// Performs the Info operation.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="title">The title.</param>
        public void Info(string message, string title = null)
        {
            Show(message, title ?? "SSDoc", OLEMSGICON.OLEMSGICON_INFO);
        }

        /// <summary>
        /// Performs the Error operation.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="title">The title.</param>
        public void Error(string message, string title = null)
        {
            Show(message, title ?? "SSDoc", OLEMSGICON.OLEMSGICON_CRITICAL);
        }

        private void Show(string message, string title, OLEMSGICON icon)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VsShellUtilities.ShowMessageBox(
                _services,
                message,
                title,
                icon,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
