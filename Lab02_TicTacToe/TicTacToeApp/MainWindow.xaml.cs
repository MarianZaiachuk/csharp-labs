using System.Windows;
using TicTacToeApp.ViewModels;

namespace TicTacToeApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
