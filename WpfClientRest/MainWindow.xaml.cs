using RestSharp;
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
using static RestClientDll.RestClient;


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

                client.FileDownloadResultEV += FileDownloadedEvent;
            }
        }

        public Dictionary<string, ApiFileInfo> Dict =  new Dictionary<string, ApiFileInfo>();

        public void FileDownloadedEvent(object sender, RestClientDll.RestClient.ChunkDowloadArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Result == RestClientDll.RestClient.Result.Ok)
                {
                    //resultTxtBox.Text += $"D: {e.Name}, {e.ID} => {e.Result.ToString()}, {e.Try}" + Environment.NewLine;
                }
                else
                {
                    resultTxtBox.Text += $"Fail to download {e.Name} {e.Try} try" + Environment.NewLine;
                }
            });
        }
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
            byte[] file = client.DownloadRequest(requestString);

            if (file != null)
            {
                string md5 = FileHelper.Md5Result(file);

                bool result = md5 == Dict[fileNameSelected].MD5 ? true : false;

                resultTxtBox.Text += $"{fileNameSelected} => Same: {result}" + Environment.NewLine;

                await FileHelper.MakeFile(file, "Test", fileNameSelected, true);
            }
            else
            {
                resultTxtBox.Text += $"RESULT: NULL" + Environment.NewLine;
            }

        }

        private async void downloadChunkBtn_Click(object sender, RoutedEventArgs e)
        {
            string fileNameSelected = (string)fileCombobox.SelectedItem;

            if (itembox.SelectedItem is null)
                itembox.SelectedItem = 0;

            string requestString = $"/api/File/DownloadFileByChunks?fileName={fileNameSelected}&Id={itembox.SelectedItem}";
            var request = new RestSharp.RestRequest(requestString, Method.Get);
            var array = client.Client.DownloadData(request);

            if (array != null)
            {
                string md5 = FileHelper.Md5Result(array);

                FileInfo f = new FileInfo(fileNameSelected);

                string filename = $"{f.Name.Replace(f.Extension, "")}_{itembox.SelectedItem}{f.Extension}";
                await FileHelper.MakeFile(array, "Test", filename, true);
                resultTxtBox.Text += $"{fileNameSelected}" + Environment.NewLine;


            }
            else
            {
                resultTxtBox.Text += $"{fileNameSelected} {itembox.SelectedItem} => RESULT: NULL" + Environment.NewLine;
            }

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

            string address = "/api/File/DownloadFileByChunks?fileName={0}&Id={1}";
            client.DownloadChunks(address, apiFileInfo.ChunksNumber, fileNameSelected, path: "Test");

            st.Stop();
            resultTxtBox.Text += $"Downloaded: {fileNameSelected} in {st.ElapsedMilliseconds}ms" + Environment.NewLine;
        }
        //private async void downloadAllChunkBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    string fileNameSelected = (string)fileCombobox.SelectedItem;
        //    var apiFileInfo = Dict[fileNameSelected];

        //    double percentage = 0;

        //    Stopwatch st = new Stopwatch();
        //    st.Start();

        //    for (int i = 0; i < apiFileInfo.ChunksNumber; i++)
        //    {
        //        percentage = (double)i / apiFileInfo.ChunksNumber* 100;
        //        //Task.Run(async () =>
        //        //{
        //        string requestString = $"/api/File/DownloadFileByChunks?fileName={fileNameSelected}&Id={i}";
        //        var request = new RestSharp.RestRequest(requestString, Method.Get);
        //        var file = client.Client.DownloadData(request);

        //        FileInfo f = new FileInfo(fileNameSelected);

        //        if (file != null)
        //        {
        //            string filename = $"{f.Name.Replace(f.Extension, "")}_{i}{f.Extension}";

        //            await FileHelper.MakeFile(file, "Test", filename, true);
        //            resultTxtBox.Text += $"Downloaded: {percentage.ToString("F4")}%" + Environment.NewLine;
        //            //});
        //        }
        //        else
        //        {
        //            resultTxtBox.Text += $"FAIL to doweenload {fileNameSelected} {i}" + Environment.NewLine;
        //        }
        //    }

        //    st.Stop();
        //    resultTxtBox.Text += $"Downloaded: {fileNameSelected} in {st.ElapsedMilliseconds}ms" + Environment.NewLine;
        //}

        int MaxTry = 5;
        public async Task DownloadChunks(string fileNameSelected, string pathName)
        {
            Stopwatch st = new Stopwatch();
            st.Start();

            var apiFileInfo = Dict[fileNameSelected];
            bool[] chunks = new bool[apiFileInfo.ChunksNumber];

            int downloadedValue = 0;
            DownloadResult operationResult = new DownloadResult(Result.Unknown);

            operationResult = await Task<DownloadResult>.Run(async () =>
            {
                //var result = await localClient.CreateRequest<List<ApiFileInfo>>(RestClientDll.RestRequestType.Get, "api/File", "List");
                
                double percentage = 0;

                var chucksToDo = new List<int>();

                for (int i = 0; i < apiFileInfo.ChunksNumber; i++)
                {
                    chucksToDo.Add(i);
                }

                int cycle = 0;


                while (true)
                {
                    if (chucksToDo.Count == 0)
                        break;

                    Dispatcher.Invoke(() =>
                    {
                        resultTxtBox.Text += $"{pathName} cycle: {cycle++} " + Environment.NewLine;
                    });

                    string address = "/api/File/DownloadFileByChunks?fileName={0}&Id={1}";

                    DownloadResult toRet = null;

                    if ((toRet = await client.DownloadChunksByIndexes(address, chucksToDo, fileNameSelected, path: pathName)).Result == RestClientDll.RestClient.Result.Ok)
                    {
                        return toRet;
                    }
                }

                return new DownloadResult(Result.Unknown);
            });

            bool status = true;

            foreach (var objDownloaded in chunks)
            {
                status &= objDownloaded;
            }

            st.Stop();

            string kBytesPerMS = ((double)operationResult.Size/ 1024/1024 /(double)st.ElapsedMilliseconds*1000).ToString("F2");
            resultTxtBox.Text += $"{pathName} => Downloaded {chunks.Length}/{apiFileInfo.ChunksNumber}, Status: {operationResult} after {st.ElapsedMilliseconds}ms, {kBytesPerMS} MBytes/s" + Environment.NewLine;
        }


        private async void DownloadStressTestBtn_Click(object sender, RoutedEventArgs e)
        {
            if(!int.TryParse(howMuchStressTxtbox.Text, out int howMuchResult))
            {
                howMuchResult = 5;
            }

            for (int index = 0; index < howMuchResult; index++)
            {
                try
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
                    Dictionary<int, string> indexFileName = new Dictionary<int, string>();

                    DownloadChunks(fileNameSelected, pathName);

                    await Task.Delay(1500);
                }
                catch(Exception ex)
                {
                    resultTxtBox.Text += $"{ex.Message}" + Environment.NewLine;

                }
            }
        }
    }
}
