using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace RateListener.Controls;

public enum ValidationMode
{
    None,
    PositiveNumber
}

public partial class ValidatedTextBox
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(ValidatedTextBox),
        new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty ValidationModeProperty = DependencyProperty.Register(
        nameof(ValidationMode), typeof(ValidationMode), typeof(ValidatedTextBox),
        new PropertyMetadata(ValidationMode.None, OnValidationModeChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public ValidationMode ValidationMode
    {
        get => (ValidationMode)GetValue(ValidationModeProperty);
        set => SetValue(ValidationModeProperty, value);
    }

    public bool IsValid { get; private set; } = true;

    public ValidatedTextBox()
    {
        InitializeComponent();
        Input.TextChanged += (_, _) => Text = Input.Text;
        Loaded += (_, _) => RefreshValidation();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ValidatedTextBox)d;
        var newText = e.NewValue as string ?? string.Empty;
        if (control.Input.Text != newText)
            control.Input.Text = newText;
        control.RefreshValidation();
    }

    private static void OnValidationModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ValidatedTextBox)d).RefreshValidation();

    private void RefreshValidation()
    {
        IsValid = Text is not null &&
            (ValidationMode != ValidationMode.PositiveNumber || double.TryParse(Text.Replace(',', '.'),
            NumberStyles.Number, CultureInfo.InvariantCulture, out var number) && number > 0);

        Border.BorderBrush = IsValid ? Brushes.Transparent : Brushes.Orange;
    }
}
