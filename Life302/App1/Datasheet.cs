using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Life302
{
    public interface IDatasheet
    {
        Int32 KeyCount { get; }
        Int32 DataItemCount { get; }
        void AddDataForKey(Object key, params Object[] items);
        void InsertDataForKey(Object key, Int32 index, params Object[] items);
        Boolean ContainsKey(Object key);
        List<Object> GetDataForKey(Object key);
        Boolean TryGetDataForKey(Object key, out List<Object> data);
        DataT GetSingleDataForKey<DataT>(Int32 index, Object key);
        Boolean TryGetSingleDataForKey<DataT>(Object key, out DataT data);
        Task SaveToFileAsync(StorageFile file, Boolean toSort);
    }

    public interface IDatasheet<T> : IDatasheet
    {
        Dictionary<T, List<Object>>.KeyCollection GetKeys();
        void AddDataForKey(T key, params Object[] items);
        void InsertDataForKey(T key, Int32 index, params Object[] items);
        Boolean ContainsKey(T key);
        List<Object> GetDataForKey(T key);
        Boolean TryGetDataForKey(T key, out List<Object> data);
        DataT GetSingleDataForKey<DataT>(Int32 index, T key);
        Boolean TryGetSingleDataForKey<DataT>(T key, out DataT data);
    }
    
    public class Datasheet<T> : IDatasheet<T>, IEnumerable
    {
        public List<String> ColumnNames = new List<String>();

        Dictionary<T, List<Object>> DataLines = new Dictionary<T, List<Object>>();
        List<IDictionary> dictionary = new List<IDictionary>();

        public Int32 DataItemCount { get; private set; }
        public Int32 KeyCount { get { return DataLines.Count; } }

        public Datasheet(params String[] columnNames)
        {
            ColumnNames.AddRange(columnNames);
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return DataLines.GetEnumerator();
        }

        public Dictionary<T, List<Object>>.KeyCollection GetKeys()
        {
            return DataLines.Keys;
        }

        public void AddDataForKey(T key, params Object[] items)
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
            DataItemCount += items.Length;
        }

        void IDatasheet.AddDataForKey(Object key, params Object[] items)
        {
            if (!(key is T)) throw new ArgumentException("key");
            this.AddDataForKey((T)key, items);
        }

        public void InsertDataForKey(T key, Int32 index, params Object[] items)
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
            DataItemCount += items.Length;
        }

        void IDatasheet.InsertDataForKey(Object key, Int32 index, params Object[] items)
        {
            if (!(key is T)) throw new ArgumentException("key");
            this.InsertDataForKey((T)key, index, items);
        }

        public Boolean ContainsKey(T key)
        {
            return DataLines.ContainsKey(key);
        }

        Boolean IDatasheet.ContainsKey(Object key)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.ContainsKey((T)key);
        }

        public List<Object> GetDataForKey(T key)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list))
                return null;
            else return list;
        }

        List<Object> IDatasheet.GetDataForKey(Object key)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.GetDataForKey((T)key);
        }

        public Boolean TryGetDataForKey(T key, out List<Object> data)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list))
            {
                data = null;
                return false;
            }
            else
            {
                data = list;
                return true;
            }
        }

        Boolean IDatasheet.TryGetDataForKey(Object key, out List<Object> data)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.TryGetDataForKey((T)key, out data);
        }

        public T2 GetSingleDataForKey<T2>(Int32 index, T key)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list) || list.Count - 1 < index)
                return default(T2);
            else return (T2)list[index];
        }

        T2 IDatasheet.GetSingleDataForKey<T2>(Int32 index, Object key)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.GetSingleDataForKey<T2>(index, (T)key);
        }

        public T2 GetSingleDataForKey<T2>(T key)
        {
            return GetSingleDataForKey<T2>(0, key);
        }

        public Boolean TryGetSingleDataForKey<T2>(Int32 index, T key, out T2 data)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list) || list.Count - 1 < index)
            {
                data = default(T2);
                return false;
            }
            else
            {
                data = (T2)list[index];
                return true;
            }
        }

        public Boolean TryGetSingleDataForKey<T2>(T key, out T2 data)
        {
            return TryGetSingleDataForKey<T2>(0, key, out data);
        }

        Boolean IDatasheet.TryGetSingleDataForKey<T2>(Object key, out T2 data)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.TryGetSingleDataForKey((T)key, out data);
        }

        public Boolean RemoveData(T key)
        {
            return DataLines.Remove(key);
        }

        public async Task SaveToFileAsync(StorageFile file, Boolean toSort)
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
                    IDictionary<T, List<Object>> dictionary;
                    if (toSort)
                        dictionary = new SortedDictionary<T, List<Object>>(DataLines);
                    else
                        dictionary = DataLines;
                    foreach (KeyValuePair<T, List<Object>> line in dictionary)
                    {
                        writer.WriteString(line.Key.ToString());
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
