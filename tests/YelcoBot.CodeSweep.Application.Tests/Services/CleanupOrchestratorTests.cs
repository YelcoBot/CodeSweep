using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Xunit;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.Services;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Routing;
using YelcoBot.CodeSweep.Domain.Selection;
using YelcoBot.CodeSweep.Domain.Services;

namespace YelcoBot.CodeSweep.Application.Tests.Services
{
    public class CleanupOrchestratorTests
    {
        private readonly IDocumentProvider _documentProvider = Substitute.For<IDocumentProvider>();
        private readonly ICodeCleaner _codeCleaner = Substitute.For<ICodeCleaner>();
        private readonly IEditorFormatter _editorFormatter = Substitute.For<IEditorFormatter>();
        private readonly CleanupOrchestrator _sut;

        private DocumentSelection? _roslynSelection;
        private IReadOnlyList<string>? _editorFiles;

        public CleanupOrchestratorTests()
        {
            _sut = new CleanupOrchestrator(_documentProvider, new DocumentRouter(new GeneratedCodeDetector()), _codeCleaner, _editorFormatter, Substitute.For<IWebFormsDesignerGenerator>(), new SweepActivity());

            _codeCleaner.CleanAsync(Arg.Do<DocumentSelection>(s => _roslynSelection = s), Arg.Any<SweepOptions>(), Arg.Any<IProgress<SweepProgress>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new SweepSummary { ProcessedFilesCount = 2, ChangedFilesCount = 1 }));

            _editorFormatter.FormatAsync(Arg.Do<IReadOnlyList<string>>(f => _editorFiles = f), Arg.Any<IProgress<SweepProgress>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new SweepSummary { ProcessedFilesCount = 1, ChangedFilesCount = 1 }));
        }

        private void GivenFiles(params string[] files)
        {
            _documentProvider.GetFilePathsAsync(Arg.Any<DocumentSelection>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IReadOnlyList<string>>(files));
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRouteFilesToRoslynAndEditor_AndSkipGenerated()
        {
            GivenFiles(
                @"C:\Repo\Service.cs",
                @"C:\Repo\Module.vb",
                @"C:\Repo\Default.aspx",
                @"C:\Repo\Default.aspx.designer.cs",
                @"C:\Repo\obj\Debug\App.g.cs",
                @"C:\Repo\logo.png");

            SweepSummary summary = await _sut.ExecuteAsync(DocumentSelection.Solution(), new SweepOptions());

            _roslynSelection!.FilePaths.Should().BeEquivalentTo(@"C:\Repo\Service.cs", @"C:\Repo\Module.vb");
            _editorFiles.Should().BeEquivalentTo(@"C:\Repo\Default.aspx");
            summary.ProcessedFilesCount.Should().Be(3);
            summary.ChangedFilesCount.Should().Be(2);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotCallEditor_WhenFormatEditorFilesIsDisabled()
        {
            GivenFiles(@"C:\Repo\Service.cs", @"C:\Repo\Default.aspx");

            await _sut.ExecuteAsync(DocumentSelection.Solution(), new SweepOptions { FormatEditorFiles = false });

            await _codeCleaner.ReceivedWithAnyArgs(1).CleanAsync(default!, default!);
            await _editorFormatter.DidNotReceiveWithAnyArgs().FormatAsync(default!);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotCallRoslyn_WhenThereAreOnlyEditorFiles()
        {
            GivenFiles(@"C:\Repo\Index.razor");

            await _sut.ExecuteAsync(DocumentSelection.OpenDocuments(), new SweepOptions());

            await _codeCleaner.DidNotReceiveWithAnyArgs().CleanAsync(default!, default!);
            _editorFiles.Should().ContainSingle().Which.Should().Be(@"C:\Repo\Index.razor");
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSkipEditorPhase_WhenRoslynPhaseIsCancelled()
        {
            GivenFiles(@"C:\Repo\Service.cs", @"C:\Repo\Default.aspx");
            _codeCleaner.CleanAsync(Arg.Any<DocumentSelection>(), Arg.Any<SweepOptions>(), Arg.Any<IProgress<SweepProgress>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(SweepSummary.Cancelled()));

            SweepSummary summary = await _sut.ExecuteAsync(DocumentSelection.Solution(), new SweepOptions());

            summary.IsCancelled.Should().BeTrue();
            await _editorFormatter.DidNotReceiveWithAnyArgs().FormatAsync(default!);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRemoveDuplicatePaths()
        {
            GivenFiles(@"C:\Repo\Service.cs", @"c:\repo\SERVICE.cs");

            await _sut.ExecuteAsync(DocumentSelection.Solution(), new SweepOptions());

            _roslynSelection!.FilePaths.Should().HaveCount(1);
        }
    }
}
