﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Life302
{
    public static class DataProcessor
    {
        public async static Task<SortedSet<String>> readSimpleList(StorageFile file)
        {
            var list = new List<String>();
            var filestr = await FileIO.ReadLinesAsync(file);

            foreach (String line in filestr)
                list.Add(line);

            return new SortedSet<String>(list);
        }

        public async static Task saveSimpleList<T>(StorageFile file, IEnumerable<T> set)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var writer = new DataWriter(stream))
                {
                    foreach (T str in set)
                        writer.WriteString(String.Format("{0}\n", str));
                    await writer.StoreAsync();
                }
            }
        }
        //public static Int32 ComparePValueAnnotationPair(KeyValuePair<Double, String> x, KeyValuePair<Double, String> y)
        //{
        //    return x.Key.CompareTo(y.Key);
        //}
        
        public async static Task<SortedDictionary<String, Double>> readDavidProteinAnnotationResult(StorageFile file)
        {
            var dictionary = new Dictionary<String, Double>();
            
            foreach (String line in await FileIO.ReadLinesAsync(file))
            {
                var splitted = line.Split('\t');
                if (splitted.Length > 1 && !splitted[0].StartsWith("Annotation") && splitted[0] != "Category")
                    dictionary.Add(splitted[1].Replace(',', '/'), Convert.ToDouble(splitted[4]));
            }

            return new SortedDictionary<String, Double>(dictionary);
        }

        public async static Task<SortedDictionary<String, SortedSet<String>>> readDrosophilaNetwork(params StorageFile[] files)
        {
            var autolister = new AutoLister<String, String>();
            foreach (StorageFile file in files)
            {
                IList<String> str = await FileIO.ReadLinesAsync(file);
                for (Int32 i = 1; i < str.Count; i++)
                {
                    String[] splitted = str[i].Split('\t');
                    String firstgene = splitted[0];
                    String secondgene = splitted[1];

                    autolister.Add(firstgene, secondgene);
                    //if (dictionary.ContainsKey(firstgene))
                    //    dictionary[firstgene].Add(secondgene);
                    //else
                    //{
                    //    var newSet = dictionary[firstgene] = new SortedSet<String>();
                    //    newSet.Add(secondgene);
                    //}
                }
            }

            return autolister.GetSortedDictionary();
        }

        public async static Task<SortedDictionary<String, SortedSet<String>>> readHumanNetwork(Boolean deleteDot, params StorageFile[] files)
        {
            Func<String, String> func;
            if (deleteDot)
                func = delegate(String npstr) { return npstr.Split('.')[0]; };
            else
                func = delegate(String npstr) { return npstr; };
            var dictionary = new Dictionary<String, SortedSet<String>>();
            var autolister = new AutoLister<String, String>(dictionary);

            foreach (StorageFile file in files)
            {
                IList<String> str = await FileIO.ReadLinesAsync(file);
                for (Int32 i = 0; i < str.Count; i++)
                {
                    String[] splitted = str[i].Split('\t');
                    String firstprotein = func(splitted[2]);
                    String secondprotein = func(splitted[5]);

                    autolister.Add(firstprotein, secondprotein);
                    //if (dictionary.ContainsKey(firstprotein))
                    //    dictionary[firstprotein].Add(secondprotein);
                    //else
                    //{
                    //    var newSet = dictionary[firstprotein] = new SortedSet<String>();
                    //    newSet.Add(secondprotein);
                    //}
                }
            }

            return autolister.GetSortedDictionary();
        }

        public async static Task<SortedDictionary<String, String>> readOrtholog(StorageFile file, String referenceSpeciesName, String comparisonSpeciesName)
        {
            XDocument xdoc = XDocument.Parse(await FileIO.ReadTextAsync(file));

            var orthologReader = new OrthoXml.InParanoidReader(xdoc);

            return orthologReader.MapGeneToGeneSeedOrtholog(referenceSpeciesName, comparisonSpeciesName);
            //InParalog는 나중에 처리하고 일단 이건 보여주기용?
        }

        public async static Task saveStringDictionary<T1, T2>(StorageFile file, String firstColumnName, String secondColumnName, SortedDictionary<T1, T2> dictionary)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteString(
                        String.Format("{0},{1}\n", firstColumnName, secondColumnName));
                    foreach (KeyValuePair<T1, T2> pair in dictionary)
                        writer.WriteString(String.Format("{0},{1}\n", pair.Key, pair.Value));
                    await writer.StoreAsync();
                }
            }
        }

        public async static Task saveStringPairList<T1>(StorageFile file, String firstColumnName, String secondColumnName, List<KeyValuePair<T1, String>> pairlist)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteString(
                        String.Format("{0},{1}\n", firstColumnName, secondColumnName));
                    foreach (KeyValuePair<T1, String> pair in pairlist)
                        writer.WriteString(String.Format("{0},{1}\n", pair.Key, pair.Value));
                    await writer.StoreAsync();
                }
            }
        }

        public async static Task<SortedDictionary<String, SortedSet<String>>> readUniprotMapper(StorageFile file, String firstColumn, String SecondColumn, Boolean deleteDot)
        {
            Func<String, String> func;
            if (deleteDot)
                func = delegate(String npstr) { return npstr.Split('.')[0]; };
            else
                func = delegate(String npstr) { return npstr; };

            var refseqDictionary = new Dictionary<String, SortedSet<String>>();
            var ensembleproDictionary = new Dictionary<String, SortedSet<String>>();

            var dictionary = new SortedDictionary<String, SortedSet<String>>();

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
                //switch (splitted[1])
                //{
                //    case firstColumn:
                //    case "Ensembl_PRO":
                //        lock (newstr)
                //        {
                //            newstr.Add(str);
                //        }
                //        break;
                //}
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
                        try
                        {
                            dictionary.Add(str, list);
                        }
                        catch
                        {
                            foreach (String item in list)
                                dictionary[str].Add(item);
                        }
                }
            }

            return dictionary;
        }

        public async static Task<SortedDictionary<String, Double>> readHumanDndsToMouse(StorageFile file)
        {
            var dictionary = new Dictionary<String, Double>();
            var filestr = await FileIO.ReadLinesAsync(file);
            filestr.RemoveAt(0);
            foreach (String line in filestr)
            {
                var splitted = line.Split('\t');
                dictionary.Add(splitted[0], Convert.ToDouble(splitted[1]));
            }

            return new SortedDictionary<String, Double>(dictionary);
        }

        public async static Task saveStringSetDictionary<T1>(StorageFile file, String firstColumnName, String secondColumnName, SortedDictionary<T1, SortedSet<String>> dictionary)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteString(
                        String.Format("{0},{1}\n", firstColumnName, secondColumnName));
                    foreach (KeyValuePair<T1, SortedSet<String>> pair in dictionary)
                        writer.WriteString(String.Format("{0},{1}\n", pair.Key, String.Join(",", pair.Value)));
                    await writer.StoreAsync();
                }
            }
        }

        public async static Task saveStringSetDictionarySpread<T1, T2>(StorageFile file, String firstColumnName, String secondColumnName, SortedDictionary<T1, SortedSet<T2>> dictionary)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteString(
                        String.Format("{0},{1}\n", firstColumnName, secondColumnName));
                    foreach (KeyValuePair<T1, SortedSet<T2>> pair in dictionary)
                        foreach (T2 secondItem in pair.Value)
                            writer.WriteString(String.Format("{0},{1}\n", pair.Key, secondItem));
                    await writer.StoreAsync();
                }
            }
        }
    }
}
