using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Life302
{
    class BioDataReader
    {
        public async static Task<Datasheet<String>> readDrosophilaNetworkAsync(params StorageFile[] files)
        {
            var datasheet = new Datasheet<String>();
            foreach (StorageFile file in files)
            {
                IList<String> str = await FileIO.ReadLinesAsync(file);
                for (Int32 i = 1; i < str.Count; i++)
                {
                    String[] splitted = str[i].Split('\t');
                    String firstgene = splitted[0];
                    String secondgene = splitted[1];

                    datasheet.AddDataForKey(firstgene, secondgene);
                }
            }

            datasheet.AdjustData(DatasheetAdjustment.SortAndFilterDuplication);
            return datasheet;
        }

        public async static Task<Datasheet<String>> readHumanNetworkAsync(Boolean deleteDot, params StorageFile[] files)
        {
            Func<String, String> func;
            if (deleteDot)
                func = delegate(String npstr) { return npstr.Split('.')[0]; };
            else
                func = delegate(String npstr) { return npstr; };
            Datasheet<String> datasheet = new Datasheet<String>();

            foreach (StorageFile file in files)
            {
                IList<String> str = await FileIO.ReadLinesAsync(file);
                for (Int32 i = 0; i < str.Count; i++)
                {
                    String[] splitted = str[i].Split('\t');
                    String firstprotein = func(splitted[2]);
                    String secondprotein = func(splitted[5]);

                    datasheet.AddDataForKey(firstprotein, secondprotein);
                }
            }

            datasheet.AdjustData(DatasheetAdjustment.SortAndFilterDuplication);
            return datasheet;
        }

        public async static Task<Datasheet<String>> readOrthologAsync(StorageFile file, String referenceSpeciesName, String comparisonSpeciesName)
        {
            XDocument xdoc = XDocument.Parse(await FileIO.ReadTextAsync(file));

            var orthologReader = new OrthoXml.InParanoidReader(xdoc);

            var datasheet = orthologReader.MapGeneToGeneSeedOrthologAsync(referenceSpeciesName, comparisonSpeciesName);
            datasheet.AdjustData(DatasheetAdjustment.SortAndFilterDuplication);
            return datasheet;
            //InParalog는 나중에 처리하고 일단 이건 보여주기용?
        }

        public async static Task<Datasheet<String>> readUniprotMapperAsync(StorageFile file, String firstColumn, String SecondColumn, Boolean deleteDot)
        {
            Func<String, String> func;
            if (deleteDot)
                func = delegate(String npstr) { return npstr.Split('.')[0]; };
            else
                func = delegate(String npstr) { return npstr; };

            var refseqDictionary = new Dictionary<String, SortedSet<String>>();
            var ensembleproDictionary = new Dictionary<String, SortedSet<String>>();

            var datasheet = new Datasheet<String>();

            IList<String> filestr = await FileIO.ReadLinesAsync(file);
            List<String> newstr = new List<String>();
            Parallel.ForEach(filestr, delegate(String str)
            {
                String[] splitted = str.Split('\t');
                if (splitted[1] == firstColumn || splitted[1] == SecondColumn)
                    lock (newstr)
                    {
                        newstr.Add(str);
                    }
            });
            SortedSet<String> sortedstr = new SortedSet<String>(newstr);

            String currentId;
            SortedSet<String> refseqList = new SortedSet<String>();
            SortedSet<String> ensembleproList = new SortedSet<String>();
            {
                String[] splitted = sortedstr.First().Split('\t');
                currentId = splitted[0];
                if (splitted[1] == firstColumn)
                    refseqList.Add(func(splitted[2]));
                else
                    ensembleproList.Add(splitted[2]);
            }
            foreach (String str in sortedstr)
            {
                String[] splitted = str.Split('\t');
                if (currentId != splitted[0])
                {
                    refseqDictionary.Add(currentId, refseqList);
                    ensembleproDictionary.Add(currentId, ensembleproList);

                    currentId = splitted[0];
                    refseqList = new SortedSet<String>();
                    ensembleproList = new SortedSet<String>();
                }

                if (splitted[1] == firstColumn)
                    refseqList.Add(func(splitted[2]));
                else
                    ensembleproList.Add(splitted[2]);
            }
            refseqDictionary.Add(currentId, refseqList);
            ensembleproDictionary.Add(currentId, ensembleproList);

            foreach (KeyValuePair<String, SortedSet<String>> pair in ensembleproDictionary)
            {
                SortedSet<String> list;
                if (!refseqDictionary.TryGetValue(pair.Key, out list))
                    break;

                foreach (String str in pair.Value)
                {
                    if (list.Count != 0)
                        datasheet.AddDataForKey(str, list.ToArray());
                }
            }

            datasheet.AdjustData(DatasheetAdjustment.SortAndFilterDuplication);
            return datasheet;
        }
    }
}
