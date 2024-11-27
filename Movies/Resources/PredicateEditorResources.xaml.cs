using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Movies
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PredicateEditorResources : ResourceDictionary
    {
        public PredicateEditorResources()
        {
            InitializeComponent();
        }
    }
}