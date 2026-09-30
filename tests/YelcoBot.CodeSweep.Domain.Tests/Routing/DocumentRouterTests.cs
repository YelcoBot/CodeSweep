using FluentAssertions;
using Xunit;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Domain.Routing;
using YelcoBot.CodeSweep.Domain.Services;

namespace YelcoBot.CodeSweep.Domain.Tests.Routing
{
    public class DocumentRouterTests
    {
        private readonly DocumentRouter _router = new DocumentRouter(new GeneratedCodeDetector());

        [Theory]
        [InlineData(@"C:\Repo\Service.cs")]
        [InlineData(@"C:\Repo\Module.VB")]
        [InlineData(@"C:\Repo\Pages\Index.razor.cs")]
        public void Route_ShouldReturnRoslyn_ForCSharpAndVisualBasic(string filePath)
        {
            _router.Route(filePath, new SweepOptions()).Should().Be(CleanupEngine.Roslyn);
        }

        [Theory]
        [InlineData(@"C:\Repo\Default.aspx")]
        [InlineData(@"C:\Repo\Controls\Menu.ascx")]
        [InlineData(@"C:\Repo\Site.Master")]
        [InlineData(@"C:\Repo\Pages\Index.razor")]
        [InlineData(@"C:\Repo\Views\Home\Index.cshtml")]
        [InlineData(@"C:\Repo\MainWindow.xaml")]
        [InlineData(@"C:\Repo\web.config")]
        public void Route_ShouldReturnEditor_ForFilesNotSupportedByRoslyn(string filePath)
        {
            _router.Route(filePath, new SweepOptions()).Should().Be(CleanupEngine.Editor);
        }

        [Theory]
        [InlineData(@"C:\Repo\Default.aspx.designer.cs")]
        [InlineData(@"C:\Repo\Form1.Designer.cs")]
        [InlineData(@"C:\Repo\obj\Debug\net8.0\App.g.cs")]
        [InlineData(@"C:\Repo\obj\Debug\Page.xaml")]
        [InlineData(@"C:\Repo\logo.png")]
        [InlineData("")]
        public void Route_ShouldReturnSkip_ForGeneratedOrUnknownFiles(string filePath)
        {
            _router.Route(filePath, new SweepOptions()).Should().Be(CleanupEngine.Skip);
        }

        [Fact]
        public void Route_ShouldReturnSkip_ForEditorFiles_WhenFormatEditorFilesIsDisabled()
        {
            SweepOptions options = new SweepOptions { FormatEditorFiles = false };

            _router.Route(@"C:\Repo\Default.aspx", options).Should().Be(CleanupEngine.Skip);
        }

        [Fact]
        public void Route_ShouldUseConfiguredEditorExtensions()
        {
            SweepOptions options = new SweepOptions { IncludeWebForms = false, IncludeRazor = false, IncludeHtml = false, AdditionalFileExtensions = "aspx; .razor" };

            _router.Route(@"C:\Repo\Default.aspx", options).Should().Be(CleanupEngine.Editor);
            _router.Route(@"C:\Repo\Index.razor", options).Should().Be(CleanupEngine.Editor);
            _router.Route(@"C:\Repo\index.html", options).Should().Be(CleanupEngine.Skip);
        }

        [Fact]
        public void Route_ShouldIncludeGeneratedFiles_WhenIgnoreGeneratedCodeIsDisabled()
        {
            SweepOptions options = new SweepOptions { IgnoreGeneratedCode = false };

            _router.Route(@"C:\Repo\Default.aspx.designer.cs", options).Should().Be(CleanupEngine.Roslyn);
        }
    }
}
