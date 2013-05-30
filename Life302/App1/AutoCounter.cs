using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Life302
{
    public class AutoCounter<T1>
    {
        Dictionary<T1, Double> dictionary = new Dictionary<T1, Double>();

        public SortedDictionary<T1, Double> GetSortedDictionary()
        {
            return new SortedDictionary<T1, Double>(dictionary);
        }

        public void Increase(T1 item)
        {
            Double value;
            if (!dictionary.TryGetValue(item, out value))
                dictionary[item] = 1;
            else
                dictionary[item]++;
        }

        public void Increase(T1 item, Double increment)
        {
            Double value;
            if (!dictionary.TryGetValue(item, out value))
                dictionary[item] = increment;
            else
                dictionary[item] += increment;
        }

        public Datasheet<T1> ToDatasheet(params T1[] keys)
        {
            var datasheet = new Datasheet<T1>();
            foreach (T1 key in keys)
            {
                Double value;
                if (dictionary.TryGetValue(key, out value))
                    datasheet.AddDataForKey(key, value);
                else
                    datasheet.AddDataForKey(key, (Double)0);
            }
            return datasheet;
        }
    }
}
