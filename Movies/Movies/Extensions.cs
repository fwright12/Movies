using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;

namespace Movies.Views
{
    public class FixiOSCollectionViewScrollsToTopPlatformEffect : RoutingEffect
    {
        public FixiOSCollectionViewScrollsToTopPlatformEffect() : base($"Movies.{nameof(FixiOSCollectionViewScrollsToTopPlatformEffect)}") { }
    }

    public class BetterAbsoluteLayout : AbsoluteLayout
    {
        protected override SizeRequest OnSizeRequest(double widthConstraint, double heightConstraint)
        {
            var bestFitSize = new Size();
            var minimum = new Size();
            foreach (View child in Children)
            {
                SizeRequest desiredSize = ComputeBoundingRegionDesiredSize(child);

                bestFitSize.Width = Math.Max(bestFitSize.Width, desiredSize.Request.Width);
                bestFitSize.Height = Math.Max(bestFitSize.Height, desiredSize.Request.Height);
                minimum.Width = Math.Max(minimum.Width, desiredSize.Minimum.Width);
                minimum.Height = Math.Max(minimum.Height, desiredSize.Minimum.Height);
            }

            return new SizeRequest(bestFitSize, minimum);
        }

        static SizeRequest ComputeBoundingRegionDesiredSize(View view)
        {
            AbsoluteLayoutFlags absFlags = GetLayoutFlags(view);

            if (absFlags == AbsoluteLayoutFlags.All)
            {
                return new SizeRequest();
            }

            Rectangle bounds = GetLayoutBounds(view);
            bool widthIsProportional = (absFlags & AbsoluteLayoutFlags.WidthProportional) != 0;
            bool heightIsProportional = (absFlags & AbsoluteLayoutFlags.HeightProportional) != 0;
            bool xIsProportional = (absFlags & AbsoluteLayoutFlags.XProportional) != 0;
            bool yIsProportional = (absFlags & AbsoluteLayoutFlags.YProportional) != 0;

            var width = 0.0;
            var height = 0.0;

            // add in required x values
            if (!xIsProportional)
            {
                width += bounds.X;
            }

            if (!yIsProportional)
            {
                height += bounds.Y;
            }

            var sizeRequest = new Lazy<SizeRequest>(() => view.Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.IncludeMargins));
            double minWidth = width;
            double minHeight = height;

            if (!widthIsProportional)
            {
                if (bounds.Width != AutoSize)
                {
                    // fixed size
                    width += bounds.Width;
                    minWidth += bounds.Width;
                }
                else
                {
                    // auto size
                    width += sizeRequest.Value.Request.Width;
                    minWidth += sizeRequest.Value.Minimum.Width;
                }
            }
            else
            {
                // proportional size
                //width += sizeRequest.Value.Request.Width / Math.Max(0.25, bounds.Width);
                //minWidth += 0;
            }

            if (!heightIsProportional)
            {
                if (bounds.Height != AutoSize)
                {
                    // fixed size
                    height += bounds.Height;
                    minHeight += bounds.Height;
                }
                else
                {
                    // auto size
                    height += sizeRequest.Value.Request.Height;
                    minHeight += sizeRequest.Value.Minimum.Height;
                }
            }
            else
            {
                // proportional size
                //height += sizeRequest.Value.Request.Height / Math.Max(0.25, bounds.Height);
                //minHeight += 0;
            }

