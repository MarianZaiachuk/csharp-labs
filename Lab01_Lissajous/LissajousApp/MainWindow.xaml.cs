using System.Windows;
using LissajousApp.ViewModels;

namespace LissajousApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void PlotCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.UpdateCanvasSize(PlotCanvas.ActualWidth, PlotCanvas.ActualHeight);
    }
}
