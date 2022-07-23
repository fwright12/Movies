using System;
using Xamarin.Forms;

namespace Movies
{
    public class BatchBehavior : Behavior
    {
        public static readonly BindableProperty BatchProperty = BindableProperty.CreateAttached("Batch", typeof(object), typeof(BindableObject), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            Print.Log("batch property changed", bindable, newValue);
        });

        private class Proxy : BindableObject { }

        protected override void OnAttachedTo(BindableObject bindable)
        {
            base.OnAttachedTo(bindable);

            DataService.Instance.BatchBegin();
            //Print.Log("\tbatch begin", bindable.GetType(), (bindable as VisualElement)?.Style?.Behaviors.Count);
            bindable.BindingContextChanged += EndBatch;
            return;
            bindable.PropertyChanging += (sender, e) =>
            {
                Print.Log("property changing", e.PropertyName);
                if (e.PropertyName == BindingContextProperty.PropertyName)
                {
                    Print.Log("binding context changing");
                    ;
                }
            };
            bindable.PropertyChanged += (sender, e) =>
            {
                Print.Log("\tproperty changed", e.PropertyName);
                return;
                if (e.PropertyName == "Renderer")
                {
                    EndBatch(null, null);
                    ;
                }
                else if (e.PropertyName == nameof(Element.Parent))
                {
                    ;
                }
                return;
                if (e.PropertyName == nameof(Element.Parent))
                {
                    Print.Log("parent changed", (sender as Element)?.Parent);
                    ;
                }
                if (e.PropertyName == VisualElement.HeightProperty.PropertyName)
                {
                    Print.Log("height changed");
                    ;
                }
            };
        }

        protected override void OnDetachingFrom(BindableObject bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.BindingContextChanged -= EndBatch;
        }

        private void EndBatch(object sender, EventArgs e)
        {
            //Print.Log(((BindableObject)sender).BindingContext?.GetType());
            DataService.Instance.BatchEnd();
            //Print.Log("\tbatch end", ((BindableObject)sender)?.BindingContext?.GetType(), ((BindableObject)sender)?.BindingContext);
            if (sender is VisualElement visualElement && !visualElement.Behaviors.Remove(this))
            {
                visualElement.Style = null;
                //visualElement.Style.Behaviors.Remove(this);
            }
        }
    }
}
