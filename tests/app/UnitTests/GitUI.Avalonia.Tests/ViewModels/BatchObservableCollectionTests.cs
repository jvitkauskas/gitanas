using System.Collections.Specialized;
using System.ComponentModel;
using GitUI.Presentation;

namespace GitUI.AvaloniaTests.ViewModels;

[TestFixture]
public sealed class BatchObservableCollectionTests
{
    [Test]
    public void Nested_updates_publish_only_the_completed_collection()
    {
        BatchObservableCollection<int> collection = [1];
        List<int[]> snapshots = [];
        List<string?> properties = [];
        collection.CollectionChanged += (_, e) =>
        {
            e.Action.Should().Be(NotifyCollectionChangedAction.Reset);
            snapshots.Add([.. collection]);
        };
        ((INotifyPropertyChanged)collection).PropertyChanged += (_, e) => properties.Add(e.PropertyName);

        using (collection.BeginUpdate())
        {
            collection.Clear();
            using (collection.BeginUpdate())
            {
                collection.Add(2);
                collection.Add(3);
            }

            snapshots.Should().BeEmpty();
            properties.Should().BeEmpty();
        }

        snapshots.Should().ContainSingle().Which.Should().Equal(2, 3);
        properties.Should().Equal("Count", "Item[]");
        using (collection.BeginUpdate())
        {
        }

        snapshots.Should().ContainSingle();
    }

    [Test]
    public void Disposing_a_scope_twice_does_not_suppress_later_notifications()
    {
        BatchObservableCollection<int> collection = [];
        List<NotifyCollectionChangedAction> actions = [];
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);
        IDisposable update = collection.BeginUpdate();
        collection.Add(1);
        update.Dispose();
        update.Dispose();
        collection.Add(2);
        actions.Should().Equal(NotifyCollectionChangedAction.Reset, NotifyCollectionChangedAction.Add);
    }
}
