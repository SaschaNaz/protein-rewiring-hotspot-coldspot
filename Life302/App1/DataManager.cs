﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;

namespace Life302
{
    public class DataManager
    {
        SortedDictionary<String, SortedSet<String>> storedDrosophilaNetwork;
        SortedDictionary<String, SortedSet<String>> storedHumanNetwork;
        SortedDictionary<String, String> storedDrosophilaToHumanOrtholog;
        SortedDictionary<String, SortedSet<String>> storedUniprotMapper;
        SortedDictionary<String, SortedSet<String>> storedMappedOrtholog;
        SortedDictionary<String, String> storedValidOrtholog;
        SortedDictionary<UInt16, SortedSet<String>>[] storedRValue;

        public async Task<SortedDictionary<String, String>> readValidOrtholog(SortedDictionary<String, SortedSet<String>> drosophila, SortedDictionary<String, SortedSet<String>> human)
        {
            if (storedValidOrtholog == null)
            {
                #region calculation
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
            }

            return storedValidOrtholog;
        }

        public async Task saveValidOrtholog()
        {
            var drosophila = await readDrosophilaNetwork();
            var human = await readHumanNetwork();
            var ortholog = await readValidOrtholog(drosophila, human);

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

                var orthologsFilteredDrosophilaHuman = await readValidOrtholog(drosophila, human);

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
            }

            return storedMappedOrtholog;
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
                storedUniprotMapper = await DataProcessor.readUniprotMapper(mapperfile, false);
                #endregion
            }

            return storedUniprotMapper;
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