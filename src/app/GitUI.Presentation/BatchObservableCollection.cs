using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace GitUI.Presentation;

/// <summary>A collection whose notifications can be combined when several items change together.</summary>
public interface IBatchObservableCollection
{
    IDisposable BeginUpdate();
}

/// <summary>Keeps observers from processing intermediate states of a bulk update.</summary>
public sealed class BatchObservableCollection<T> : ObservableCollection<T>, IBatchObservableCollection
{
    private int _updateDepth;
    private bool _changed;

    public IDisposable BeginUpdate()
    {
        ++_updateDepth;
        return new UpdateScope(this);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_updateDepth > 0)
        {
            _changed = true;
            return;
        }

        base.OnCollectionChanged(e);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (_updateDepth == 0)
        {
            base.OnPropertyChanged(e);
        }
    }

    private sealed class UpdateScope(BatchObservableCollection<T> collection) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (--collection._updateDepth == 0 && collection._changed)
            {
                collection._changed = false;
                collection.OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
                collection.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                collection.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
