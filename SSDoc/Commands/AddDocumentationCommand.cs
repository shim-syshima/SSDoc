using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;
using SSDoc.Roslyn;
using SSDoc.Services;
using SummaryDocumentation.Core.Symbols;

namespace SSDoc.Commands
{
    /// <summary>
    /// Represents the AddDocumentationCommand class.
    /// </summary>
    internal sealed class AddDocumentationCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet =
            new Guid("2b71e9e0-2fd2-4b3b-9ad7-5e41cb9a9e40");

        private readonly AsyncPackage _package;
        private readonly IServiceProvider _serviceProvider;

        private readonly CaretService _caretService;
        private readonly VsShellNotifier _notifier;

        private VisualStudioWorkspace _workspace;
        private RoslynContext _roslynContext;

        private readonly SymbolService _symbolService;
        private readonly DocumentEditor _insertionService;
        private readonly SymbolDocumentationService _documentationService;

        /// <summary>
        /// Performs the InitializeAsync operation.
        /// </summary>
        /// <param name="package">The package parameter.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Assumes.Present(commandService);

            IServiceProvider serviceProvider = package;

            _ = new AddDocumentationCommand(package, commandService, serviceProvider);
        }

        private AddDocumentationCommand(
            AsyncPackage package,
            OleMenuCommandService commandService,
            IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _package = package;
            _serviceProvider = serviceProvider;

            _caretService = new CaretService(serviceProvider);
            _notifier = new VsShellNotifier(serviceProvider);

            _symbolService = new SymbolService();
            _insertionService = new DocumentEditor();
            _documentationService = new SymbolDocumentationService();

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        private bool EnsureRoslynInitialized()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_workspace != null && _roslynContext != null)
                return true;

            var componentModel =
                _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;

            if (componentModel == null)
            {
                _notifier.Error("Failed to get component.");
                return false;
            }

            _workspace = componentModel.GetService<VisualStudioWorkspace>();
            if (_workspace == null)
            {
                _notifier.Error("Failed to get workspace.");
                return false;
            }

            _roslynContext = new RoslynContext(_workspace);
            return true;
        }

        private void Execute(object sender, EventArgs e)
        {
#pragma warning disable VSSDK007
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
#pragma warning restore VSSDK007
            {
                await ExecuteInternalAsync(_package.DisposalToken);
            }).Task.Forget();
        }

        private async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!EnsureRoslynInitialized())
                {
                    return;
                }

                var caret = _caretService.GetActiveCaretPosition();
                if (!caret.HasValue)
                {
                    return;
                }

                var (document, semanticModel, position) =
                    await _roslynContext.GetDocumentContextAsync(caret.Value, cancellationToken);

                var symbol = await _symbolService.FindSymbolAtPositionAsync(
                    document, semanticModel, position, cancellationToken);

                if (symbol == null)
                {
                    return;
                }

                if (!_documentationService.TryCreateDocumentation(symbol, out var documentationText))
                {
                    return;
                }

                var newSolution = await _insertionService.InsertAsync(
                    document, semanticModel, symbol, documentationText, cancellationToken);

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                if (!_workspace.TryApplyChanges(newSolution))
                {
                    _notifier.Error("Failed to apply changes.");
                }
            }
            catch (Exception ex)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                _notifier.Error("An error occurred: " + ex.Message);
            }
        }
    }
}
