using System.Windows;
using InterpolationApp.ViewModels;

namespace InterpolationApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void MainPlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.UpdateMainCanvasSize(MainPlotCanvas.ActualWidth, MainPlotCanvas.ActualHeight);
    }

    private void ErrorPlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.UpdateErrorCanvasSize(ErrorPlotCanvas.ActualWidth, ErrorPlotCanvas.ActualHeight);
    }
}
