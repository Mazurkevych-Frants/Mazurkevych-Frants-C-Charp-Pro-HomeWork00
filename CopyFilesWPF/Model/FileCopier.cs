using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CopyFilesWPF.Model
{
    public class FileCopier
    {
        private readonly Grid _gridPanel;
        private readonly FilePath _filePath;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public delegate void ProgressChangeDelegate(double progress, ref bool cancel, Grid gridPanel);
        public delegate void CompleteDelegate(Grid gridPanel);
        public event ProgressChangeDelegate OnProgressChanged;
        public event CompleteDelegate OnComplete;

        public bool CancelFlag = false;
        public ManualResetEvent PauseFlag = new(true);

        public FileCopier(
            FilePath filePath,
            ProgressChangeDelegate onProgressChange,
            CompleteDelegate onComplete,
            Grid gridPanel)
        {
            OnProgressChanged += onProgressChange;
            OnComplete += onComplete;
            _filePath = filePath;
            _gridPanel = gridPanel;
        }

        public void CopyFile()
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            byte[] buffer = new byte[1024 * 1024];
            bool continueCopy = true;

            while (continueCopy)
            {
                try
                {
                    using (var source = new FileStream(_filePath.PathFrom, FileMode.Open, FileAccess.Read))
                    {

                        var fileLength = source.Length;
                        using var destination = new FileStream(_filePath.PathTo, FileMode.CreateNew, FileAccess.Write);
                        long totalBytes = 0;
                        int currentBlockSize = 0;

                        while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                File.Delete(_filePath.PathTo);
                                return;
                            }

                            totalBytes += currentBlockSize;
                            double percentage = totalBytes * 100.0 / fileLength;
                            destination.Write(buffer, 0, currentBlockSize);

                            OnProgressChanged(percentage, ref CancelFlag, _gridPanel);

                            Thread.CurrentThread.Suspend();
                        }
                    }

                    continueCopy = false;
                }
                catch (IOException error)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        var result = MessageBox.Show(error.Message + " Replace?", "Replace?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        continueCopy = result == MessageBoxResult.Yes;

                        if (continueCopy)
                        {
                            File.Delete(_filePath.PathTo);
                        }
                    }
                    else
                    {
                        MessageBox.Show(error.Message + " Copying was canceled!", "Cancel", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        continueCopy = false;
                        File.Delete(_filePath.PathTo);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "Error occurred!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            OnComplete(_gridPanel);
        }
    }
}
