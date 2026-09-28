using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using YelcoBot.CodeSweep.Application.Abstractions;
using YelcoBot.CodeSweep.Application.UseCases;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Results;
using YelcoBot.CodeSweep.Domain.Selection;

namespace YelcoBot.CodeSweep.Application.Tests.UseCases
{
    public class CleanupActiveDocumentUseCaseTests
    {
        private readonly ICodeCleaner _codeCleaner = Substitute.For<ICodeCleaner>();
        private readonly IEditorContext _editorContext = Substitute.For<IEditorContext>();
        private readonly ISettingsStore _settingsStore = Substitute.For<ISettingsStore>();
        private readonly IUserInteraction _userInteraction = Substitute.For<IUserInteraction>();

        private readonly CleanupActiveDocumentUseCase _sut;

        public CleanupActiveDocumentUseCaseTests()
        {
            _sut = new CleanupActiveDocumentUseCase(_codeCleaner, _editorContext, _settingsStore, _userInteraction);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldDoNothing_WhenNoActiveDocument()
        {
            _editorContext.GetActiveDocumentPathAsync().Returns(Task.FromResult<string?>(null));

            await _sut.ExecuteAsync();

            await _codeCleaner.DidNotReceiveWithAnyArgs().CleanAsync(Arg.Any<DocumentSelection>(), Arg.Any<SweepOptions>());
        }

        [Fact]
        public async Task ExecuteAsync_ShouldInvokeCleanerAndShowSummary_WhenActiveDocumentExists()
        {
            string filePath = @"C:\Repo\Class1.cs";
            _editorContext.GetActiveDocumentPathAsync().Returns(Task.FromResult<string?>(filePath));
            _settingsStore.GetOptionsAsync().Returns(Task.FromResult(new SweepOptions()));

            SweepSummary expectedSummary = new SweepSummary { ProcessedFilesCount = 1, ChangedFilesCount = 1 };
            _codeCleaner.CleanAsync(Arg.Any<DocumentSelection>(), Arg.Any<SweepOptions>(), Arg.Any<IProgress<SweepProgress>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(expectedSummary));

            _userInteraction.When(x => x.RunWithProgressAsync(Arg.Any<string>(), Arg.Any<Func<IProgress<SweepProgress>, CancellationToken, Task>>()))
                .Do(call =>
                {
                    Func<IProgress<SweepProgress>, CancellationToken, Task> action = call.Arg<Func<IProgress<SweepProgress>, CancellationToken, Task>>();
                    action(Substitute.For<IProgress<SweepProgress>>(), CancellationToken.None).Wait();
                });

            await _sut.ExecuteAsync();

            await _userInteraction.Received(1).ShowSummaryAsync(Arg.Any<SweepSummary>());
        }
    }
}
