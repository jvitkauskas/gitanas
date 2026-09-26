using Avalonia.Threading;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests.Views;

[TestFixture]
public sealed class LinuxWindowingTests : HeadlessTest
{
    [TestCase(true, "wayland-1", null, true)]
    [TestCase(true, "wayland-1", "", true)]
    [TestCase(true, "/run/user/1000/wayland-1", "1", true)]
    [TestCase(true, "wayland-1", "1", true)]
    [TestCase(true, "wayland-1", "0", false)]
    [TestCase(true, null, null, false)]
    [TestCase(true, null, "1", false)]
    [TestCase(true, "", "1", false)]
    [TestCase(true, " ", "1", false)]
    [TestCase(false, "wayland-1", "1", false)]
    [TestCase(false, "wayland-1", null, false)]
    public void Backend_selection_prefers_a_Linux_Wayland_session_unless_opted_out(bool isLinux, string? display, string? useWayland, bool expected)
        => AvaloniaUi.ShouldUseWayland(isLinux, display, useWayland).Should().Be(expected);

    [Test]
    [Platform(Exclude = "Win")]
    public Task Windows_without_native_handles_keep_distinct_owners() => OnUiThreadAsync(() =>
    {
        DialogWindow first = new();
        DialogWindow second = new();
        DialogWindow child = new();
        try
        {
            // The headless backend, like Wayland, has no native window handle.
            first.NativeHandle.Should().Be(0);
            AvaloniaDialogHost.Show(first, 0);
            AvaloniaDialogHost.Show(second, 0);
            first.OwnerHandle.Should().NotBe(0);
            first.OwnerHandle.Should().NotBe(second.OwnerHandle);
            AvaloniaDialogHost.Show(child, first.OwnerHandle);

            child.Owner.Should().BeSameAs(first, "the explicitly supplied owner wins over the last opened window");
            AvaloniaDialogHost.FindOpenWindow(first.OwnerHandle).Should().BeSameAs(first);
            first.Close();
            child.IsVisible.Should().BeFalse("owned windows close with their owner");
            AvaloniaDialogHost.FindOpenWindow(first.OwnerHandle).Should().BeNull();
        }
        finally
        {
            child.Close();
            second.Close();
            first.Close();
        }
    });

    [Test]
    [Platform(Exclude = "Win")]
    public Task Owner_lookup_follows_nested_dialogs_without_native_handles() => OnUiThreadAsync(() =>
    {
        DialogWindow owner = new();
        DialogWindow unrelated = new();
        DialogWindow child = new();
        DialogWindow grandchild = new();
        try
        {
            AvaloniaDialogHost.Show(owner, 0);
            AvaloniaDialogHost.Show(child, owner.OwnerHandle);
            AvaloniaDialogHost.Show(grandchild, child.OwnerHandle);
            AvaloniaDialogHost.Show(unrelated, 0);

            AvaloniaDialogHost.GetWindowAndOwnerHandles(grandchild.OwnerHandle)
                .Should().Equal(grandchild.OwnerHandle, child.OwnerHandle, owner.OwnerHandle);
            AvaloniaDialogHost.GetWindowAndOwnerHandles(owner.OwnerHandle).Should().Equal(owner.OwnerHandle);
            AvaloniaDialogHost.GetWindowAndOwnerHandles(0).Should().BeEmpty();
            AvaloniaDialogHost.GetWindowAndOwnerHandles(nint.MaxValue).Should().BeEmpty();

            grandchild.Close();
            AvaloniaDialogHost.GetWindowAndOwnerHandles(grandchild.OwnerHandle).Should().BeEmpty();
        }
        finally
        {
            grandchild.Close();
            child.Close();
            unrelated.Close();
            owner.Close();
        }
    });

    [Test]
    [Platform(Exclude = "Win")]
    public Task A_modal_dialog_without_native_handles_keeps_its_owner() => OnUiThreadAsync(() =>
    {
        DialogWindow owner = new();
        DialogWindow dialog = new();
        bool openedWithOwner = false;
        try
        {
            AvaloniaDialogHost.Show(owner, 0);
            dialog.Opened += (_, _) =>
            {
                openedWithOwner = ReferenceEquals(dialog.Owner, owner);
                Dispatcher.UIThread.Post(dialog.Close);
            };

            AvaloniaDialogHost.ShowDialog(dialog, owner.OwnerHandle);

            openedWithOwner.Should().BeTrue();
            owner.IsVisible.Should().BeTrue();
            AvaloniaDialogHost.FindOpenWindow(dialog.OwnerHandle).Should().BeNull();
        }
        finally
        {
            dialog.Close();
            owner.Close();
        }
    });
}
