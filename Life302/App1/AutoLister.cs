using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Life302
{
    public class AutoLister<T1, T2>
    {
        Dictionary<T1, SortedSet<T2>> dictionary;
        UInt32 listedItemCount = 0;

        public AutoLister(Dictionary<T1, SortedSet<T2>> dictionary)
        {
            this.dictionary = dictionary;
            foreach (KeyValuePair<T1, SortedSet<T2>> pair in dictionary)
                listedItemCount += (UInt32)pair.Value.Count;
        }

        public AutoLister()
        {
            this.dictionary = new Dictionary<T1, SortedSet<T2>>();
        }

        public SortedDictionary<T1, SortedSet<T2>> GetSortedDictionary()
        {
            return new SortedDictionary<T1, SortedSet<T2>>(dictionary);
        }

        public void Add(T1 level, T2 item)
        {
            SortedSet<T2> levelset;
            if (!dictionary.TryGetValue(level, out levelset))
                levelset = dictionary[level] = new SortedSet<T2>();
            levelset.Add(item);
            listedItemCount++;
        }
    }
}
