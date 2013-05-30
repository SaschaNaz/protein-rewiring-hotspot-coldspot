using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;
using QuickGraph.Algorithms;

namespace Life302
{
    class BetweennessCalculator
    {
        BidirectionalGraph<String, Edge<String>> biGraph = new BidirectionalGraph<String, Edge<String>>();
        Func<Edge<String>, double> edgeCost = e => 1;

        public BetweennessCalculator(Datasheet<String> network)
        {
            biGraph.AddVertexRange(network.GetKeys());
            foreach (KeyValuePair<String, List<Object>> pair in network)
                foreach (String target in pair.Value)
                    try
                    {
                        biGraph.AddEdge(new Edge<string>(pair.Key, target));
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine(String.Format("{0} was not in the network key list, added later", target));
                        biGraph.AddVertex(target);
                        biGraph.AddEdge(new Edge<string>(pair.Key, target));
                    }
        }

        public Datasheet<String> Calculate()
        {
            var counter = new AutoCounter<String>();
            var totalworks = Math.Pow(biGraph.VertexCount, 2);
            Double currentfinished = 0;

            Parallel.ForEach(biGraph.Vertices, (vertex) =>
            {
                var rootvertexDijkstra = biGraph.ShortestPathsDijkstra(edgeCost, vertex);
                var otherkeys = biGraph.Vertices.ToList();
                otherkeys.Remove(vertex);

                foreach (String otherkey in otherkeys)
                {
                    IEnumerable<Edge<String>> result;
                    List<Edge<String>> list;
                    if (rootvertexDijkstra.Invoke(otherkey, out result))
                    {
                        list = result.ToList();
                        lock (counter)
                        {
                            currentfinished++;
                            var count = list.RemoveAll((Edge<String> edge) =>
                            {
                                return edge.Target == otherkey;
                            });

                            if (count > 1)
                                foreach (Edge<String> edge in list)
                                    counter.Increase(edge.Target, 1 / (Double)count);
                            else
                                foreach (Edge<String> edge in list)
                                    counter.Increase(edge.Target);
                        }
                    }
                }
            });

            var datasheet = counter.ToDatasheet(biGraph.Vertices.ToArray());
            datasheet.AdjustData(DatasheetAdjustment.Sort);
            return datasheet;
        }
    }
}
