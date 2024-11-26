using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies
{
    public class SpaceSpansBehavior : Behavior<Label>
    {
        public string Spacer { get; set; }
        private Span SpacerSpan;

        protected override void OnAttachedTo(Label bindable)
        {
            base.OnAttachedTo(bindable);

            SpacerSpan = new Span { Text = Spacer, FontAttributes = FontAttributes.Bold };
            AddSpacers(bindable);
            bindable.PropertyChanged += LabelPropertyChanged;
        }

        protected override void OnDetachingFrom(Label bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.PropertyChanged -= LabelPropertyChanged;
        }

        private void LabelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Label.FormattedTextProperty.PropertyName)
            {
                AddSpacers((Label)sender);
            }
        }

        private void AddSpacers(Label label)
        {
            label.PropertyChanged -= LabelPropertyChanged;
            int lastSpacer = -2;

            if (label.FormattedText?.Spans is IList<Span> spans)
            {
                for (int i = 0; i < spans.Count; i++)
                {
                    Span span = spans[i];

                    if (span == SpacerSpan)
                    {
                        if (lastSpacer != -1)
                        {
                            spans.RemoveAt(i--);
                        }
                        else
                        {
                            lastSpacer = i;
                        }
                    }
                    else if (!IsHidden(span.Text))
                    {
                        if (lastSpacer == -1)
                        {
                            spans.Insert(i++, SpacerSpan);
                        }

                        lastSpacer = -1;
                    }
                }

                if (lastSpacer > 0)
                {
                    spans.RemoveAt(lastSpacer);
                }
            }

            label.PropertyChanged += LabelPropertyChanged;
        }

        private bool IsHidden(string str) => str == null || str.Length == 0;

        private void OldAddSpacers(Label label)
        {
            if (label.FormattedText == null)
            {
                return;
            }

            //label.PropertyChanged -= LabelPropertyChanged;
            label.Style = null;

            IList<Span> spans = label.FormattedText.Spans;

            for (int i = 0; i < spans.Count - 1; i++)
            {
                Span spacer = new Span();
                spacer.SetBinding(Span.TextProperty, new Binding("Text", source: spans[i], converter: TextLengthConverter.Instance, converterParameter: Spacer));
                spans.Insert(++i, spacer);
            }
        }

        private class TextLengthConverter : IValueConverter
        {
            public static readonly TextLengthConverter Instance = new TextLengthConverter();

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == null || ((string)value).Length == 0 ? string.Empty : parameter;

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
