namespace Movies;

public partial class SelectableOptionsLayout : FlexLayout
{
    public SelectableOptionsLayout()
    {
        InitializeComponent();

        PropertyChanged += SelectionModeChanged;
    }

    private void SelectionModeChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != Selection.SelectionModeProperty.PropertyName)
        {
            return;
        }

        var selectionMode = this.GetSelectionMode();
        string resourceName;
        if (selectionMode == Selection.SelectionMode.Single)
        {
            resourceName = "SingleSelectionLayoutStyle";
        }
        else if (selectionMode == Selection.SelectionMode.Multiple)
        {
            resourceName = "MultiSelectionLayoutStyle";
        }
        else
        {
            return;
        }

        Style = (Style)Resources[resourceName];
    }
}