namespace Movies.Views;

public partial class CondensedItemList : SectionView
{
	public static readonly BindableProperty ItemWidthProperty = BindableProperty.Create(nameof(ItemWidth), typeof(double), typeof(CondensedItemList), -1.0);

	public double ItemWidth
	{
		get => (double)GetValue(ItemWidthProperty);
		set => SetValue(ItemWidthProperty, value);
	}

	public CondensedItemList()
	{
		InitializeComponent();
	}
}