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
    public enum DatasheetAdjustment
    {
        Sort, SortAndFilterDuplication
    }

    public interface IDatasheet
    {
        Int32 KeyCount { get; }
        Int32 DataItemCount { get; }

        void AddDataForKey(Object key, params Object[] items);
        void InsertDataAttributeForKey(Int32 index, Object key, Object item);
        Boolean ContainsKey(Object key);
        List<DataT> GetDataForKey<DataT>(Object key);
        Boolean TryGetDataForKey<DataT>(Object key, out List<DataT> data);

        DataT GetSingleDataForKey<DataT>(Int32 index, Object key);
        Boolean TryGetSingleDataForKey<DataT>(Int32 index, Object key, out DataT data);
        DataT GetFirstDataForKey<DataT>(Object key);
        Boolean TryGetFirstDataForKey<DataT>(Object key, out DataT data);

        DataT GetDataAttributeForKey<DataT>(Int32 index, Object key);
        Boolean TryGetDataAttributeForKey<DataT>(Int32 index, Object key, out DataT data);

        Task SaveToFileAsync(StorageFile file);
        void AdjustData(DatasheetAdjustment adjust);
    }

    public interface IDatasheet<T> : IDatasheet
    {
        Dictionary<T, List<Object>>.KeyCollection GetKeys();

        void AddDataForKey(T key, params Object[] items);
        void InsertDataAttributeForKey(Int32 index, T key, Object item);
        Boolean ContainsKey(T key);
        List<DataT> GetDataForKey<DataT>(T key);
        Boolean TryGetDataForKey<DataT>(T key, out List<DataT> data);

        DataT GetSingleDataForKey<DataT>(Int32 index, T key);
        Boolean TryGetSingleDataForKey<DataT>(Int32 index, T key, out DataT data);
        DataT GetFirstDataForKey<DataT>(T key);
        Boolean TryGetFirstDataForKey<DataT>(T key, out DataT data);

        DataT GetDataAttributeForKey<DataT>(Int32 index, T key);
        Boolean TryGetDataAttributeForKey<DataT>(Int32 index, T key, out DataT data);
    }
    
    public class Datasheet<T> : IDatasheet<T>, IEnumerable
    {
        public List<String> ColumnNames = new List<String>();

        Dictionary<T, List<Object>> DataLines = new Dictionary<T, List<Object>>();
        List<IDictionary> DataAttributeColumns = new List<IDictionary>();

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

        public void InsertDataAttributeForKey(Int32 index, T key, Object item)
        {
            while (DataAttributeColumns.Count <= index)
                DataAttributeColumns.Add(new Dictionary<T, Object>());

            DataAttributeColumns[index][key] = item;
            DataItemCount += 1;
        }

        void IDatasheet.InsertDataAttributeForKey(Int32 index, Object key, Object data)
        {
            if (!(key is T)) throw new ArgumentException("key");
            this.InsertDataAttributeForKey(index, (T)key, data);
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

        public List<DataT> GetDataForKey<DataT>(T key)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list))
                return null;
            else return list.Cast<DataT>().ToList();
        }

        List<DataT> IDatasheet.GetDataForKey<DataT>(Object key)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.GetDataForKey<DataT>((T)key);
        }

        public Boolean TryGetDataForKey<DataT>(T key, out List<DataT> data)
        {
            List<Object> list;
            if (!DataLines.TryGetValue(key, out list))
            {
                data = null;
                return false;
            }
            else
            {
                data = list.Cast<DataT>().ToList();
                return true;
            }
        }

        Boolean IDatasheet.TryGetDataForKey<DataT>(Object key, out List<DataT> data)
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

        public T2 GetFirstDataForKey<T2>(T key)
        {
            return GetSingleDataForKey<T2>(0, key);
        }

        T2 IDatasheet.GetFirstDataForKey<T2>(Object key)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.GetFirstDataForKey<T2>((T)key);
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

        Boolean IDatasheet.TryGetSingleDataForKey<T2>(Int32 index, Object key, out T2 data)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.TryGetSingleDataForKey<T2>(index, (T)key, out data);
        }

        public Boolean TryGetFirstDataForKey<T2>(T key, out T2 data)
        {
            return TryGetSingleDataForKey<T2>(0, key, out data);
        }

        Boolean IDatasheet.TryGetFirstDataForKey<T2>(Object key, out T2 data)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.TryGetFirstDataForKey((T)key, out data);
        }

        public T2 GetDataAttributeForKey<T2>(Int32 index, T key)
        {
            Object data;
            if (DataAttributeColumns.Count <= index || !(DataAttributeColumns[index] as Dictionary<T, Object>).TryGetValue(key, out data))
                return default(T2);
            else return (T2)data;
        }

        T2 IDatasheet.GetDataAttributeForKey<T2>(Int32 index, Object key)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.GetDataAttributeForKey<T2>(index, (T)key);
        }

        public Boolean TryGetDataAttributeForKey<T2>(Int32 index, T key, out T2 data)
        {
            Object odata;
            if (DataAttributeColumns.Count <= index || !(DataAttributeColumns[index] as Dictionary<T, Object>).TryGetValue(key, out odata))
            {
                data = default(T2);
                return false;
            }
            else
            {
                data = (T2)odata;
                return true;
            }
        }

        Boolean IDatasheet.TryGetDataAttributeForKey<T2>(Int32 index, Object key, out T2 data)
        {
            if (!(key is T)) throw new ArgumentException("key");
            return this.TryGetDataAttributeForKey(index, (T)key, out data);
        }

        public Boolean RemoveData(T key)
        {
            return DataLines.Remove(key);
        }

        public async Task SaveToFileAsync(StorageFile file)
        {
            SortedSet<T> keys = new SortedSet<T>(DataLines.Keys);
            foreach (Dictionary<T, Object> column in DataAttributeColumns)
                keys.UnionWith(column.Keys);
            using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var writer = new DataWriter(stream))
                {
                    if (ColumnNames.Count > 0)
                    {
                        writer.WriteString(String.Join("\t", ColumnNames));
                        writer.WriteString("\n");
                    }

                    foreach (T key in keys)
                    {
                        writer.WriteString(key.ToString());
                        foreach (Dictionary<T, Object> column in DataAttributeColumns)
                        {
                            writer.WriteString("\t");
                            Object dataInColumn;
                            if (column.TryGetValue(key, out dataInColumn))
                                writer.WriteString(((T)dataInColumn).ToString());
                        }
                        List<Object> line;
                        if (DataLines.TryGetValue(key, out line))
                        {
                            foreach (Object o in line)
                            {
                                writer.WriteString("\t");
                                writer.WriteString(o.ToString());
                            }
                        }
                        writer.WriteString("\n");
                    }
                    await writer.StoreAsync();
                }
            }
        }

        public void AdjustData(DatasheetAdjustment adjust)
        {
            switch (adjust)
            {
                case DatasheetAdjustment.Sort:
                    {
                        foreach (List<Object> list in DataLines.Values)
                            list.Sort();
                        break;
                    }
                case DatasheetAdjustment.SortAndFilterDuplication:
                    {
                        Object[] temp;
                        foreach (List<Object> list in DataLines.Values)
                        {
                            list.Sort();
                            temp = list.Distinct().ToArray();
                            list.Clear();
                            list.AddRange(temp);
                        }
                        break;
                    }
            }
        }
    }
}
