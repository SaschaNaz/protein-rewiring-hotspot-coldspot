using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Life302
{
    public class DataSheet
    {
        public List<String> ColumnNames = new List<String>();

        Dictionary<String, List<Object>> DataLines = new Dictionary<String, List<Object>>();

        public DataSheet(params String[] columnNames)
        {
            ColumnNames.AddRange(columnNames);
        }

        public void AddDataForKey(String key, params Object[] items)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list))
            {
                list = new List<Object>();
                DataLines.Add(key, list);
            }

            //if (items.Length + list.Count > ColumnNames.Count)
            //    throw new Exception("You can't add more items with this key. Item count cannot be larger than the column count.");
            //else
            list.AddRange(items);
        }

        public void InsertDataForKey(String key, Int32 index, params Object[] items)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list))
            {
                list = new List<Object>();
                DataLines.Add(key, list);
            }

            //if (items.Length + list.Count > ColumnNames.Count)
            //    throw new Exception("You can't add more items with this key. Item count cannot be larger than the column count.");
            //else
            list.InsertRange(index, items);
        }

        public List<Object> GetData(String key)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list))
                return null;
            else return list;
        }

        public Boolean RemoveData(String key)
        {
            return DataLines.Remove(key);
        }

        public async Task SaveToFile(StorageFile file, Boolean toSort)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var writer = new DataWriter(stream))
                {
                    if (ColumnNames.Count > 0)
                    {
                        writer.WriteString(String.Join("\t", ColumnNames));
                        writer.WriteString("\n");
                    }
                    IDictionary<String, List<Object>> dictionary;
                    if (toSort)
                        dictionary = new SortedDictionary<String, List<Object>>(DataLines);
                    else
                        dictionary = DataLines;
                    foreach (KeyValuePair<String, List<Object>> line in dictionary)
                    {
                        writer.WriteString(line.Key);
                        writer.WriteString("\t");
                        foreach (Object o in line.Value)
                        {
                            writer.WriteString(o.ToString());
                            writer.WriteString("\t");
                        }
                        writer.WriteString("\n");
                    }
                    await writer.StoreAsync();
                }
            }
        }
    }
}
