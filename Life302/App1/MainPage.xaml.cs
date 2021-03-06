﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using System.Xml.Linq;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

/* 각 코드를 만들면 한번 쓰고 버리는 지금 방식에서
 * 어떤 작업을 하면 거기에 필요한 구성 성분을 각각 생성하고선 이를 전역 변수로 저장하고, 그 여부를 UI로 알 수 있도록 한다
 * 매번 다시 연산하지 않고 전역 변수에 저장된 걸 사용
 * 필요한 게 없으면 생성한다
 * 불러오는 함수를 만들자, 없으면 만들어서 저장한 뒤 리턴하고 있으면 그대로 리턴하는 함수 - 최대한 코드를 기존 그대로 쓸 수 있게끔
 * UI에서 파일저장 작업 자유롭게 할 수 있도록 만들기
 */

namespace Life302
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public DataManager manager = new DataManager();
        public BioDataProcessor processor = new BioDataProcessor();

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = manager;
            Process();
        }

        async void Process()
        {
            //await manager.readDavidResults();
            //await manager.saveRvalueDndsSpread();

            //var datasheet = await processor.ReadUniprotMapperAsync();
            //FileSavePicker picker = new FileSavePicker();
            //picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            //picker.FileTypeChoices.Add("datasheet text", new List<String> { ".txt" });
            //StorageFile savefile = await picker.PickSaveFileAsync();
            //if (savefile != null)
            //{
            //    await datasheet.SaveToFileAsync(savefile, true);
            //    await new MessageDialog("Completed").ShowAsync();
            //}
            //else
            //{
            //    await new MessageDialog("Canceled").ShowAsync();
            //}
            //await TxtFileSave(async delegate(StorageFile savefile)
            //{
            //    var datasheet = await processor.GetRvalueForDrosophilaSpecific();
            //    await datasheet.SaveToFileAsync(savefile, true);
            //}, "RvalueForDrosophilaSpecific");
            //await TxtFileSave(async delegate(StorageFile savefile)
            //{
            //    var datasheet = await processor.GetRvalueForHumanSpecific();
            //    await datasheet.SaveToFileAsync(savefile, true);
            //}, "RvalueForHumanSpecific");
            //await TxtFileSave(async delegate(StorageFile savefile)
            //{
            //    var datasheet = await processor.GetRvalueForOrthologsByDrosophilaId();
            //    await datasheet.SaveToFileAsync(savefile, true);
            //}, "RvalueForOrthologsByDrosophilaId");
            //await TxtFileSave(async delegate(StorageFile savefile)
            //{
            //    var datasheet = await processor.GetRvalueForOrthologsByHumanId();
            //    await datasheet.SaveToFileAsync(savefile, true);
            //}, "RvalueForOrthologsByHumanId");
            //await manager.saveDrosophilaToHumanOrtholog();
            
            //StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("DroID");
            //var files = await folder.GetFilesAsync();
            //await BioDataReader.readDrosophilaNetworkAsync(files.ToArray());

            //StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("HPRD");
            //StorageFile file = (await folder.GetFilesAsync())[0];
            //await BioDataReader.readHumanNetworkAsync(false, file);

            //StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("InParanoid");
            //StorageFile file = (await folder.GetFilesAsync())[0];
            //var abc = await BioDataReader.readOrtholog(file, "Drosophila melanogaster", "Homo Sapiens");

            //StorageFolder mapperfolder = await Package.Current.InstalledLocation.GetFolderAsync("Mapper");
            //StorageFile mapperfile = await mapperfolder.GetFileAsync("HUMAN_9606_idmapping.txt");
            //await BioDataReader.readUniprotMapperAsync(mapperfile, "RefSeq", "Ensembl_PRO", false);

            //await processor.readDrosophilaToHumanOrthologAsync();

            //await processor.GetMappedOrthologAsync();

            //await processor.GetValidOrthologAsync();
            //var a = await manager.readValidOrtholog();

            //await manager.readRValue();
            //await processor.MakeRValueAsync();
            //await TxtFileSave(async delegate(StorageFile savefile)
            //{
            //    var datasheet = await processor.GetRewiringClassificationDrosophilaAsync();//await manager.makeHotColdSpecifiedNetworks();//
            //    await datasheet.SaveToFileAsync(savefile);
            //}, "");
            //await TxtFileSave(async delegate(StorageFile savefile)
            //{
            //    var datasheet = await processor.GetRewiringClassifiedMeanBetweennessHumanAsync();
            //    await datasheet.SaveToFileAsync(savefile);
            //}, "");
            await manager.readDavidResults();
        }

        public async Task TxtFileSave(Action<StorageFile> action, String filename)
        {
            await basicFileSave(action, filename, "Basic text format", ".txt");
        }

        async Task basicFileSave(Action<StorageFile> action, String filename, String filetypeExplanation, String filetype)
        {
            FileSavePicker picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add(filetypeExplanation, new List<String> { filetype });
            picker.SuggestedFileName = filename;
            StorageFile savefile = await picker.PickSaveFileAsync();
            if (savefile != null)
            {
                action(savefile);
                await new MessageDialog("Completed").ShowAsync();
            }
            else
            {
                await new MessageDialog("Canceled").ShowAsync();
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private async void SaveDrosophilaPPINetwork(object sender, RoutedEventArgs e)
        {
            await manager.saveDrosophilaNetwork();
        }

        private async void SaveHumanPPINetwork(object sender, RoutedEventArgs e)
        {
            await manager.saveHumanNetwork();
        }

        private async void SaveDrosophilaToHumanOrtholog(object sender, RoutedEventArgs e)
        {
            await manager.saveDrosophilaToHumanOrtholog();
        }

        private async void SaveUniprotMapper(object sender, RoutedEventArgs e)
        {
            await manager.saveUniprotMapper();
        }

        private async void SaveMappedOrtholog(object sender, RoutedEventArgs e)
        {
            await manager.saveMappedOrtholog();
        }

        private async void SaveValidOrtholog(object sender, RoutedEventArgs e)
        {
            await manager.saveValidOrtholog();
        }

        private async void SaveRValue(object sender, RoutedEventArgs e)
        {
            await manager.saveRValue();
        }

        private async void SaveRValueSpread(object sender, RoutedEventArgs e)
        {
            await manager.saveRValueSpread();
        }

        private async void LoadDrosophilaPPINetwork(object sender, RoutedEventArgs e)
        {
            await manager.readDrosophilaNetwork();
        }

        private async void LoadHumanPPINetwork(object sender, RoutedEventArgs e)
        {
            await manager.readHumanNetwork();
        }

        private async void LoadDrosophilaToHumanOrtholog(object sender, RoutedEventArgs e)
        {
            await manager.readDrosophilaToHumanOrtholog();
        }

        private async void LoadUniprotMapper(object sender, RoutedEventArgs e)
        {
            await manager.readUniprotMapper();
        }

        private async void LoadMappedOrtholog(object sender, RoutedEventArgs e)
        {
            await manager.readMappedOrtholog();
        }

        private async void LoadValidOrtholog(object sender, RoutedEventArgs e)
        {
            await manager.readValidOrtholog();
        }

        private async void LoadRValue(object sender, RoutedEventArgs e)
        {
            await manager.readRValue();
        }
    }
}
