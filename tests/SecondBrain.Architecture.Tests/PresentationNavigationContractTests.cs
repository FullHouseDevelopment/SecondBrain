using NUnit.Framework;

namespace SecondBrain.Architecture.Tests;

[TestFixture]
public sealed class PresentationNavigationContractTests
{
    [Test]
    public void AppShell_PrimaryNavigationExpressesProductLifecycle()
    {
        var source = ReadPresentationFile("AppShell.cs");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("PrimaryItem(\"home\", \"Home\""));
            Assert.That(source, Does.Contain("PrimaryItem(\"inbox\", \"Process\""));
            Assert.That(source, Does.Contain("PrimaryItem(\"para\", \"Browse\""));
            Assert.That(source, Does.Contain("PrimaryItem(\"search\", \"Search\""));
            Assert.That(source, Does.Contain("PrimaryItem(\"review\", \"Review\""));

            Assert.That(source, Does.Contain("PrimaryContent(\"home\", \"Home\""));
            Assert.That(source, Does.Contain("PrimaryContent(\"inbox\", \"Process\""));
            Assert.That(source, Does.Contain("PrimaryContent(\"para\", \"Browse\""));
            Assert.That(source, Does.Contain("PrimaryContent(\"search\", \"Search\""));
            Assert.That(source, Does.Contain("PrimaryContent(\"review\", \"Review\""));
        });
    }

    [Test]
    public void AppShell_ImplementationAndUtilitySurfacesRemainContextual()
    {
        var source = ReadPresentationFile("AppShell.cs");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("HiddenItem(\"inbox-process\""));
            Assert.That(source, Does.Contain("HiddenItem(\"journals\""));
            Assert.That(source, Does.Contain("HiddenItem(\"create\""));
            Assert.That(source, Does.Contain("HiddenItem(\"editor\""));
            Assert.That(source, Does.Contain("HiddenItem(\"settings\""));
            Assert.That(source, Does.Contain("HiddenItem(\"data-import\""));

            foreach (var route in new[] { "journals", "create", "editor", "settings", "data-import" })
            {
                Assert.That(source, Does.Not.Contain($"PrimaryItem(\"{route}\""));
                Assert.That(source, Does.Not.Contain($"PrimaryContent(\"{route}\""));
            }
        });
    }

    [Test]
    public void KnowledgeCreationAndEditing_AreSeparateContextualSurfaces()
    {
        var root = FindRepositoryRoot();
        var shell = ReadPresentationFile("AppShell.cs");
        var create = ReadPresentationFile("CoreCreatePage.cs");
        var edit = ReadPresentationFile("CoreEditPage.cs");
        var combinedEditorPath = Path.Combine(
            root,
            "SecondBrain.Presentation",
            "CoreEditorPage.cs");

        Assert.Multiple(() =>
        {
            Assert.That(shell, Does.Contain("CoreCreatePage createPage"));
            Assert.That(shell, Does.Contain("CoreEditPage editPage"));
            Assert.That(shell, Does.Not.Contain("CoreEditorPage coreEditorPage"));
            Assert.That(File.Exists(combinedEditorPath), Is.False,
                "The old combined Create/Edit surface must not remain available for accidental reuse.");

            Assert.That(create, Does.Contain("Title = \"Create knowledge\""));
            Assert.That(create, Does.Not.Contain("Existing item"));
            Assert.That(create, Does.Contain("ResetSurface();"),
                "The contextual creation page must reset drafts and controls between route entries.");
            Assert.That(create, Does.Contain("if (_pendingPlacement is not null || _deriveFromId is not null)"),
                "A preselected type alone must not silently choose the first available home.");
            Assert.That(create, Does.Contain("CreateKnowledgeManageHomes"),
                "Creation must offer a recovery path when no suitable home exists yet.");

            Assert.That(edit, Does.Contain("Title = \"Edit knowledge\""));
            Assert.That(edit, Does.Contain("Creation is a separate flow."));
            Assert.That(edit, Does.Not.Contain("New item"));
        });
    }

    [Test]
    public void LegacyCreateLinks_AreForwardedDeterministically()
    {
        var edit = ReadPresentationFile("CoreEditPage.cs");

        Assert.Multiple(() =>
        {
            Assert.That(edit, Does.Contain("_pendingCreateForward"));
            Assert.That(edit, Does.Contain("await Shell.Current.GoToAsync(\"//create\", forward);"));
            Assert.That(edit, Does.Not.Contain("Dispatcher.Dispatch(async"),
                "Route forwarding must not be fire-and-forget from ApplyQueryAttributes.");
        });
    }

    [Test]
    public void Browse_ContextualActionsPreserveTheirReturnJourney()
    {
        var browse = ReadPresentationFile("ParaBrowserPage.cs");
        var process = ReadPresentationFile("InboxProcessPage.cs");
        var journals = ReadPresentationFile("JournalBrowserPage.cs");

        Assert.Multiple(() =>
        {
            Assert.That(browse, Does.Contain("\"inbox-process\" => \"inbox-process\""));
            Assert.That(browse, Does.Contain("_returnInboxProcessItemId"));
            Assert.That(browse, Does.Contain("[\"contextKind\"] = savedContext.Kind.ToString()"),
                "A newly-created PARA home must be carried back into the focused Create flow.");
            Assert.That(browse, Does.Contain("private async Task OpenWorkspaceCreateAsync(BrainItemKind kind)"),
                "Workspace creation should have a dedicated contextual navigation path.");
            Assert.That(browse, Does.Contain("\"//create\","),
                "Workspace creation buttons should target the Create surface directly without depending on source formatting.");
            Assert.That(browse, Does.Contain("[\"itemKind\"] = target.Kind.ToString()"),
                "Workspace creation must preserve the requested knowledge kind when entering the Create surface.");
            Assert.That(process, Does.Contain("[\"itemId\"] = itemId.Value.Value.ToString()"),
                "Managing homes while processing must preserve the Inbox item identity.");
            Assert.That(journals, Does.Contain("\"//create\""));
            Assert.That(journals, Does.Not.Contain("[\"mode\"] = \"create\""),
                "Journal entry creation should not bounce through the Edit compatibility shim.");
        });
    }

    [Test]
    public void Review_PrimaryNavigationResetsScopeButContextualReturnCanResume()
    {
        var review = ReadPresentationFile("ReviewPage.cs");

        Assert.Multiple(() =>
        {
            Assert.That(review, Does.Contain("_configuredByNavigation"));
            Assert.That(review, Does.Contain("_resumeConfiguredReview"));
            Assert.That(review, Does.Contain("ReviewQueueKind.Para"));
            Assert.That(review, Does.Contain("_resumeConfiguredReview = true;"));
        });
    }

    [Test]
    public void DesktopRail_UsesExplicitReadableInactiveText()
    {
        var shell = ReadPresentationFile("AppShell.cs");
        var visual = ReadPresentationFile("SecondBrainVisual.cs");

        Assert.Multiple(() =>
        {
            Assert.That(visual, Does.Contain("NavigationText = Color.FromArgb(\"#D9E4EE\")"));
            Assert.That(shell, Does.Contain("Shell.SetUnselectedColor(this, SecondBrainVisual.NavigationText)"));
            Assert.That(shell, Does.Contain("Shell.SetUnselectedColor(item, SecondBrainVisual.NavigationText)"));
            Assert.That(shell, Does.Contain("ItemTemplate = DesktopNavigationItemTemplate();"),
                "Windows ignored the Shell unselected-color hint in runtime screenshots; the rail now needs an explicit item template.");
            Assert.That(shell, Does.Contain("TextColor = SecondBrainVisual.NavigationText"));
        });
    }

    [Test]
    public void Android_MainActivityUsesMauiAppCompatTheme()
    {
        var activity = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SecondBrain.Presentation",
            "Platforms",
            "Android",
            "MainActivity.cs"));

        Assert.That(activity, Does.Contain("Theme = \"@style/Maui.SplashTheme\""),
            "MauiAppCompatActivity must launch with MAUI's AppCompat-based theme or Android crashes before first frame.");
    }

    private static string ReadPresentationFile(string fileName) =>
        File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SecondBrain.Presentation",
            fileName));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SecondBrain.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root containing SecondBrain.slnx.");
    }
}
