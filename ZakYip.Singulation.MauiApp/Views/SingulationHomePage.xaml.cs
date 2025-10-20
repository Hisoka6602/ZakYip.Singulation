using ZakYip.Singulation.MauiApp.ViewModels;

namespace ZakYip.Singulation.MauiApp.Views;

public partial class SingulationHomePage : ContentPage
{
    private bool _isLandscape;

    public SingulationHomePage(SingulationHomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        
        // Determine if device is in landscape mode
        bool isLandscape = width > height;
        
        // Only update layout if orientation changed
        if (_isLandscape != isLandscape)
        {
            _isLandscape = isLandscape;
            UpdateLayout();
        }
    }

    private void UpdateLayout()
    {
        // Show/hide appropriate layouts based on orientation
        PortraitLayout.IsVisible = !_isLandscape;
        LandscapeLayout.IsVisible = _isLandscape;
    }
}
