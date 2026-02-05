namespace Movies.Views;

public partial class ItemListCompactView : SectionView
{
	public static readonly BindableProperty BaseItemHeightProperty = BindableProperty.Create(nameof(BaseItemHeight), typeof(double), typeof(ItemListCompactView), -1d);

	public double BaseItemHeight
	{
		get => (double)GetValue(BaseItemHeightProperty);
		set => SetValue(BaseItemHeightProperty, value);
	}

	public ItemListCompactView()
	{
		InitializeComponent();
	}
}