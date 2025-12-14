using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies
{
    public class RatingTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ExternalRatingTemplate { get; set; }
        public DataTemplate PersonalRatingTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Models.Rating rating = (Models.Rating)item;
            return rating.Company.Name == "Me" ? PersonalRatingTemplate : ExternalRatingTemplate;
        }
    }
}
