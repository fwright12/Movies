using MauiExtensions.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Layouts;
using Movies.Models;
using Movies.ViewModels;
using System.Globalization;

namespace Movies
{
    [BindingValueConverter]
    public class ItemViewModelConverter : IValueConverter<object, ItemViewModel>
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Movie movie) return new MovieViewModel(movie);
            else if (value is TVShow show) return new TVShowViewModel(show);
            else if (value is TVSeason season) return new TVSeasonViewModel(season);
            else if (value is TVEpisode episode) return new TVEpisodeViewModel(episode);
            else if (value is Collection collection) return new CollectionViewModel(collection);
            else if (value is Person person) return new PersonViewModel(person);

            return value;
        }

        public object? ConvertBack(ItemViewModel? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [BindingValueConverter]
    public class IsNullOrEmptyConverter : IValueConverter<string, bool>
    {
        public object? Convert(string? value, Type targetType, object? parameter, CultureInfo culture) => string.IsNullOrEmpty(value);

        public object? ConvertBack(bool value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [BindingValueConverter]
    public class QuickActionsAvailableConverter : IValueConverter<ItemViewModel, bool>
    {
        private static ISet<Type> Types = new HashSet<Type>
        {
            typeof(TVSeasonViewModel),
            typeof(TVEpisodeViewModel),
            typeof(ListViewModel),
            typeof(NamedListViewModel)
        };

        public object? Convert(ItemViewModel? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            return value.Item != null && !Types.Contains(value.GetType());
        }

        public object? ConvertBack(bool value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseAdmobAds(App.AdKeywords)
                .ConfigureMauiHandlers(handlers =>
                {
                    //c.AddHandler<CollectionView, MauiExtensions.CollectionViewHandler>();
                    handlers.AddHandler<AdView, AdViewHandler>();
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Ionicons.ttf", "Ionicons");
                })
                .RegisterConverters();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.Add(new ServiceDescriptor(typeof(ILayoutManagerFactory), new CustomLayoutManagerFactory()));

            return builder.Build();
        }
    }

    public class CustomLayoutManagerFactory : ILayoutManagerFactory
    {
        public ILayoutManager CreateLayoutManager(Layout layout)
        {
            if (layout is HorizontalStackLayout hsl)
            {
                return new HorizontalStackLayoutManager(hsl);
            }
            if (layout is VerticalStackLayout vsl)
            {
                return new VerticalStackLayoutManager(vsl);
            }
            return null!;
        }
    }

    public abstract class StackLayoutManager : Microsoft.Maui.Layouts.StackLayoutManager
    {
        protected abstract double MainAxisDimension { get; }
        protected abstract double MainAxisMinimumDimension { get; }
        protected abstract double MainAxisMaximumDimension { get; }

        protected abstract double CrossAxisDimension { get; }
        protected abstract double CrossAxisMinimumDimension { get; }
        protected abstract double CrossAxisMaximumDimension { get; }

        protected abstract BindableProperty CrossAxisLayoutOptions { get; }

        protected StackLayoutManager(IStackLayout stack) : base(stack) { }

        protected Size OrientationAgnosticMeasure(double mainAxisConstraint, double crossAxisConstraint)
        {
            var padding = Stack.Padding;

            double mainAxisSize = 0;
            double crossAxisSize = 0;

            for (int n = 0; n < Stack.Count; n++)
            {
                var child = Stack[n];

                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                var measure = MeasureChild(child, double.PositiveInfinity, crossAxisConstraint - GetCrossAxisThickness(padding));

                mainAxisSize += GetMainAxisDimension(measure);
                crossAxisSize = Math.Max(crossAxisSize, GetCrossAxisDimension(measure));
            }

            mainAxisSize += MeasureDecorativeSpace();
            crossAxisSize += GetCrossAxisThickness(padding);

            var extraSpace = double.IsInfinity(mainAxisConstraint) ? 0 : mainAxisConstraint - mainAxisSize;
            var flexTotal = CalculateFlexTotal(extraSpace);

            double finalMainAxisSize;
            if (flexTotal > 0)
            {
                finalMainAxisSize = mainAxisConstraint;

                foreach (var child in Stack)
                {
                    var flex = CalculateFlexAmount(child, extraSpace, flexTotal);
                    if (flex != 0)
                    {
                        var measure = MeasureChild(child, GetMainAxisDimension(child.DesiredSize) + flex, crossAxisConstraint - GetCrossAxisThickness(padding));
                        crossAxisSize = Math.Max(crossAxisSize, GetCrossAxisDimension(measure));
                    }
                }
            }
            else
            {
                finalMainAxisSize = ResolveConstraints(mainAxisConstraint, MainAxisDimension, mainAxisSize, MainAxisMinimumDimension, MainAxisMaximumDimension);
            }
            var finalCrossAxisSize = ResolveConstraints(crossAxisConstraint, CrossAxisDimension, crossAxisSize, CrossAxisMinimumDimension, CrossAxisMaximumDimension);

            return CreateSize(finalMainAxisSize, finalCrossAxisSize);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var padding = Stack.Padding;

            double position = GetMainAxisStart(padding) + GetMainAxisValue(bounds.Location);
            double inset = GetCrossAxisStart(padding) + GetCrossAxisValue(bounds.Location);
            double crossAxisSize = GetCrossAxisDimension(bounds.Size) - GetCrossAxisThickness(padding);

            //var extraSpace = GetMainAxisDimension(bounds.Size) - GetMainAxisDimension(Stack.DesiredSize);
            var extraSpace = GetMainAxisDimension(bounds.Size);
            foreach (var child in Stack)
            {
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                extraSpace -= GetMainAxisDimension(child.DesiredSize);
            }
            extraSpace -= MeasureDecorativeSpace();

            var flexTotal = CalculateFlexTotal(extraSpace);

            for (int n = 0; n < Stack.Count; n++)
            {
                var child = Stack[n];

                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                var size = GetMainAxisDimension(child.DesiredSize) + CalculateFlexAmount(child, extraSpace, flexTotal);
                var destination = new Rect(CreatePoint(position, inset), CreateSize(size, crossAxisSize));
                child.Arrange(destination);

                position += Math.Min(GetMainAxisDimension(destination.Size), GetMainAxisDimension(destination.Size)) + Stack.Spacing;
            }

            var result = CreateSize(position, crossAxisSize);
            return result.AdjustForFill(bounds, Stack);
        }

        private double MeasureDecorativeSpace()
        {
            var spacingCount = 0;

            for (int n = 0; n < Stack.Count; n++)
            {
                var child = Stack[n];
                if (child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                spacingCount += 1;
            }

            return MeasureSpacing(Stack.Spacing, spacingCount) + GetMainAxisThickness(Stack.Padding);
        }

        private float CalculateFlexTotal(double extraSpace)
        {
            if (extraSpace == 0)
            {
                return 0;
            }

            var canFlex = false;
            var total = 0f;

            foreach (var child in Stack)
            {
                if (child is BindableObject bindable)
                {
                    total += extraSpace > 0 ? FlexLayout.GetGrow(bindable) : FlexLayout.GetShrink(bindable);

                    if (bindable.IsSet(FlexLayout.GrowProperty) || bindable.IsSet(FlexLayout.ShrinkProperty))
                    {
                        canFlex = true;
                    }
                }
            }

            return canFlex ? total : 0;
        }

        private double CalculateFlexAmount(IView child, double extraSpace, float total)
        {
            if (extraSpace == 0 || child is not BindableObject bindable || total == 0)
            {
                return 0;
            }

            var flexFactor = (extraSpace > 0 ? FlexLayout.GetGrow(bindable) : FlexLayout.GetShrink(bindable)) / total;
            return extraSpace * flexFactor;
        }

        protected abstract double GetMainAxisValue(Point point);
        protected abstract double GetMainAxisDimension(Size size);
        protected abstract double GetMainAxisStart(Thickness thickness);
        protected abstract double GetMainAxisThickness(Thickness thickness);

        protected abstract double GetCrossAxisValue(Point point);
        protected abstract double GetCrossAxisDimension(Size size);
        protected abstract double GetCrossAxisStart(Thickness thickness);
        protected abstract double GetCrossAxisThickness(Thickness thickness);

        protected abstract Point CreatePoint(double mainAxisPosition, double crossAxisPosition);
        protected abstract Size CreateSize(double mainAxisDimension, double crossAxisDimension);

        protected abstract Size MeasureChild(IView child, double mainAxisConstraint, double crossAxisConstraint);
    }

    public class HorizontalStackLayoutManager : StackLayoutManager
    {
        protected override double MainAxisDimension => Stack.Width;
        protected override double MainAxisMinimumDimension => Stack.MinimumWidth;
        protected override double MainAxisMaximumDimension => Stack.MaximumWidth;

        protected override double CrossAxisDimension => Stack.Height;
        protected override double CrossAxisMinimumDimension => Stack.MinimumHeight;
        protected override double CrossAxisMaximumDimension => Stack.MaximumHeight;

        protected override BindableProperty CrossAxisLayoutOptions => View.VerticalOptionsProperty;

        public HorizontalStackLayoutManager(IStackLayout stack) : base(stack) { }

        public override Size Measure(double widthConstraint, double heightConstraint) => OrientationAgnosticMeasure(widthConstraint, heightConstraint);

        protected override double GetMainAxisValue(Point point) => point.X;
        protected override double GetMainAxisDimension(Size size) => size.Width;
        protected override double GetMainAxisStart(Thickness thickness) => thickness.Left;
        protected override double GetMainAxisThickness(Thickness thickness) => thickness.HorizontalThickness;


        protected override double GetCrossAxisValue(Point point) => point.Y;
        protected override double GetCrossAxisDimension(Size size) => size.Height;
        protected override double GetCrossAxisStart(Thickness thickness) => thickness.Top;
        protected override double GetCrossAxisThickness(Thickness thickness) => thickness.VerticalThickness;


        protected override Point CreatePoint(double mainAxisPosition, double crossAxisPosition) => new Point(mainAxisPosition, crossAxisPosition);
        protected override Size CreateSize(double mainAxisDimension, double crossAxisDimension) => new Size(mainAxisDimension, crossAxisDimension);

        protected override Size MeasureChild(IView child, double mainAxisConstraint, double crossAxisConstraint) => child.Measure(mainAxisConstraint, crossAxisConstraint);
    }

    public class VerticalStackLayoutManager : StackLayoutManager
    {
        protected override double MainAxisDimension => Stack.Height;
        protected override double MainAxisMinimumDimension => Stack.MinimumHeight;
        protected override double MainAxisMaximumDimension => Stack.MaximumHeight;

        protected override double CrossAxisDimension => Stack.Width;
        protected override double CrossAxisMinimumDimension => Stack.MinimumWidth;
        protected override double CrossAxisMaximumDimension => Stack.MaximumWidth;

        protected override BindableProperty CrossAxisLayoutOptions => View.HorizontalOptionsProperty;

        public VerticalStackLayoutManager(IStackLayout stack) : base(stack) { }

        public override Size Measure(double widthConstraint, double heightConstraint) => OrientationAgnosticMeasure(heightConstraint, widthConstraint);

        protected override double GetMainAxisValue(Point point) => point.Y;
        protected override double GetMainAxisDimension(Size size) => size.Height;
        protected override double GetMainAxisStart(Thickness thickness) => thickness.Top;
        protected override double GetMainAxisThickness(Thickness thickness) => thickness.VerticalThickness;

        protected override double GetCrossAxisValue(Point point) => point.X;
        protected override double GetCrossAxisDimension(Size size) => size.Width;
        protected override double GetCrossAxisStart(Thickness thickness) => thickness.Left;
        protected override double GetCrossAxisThickness(Thickness thickness) => thickness.HorizontalThickness;

        protected override Point CreatePoint(double mainAxisPosition, double crossAxisPosition) => new Point(crossAxisPosition, mainAxisPosition);
        protected override Size CreateSize(double mainAxisDimension, double crossAxisDimension) => new Size(crossAxisDimension, mainAxisDimension);

        protected override Size MeasureChild(IView child, double mainAxisConstraint, double crossAxisConstraint) => child.Measure(crossAxisConstraint, mainAxisConstraint);
    }
}
