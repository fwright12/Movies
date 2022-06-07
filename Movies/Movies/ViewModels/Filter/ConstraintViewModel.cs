namespace Movies.ViewModels
{
    public sealed class ConstraintViewModel : BindableViewModel
    {
        public Constraint Constraint
        {
            get => _Constraint;
            set => UpdateValue(ref _Constraint, value);
        }

        public Operators Comparison
        {
            get => _Comparison;
            set => UpdateValue(ref _Comparison, value);
        }

        public object Value
        {
            get => _Value;
            set => UpdateValue(ref _Value, value);
        }

        public bool IsShowing
        {
            get => _IsShowing;
            set => UpdateValue(ref _IsShowing, value);
        }

        public bool ShowLabel
        {
            get => _ShowLabel;
            set => UpdateValue(ref _ShowLabel, value);
        }

        public bool ShowOperator
        {
            get => _ShowOperator;
            set => UpdateValue(ref _ShowOperator, value);
        }

        private Constraint _Constraint;
        private Operators _Comparison;
        private object _Value;
        private bool _IsShowing = true;
        private bool _ShowLabel = false;
        private bool _ShowOperator = false;

        public ConstraintViewModel()
        {
            ComponentsChanged();

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Constraint))
                {
                    ConstraintChanged();
                }
                else if (e.PropertyName == nameof(Value) || e.PropertyName == nameof(Comparison))
                {
                    ComponentsChanged();
                }
            };
        }

        private void ConstraintChanged()
        {
            var backup = Constraint;

            Value = backup.Value;
            Comparison = backup.Comparison;
        }

        private void ComponentsChanged()
        {
            Constraint = new Constraint(Constraint.Property)
            {
                Value = Value,
                Comparison = Comparison
            };
        }
    }
}