            return new SizeRequest(new Size(width, height), new Size(minWidth, minHeight));
        }
    }

    public class UniformStack : Grid
    {
        public static readonly BindableProperty ItemsLayoutProperty = BindableProperty.Create(nameof(ItemsLayout), typeof(IItemsLayout), typeof(UniformStack), LinearItemsLayout.Vertical, propertyChanging: (bindable, oldValue, newValue) => ((UniformStack)bindable).ItemsLayoutChanging((IItemsLayout)oldValue, (IItemsLayout)newValue), propertyChanged: (bindable, oldValue, newValue) => ((UniformStack)bindable).ItemsLayoutChanged((IItemsLayout)oldValue, (IItemsLayout)newValue));

        public IItemsLayout ItemsLayout
        {
            get => (IItemsLayout)GetValue(ItemsLayoutProperty);
            set => SetValue(ItemsLayoutProperty, value);
        }

        public UniformStack()
        {
            ChildrenReordered += ReorderChildren;
        }

        protected override void OnChildMeasureInvalidated() { }

        private void ItemsLayoutChanging(IItemsLayout oldValue, IItemsLayout newValue)
        {
            if (!(oldValue is ItemsLayout oldLayout) || !(newValue is ItemsLayout newLayout) || oldLayout.Orientation != newLayout.Orientation)
            {
                RepositionChildren(Enumerable.Repeat(0, Children.Count));
            }
        }

        private void ItemsLayoutChanged(IItemsLayout oldValue, IItemsLayout newValue)
        {
            if (!(oldValue is ItemsLayout oldLayout) || !(newValue is ItemsLayout newLayout) || oldLayout.Orientation != newLayout.Orientation)
            {
                RepositionChildren();
            }

            InvalidateLayout();
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            if (Children.Count == 0)
            {
                return base.OnMeasure(widthConstraint, heightConstraint);
            }

            var layout = ItemsLayout as ItemsLayout;
            var sr = Children[0].Measure(widthConstraint, heightConstraint);

            if (layout?.Orientation == ItemsLayoutOrientation.Vertical)
            {
                var spacing = RowSpacing;

                if (ItemsLayout is LinearItemsLayout linear)
                {
                    spacing = linear.ItemSpacing;
                }
                else if (ItemsLayout is GridItemsLayout grid)
                {
                    spacing = grid.VerticalItemSpacing;
                }

                sr = new SizeRequest(new Size(widthConstraint, GetSize(sr.Request.Height, spacing)), new Size(widthConstraint, GetSize(sr.Minimum.Height, spacing)));
            }
            else if (layout?.Orientation == ItemsLayoutOrientation.Horizontal)
            {
                var spacing = ColumnSpacing;

                if (ItemsLayout is LinearItemsLayout linear)
                {
                    spacing = linear.ItemSpacing;
                }
                else if (ItemsLayout is GridItemsLayout grid)
                {
                    spacing = grid.HorizontalItemSpacing;
                }

                sr = new SizeRequest(new Size(GetSize(sr.Request.Width, spacing), heightConstraint), new Size(GetSize(sr.Minimum.Width, spacing), heightConstraint));
            }

            return sr;
        }

        private double GetSize(double length, double spacing) => length * Children.Count + spacing * (Children.Count - 1);

        protected override void OnChildAdded(Element child)
        {
            base.OnChildAdded(child);

            if (Children.Count > 0 && Children[0] == child)
            {
                Children[0].MeasureInvalidated += InvalidateMeasure;

                if (Children.Count > 1)
                {
                    Children[1].MeasureInvalidated -= InvalidateMeasure;
                }
            }

            RepositionChildren();
        }

        protected override void OnChildRemoved(Element child, int oldLogicalIndex)
        {
            base.OnChildRemoved(child, oldLogicalIndex);

            if (child is VisualElement ve)
            {
                ve.MeasureInvalidated -= InvalidateMeasure;
            }

            if (oldLogicalIndex == 0 && Children.Count > 0)
            {
                Children[0].MeasureInvalidated += InvalidateMeasure;
            }

            RepositionChildren();
        }

        private void ReorderChildren(object sender, EventArgs e)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                child.MeasureInvalidated -= InvalidateMeasure;
                if (i == 0)
                {
                    child.MeasureInvalidated += InvalidateMeasure;
                }
            }

            RepositionChildren();
        }

        private void InvalidateMeasure(object sender, EventArgs e) => base.OnChildMeasureInvalidated();

        private void RepositionChildren() => RepositionChildren(Enumerable.Range(0, Children.Count));
        private void RepositionChildren(IEnumerable<int> indices)
        {
            BatchBegin();

            var positionProperty = GetPositionProperty(ItemsLayout);

            var itr = indices.GetEnumerator();
            for (int i = 0; i < Children.Count && itr.MoveNext(); i++)
            {
                Children[i].SetValue(positionProperty, itr.Current);
            }

            if ((ItemsLayout as ItemsLayout)?.Orientation == ItemsLayoutOrientation.Horizontal)
            {
                BalanceCollection(ColumnDefinitions, Children.Count);
            }
            else
            {
                BalanceCollection(RowDefinitions, Children.Count);
            }

            BatchCommit();
        }

        private void BalanceCollection<T>(IList<T> list, int count) where T : new()
        {
            while (list.Count != count)
            {
                if (list.Count > count)
                {
                    list.RemoveAt(list.Count - 1);
                }
                else
                {
                    list.Add(new T());
                }
            }
        }

        private DefinitionCollection<T> GetDefinition<T>(DefinitionCollection<T> collection) where T : IDefinition, new()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                collection.Add(new T());
            }

            return collection;
        }

        private BindableProperty GetLayoutProperty(IItemsLayout layout) => (layout as ItemsLayout)?.Orientation == ItemsLayoutOrientation.Horizontal ? Grid.ColumnDefinitionsProperty : Grid.RowDefinitionsProperty;
        private BindableProperty GetPositionProperty(IItemsLayout layout) => (layout as ItemsLayout)?.Orientation == ItemsLayoutOrientation.Horizontal ? Grid.ColumnProperty : Grid.RowProperty;
    }

    public static class Extensions
    {
        public static readonly BindableProperty DisappearingCommandProperty = BindableProperty.Create("DisappearingCommand", typeof(ICommand), typeof(Page), defaultValueCreator: bindable =>
        {
            Page page = (Page)bindable;
            page.Disappearing += (sender, e) =>
            {
                var command = page.GetDisappearingCommand();
                var commandParameter = page.GetDisappearingCommandParameter();

                if (command?.CanExecute(commandParameter) == true)
                {
                    command.Execute(commandParameter);
                }
            };

            return null;
        });
        public static readonly BindableProperty DisappearingCommandParameterProperty = BindableProperty.Create("DisappearingCommandParameter", typeof(object), typeof(Page));

        public static ICommand GetDisappearingCommand(this Page page) => (ICommand)page.GetValue(DisappearingCommandProperty);
        public static void SetDisappearingCommand(this Page page, ICommand value) => page.SetValue(DisappearingCommandProperty, value);

        public static ICommand GetDisappearingCommandParameter(this Page page) => (ICommand)page.GetValue(DisappearingCommandParameterProperty);
        public static void SetDisappearingCommandParameter(this Page page, object value) => page.SetValue(DisappearingCommandParameterProperty, value);

        public static readonly BindableProperty AutoSizeFontProperty = BindableProperty.CreateAttached("AutoSizeFont", typeof(bool), typeof(Label), false);

        public static bool GetAutoSizeFont(this Label bindable) => (bool)bindable.GetValue(AutoSizeFontProperty);
        public static void SetAutoSizeFont(this Label bindable, bool value) => bindable.SetValue(AutoSizeFontProperty, value);

        public static readonly BindableProperty YearProperty = BindableProperty.CreateAttached(nameof(DateTime.Year), typeof(int), typeof(DatePicker), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var picker = (DatePicker)bindable;
            picker.Date = new DateTime((int)newValue, picker.Date.Month, picker.Date.Day);
        });

        public static int GetYear(this DatePicker bindable) => (int)bindable.GetValue(YearProperty);
        public static void SetYear(this DatePicker bindable, int value) => bindable.SetValue(YearProperty, value);

        public static readonly BindableProperty ContentProperty = BindableProperty.CreateAttached("Content", typeof(View), typeof(ScrollView), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var scrollView = (ScrollView)bindable;
            var content = (View)newValue;

            scrollView.Content = content;
        });

        public static View GetContent(this ScrollView bindable) => (View)bindable.GetValue(ContentProperty);
        public static void SetContent(this ScrollView bindable, object value) => bindable.SetValue(ContentProperty, value);

        public static readonly BindableProperty ChildCountProperty = BindableProperty.CreateAttached("ChildCount", typeof(int), typeof(Layout), 0, coerceValue: (bindable, value) =>
        {
            if (bindable is ContentView contentView)
            {
                return contentView.Content == null ? 0 : 1;
            }

            return (bindable as Layout<View>)?.Children.Count ?? value;
        }, defaultValueCreator: bindable =>
        {
            if (bindable is ContentView)
            {
                bindable.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == ContentView.ContentProperty.PropertyName)
                    {
                        ((Layout)sender).SetChildCount(0);
                    }
                };
            }
            else if (bindable is Layout<View> layout && layout.Children is INotifyCollectionChanged observable)
            {
                observable.CollectionChanged += (sender, e) =>
                {
                    layout.SetChildCount(0);
                };
            }

            return 0;
        });

        public static int GetChildCount(this Layout bindable) => (int)bindable.GetValue(ChildCountProperty);
        public static void SetChildCount(this Layout bindable, int value) => bindable.SetValue(ChildCountProperty, value);

        public static readonly BindableProperty StepProperty = BindableProperty.CreateAttached("Step", typeof(double), typeof(Slider), null, defaultValueCreator: bindable =>
        {
            ((Slider)bindable).DragStarted += (sender, e) => ((Slider)sender).ValueChanged += CoerceSliderValue;
            ((Slider)bindable).DragCompleted += (sender, e) => ((Slider)sender).ValueChanged -= CoerceSliderValue;
            return 1.0;
        });

        private static void CoerceSliderValue(object sender, ValueChangedEventArgs e)
        {
            Slider slider = (Slider)sender;
            var step = slider.GetStep();

            slider.Value = Math.Round(e.NewValue / step) * step;
        }

        public static double GetStep(this Slider bindable) => (double)bindable.GetValue(StepProperty);
        public static void SetStep(this Slider bindable, double value) => bindable.SetValue(StepProperty, value);

        public static readonly BindableProperty SelectedItemChangedCommandProperty = BindableProperty.CreateAttached("SelectedItemChangedCommand", typeof(ICommand), typeof(Picker), null, propertyChanged: (bindable, oldValue, newValue) =>
        {

        }, defaultValueCreator: bindable =>
        {
            ((Picker)bindable).SelectedIndexChanged += (sender, e) =>
            {
                var picker = (Picker)sender;
                var command = picker.GetSelectedItemChangedCommand();
                var parameter = picker.GetSelectedItemChangedCommandParameter();

                if (command != null && command.CanExecute(parameter))
                {
                    command.Execute(parameter);
                }
            };

            return null;
        });
        public static readonly BindableProperty SelectedItemChangedCommandParameterProperty = BindableProperty.CreateAttached("SelectedItemChangedCommandParameter", typeof(object), typeof(Picker), null);

        public static ICommand GetSelectedItemChangedCommand(this Picker picker) => (ICommand)picker.GetValue(SelectedItemChangedCommandProperty);
        public static object GetSelectedItemChangedCommandParameter(this Picker picker) => picker.GetValue(SelectedItemChangedCommandParameterProperty);
        public static void SetSelectedItemChangedCommand(this Picker picker, ICommand value) => picker.SetValue(SelectedItemChangedCommandProperty, value);
        public static void SetSelectedItemChangedCommandParameter(this Picker picker, ICommand value) => picker.SetValue(SelectedItemChangedCommandParameterProperty, value);

        private class Proxy : BindableObject { }

        public static readonly BindableProperty BatchProperty = BindableProperty.CreateAttached("Batch", typeof(object), typeof(BindableObject), null, propertyChanging: (bindable, oldValue, newValue) =>
        {
            Print.Log("batch changed", bindable.GetHashCode(), oldValue, newValue);
            if (newValue == null)
            {
                return;
            }

            bindable.BindingContextChanged -= EndBatch;
            bindable.BindingContextChanged += EndBatch;

            //Print.Log("batch property changed", bindable, newValue);
            Print.Log("\tbatch begin", bindable.GetType());//, (bindable as VisualElement)?.Style?.Behaviors.Count);
            DataService.Instance.BatchBegin();

            //bindable.BindingContextChanged -= EndBatch;
            //bindable.BindingContextChanged += EndBatch;
        });

        public static void EndBatch(object sender, EventArgs e)
        {
            if (((BindableObject)sender).BindingContext == null)
            {
                return;
            }
            Print.Log("\tbatch will end", sender.GetHashCode(), ((BindableObject)sender)?.BindingContext?.GetType(), ((BindableObject)sender)?.BindingContext);
            DataService.Instance.BatchEnd();
        }

        public static object GetBatch(this BindableObject bindable) => bindable.GetValue(BatchProperty);
        public static void SetBatch(this BindableObject bindable, object value) => bindable.SetValue(BatchProperty, value);

        public static readonly BindableProperty AspectRequestProperty = BindableProperty.CreateAttached("AspectRequest", typeof(double), typeof(VisualElement), null, defaultValueCreator: bindable =>
        {
            ((VisualElement)bindable).SizeChanged += AdjustAspect;
            return null;
        });

        private static void AdjustAspect(object sender, EventArgs e)
        {
            var visualElement = (VisualElement)sender;

            double aspect = visualElement.Width / visualElement.Height;
            double request = GetAspectRequest(visualElement);

#if DEBUG
            if (sender is ImageView image && image.AltText?.ToLower() == "m. m.")
            //if (request == 5)
            {
                Print.Log(visualElement.IsSet(VisualElement.WidthRequestProperty), visualElement.IsSet(VisualElement.HeightRequestProperty));
                ;
            }
#endif

            if (request > aspect && !visualElement.IsSet(VisualElement.WidthRequestProperty))
            {
                visualElement.WidthRequest = request * visualElement.Height;
            }
            else if (request < aspect && !visualElement.IsSet(VisualElement.HeightRequestProperty))
            {
                visualElement.HeightRequest = visualElement.Width / request;
            }
            else if (request < aspect)
            {
                visualElement.WidthRequest = request * visualElement.Height;
            }
            else if (request > aspect)
            {
                visualElement.HeightRequest = visualElement.Width / request;
            }
            return;

            Size size = new Size();

            if (!visualElement.IsSet(VisualElement.WidthRequestProperty) || !visualElement.IsSet(VisualElement.HeightRequestProperty))
            {
                visualElement.WidthRequest = visualElement.Width;
                visualElement.HeightRequest = visualElement.Width / request;
            }

            //visualElement.WidthRequest = visualElement.IsSet(VisualElement.WidthRequestProperty) ? (aspect < request ? visualElement.Width : request * visualElement.Height) : Math.Max(visualElement.Width, request * visualElement.Height);
            //visualElement.HeightRequest = visualElement.IsSet(VisualElement.HeightRequestProperty) ? (aspect < request ? visualElement.Width / request : visualElement.Height) : Math.Max(visualElement.Height, visualElement.Width / request);

            /*if (!visualElement.IsSet(VisualElement.WidthRequestProperty) || !visualElement.IsSet(VisualElement.HeightRequestProperty))
            {
                visualElement.WidthRequest = request * visualElement.Height;
                visualElement.HeightRequest = visualElement.Width / request;
            }
            else*/
            else if (aspect < request)// && visualElement.Width < visualElement.Height))
            {
                visualElement.WidthRequest = visualElement.Width;
                visualElement.HeightRequest = visualElement.Width / request;
            }
            else if (aspect > request)// && visualElement.Height < visualElement.Width))
            {
                visualElement.WidthRequest = request * visualElement.Height;
                visualElement.HeightRequest = visualElement.Height;
            }
        }

        public static double GetAspectRequest(this VisualElement visualElement) => (double)visualElement.GetValue(AspectRequestProperty);
        public static void SetAspectRequest(this VisualElement visualElement, double value) => visualElement.SetValue(AspectRequestProperty, value);

        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.CreateAttached("ItemsSource", typeof(object), typeof(ItemsView), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            ItemsView items = (ItemsView)bindable;

            if (newValue is IAsyncEnumerable<object> asyncEnumerable)
            {
                ICollection<object> itemsSource = asyncEnumerable is ICollection<object> asyncCollection && asyncEnumerable is INotifyCollectionChanged ? asyncCollection : new ObservableCollection<object>();
                IAsyncEnumerator<object> itr = asyncEnumerable.GetAsyncEnumerator();
                bool loading = false;

                var command = new Command<int?>(async count =>
                {
                    if (loading)
                    {
                        return;
                    }
                    loading = true;

                    try
                    {
                        for (int i = 0; i < (count ?? 1) && (await itr.MoveNextAsync()); i++)
                        {
                            itemsSource.Add(itr.Current);
                        }
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Print.Log(e);
#endif
                    }

                    loading = false;
                });

                command.Execute(10);

                items.RemainingItemsThresholdReachedCommand = command;
                items.ItemsSource = itemsSource;
            }
            else if (newValue is IEnumerable enumerable)
            {
                items.ItemsSource = enumerable;
            }
            else
            {
                items.ItemsSource = new List<object> { newValue };
            }
        });

        public static object GetItemsSource(this ItemsView bindable) => bindable.GetValue(ItemsSourceProperty);
        public static void SetItemsSource(this ItemsView bindable, object value) => bindable.SetValue(ItemsSourceProperty, value);

        //public static readonly BindableProperty ValueIfAvailableProperty = BindableProperty.CreateAttached("ValueIfAvailable", typeof(Binding), typeof(VisualElement), null);

        public static readonly BindableProperty PaddingProperty = BindableProperty.CreateAttached("Padding", typeof(Thickness), typeof(ItemsView), default(Thickness));

        public static Thickness GetPadding(this ItemsView itemsView) => (Thickness)itemsView.GetValue(PaddingProperty);
        public static void SetPadding(this ItemsView itemsView, Thickness value) => itemsView.SetValue(PaddingProperty, value);

        private class DecoratorDataTemplate : DataTemplate
        {
            public DecoratorDataTemplate(DataTemplate baseTemplate, Action<object> decorator) : base(() =>
            {
                var content = baseTemplate.CreateContent();
                decorator(content);
                return content;
            })
            { }
        }
    }

    public static class CollectionViewExt
    {
        public static readonly ICommand SelectAllCommand = new Command<CollectionView>(SelectAll);

        public static readonly ICommand DeselectAllCommand = new Command<CollectionView>(DeselectAll);

        public static readonly ICommand DeleteSelectedCommand = new Command<CollectionView>(DeleteSelected);

        public static void SelectAll(this CollectionView collectionView)
        {
            collectionView.SelectedItems = new List<object>(collectionView.ItemsSource.OfType<object>());
            collectionView.SetIsAllSelected(true);

            if (collectionView.ItemsSource is INotifyCollectionChanged observable)
            {
                var handler = (NotifyCollectionChangedEventHandler)collectionView.GetValue(AllSelectedHandlerProperty);

                // Make sure we don't add it multiple times
                observable.CollectionChanged -= handler;
                observable.CollectionChanged += handler;
            }
        }

        public static void DeselectAll(this CollectionView collectionView)
        {
            collectionView.SelectedItems = null;

            if (collectionView.ItemsSource is INotifyCollectionChanged observable)
            {
                var handler = (NotifyCollectionChangedEventHandler)collectionView.GetValue(AllSelectedHandlerProperty);

                observable.CollectionChanged -= handler;
            }
        }

        public static void DeleteSelected(this CollectionView collectionView)
        {
            if (collectionView.SelectionMode == SelectionMode.Single)
            {
                if (collectionView.SelectedItem != null && collectionView.ItemsSource is IList list)
                {
                    list.Remove(collectionView.SelectedItem);
                }

                collectionView.SelectedItem = null;
            }
            else if (collectionView.SelectionMode == SelectionMode.Multiple)
            {
                if (collectionView.SelectedItems != null && collectionView.ItemsSource is IList list)
                {
                    foreach (var item in collectionView.SelectedItems)
                    {
                        list.Remove(item);
                    }
                }

                collectionView.SelectedItems = null;
            }
        }

        private static readonly BindableProperty AllSelectedHandlerProperty = BindableProperty.CreateAttached("AllSelectedHandler", typeof(NotifyCollectionChangedEventHandler), typeof(CollectionView), null, defaultValueCreator: bindable =>
        {
            var collectionView = (CollectionView)bindable;

            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.NewItems != null)
                {
                    if (collectionView.SelectedItems == null)
                    {
                        //collectionView.SelectedItems = new List<object>();
                    }

                    foreach (var item in e.NewItems)
                    {
                        collectionView.SelectedItems?.Add(item);
                    }
                }
            };

            return (NotifyCollectionChangedEventHandler)collectionChanged;
        });

        public static readonly BindableProperty IsAllSelectedProperty = BindableProperty.CreateAttached("IsAllSelected", typeof(bool), typeof(CollectionView), false, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var collectionView = (CollectionView)bindable;

            if (Equals(newValue, true))
            {
                //collectionView.SelectAll();
            }
        }, defaultValueCreator: bindable =>
        {
            ((CollectionView)bindable).SelectionChanged += (sender, e) =>
            {
                var collectionView = (CollectionView)sender;

                if (e.CurrentSelection.Count < e.PreviousSelection.Count)
                {
                    collectionView.SetIsAllSelected(false);
                }
            };

            return false;
        });

        private static bool IsAllSelected(this CollectionView collectionView) => collectionView.SelectedItems != null && collectionView.ItemsSource is IList items && collectionView.SelectedItems.Count == items.Count;

        public static bool GetIsAllSelected(this CollectionView collectionView) => (bool)collectionView.GetValue(IsAllSelectedProperty);
        public static void SetIsAllSelected(this CollectionView collectionView, bool value) => collectionView.SetValue(IsAllSelectedProperty, value);

        public static readonly ICommand ToggleIsEditingCommand = new Command<CollectionView>(collection => collection.SetIsEditing(!collection.GetIsEditing()));

        public static readonly ICommand ToggleSelectionModeCommand = new Command<CollectionView>(collection => collection.SelectionMode = collection.SelectionMode == SelectionMode.None ? SelectionMode.Multiple : SelectionMode.None);

        public static readonly BindableProperty IsEditingProperty = BindableProperty.CreateAttached("IsEditing", typeof(bool), typeof(CollectionView), false, propertyChanged: (bindable, oldValue, newValue) =>
        {
            CollectionView collection = (CollectionView)bindable;
            var template = collection.ItemTemplate;
        }, defaultValueCreator: bindable =>
        {
            SetupEditCollectionView((CollectionView)bindable);
            return false;
        });

        private static void SetupEditCollectionView(CollectionView collectionView)
        {
            collectionView.PropertyChanged += ItemTemplateChanged;
            ItemTemplateChanged(collectionView, new PropertyChangedEventArgs(ItemsView.ItemTemplateProperty.PropertyName));
        }

        private static void ItemTemplateChanged(object sender, PropertyChangedEventArgs e)
        {
            CollectionView collection = (CollectionView)sender;

            if (collection.IsSet(ItemsView.ItemTemplateProperty) && collection.ItemTemplate is EditDataTemplateSelector editTemplate)
            {
                editTemplate.UpdateCollectionView(collection);
            }
        }

        public static bool GetIsEditing(this CollectionView collectionView) => (bool)collectionView.GetValue(IsEditingProperty);
        public static void SetIsEditing(this CollectionView collectionView, bool value) => collectionView.SetValue(IsEditingProperty, value);
    }

    public class AutoSizeBehavior : Behavior<View>
    {
        protected override void OnAttachedTo(View bindable)
        {
            base.OnAttachedTo(bindable);

            bindable.WidthRequest = bindable.HeightRequest = 0;
            AddHandlers(bindable);
            //bindable.BatchCommitted += (sender, e) => Print.Log("batch committed");
            //bindable.SizeChanged += (sender, e) => Print.Log("size changed", ((View)sender).Width, ((View)sender).Height, ((View)sender).WidthRequest, ((View)sender).HeightRequest, sender.GetHashCode());
            //bindable.MeasureInvalidated += (sender, e) => Print.Log("measure invalidated");
        }

        protected override void OnDetachingFrom(View bindable)
        {
            base.OnDetachingFrom(bindable);
            RemoveHandlers(bindable);
        }

        private void AddHandlers(View view)
        {
            view.SizeChanged += Update;
            //view.PropertyChanged += WidthChanged;
            //view.PropertyChanged += HeightChanged;
            view.MeasureInvalidated += Update;
        }

        private void WidthChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.WidthProperty.PropertyName && sender is VisualElement visualElement && (visualElement.Width != visualElement.WidthRequest || visualElement.WidthRequest == 0))
            {
                Print.Log("width changed", visualElement.Width);
                Update(sender, EventArgs.Empty);
            }
        }

        private void HeightChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.HeightProperty.PropertyName && sender is VisualElement visualElement && (visualElement.Height != visualElement.HeightRequest || visualElement.HeightRequest == 0))
            {
                Print.Log("height changed", visualElement.Height);
                Update(sender, EventArgs.Empty);
            }
        }

        private void RemoveHandlers(View view)
        {
            view.MeasureInvalidated -= Update;
            //view.PropertyChanged -= HeightChanged;
            //view.PropertyChanged -= WidthChanged;
            view.SizeChanged -= Update;
        }

        private void Update(object sender, EventArgs e)
        {
            var view = (View)sender;
            if (!view.IsSet(VisualElement.HeightProperty) || !view.IsSet(VisualElement.WidthProperty))
            {
                return;
            }

            //Print.Log("size changed", view.Bounds.Size);

            bool autoWidth = !view.IsSet(VisualElement.WidthRequestProperty) || view.Width == view.WidthRequest;
            bool autoHeight = !view.IsSet(VisualElement.HeightRequestProperty) || view.Height == view.HeightRequest;
            Size size = default;

            RemoveHandlers(view);
            view.BatchBegin();
            //view.IsVisible = false;

            if (autoWidth || autoHeight)
            {
                double widthConstraint = autoWidth ? double.PositiveInfinity : view.Width;
                double heightConstraint = autoHeight ? double.PositiveInfinity : view.Height;

                view.ClearValue(VisualElement.WidthRequestProperty);
                view.ClearValue(VisualElement.HeightRequestProperty);
                size = view.Measure(widthConstraint, heightConstraint).Request;

                //Print.Log(view.WidthRequest, view.HeightRequest, autoWidth, autoHeight, widthConstraint, heightConstraint, size);
            }

            view.WidthRequest = autoWidth ? size.Width : 0;
            view.HeightRequest = autoHeight ? size.Height : 0;

            EventHandler<Xamarin.Forms.Internals.EventArg<VisualElement>> handler = null;
            handler = (sender, e) =>
            {
                var view = (View)sender;

                if (!view.Batched)
                {
                    view.BatchCommitted -= handler;
                    AddHandlers(view);
                }
            };

            //view.IsVisible = true;
            handler(view, null);
            view.BatchCommitted += handler;
            view.BatchCommit();
        }
    }

    public class PreloadItemsBehavior : Behavior<CollectionView>
    {
        protected override void OnAttachedTo(CollectionView bindable)
        {
            base.OnAttachedTo(bindable);
            bindable.PropertyChanged += ThresholdCommandChanged;
            ThresholdCommandChanged(bindable, new PropertyChangedEventArgs(ItemsView.RemainingItemsThresholdReachedCommandProperty.PropertyName));
        }

        protected override void OnDetachingFrom(CollectionView bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.PropertyChanged -= ThresholdCommandChanged;
        }

        private void ThresholdCommandChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != ItemsView.RemainingItemsThresholdReachedCommandProperty.PropertyName)
            {
                return;
            }

            var collectionView = (CollectionView)sender;
            collectionView.RemainingItemsThresholdReachedCommand?.Execute(collectionView.RemainingItemsThresholdReachedCommandParameter);
        }
    }

    public class DecoratorDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Main { get; set; }
        public DataTemplate Decorator { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var main = (Main as DataTemplateSelector)?.SelectTemplate(item, container) ?? Main;
            var decorator = ((Decorator as DataTemplateSelector)?.SelectTemplate(item, container) ?? Decorator)?.CreateContent() as ContentView;

            if (decorator != null && main?.CreateContent() is View view)
            {
                decorator.Content = view;

                return new DataTemplate(() => decorator);
            }
            else
            {
                return main;
            }
        }
    }

    public class ListEditView : ContentView
    {
        public static readonly BindableProperty SelectedProperty = BindableProperty.Create(nameof(Selected), typeof(bool), typeof(ListEditView), false);

        public bool Selected
        {
            get => (bool)GetValue(SelectedProperty);
            set => SetValue(SelectedProperty, value);
        }

        public readonly ICommand ToggleSelectedCommand;

        public ListEditView()
        {
            ToggleSelectedCommand = new Command(() => Selected = !Selected);
        }
    }

    public class SelectionModeToBoolConverter : IValueConverter
    {
        public static readonly SelectionModeToBoolConverter Instance = new SelectionModeToBoolConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is SelectionMode mode && mode != SelectionMode.None;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToSelectionModeConverter : IValueConverter
    {
        public static readonly BoolToSelectionModeConverter Instance = new BoolToSelectionModeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Equals(value, true) ? (Equals(parameter, true) ? SelectionMode.Single : SelectionMode.Multiple) : SelectionMode.None;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DateTime Date
        {
            get => _Date;
            set
            {
                if (value != _Date)
                {
                    _Date = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Year
        {
            get => _Year;
            set
            {
                if (value != _Year)
                {
                    _Year = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Month
        {
            get => _Month;
            set
            {
                if (value != _Month)
                {
                    _Month = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Day
        {
            get => _Day;
            set
            {
                if (value != _Day)
                {
                    _Day = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DaysInMonth
        {
            get => _DaysInMonth;
            private set
            {
                if (value != _DaysInMonth)
                {
                    _DaysInMonth = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _Date;
        private int _Year;
        private int _Month;
        private int _Day;
        private int _DaysInMonth;

        public DateTimeViewModel()
        {
            UpdateComponents();

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Date))
                {
                    UpdateComponents();

                    OnPropertyChanged(nameof(Year));
                    OnPropertyChanged(nameof(Month));
                    OnPropertyChanged(nameof(Day));
                }
                else if (e.PropertyName == nameof(Year) || e.PropertyName == nameof(Month) || e.PropertyName == nameof(Day))
                {
                    if (e.PropertyName != nameof(Day))
                    {
                        DaysInMonth = DateTime.DaysInMonth(Year, Month);
                    }

                    UpdateDate();
                }
            };
        }

        private void UpdateDate()
        {
            Date = new DateTime(Year, Month, Day);
        }

        private void UpdateComponents()
        {
            _Year = Date.Year;
            _Month = Date.Month;
            _Day = Date.Day;
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    public class EditDataTemplateSelector : DecoratorDataTemplateSelector
    {
        public CollectionView CollectionView { get; private set; }
        public ControlTemplate SwipeTemplate
        {
            get => SafeSwipeTemplate?.Value;
            set => SafeSwipeTemplate = new Lazy<ControlTemplate>(() =>
            {
                var swipe = value?.CreateContent() as SwipeView;

                if (swipe.Content is ContentPresenter)
                {
                    return value;
                }
                else
                {
                    return new ControlTemplate(() =>
                    {
                        var swipe = value?.CreateContent() as SwipeView;
                        swipe.Content = new ContentPresenter();
                        return swipe;
                    });
                }
            });
        }
        public ControlTemplate EditTemplate { get; set; }

        private List<Element> Children = new List<Element>();
        private Lazy<ControlTemplate> SafeSwipeTemplate;

        public EditDataTemplateSelector()
        {
            Decorator = new DataTemplate(() =>
            {
                var content = new ListEditView();
                UpdateTemplates(content);
                return content;
            });
        }

        public void UpdateCollectionView(CollectionView collectionView)
        {
            CollectionView = collectionView;

            CollectionView.PropertyChanged += ToggleEditMode;
            CollectionView.ChildAdded += (sender, e) =>
            {
                Children.Add(e.Element);
            };
            CollectionView.ChildRemoved += (sender, e) =>
            {
                Children.Remove(e.Element);
            };
        }

        private void ToggleEditMode(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != SelectableItemsView.SelectionModeProperty.PropertyName)
            {
                return;
            }
            
            UpdateTemplates();
        }

        public void UpdateTemplates() => UpdateTemplates(Children.OfType<TemplatedView>());
        private void UpdateTemplates(params TemplatedView[] items) => UpdateTemplates((IEnumerable<TemplatedView>)items);
        private void UpdateTemplates(IEnumerable<TemplatedView> items)
        {
            var editing = CollectionView?.SelectionMode != SelectionMode.None;
            ControlTemplate template = editing ? EditTemplate : SwipeTemplate;

            foreach (var item in items)
            {
                item.ControlTemplate = template;
            }
        }
    }

    public class ToViewConverter : IValueConverter
    {
        public static readonly ToViewConverter Instance = new ToViewConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is View view)
            {
                return view;
            }
            else if (value is DataTemplate template && template.CreateContent() is View content)
            {
                return content;
            }
            else
            {
                throw new Exception("Cound not convert " + value.GetType() + " to a view");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ContentProperty(nameof(PageTemplate))]
    public class PushPageCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public ElementTemplate PageTemplate { get; set; }
        public bool Modal { get; set; }

        public virtual bool CanExecute(object parameter) => true;

        public virtual async void Execute(object parameter)
        {
            object content = PageTemplate.CreateContent();
            Page page = content as Page ?? new ContentPage { Content = (View)content };

            if (parameter != null)
            {
                page.BindingContext = parameter;
            }

            if (Modal)
            {
                await Application.Current.MainPage.Navigation.PushModalAsync(page);
            }
            else
            {
                await Application.Current.MainPage.Navigation.PushAsync(page);
            }
        }
    }

    public class PopPageCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool Modal { get; set; }

        public bool CanExecute(object parameter) => true;

        public async void Execute(object parameter)
        {
            if (Modal)
            {
                await Application.Current.MainPage.Navigation.PopModalAsync();
            }
            else
            {
                await Application.Current.MainPage.Navigation.PopAsync();
            }
        }
    }

    public class PushPageEventArgs : EventArgs
    {
        public bool Modal { get; set; }
        public bool IsAnimated { get; set; }
        public object BindingContext { get; set; }
    }

    public class HideIfNoVisibleChildrenBehavior : Behavior<Layout<View>>
    {
        public static readonly Type ViewLayout = typeof(Layout<View>);

        protected override void OnAttachedTo(Layout<View> bindable)
        {
            base.OnAttachedTo(bindable);

            foreach (var child in bindable.Children)
            {
                ChildAdded(bindable, new ElementEventArgs(child));
            }
            bindable.ChildAdded += ChildAdded;
            bindable.ChildRemoved += ChildRemoved;

            UpdateIsVisible(bindable, new EventArgs());
        }

        private static void ChildAdded(object sender, ElementEventArgs e)
        {
            e.Element.PropertyChanged += IsVisibleChanged;
            IsVisibleChanged(e.Element, new PropertyChangedEventArgs(VisualElement.IsVisibleProperty.PropertyName));
        }

        private static void ChildRemoved(object sender, ElementEventArgs e)
        {
            e.Element.PropertyChanged -= IsVisibleChanged;
            if (e.Element.Parent is Layout<View> layout)
            {
                UpdateIsVisible(layout, new EventArgs());
            }
        }

        private static void IsVisibleChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.IsVisibleProperty.PropertyName && sender is View view && view.Parent is Layout<View> layout)
            {
                if (view.IsVisible)
                {
                    layout.IsVisible = true;
                }
                else
                {
                    UpdateIsVisible(layout, new EventArgs());
                }
            }
        }

        private static void UpdateIsVisible(object sender, EventArgs e)
        {
            var layout = (Layout<View>)sender;

            foreach (View view in layout.Children)
            {
                if (view.IsVisible)
                {
                    layout.IsVisible = true;
                    return;
                }
            }

            layout.IsVisible = false;
        }
    }

    public class ChildViewModel : BindableObject
    {
        public static readonly BindableProperty LayoutProperty = BindableProperty.Create(nameof(Layout), typeof(Layout<View>), typeof(ChildViewModel), propertyChanged: (bindable, oldValue, newValue) =>
        {
            ChildViewModel model = (ChildViewModel)bindable;

            if (oldValue is Layout<View> oldLayout)
            {
                oldLayout.ChildAdded -= model.ChildrenChanged;
                oldLayout.ChildRemoved -= model.ChildrenChanged;
                oldLayout.ChildrenReordered -= model.ChildrenChanged;
            }

            if (newValue is Layout<View> layout)
            {
                model.ChildrenChanged(layout, new EventArgs());

                layout.ChildAdded += model.ChildrenChanged;
                layout.ChildRemoved += model.ChildrenChanged;
                layout.ChildrenReordered += model.ChildrenChanged;
            }
        });

        public Layout<View> Layout
        {
            get => (Layout<View>)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        public View Child
        {
            get => _Child;
            private set
            {
                if (value != _Child)
                {
                    _Child = value;
                    OnPropertyChanged(nameof(Child));
                }
            }
        }

        public int Index { get; set; }

        private View _Child;

        private void ChildrenChanged(object sender, EventArgs e) => Child = Index < Layout.Children.Count ? Layout.Children[Index] : null;

        public override string ToString() => "Children[" + Index + "]";
    }

    public class MaxContentView : ContentView
    {
        public static readonly BindableProperty MaxWidthProperty = BindableProperty.Create(nameof(MaxWidth), typeof(double), typeof(MaxContentView));

        public static readonly BindableProperty MaxHeightProperty = BindableProperty.Create(nameof(MaxHeight), typeof(double), typeof(MaxContentView));

        public double MaxWidth
        {
            get => (double)GetValue(MaxWidthProperty);
            set => SetValue(MaxWidthProperty, value);
        }

        public double MaxHeight
        {
            get => (double)GetValue(MaxHeightProperty);
            set => SetValue(MaxHeightProperty, value);
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint) => base.OnMeasure(Math.Min(MaxWidth, widthConstraint), Math.Min(MaxHeight, heightConstraint));
    }
}