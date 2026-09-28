using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

using RoslynSolution = Microsoft.CodeAnalysis.Solution;

namespace YelcoBot.CodeSweep.Infrastructure.VisualStudio.Workspace
{
    public class VsWorkspaceAccessor : IWorkspaceAccessor
    {
        private VisualStudioWorkspace? _workspace;

        private VisualStudioWorkspace? Workspace
        {
            get
            {
                if (_workspace == null)
                {
                    IComponentModel componentModel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SComponentModel));
                    _workspace = componentModel?.GetService<VisualStudioWorkspace>();
                }
                return _workspace;
            }
        }

        public RoslynSolution? CurrentSolution => Workspace?.CurrentSolution;

        public IReadOnlyList<DocumentId> GetOpenDocumentIds()
        {
            VisualStudioWorkspace? workspace = Workspace;
            if (workspace == null || workspace.CurrentSolution == null)
                return Array.Empty<DocumentId>();

            List<DocumentId> openDocIds = workspace.GetOpenDocumentIds().ToList();
            return openDocIds;
        }

        public Task<bool> TryApplyChangesAsync(RoslynSolution newSolution, CancellationToken cancellationToken = default)
        {
            VisualStudioWorkspace? workspace = Workspace;
            if (workspace == null)
                return Task.FromResult(false);

            return Task.FromResult(workspace.TryApplyChanges(newSolution));
        }
    }
}
