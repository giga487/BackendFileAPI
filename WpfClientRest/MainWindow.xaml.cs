using System;
using System.Collections.Generic;
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
            var result = await client.CreateRequest<List<ApiFileInfo>>(RestClientDll.RestRequestType.Get, "api/File", "List");
            Dict.Clear();

            resultTxtBox.Text += $"Found {result.Count} elements"+Environment.NewLine;
            foreach (var file in result)
            {
                resultTxtBox.Text += $"{file.Filename} => {file.Dim} bytes" + Environment.NewLine;
                Dict[file.Filename] = file;

                fileCombobox.Items.Add(file.Filename);
            }

            if(fileCombobox.Items.Count != 0)
                fileCombobox.SelectedIndex = 0;
        }

        private async void downloadBtn_Click(object sender, RoutedEventArgs e)
        { 
            string fileNameSelected = (string)fileCombobox.SelectedItem;

            string requestString = $"/api/File/DownloadFile?fileName={fileNameSelected}";
            byte[] file = await client.DownloadRequest(requestString);
            string md5 = FileHelper.Md5Result(file);

            bool result = md5 == Dict[fileNameSelected].MD5 ? true : false;

            resultTxtBox.Text += $"{fileNameSelected} => Same: {result}" + Environment.NewLine;

            await FileHelper.MakeFile(file, fileNameSelected, "", true);
        }

        private void createFileBtn_Click(object sender, RoutedEventArgs e)
        {
            FileHelper.MakeTestFile(1024 * 1024 * 1024, Directory.GetCurrentDirectory());
        }
    }
}
