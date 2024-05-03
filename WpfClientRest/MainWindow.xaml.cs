using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Utils.FileHelper;

namespace WpfClientRest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RestClientDll.RestClient client = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void createUriBtn_Click(object sender, RoutedEventArgs e)
        {
            Uri createdUri = null;
            if (int.TryParse(portTxt.Text, out int port))
            {
                createdUri = new UriBuilder(schemeCombobox.Text, hostTxt.Text, port).Uri;
                client = new RestClientDll.RestClient(createdUri);

                uriTxt.Text = createdUri.AbsoluteUri;
            }
        }

        public Dictionary<string, ApiFileInfo> Dict =  new Dictionary<string, ApiFileInfo>();

        private async void listBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await client.CreateRequest<List<ApiFileInfo>>(RestClientDll.RestRequestType.Get, "api/File", "List");
                Dict.Clear();

                resultTxtBox.Text += $"Found {result.Count} elements" + Environment.NewLine;
                foreach (var file in result)
                {
                    resultTxtBox.Text += $"{file.Filename} => {file.Dim} bytes, Chunks: {file.ChunksNumber}" + Environment.NewLine;
                    Dict[file.Filename] = file;

                    fileCombobox.Items.Add(file.Filename);
                }

                if (fileCombobox.Items.Count != 0)
                    fileCombobox.SelectedIndex = 0;
            }
            catch(Exception ex)
            {
                resultTxtBox.Text += $"{ex.Message}" + Environment.NewLine;
            }
        }

        private async void downloadBtn_Click(object sender, RoutedEventArgs e)
        { 
            string fileNameSelected = (string)fileCombobox.SelectedItem;

            string requestString = $"/api/File/DownloadFile?fileName={fileNameSelected}";
            byte[] file = await client.DownloadRequest(requestString);
            string md5 = FileHelper.Md5Result(file);

            bool result = md5 == Dict[fileNameSelected].MD5 ? true : false;

            resultTxtBox.Text += $"{fileNameSelected} => Same: {result}" + Environment.NewLine;

            await FileHelper.MakeFile(file, "", fileNameSelected, true);
        }

        private async void downloadChunkBtn_Click(object sender, RoutedEventArgs e)
        {
            string fileNameSelected = (string)fileCombobox.SelectedItem;

            string requestString = $"/api/File/DownloadFileByChunks?fileName={fileNameSelected}&Id={itembox.SelectedItem}";
            byte[] file = await client.DownloadRequest(requestString);
            string md5 = FileHelper.Md5Result(file);

            FileInfo f = new FileInfo(fileNameSelected);

            string filename = $"{f.Name.Replace(f.Extension, "")}_{itembox.SelectedItem}{f.Extension}"; 
            resultTxtBox.Text += $"{fileNameSelected}" + Environment.NewLine;

            await FileHelper.MakeFile(file, "Test", filename, true);
        }

        private void createFileBtn_Click(object sender, RoutedEventArgs e)
        {
            FileHelper.MakeTestFile(1024 * 1024 * 1024, Directory.GetCurrentDirectory(), "t1.bin");
            FileHelper.MakeTestFile(1024 * 1024 * 500, Directory.GetCurrentDirectory(), "t12.mul");
            FileHelper.MakeTestFile(1024 * 1024 * 100, Directory.GetCurrentDirectory(), "t3.mul");
            FileHelper.MakeTestFile(1024 * 1024 * 20, Directory.GetCurrentDirectory(), "t4.mul");
            FileHelper.MakeTestFile(1024 * 1024 * 40, Directory.GetCurrentDirectory(), "t45.mul");
            FileHelper.MakeTestFile(1024 * 1024 * 60, Directory.GetCurrentDirectory(), "t6.mul");
            FileHelper.MakeTestFile(1024 * 1024 * 40, Directory.GetCurrentDirectory(), "t7.mul");
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e)
        {
            resultTxtBox.Clear();
        }

        private void fileCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            itembox.Items.Clear();

            var apiFileInfo = Dict[(string)fileCombobox.SelectedItem];

            for (int i = 0; i < apiFileInfo.ChunksNumber; i++) 
            {
                itembox.Items.Add(i);
            }
        }

        private async void downloadAllChunkBtn_Click(object sender, RoutedEventArgs e)
        {
            string fileNameSelected = (string)fileCombobox.SelectedItem;
            var apiFileInfo = Dict[fileNameSelected];

            double percentage = 0;

            Stopwatch st = new Stopwatch();
            st.Start();

            for (int i = 0; i < apiFileInfo.ChunksNumber; i++)
            {
                percentage = (double)i / apiFileInfo.ChunksNumber* 100;
                //Task.Run(async () =>
                //{
                string requestString = $"/api/File/DownloadFileByChunks?fileName={fileNameSelected}&Id={i}";
                byte[] file = await client.DownloadRequest(requestString);

                FileInfo f = new FileInfo(fileNameSelected);

                string filename = $"{f.Name.Replace(f.Extension, "")}_{i}{f.Extension}";

                await FileHelper.MakeFile(file, "Test", filename, true);
                //resultTxtBox.Text += $"Downloaded: {percentage.ToString("F4")}%" + Environment.NewLine;
                //});
            }

            st.Stop();
            resultTxtBox.Text += $"Downloaded: {fileNameSelected} in {st.ElapsedMilliseconds}ms" + Environment.NewLine;
        }

        private void DownloadStressTestBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!int.TryParse(howMuchStressTxtbox.Text, out int howMuchResult))
            {
                howMuchResult = 5;
            }

            for (int index = 0; index < howMuchResult; index++)
            {
                Uri createdUri = null;
                string fileNameSelected = "";
                RestClientDll.RestClient localClient = null;
                string pathName = $"Test_{index}";

                Dispatcher.Invoke(() =>
                {
                    if (int.TryParse(portTxt.Text, out int port))
                    {
                        createdUri = new UriBuilder(schemeCombobox.Text, hostTxt.Text, port).Uri;
                        localClient = new RestClientDll.RestClient(createdUri);

                        uriTxt.Text = createdUri.AbsoluteUri;
                    }
                    fileNameSelected = (string)fileCombobox.SelectedItem;
                });

                Directory.CreateDirectory(pathName);

                Task.Run(async () =>
                {
                    var result = await localClient.CreateRequest<List<ApiFileInfo>>(RestClientDll.RestRequestType.Get, "api/File", "List");

                    var apiFileInfo = Dict[fileNameSelected];

                    double percentage = 0;
                    for (int t = 0; t < apiFileInfo.ChunksNumber; t++)
                    {
                        percentage = (double)t / apiFileInfo.ChunksNumber * 100;
                        //Task.Run(async () =>
                        //{
                        string requestString = $"/api/File/DownloadFileByChunks?fileName={fileNameSelected}&Id={t}";
                        byte[] file = await localClient.DownloadRequest(requestString);

                        FileInfo f = new FileInfo(fileNameSelected);
                        string filename = $"{f.Name.Replace(f.Extension, "")}_{t}{f.Extension}";

                        if (file is null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                resultTxtBox.Text += $"Fail to download {filename} for {pathName} test" + Environment.NewLine;
                            });
                            continue;
                        }

                        await FileHelper.MakeFile(file, pathName, filename, true);
                        //resultTxtBox.Text += $"Downloaded: {percentage.ToString("F4")}%" + Environment.NewLine;
                        //});
                        await Task.Delay(200);
                    }
                });

                Task.Delay(500);
            }
        }
    }
}
