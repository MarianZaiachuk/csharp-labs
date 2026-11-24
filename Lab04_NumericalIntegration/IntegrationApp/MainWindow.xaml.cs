using System.Windows;
using IntegrationApp.ViewModels;

namespace IntegrationApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void ErrorCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.UpdateCanvasSize(ErrorCanvas.ActualWidth, ErrorCanvas.ActualHeight);
    }
}
