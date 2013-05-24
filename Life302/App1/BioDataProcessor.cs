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
    public enum BioDataType
    {
        DrosophilaNetwork,
        HumanNetwork,
        DrosophilaToHumanOrtholog,
        UniprotMapper,
        MappedOrtholog,
        ValidOrtholog,
        RvalueForOrthologsByDrosophilaId,
        RvalueForOrthologsByHumanId,
        RvalueForDrosophilaSpecific,
        RvalueForHumanSpecific
    }

    delegate void DatasheetAddedEventHandler(object sender, DatasheetAddedEventArgs e);

    public class DatasheetAddedEventArgs : EventArgs
    {
        public BioDataType Key;
        public IDatasheet AddedDatasheet;

        public DatasheetAddedEventArgs(BioDataType key, IDatasheet datasheet)
        {
            Key = key;
            AddedDatasheet = datasheet;
        }
    }

    public class BioDataProcessor
    {
        event DatasheetAddedEventHandler DatasheetAdded;
        protected virtual void OnDatasheetAdded(DatasheetAddedEventArgs e)
        {
            if (DatasheetAdded != null)
            {
                DatasheetAdded(this, e);
            }
        }

        SortedDictionary<BioDataType, IDatasheet> DatasheetBase = new SortedDictionary<BioDataType, IDatasheet>();

        void RememberDatasheet(BioDataType key, IDatasheet datasheet)
        {
            if (!DatasheetBase.ContainsKey(key))
            {
                DatasheetBase.Add(key, datasheet);
                OnDatasheetAdded(new DatasheetAddedEventArgs(key, datasheet));
            }
            else
                DatasheetBase[key] = datasheet;
        }

        public async Task<Datasheet<String>> ReadDrosophilaNetworkAsync()
        {
            IDatasheet datasheet;
            BioDataType datatype = BioDataType.DrosophilaNetwork;
            if (!DatasheetBase.TryGetValue(datatype, out datasheet))
            {
                #region calculation
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("DroID");
                var files = await folder.GetFilesAsync();
                datasheet = await BioDataReader.readDrosophilaNetworkAsync(files.ToArray());
                #endregion
                RememberDatasheet(datatype, datasheet);
            }

            return datasheet as Datasheet<String>;
        }

        public async Task<Datasheet<String>> ReadHumanNetworkAsync()
        {
            IDatasheet datasheet;
            BioDataType datatype = BioDataType.HumanNetwork;
            if (!DatasheetBase.TryGetValue(datatype, out datasheet))
            {
                #region calculation
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("HPRD");
                StorageFile file = (await folder.GetFilesAsync())[0];
                datasheet = await BioDataReader.readHumanNetworkAsync(false, file);
                #endregion
                RememberDatasheet(datatype, datasheet);
            }

            return datasheet as Datasheet<String>;
        }

        public async Task<Datasheet<String>> ReadDrosophilaToHumanOrthologAsync()
        {
            IDatasheet datasheet;
            BioDataType datatype = BioDataType.DrosophilaToHumanOrtholog;
            if (!DatasheetBase.TryGetValue(datatype, out datasheet))
            {
                #region calculation
                StorageFolder folder = await Package.Current.InstalledLocation.GetFolderAsync("InParanoid");
                StorageFile file = (await folder.GetFilesAsync())[0];
                datasheet = await BioDataReader.readOrthologAsync(file, "Drosophila melanogaster", "Homo Sapiens");
                #endregion
                RememberDatasheet(datatype, datasheet);
            }

            return datasheet as Datasheet<String>;
        }

        public async Task<Datasheet<String>> ReadUniprotMapperAsync()
        {
            IDatasheet datasheet;
            BioDataType datatype = BioDataType.UniprotMapper;
            if (!DatasheetBase.TryGetValue(datatype, out datasheet))
            {
                #region calculation
                StorageFolder mapperfolder = await Package.Current.InstalledLocation.GetFolderAsync("Mapper");
                StorageFile mapperfile = await mapperfolder.GetFileAsync("HUMAN_9606_idmapping.txt");
                datasheet = await BioDataReader.readUniprotMapperAsync(mapperfile, "RefSeq", "Ensembl_PRO", false);
                #endregion
                RememberDatasheet(datatype, datasheet);
            }

            return datasheet as Datasheet<String>;
        }

        public async Task<Datasheet<String>> GetMappedOrthologAsync()
        {
            IDatasheet datasheet;
            BioDataType datatype = BioDataType.MappedOrtholog;
            if (!DatasheetBase.TryGetValue(datatype, out datasheet))
            {
                #region calculation
                var mapper = await ReadUniprotMapperAsync();
                var ortholog = await ReadDrosophilaToHumanOrthologAsync();

                var newlyMappedOrtholog = new Datasheet<String>();
                var notMapped = new SortedSet<String>();
                foreach (KeyValuePair<String, List<Object>> pair in ortholog)
                {
                    List<Object> mappedProteins;
                    if (mapper.TryGetDataForKey((String)pair.Value[0], out mappedProteins))
                        newlyMappedOrtholog.AddDataForKey(pair.Key, mappedProteins.ToArray());
                    else
                        notMapped.Add((String)pair.Value[0]);
                }

                datasheet = newlyMappedOrtholog;
                #endregion
                RememberDatasheet(datatype, datasheet);
            }

            return datasheet as Datasheet<String>;
        }

        public async Task<Datasheet<String>> GetValidOrthologAsync()
        {
            IDatasheet datasheet;
            BioDataType datatype = BioDataType.ValidOrtholog;
            if (!DatasheetBase.TryGetValue(datatype, out datasheet))
            {
                #region calculation
                var drosophila = await ReadDrosophilaNetworkAsync();
                var human = await ReadHumanNetworkAsync();
                var mapped = await GetMappedOrthologAsync();
                var orthologsFilteredDrosophilaHuman = new Datasheet<String>();
                var orthologsAmbiguous = new Dictionary<String, SortedSet<String>>();

                System.Diagnostics.Debug.WriteLine("Started filtering ortholog data");
                var drosophilaGenes = drosophila.GetKeys();

                foreach (String drosophilaGene in drosophilaGenes)
                {
                    List<Object> mappedOrthologs;
                    if (!mapped.TryGetDataForKey(drosophilaGene, out mappedOrthologs))//check the gene is mapped as ortholog
                        continue;
                    else//filter the genes so that they exists in the human protein network data
                    {
                        var checkedOrthologs = new SortedSet<String>();
                        foreach (String tocheck in mappedOrthologs)
                            if (human.ContainsKey(tocheck))
                                checkedOrthologs.Add(tocheck);
                        if (checkedOrthologs.Count == 1)
                            orthologsFilteredDrosophilaHuman.AddDataForKey(drosophilaGene, checkedOrthologs.First());
                        else if (checkedOrthologs.Count > 1)
                            orthologsAmbiguous.Add(drosophilaGene, checkedOrthologs);
                    }
                }

                datasheet = orthologsFilteredDrosophilaHuman;
                #endregion
                RememberDatasheet(datatype, datasheet);
            }

            return datasheet as Datasheet<String>;
        }

        public async Task MakeRValueAsync()
        {
            if (!DatasheetBase.ContainsKey(BioDataType.RvalueForDrosophilaSpecific)
                || !DatasheetBase.ContainsKey(BioDataType.RvalueForHumanSpecific)
                || !DatasheetBase.ContainsKey(BioDataType.RvalueForOrthologsByDrosophilaId)
                || !DatasheetBase.ContainsKey(BioDataType.RvalueForOrthologsByHumanId))
            {
                #region calculation
                System.Diagnostics.Debug.WriteLine("Started reading drosophila network");
                var drosophila = await ReadDrosophilaNetworkAsync();
                System.Diagnostics.Debug.WriteLine("Started reading human network");
                var human = await ReadHumanNetworkAsync();

                var orthologsFilteredDrosophilaHuman = await GetValidOrthologAsync();

                System.Diagnostics.Debug.WriteLine("Started calculating r values");

                var humanRemainingGenes = human.GetKeys().ToList();

                var orthologsRValueList = new Datasheet<UInt16>();
                var orthologsRValueListHuman = new Datasheet<UInt16>();
                var drosophilaSpecificRValueList = new Datasheet<UInt16>();
                var humanSpecificRValueList = new Datasheet<UInt16>();

                foreach (KeyValuePair<String, List<Object>> drosophilaPpi in drosophila)
                {
                    String humanOrtholog;
                    if (orthologsFilteredDrosophilaHuman.TryGetSingleDataForKey(drosophilaPpi.Key, out humanOrtholog))
                    {
                        SortedSet<String> drosophilaInteractionsOrthologMapped = new SortedSet<String>();
                        List<String> humanInteractions = human.GetDataForKey(humanOrtholog).Cast<String>().ToList();
                        humanRemainingGenes.Remove(humanOrtholog);
                        foreach (String ppi in drosophilaPpi.Value)
                        {
                            String mappedInteraction;
                            if (orthologsFilteredDrosophilaHuman.TryGetSingleDataForKey(ppi, out mappedInteraction))
                                drosophilaInteractionsOrthologMapped.Add(mappedInteraction);
                        }
                        drosophilaInteractionsOrthologMapped.IntersectWith(humanInteractions);
                        if (drosophilaInteractionsOrthologMapped.Count > 0)
                        {

                        }

                        orthologsRValueList.AddDataForKey(
                            (UInt16)(drosophilaPpi.Value.Count + humanInteractions.Count - drosophilaInteractionsOrthologMapped.Count * 2), drosophilaPpi.Key);
                        orthologsRValueListHuman.AddDataForKey(
                            (UInt16)(drosophilaPpi.Value.Count + humanInteractions.Count - drosophilaInteractionsOrthologMapped.Count * 2), humanOrtholog);
                    }
                    else // drosophila specific
                        drosophilaSpecificRValueList.AddDataForKey((UInt16)drosophilaPpi.Value.Count, drosophilaPpi.Key);
                }
                foreach (String humanRemainingGene in humanRemainingGenes)
                    humanSpecificRValueList.AddDataForKey((UInt16)human.GetDataForKey(humanRemainingGene).Count, humanRemainingGene);
                #endregion

                RememberDatasheet(BioDataType.RvalueForDrosophilaSpecific, drosophilaSpecificRValueList);
                RememberDatasheet(BioDataType.RvalueForHumanSpecific, humanSpecificRValueList);
                RememberDatasheet(BioDataType.RvalueForOrthologsByDrosophilaId, orthologsRValueList);
                RememberDatasheet(BioDataType.RvalueForOrthologsByHumanId, orthologsRValueListHuman);
            }
            //이러고, 4개 각 데이터시트 불러오는 함수 만들어서 데이터시트베이스에서 찾고 없으면 이 함수 부르기, 어차피 하나 없으면 다 없는거임 ㅇㅇ
        }

        public async Task<Datasheet<UInt16>> GetRvalueForDrosophilaSpecific()
        {
            BioDataType datatype = BioDataType.RvalueForDrosophilaSpecific;
            if (!DatasheetBase.ContainsKey(datatype))
                await MakeRValueAsync();
            return DatasheetBase[datatype] as Datasheet<UInt16>;
        }

        public async Task<Datasheet<UInt16>> GetRvalueForHumanSpecific()
        {
            BioDataType datatype = BioDataType.RvalueForHumanSpecific;
            if (!DatasheetBase.ContainsKey(datatype))
                await MakeRValueAsync();
            return DatasheetBase[datatype] as Datasheet<UInt16>;
        }

        public async Task<Datasheet<UInt16>> GetRvalueForOrthologsByDrosophilaId()
        {
            BioDataType datatype = BioDataType.RvalueForOrthologsByDrosophilaId;
            if (!DatasheetBase.ContainsKey(datatype))
                await MakeRValueAsync();
            return DatasheetBase[datatype] as Datasheet<UInt16>;
        }

        public async Task<Datasheet<UInt16>> GetRvalueForOrthologsByHumanId()
        {
            BioDataType datatype = BioDataType.RvalueForOrthologsByHumanId;
            if (!DatasheetBase.ContainsKey(datatype))
                await MakeRValueAsync();
            return DatasheetBase[datatype] as Datasheet<UInt16>;
        }
    }
}
