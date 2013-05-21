using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Life302
{
    public class DataManager : DependencyObject
    {
        SortedDictionary<String, SortedSet<String>> storedDrosophilaNetwork;
        public Boolean IsDrosophilaNetworkStored
        {
            get { return (Boolean)GetValue(DrosophilaNetworkStoredProperty); }
            set { SetValue(DrosophilaNetworkStoredProperty, value); }
        }
        public static readonly DependencyProperty DrosophilaNetworkStoredProperty
            = DependencyProperty.Register("IsDrosophilaNetworkStored", typeof(Boolean), typeof(DataManager), new PropertyMetadata(false));

        SortedDictionary<String, SortedSet<String>> storedHumanNetwork;
        public Boolean IsHumanNetworkStored
        {
            get { return (Boolean)GetValue(HumanNetworkStoredProperty); }
            set { SetValue(HumanNetworkStoredProperty, value); }
        }
        public static readonly DependencyProperty HumanNetworkStoredProperty
            = DependencyProperty.Register("IsHumanNetworkStored", typeof(Boolean), typeof(DataManager), new PropertyMetadata(false));

        SortedDictionary<String, String> storedDrosophilaToHumanOrtholog;
        public Boolean IsDrosophilaToHumanOrthologStored
        {
            get { return (Boolean)GetValue(DrosophilaToHumanOrthologStoredProperty); }
            set { SetValue(DrosophilaToHumanOrthologStoredProperty, value); }
        }
        public static readonly DependencyProperty DrosophilaToHumanOrthologStoredProperty
            = DependencyProperty.Register("IsDrosophilaToHumanOrthologStored", typeof(Boolean), typeof(DataManager), new PropertyMetadata(false));

        SortedDictionary<String, SortedSet<String>> storedUniprotMapper;
        public Boolean IsUniprotMapperStored
        {
            get { return (Boolean)GetValue(UniprotMapperStoredProperty); }
            set { SetValue(UniprotMapperStoredProperty, value); }
        }
        public static readonly DependencyProperty UniprotMapperStoredProperty
            = DependencyProperty.Register("IsUniprotMapperStored", typeof(Boolean), typeof(DataManager), new PropertyMetadata(false));

        SortedDictionary<String, SortedSet<String>> storedMappedOrtholog;
        public Boolean IsMappedOrthologStored
        {
            get { return (Boolean)GetValue(MappedOrthologStoredProperty); }
            set { SetValue(MappedOrthologStoredProperty, value); }
        }
        public static readonly DependencyProperty MappedOrthologStoredProperty
            = DependencyProperty.Register("IsMappedOrthologStored", typeof(Boolean), typeof(DataManager), new PropertyMetadata(false));

        SortedDictionary<String, String> storedValidOrtholog;
        public Boolean IsValidOrthologStored
        {
            get { return (Boolean)GetValue(ValidOrthologStoredProperty); }
            set { SetValue(ValidOrthologStoredProperty, value); }
        }
        public static readonly DependencyProperty ValidOrthologStoredProperty
            = DependencyProperty.Register("IsValidOrthologStored", typeof(Boolean), typeof(DataManager), new PropertyMetadata(false));

        SortedDictionary<UInt16, SortedSet<String>>[] storedRValue;
        public Boolean IsRValueStored
        {
            get { return (Boolean)GetValue(RValueStoredProperty); }
            set { SetValue(RValueStoredProperty, value); }
        }
        public static readonly DependencyProperty RValueStoredProperty
            = DependencyProperty.Register("IsRValueStored", typeof(Boolean), typeof(DataManager), new PropertyMetadata(false));

        public async Task<DataSheet> makeHotColdSpecifiedNetworks()
        {
            var datasheet = new DataSheet("Protein Name", "Rewiring Type", "Protein Interactions");
            
            var humanNetwork = await readHumanNetwork();
            //var drosophilaNetwork = await readDrosophilaNetwork();

            var humanHotspot = await readHumanHotspot();
            var humanColdspot = await readHumanColdspot();

            foreach (KeyValuePair<String, SortedSet<String>> pair in humanNetwork)
            {
                String nodot = pair.Key.Split('.')[0];
                if (humanHotspot.Contains(nodot))
                    datasheet.AddDataForKey(nodot, "Hotspot");
                else if (humanColdspot.Contains(nodot))
                    datasheet.AddDataForKey(nodot, "Coldspot");
                else
                    datasheet.AddDataForKey(nodot, "Unknown");
                datasheet.AddDataForKey(nodot, pair.Value.ToArray());
            }
            return datasheet;
        }

        public async Task<List<Double>[]> readHumanRvalueDndsPair()
        {
            //var rvalues = await readRValue();
            //var humanOrthologRvalue = rvalues[1];
            //var humanSpecificRvalue = rvalues[3];
            //var humanRvalueSpread = new Dictionary<String, UInt16>();
            //foreach (KeyValuePair<UInt16, SortedSet<String>> pair in humanOrthologRvalue)
            //    foreach (String str in pair.Value)
            //        humanRvalueSpread.Add(str, pair.Key);
            //foreach (KeyValuePair<UInt16, SortedSet<String>> pair in humanSpecificRvalue)
            //    foreach (String str in pair.Value)
            //        humanRvalueSpread.Add(str, pair.Key);
            //var humanRvalueSpreadSorted = new SortedDictionary<String, UInt16>(humanRvalueSpread);
            var humanHotspot = await readHumanHotspot();
            var humanColdspot = await readHumanColdspot();
            var mapper = await readUniprotMapperENSG();
            //var mapperReversedLister = new AutoLister<String, String>();
            //foreach (KeyValuePair<String, SortedSet<String>> pair in mapper)
            //    foreach (String str in pair.Value)
            //        mapperReversedLister.Add(str, pair.Key);
            //var mapperReversed = mapperReversedLister.GetSortedDictionary();
            
            
            var humanDndsToMouse = await readHumanDndsToMouse();
            var hotspotDnds = new List<Double>();
            var coldspotDnds = new List<Double>();
            foreach (KeyValuePair<String, Double> pair in humanDndsToMouse)
            {
                SortedSet<String> refseqIds;
                if (mapper.TryGetValue(pair.Key, out refseqIds) && refseqIds.Count == 1)
                {
                    String refseqId = refseqIds.First().Split('.')[0];
                    if (humanHotspot.Contains(refseqId))
                        hotspotDnds.Add(pair.Value);
                    else if (humanColdspot.Contains(refseqId))
                        coldspotDnds.Add(pair.Value);
                }
            }
            return new List<Double>[] { hotspotDnds, coldspotDnds };
        }

        public async Task<SortedSet<String>> readHumanHotspot()
        {
            StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("SecondaryData");
            var file = await folder.GetFileAsync("humanHotspot.txt");
            return await DataProcessor.readSimpleList(file);
        }

        public async Task<SortedSet<String>> readHumanColdspot()
        {
            StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("SecondaryData");
            var file = await folder.GetFileAsync("humanColdspot.txt");
            return await DataProcessor.readSimpleList(file);
        }

        public async Task readDavidResults()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add(".txt");

            //변수 네 가지에 BP를 불러들이고 annotation마다로 검색한 뒤 평균치 검사해 분류
            //아래거는 지우고
            //폴더 던져주면 drosophila - human 따로 변수 네 가지 불러오는 함수 만들기. loadBP, loadDomain 등으로

            var folder = await folderPicker.PickSingleFolderAsync();
            //var processfolder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                await readDavidPvalue(await folder.GetFolderAsync("Drosophila"));
                await readDavidPvalue(await folder.GetFolderAsync("Human"));
            }
        }

        async Task readDavidPvalue(StorageFolder folder)
        {
            SortedDictionary<String, Double> nonorthologColdspotPvalues;
            SortedDictionary<String, Double> nonorthologHotspotPvalues;
            SortedDictionary<String, Double> orthologColdspotPvalues;
            SortedDictionary<String, Double> orthologHotspotPvalues;

            {
                var childfolder = await folder.GetFolderAsync("nonortholog-coldspot");
                nonorthologColdspotPvalues = await DataProcessor.readDavidProteinAnnotationResult(await childfolder.GetFileAsync("bp.txt"));
            }
            {
                var childfolder = await folder.GetFolderAsync("nonortholog-hotspot");
                nonorthologHotspotPvalues = await DataProcessor.readDavidProteinAnnotationResult(await childfolder.GetFileAsync("bp.txt"));
            }
            {
                var childfolder = await folder.GetFolderAsync("ortholog-coldspot");
                orthologColdspotPvalues = await DataProcessor.readDavidProteinAnnotationResult(await childfolder.GetFileAsync("bp.txt"));
            }
            {
                var childfolder = await folder.GetFolderAsync("ortholog-hotspot");
                orthologHotspotPvalues = await DataProcessor.readDavidProteinAnnotationResult(await childfolder.GetFileAsync("bp.txt"));
            }

            foreach (KeyValuePair<String, Double> pair in nonorthologColdspotPvalues)
            {
                Double value;
                if (orthologColdspotPvalues.TryGetValue(pair.Key, out value) && Math.Abs(Math.Log(pair.Value) - Math.Log(value)) < Math.Abs(Math.Log(Math.Min(pair.Value, value))) * 0.4)
                {
                }
            }
        }

        async Task readDavidFolder(StorageFolder folder, StorageFolder processfolder)
        {
            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                var processfile = await processfolder.CreateFileAsync(file.DisplayName + ".csv");
                await DataProcessor.saveStringDictionary(processfile, "Annotation Number", "p value", await DataProcessor.readDavidProteinAnnotationResult(file));
            }
            foreach (StorageFolder childfolder in await folder.GetFoldersAsync())
            {
                var processChildFolder = await processfolder.CreateFolderAsync(childfolder.Name);
                await readDavidFolder(childfolder, processChildFolder);
            }
        }

        public async Task<SortedDictionary<String, String>> readValidOrtholog()
        {
            if (storedValidOrtholog == null)
            {
                #region calculation
                var drosophila = await readDrosophilaNetwork();
                var human = await readHumanNetwork();
                var mapped = await readMappedOrtholog();
                var orthologsFilteredDrosophilaHuman = new Dictionary<String, String>();
                var orthologsAmbiguous = new Dictionary<String, SortedSet<String>>();

                System.Diagnostics.Debug.WriteLine("Started filtering ortholog data");
                var drosophilaGenes = drosophila.Keys;

                foreach (String drosophilaGene in drosophilaGenes)
                {
                    SortedSet<String> mappedOrthologs;
                    if (!mapped.TryGetValue(drosophilaGene, out mappedOrthologs))//check the gene is mapped as ortholog
                        continue;
                    else//filter the genes so that they exists in the human protein network data
                    {
                        var checkedOrthologs = new SortedSet<String>();
                        foreach (String tocheck in mappedOrthologs)
                            if (human.ContainsKey(tocheck))
                                checkedOrthologs.Add(tocheck);
                        if (checkedOrthologs.Count == 1)
                        {
                            orthologsFilteredDrosophilaHuman.Add(drosophilaGene, checkedOrthologs.First());
                        }
                        else if (checkedOrthologs.Count > 1)
                            orthologsAmbiguous.Add(drosophilaGene, checkedOrthologs);
                    }
                }

                storedValidOrtholog = new SortedDictionary<String, String>(orthologsFilteredDrosophilaHuman);
                #endregion
                IsValidOrthologStored = true;
            }

            return storedValidOrtholog;
        }

        public async Task saveRvalueDndsSpread()
        {
            var RvalueDnds = await readHumanRvalueDndsPair();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveSimpleList(savefile, RvalueDnds[0]);
            }, "RvalueDndsHotspot");
            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveSimpleList(savefile, RvalueDnds[1]);
            }, "RvalueDndsColdspot");
        }

        public async Task saveValidOrtholog()
        {
            var ortholog = await readValidOrtholog();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringDictionary(savefile, "Protein", "Otholog protein", ortholog);
            }, "ValidOrtholog");
        }

        public async Task<SortedDictionary<UInt16, SortedSet<String>>[]> readRValue()
        {
            if (storedRValue == null)
            {
                #region calculation
                System.Diagnostics.Debug.WriteLine("Started reading drosophila network");
                var drosophila = await readDrosophilaNetwork();
                System.Diagnostics.Debug.WriteLine("Started reading human network");
                var human = await readHumanNetwork();

                var orthologsFilteredDrosophilaHuman = await readValidOrtholog();

                System.Diagnostics.Debug.WriteLine("Started calculating r values");

                var humanRemainingGenes = human.Keys.ToList();

                var orthologsRValueList = new AutoLister<UInt16, String>();
                var orthologsRValueListHuman = new AutoLister<UInt16, String>();
                var drosophilaSpecificRValueList = new AutoLister<UInt16, String>();
                var humanSpecificRValueList = new AutoLister<UInt16, String>();

                foreach (KeyValuePair<String, SortedSet<String>> drosophilaPpi in drosophila)
                {
                    String humanOrtholog;
                    if (orthologsFilteredDrosophilaHuman.TryGetValue(drosophilaPpi.Key, out humanOrtholog))
                    {
                        SortedSet<String> drosophilaInteractionsOrthologMapped = new SortedSet<String>();
                        SortedSet<String> humanInteractions = human[humanOrtholog];
                        humanRemainingGenes.Remove(humanOrtholog);
                        foreach (String ppi in drosophilaPpi.Value)
                        {
                            String mappedInteraction;
                            if (orthologsFilteredDrosophilaHuman.TryGetValue(ppi, out mappedInteraction))
                                drosophilaInteractionsOrthologMapped.Add(mappedInteraction);
                        }
                        drosophilaInteractionsOrthologMapped.IntersectWith(humanInteractions);

                        orthologsRValueList.Add(
                            (UInt16)(drosophilaPpi.Value.Count + humanInteractions.Count - drosophilaInteractionsOrthologMapped.Count * 2), drosophilaPpi.Key);
                        orthologsRValueListHuman.Add(
                            (UInt16)(drosophilaPpi.Value.Count + humanInteractions.Count - drosophilaInteractionsOrthologMapped.Count * 2), humanOrtholog);
                    }
                    else // drosophila specific
                        drosophilaSpecificRValueList.Add((UInt16)drosophilaPpi.Value.Count, drosophilaPpi.Key);
                }
                foreach (String humanRemainingGene in humanRemainingGenes)
                    humanSpecificRValueList.Add((UInt16)human[humanRemainingGene].Count, humanRemainingGene);

                storedRValue = new SortedDictionary<UInt16, SortedSet<String>>[]
                { 
                    orthologsRValueList.GetSortedDictionary(),
                    orthologsRValueListHuman.GetSortedDictionary(),
                    drosophilaSpecificRValueList.GetSortedDictionary(), 
                    humanSpecificRValueList.GetSortedDictionary()
                };
                #endregion
                IsRValueStored = true;
            }

            return storedRValue;
        }

        public async Task saveRValue()
        {
            var rvalues = await readRValue();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "r value", "Orthologs by Drosophila Gene ID", rvalues[0]);
            }, "OrthologDrosophilaIdRValue");
            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "r value", "Orthologs by Human Gene ID", rvalues[1]);
            }, "OrthologHumanIdRValue");
            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "r value", "Drosophila Specific Genes", rvalues[2]);
            }, "DrosophilaRValue");
            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "r value", "Human Specific Proteins", rvalues[3]);
            }, "HumanRValue");
        }

        public async Task CsvFileSave(Action<StorageFile> action, String filename)
        {
            await basicFileSave(action, filename, "CSV Spreadsheet format", ".csv");
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

        public async Task saveRValueSpread()
        {
            var rvalues = await readRValue();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionarySpread(savefile, "r value", "Orthologs by Drosophila Gene ID", rvalues[0]);
            }, "OrthologDrosophilaIdRValueSpread");
            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionarySpread(savefile, "r value", "Orthologs by Human Gene ID", rvalues[1]);
            }, "OrthologHumanIdRValueSpread");
            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionarySpread(savefile, "r value", "Drosophila Specific Genes", rvalues[2]);
            }, "DrosophilaRValueSpread");
            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionarySpread(savefile, "r value", "Human Specific Proteins", rvalues[3]);
            }, "HumanRValueSpread");
        }

        public async Task<SortedDictionary<String, SortedSet<String>>> readMappedOrtholog()
        {
            if (storedMappedOrtholog == null)
            {
                #region calculation
                var mapper = await readUniprotMapper();
                var ortholog = await readDrosophilaToHumanOrtholog();

                var newlyMappedOrtholog = new SortedDictionary<String, SortedSet<String>>();
                SortedSet<String> notMapped = new SortedSet<String>();
                foreach (KeyValuePair<String, String> pair in ortholog)
                {
                    SortedSet<String> mappedProteins;
                    if (mapper.TryGetValue(pair.Value, out mappedProteins))
                        newlyMappedOrtholog.Add(pair.Key, mappedProteins);
                    else
                        notMapped.Add(pair.Value);
                }

                storedMappedOrtholog = newlyMappedOrtholog;
                #endregion
                IsMappedOrthologStored = true;
            }

            return storedMappedOrtholog;
        }

        public async Task<SortedDictionary<String, Double>> readHumanDndsToMouse()
        {
            StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("HumanDnds");
            StorageFile file = (await folder.GetFilesAsync())[0];
            return await DataProcessor.readHumanDndsToMouse(file);   
        }

        public async Task saveMappedOrtholog()
        {
            var mapped = await readMappedOrtholog();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "Drosophila Protein", "Human Ortholog Proteins", mapped);
            }, "MappedOrtholog");
        }

        public async Task<SortedDictionary<String, SortedSet<String>>> readDrosophilaNetwork()
        {
            if (storedDrosophilaNetwork == null)
            {
                #region calculation
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("DroID");
                var files = await folder.GetFilesAsync();
                storedDrosophilaNetwork = await DataProcessor.readDrosophilaNetwork(files.ToArray());
                #endregion
                IsDrosophilaNetworkStored = true;
            }

            return storedDrosophilaNetwork;
        }

        public async Task saveDrosophilaNetwork()
        {
            var network = await readDrosophilaNetwork();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "Drosophila Protein", "Human Ortholog Protein", network);
            }, "DrosophilaNetwork");
        }

        public async Task<SortedDictionary<String, SortedSet<String>>> readHumanNetwork()
        {
            if (storedHumanNetwork == null)
            {
                #region calculation
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("HPRD");
                StorageFile file = (await folder.GetFilesAsync())[0];
                storedHumanNetwork = await DataProcessor.readHumanNetwork(false, file);
                #endregion
                IsHumanNetworkStored = true;
            }

            return storedHumanNetwork;
        }

        public async Task saveHumanNetwork()
        {
            var network = await readHumanNetwork();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "Protein", "Protein Interaction List", network);
            }, "HumanNetwork");
        }

        public async Task<SortedDictionary<String, SortedSet<String>>> readUniprotMapper()
        {
            if (storedUniprotMapper == null)
            {
                #region calculation
                StorageFolder mapperfolder = await Package.Current.InstalledLocation.GetFolderAsync("Mapper");
                StorageFile mapperfile = await mapperfolder.GetFileAsync("HUMAN_9606_idmapping.txt");
                storedUniprotMapper = await DataProcessor.readUniprotMapper(mapperfile, "RefSeq", "Ensembl_PRO", false);
                #endregion
                IsUniprotMapperStored = true;
            }

            return storedUniprotMapper;
        }

        public async Task<SortedDictionary<String, SortedSet<String>>> readUniprotMapperENSG()
        {
            StorageFolder mapperfolder = await Package.Current.InstalledLocation.GetFolderAsync("Mapper");
            StorageFile mapperfile = await mapperfolder.GetFileAsync("HUMAN_9606_idmapping.txt");
            return await DataProcessor.readUniprotMapper(mapperfile, "RefSeq", "Ensembl", false);
        }

        public async Task saveUniprotMapper()
        {
            var mapper = await readUniprotMapper();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringSetDictionary(savefile, "Ensembl Protein", "RefSeq Proteins", mapper);
            }, "UniprotMapper");
        }

        public async Task<SortedDictionary<String, String>> readDrosophilaToHumanOrtholog()
        {
            if (storedDrosophilaToHumanOrtholog == null)
            {
                #region calculation
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("InParanoid");
                StorageFile file = (await folder.GetFilesAsync())[0];
                storedDrosophilaToHumanOrtholog = await DataProcessor.readOrtholog(file, "Drosophila melanogaster", "Homo Sapiens");
                #endregion
                IsDrosophilaToHumanOrthologStored = true;
            }

            return storedDrosophilaToHumanOrtholog;
        }

        public async Task saveDrosophilaToHumanOrtholog()
        {
            var ortholog = await readDrosophilaToHumanOrtholog();

            await CsvFileSave(async delegate(StorageFile savefile)
            {
                await DataProcessor.saveStringDictionary(savefile, "Gene Name", "Ortholog Gene Name", ortholog);
            }, "DrosophilaToHumanOrtholog");
        }
    }
}
