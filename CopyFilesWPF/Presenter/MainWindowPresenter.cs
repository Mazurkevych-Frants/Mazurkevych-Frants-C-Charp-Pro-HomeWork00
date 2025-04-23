using CopyFilesWPF.Model;
using CopyFilesWPF.View;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CopyFilesWPF.Presenter
{
    public class MainWindowPresenter : IMainWindowPresenter
    {
        private readonly IMainWindowView _mainWindowView;
        private readonly MainWindowModel _mainWindowModel;

        public MainWindowPresenter(IMainWindowView mainWindowView)
        {
            _mainWindowView = mainWindowView;
            _mainWindowModel = new MainWindowModel();
        }

        public void ChooseFileFromButtonClick(string path) => _mainWindowModel.FilePath.PathFrom = path;

        public void ChooseFileToButtonClick(string path) => _mainWindowModel.FilePath.PathTo = path;

        public void CopyButtonClick()
        {
            PrepareFilePaths();
            var panel = CreateFileCopyPanel(Path.GetFileName(_mainWindowModel.FilePath.PathFrom));
            AddPauseButton(panel);
            _mainWindowView.MainWindowView.MainPanel.Children.Add(panel);
        }

        public void CanselButtonClick()
        {
            PrepareFilePaths();
            var panel = CreateFileCopyPanel(Path.GetFileName(_mainWindowModel.FilePath.PathFrom));
            AddCancelButton(panel);
            _mainWindowView.MainWindowView.MainPanel.Children.Add(panel);
            _mainWindowModel.CopyFile(ProgressChanged, ModelOnComplete, panel);
        }

        private void PrepareFilePaths()
        {
            _mainWindowModel.FilePath.PathFrom = _mainWindowView.MainWindowView.FromTextBox.Text;
            _mainWindowModel.FilePath.PathTo = _mainWindowView.MainWindowView.ToTextBox.Text;
            _mainWindowView.MainWindowView.FromTextBox.Clear();
            _mainWindowView.MainWindowView.ToTextBox.Clear();
            _mainWindowView.MainWindowView.Height += 60;
        }

        private Grid CreateFileCopyPanel(string fileName)
        {
            var panel = new Grid { Height = 60 };
            DockPanel.SetDock(panel, Dock.Top);

            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
            panel.ColumnDefinitions.Add(new ColumnDefinition());
            panel.ColumnDefinitions.Add(new ColumnDefinition());
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            panel.RowDefinitions.Add(new RowDefinition());

            var fileLabel = new TextBlock
            {
                Text = fileName,
                Margin = new Thickness(5, 0, 5, 0)
            };
            Grid.SetRow(fileLabel, 0);
            Grid.SetColumn(fileLabel, 0);
            panel.Children.Add(fileLabel);

            var progressBar = new ProgressBar
            {
                Margin = new Thickness(10)
            };
            Grid.SetRow(progressBar, 1);
            panel.Children.Add(progressBar);

            return panel;
        }

        private void AddPauseButton(Grid panel)
        {
            var pauseButton = new Button
            {
                Content = "Pause",
                Margin = new Thickness(5),
                Tag = panel
            };
            pauseButton.Click += PauseClick;
            Grid.SetRow(pauseButton, 1);
            Grid.SetColumn(pauseButton, 1);
            panel.Children.Add(pauseButton);
        }

        private void AddCancelButton(Grid panel)
        {
            var cancelButton = new Button
            {
                Content = "Cancel",
                Margin = new Thickness(5),
                Tag = panel
            };
            cancelButton.Click += CanselClick;
            Grid.SetRow(cancelButton, 1);
            Grid.SetColumn(cancelButton, 2);
            panel.Children.Add(cancelButton);
        }

        private void PauseClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Grid grid || grid.Tag is not FileCopier copier) return;

            button.IsEnabled = false;
            if (button.Content.ToString() == "Pause")
            {
                copier.PauseFlag.Reset();
                button.Content = "Resume";
            }
            else
            {
                copier.PauseFlag.Set();
                button.Content = "Pause";
            }
            button.IsEnabled = true;
        }

        private void CanselClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Grid grid || grid.Tag is not FileCopier copier) return;

            button.IsEnabled = false;
            copier.CancelFlag = true;
        }

        private void ModelOnComplete(Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate
                {
                    _mainWindowView.MainWindowView.Height -= 60;
                    _mainWindowView.MainWindowView.MainPanel.Children.Remove(panel);
                    _mainWindowView.MainWindowView.CopyButton.IsEnabled = true;
                });
        }

        private void ProgressChanged(double percentage, ref bool cancelFlag, Grid panel)
        {
            _mainWindowView.MainWindowView.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                (ThreadStart)delegate
                {
                    foreach (var child in panel.Children)
                    {
                        switch (child)
                        {
                            case ProgressBar progressBar:
                                progressBar.Value = percentage;
                                break;

                            case Button button:
                                if (!button.IsEnabled && (button.Content?.ToString() == "Resume" || button.Content?.ToString() == "Pause"))
                                {
                                    button.Content = button.Content.ToString() == "Resume" ? "Pause" : "Resume";
                                    button.IsEnabled = true;
                                }
                                break;
                        }
                    }
                });
        }
    }
}
