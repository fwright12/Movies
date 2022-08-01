using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    public static class CultureExtensions
    {
        public static CultureInfo GetNeutralCulture(this CultureInfo culture) => culture.IsNeutralCulture ? culture : culture.Parent;
    }
}

namespace Movies
{
    public class BindableDictionary<TKey, TValue> : BindableViewModel
    {
        private IDictionary<TKey, TValue> Cache { get; }

        public BindableDictionary(IDictionary<TKey, TValue> cache)
        {
            Cache = cache;
        }

        protected T GetValue<T>(TKey key) => Cache.TryGetValue(key, out var value) && value is T t ? t : default;

        protected virtual bool UpdateValue(TKey key, TValue value, [CallerMemberName] string propertyName = null)
        {
            object oldValue = null;
            if (Cache.TryGetValue(key, out var cached))
            {
                oldValue = cached;
            }

            if (!Equals(oldValue, value))
            {
                OnPropertyChanging(propertyName);
                var e = new PropertyChangeEventArgs(propertyName, oldValue, Cache[key] = value);
                OnPropertyChanged(propertyName);
                OnPropertyChange(this, e);

                return true;
            }

            return false;
        }
    }

    public class Language
    {
        public CultureInfo Culture { get; }
        public string Name => Culture?.Name ?? Iso_639;
        public string NativeName => Culture?.NativeName ?? Iso_639;
        public string DisplayName => Culture?.DisplayName ?? Iso_639;
        public string Iso_639 { get; }

        public Language(string iso_639)
        {
            Iso_639 = iso_639;

            try
            {
                Culture = new CultureInfo(Iso_639);
            }
            catch { }
        }

        public Language(CultureInfo culture)
        {
            Culture = culture;
            Iso_639 = Culture.Name;
        }

        public static implicit operator Language(CultureInfo culture) => new Language(culture);

        public override bool Equals(object obj) => obj is Language lang && lang.Iso_639 == Iso_639;
        public override int GetHashCode() => Iso_639.GetHashCode();
        public override string ToString() => DisplayName;
    }

    public class Region
    {
        public RegionInfo RegionInfo { get; }
        public string Name => RegionInfo?.Name ?? Iso_3166;
        public string NativeName => RegionInfo?.NativeName ?? Iso_3166;
        public string DisplayName => RegionInfo?.DisplayName ?? Iso_3166;
        public string Iso_3166 { get; }

        public Region(string iso_3166)
        {
            Iso_3166 = iso_3166;

            try
            {
                RegionInfo = new RegionInfo(Iso_3166);
            }
            catch { }
        }

        public Region(RegionInfo region)
        {
            RegionInfo = region;
            Iso_3166 = RegionInfo.TwoLetterISORegionName;
        }

        public static implicit operator Region(RegionInfo region) => new Region(region);

        public override bool Equals(object obj) => obj is Region region && region.Iso_3166 == Iso_3166;
        public override int GetHashCode() => Iso_3166.GetHashCode();
        public override string ToString() => DisplayName;
    }

    public class UserPrefs : BindableDictionary<string, object>
    {
        public const string LANGUAGE_KEY = "language";
        public const string REGION_KEY = "region";

        public Language Language
        {
            get => GetValue<string>(LANGUAGE_KEY) is string iso ? new Language(iso) : null;
            set => UpdateValue(LANGUAGE_KEY, value.Name);
        }

        public Region Region
        {
            get => GetValue<string>(REGION_KEY) is string iso ? new Region(iso) : null;
            set => UpdateValue(REGION_KEY, value.Name);
        }

        public bool RestartRequired { get; private set; }

        public ICollection<Language> Languages { get; } = new ObservableCollection<Language>(CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures).Except(new List<CultureInfo> { CultureInfo.InvariantCulture }).OrderBy(culture => culture.NativeName).Select(lang => new Language(lang)));
        public ICollection<Region> Regions { get; } = new ObservableCollection<Region>();

        public UserPrefs(IDictionary<string, object> cache) : base(cache)
        {
            Language ??= CultureInfo.CurrentCulture;
            Region ??= RegionInfo.CurrentRegion;
            
            PropertyChanged += UpdateRestartRequired;
        }

        private void UpdateRestartRequired(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Language) || e.PropertyName == nameof(Region))
            {
                RestartRequired = true;
                OnPropertyChanged(nameof(RestartRequired));
            }
        }
    }
}
