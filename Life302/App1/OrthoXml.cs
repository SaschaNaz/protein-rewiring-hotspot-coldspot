using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Life302.OrthoXml
{
    public class Species
    {
        public String Name { get; private set; }
        public UInt32 NcbiTaxId { get; private set; }
        public readonly SortedDictionary<UInt32, Gene> dictionary = new SortedDictionary<UInt32, Gene>();

        public Species(XElement xspecies)
        {
            this.Name = (String)xspecies.Attribute("name");
            this.NcbiTaxId = (UInt32)xspecies.Attribute("NCBITaxId");
            foreach (XElement xgene in xspecies.Element("{http://orthoXML.org/2011/}database").Element("{http://orthoXML.org/2011/}genes").Elements("{http://orthoXML.org/2011/}gene"))
                dictionary.Add((UInt32)xgene.Attribute("id"), new Gene(xgene));
        }

        public override String ToString()
        {
            return Name;
        }
    }

    class OrthologDatabase
    {
        readonly List<OrthologGroup> infos = new List<OrthologGroup>();

        public OrthologGroup FindGeneOrtholog(String geneId)
        {
            return infos.Find(delegate (OrthologGroup info) { if (info.SeedOrthologContainsGene(geneId) || info.InParalogContainsGene(geneId)) return true; else return false; });
        }

        public OrthologGroup FindProteinOrtholog(String proteinId)
        {
            return infos.Find(delegate (OrthologGroup info) { if (info.SeedOrthologContainsProtein(proteinId) || info.InParalogContainsProtein(proteinId)) return true; else return false; });
        }

        public OrthologDatabase(XElement xorthologs, params Species[] allspecies)
        {
            foreach (XElement group in xorthologs.Elements("{http://orthoXML.org/2011/}orthologGroup"))
                infos.Add(new OrthologGroup(group, allspecies));
        }
    }

    class OrthologGroup
    {
        public Int32 BitScore { get; private set; }
        public readonly List<SeedOrtholog> SeedOrthologs = new List<SeedOrtholog>();
        public readonly List<InParalog> InParalogs = new List<InParalog>();

        public Boolean SeedOrthologContainsGene(String geneId)
        {
            foreach (SeedOrtholog ortholog in SeedOrthologs)
                if (ortholog.OrthologGene.GeneId == geneId)
                    return true;
            return false;
        }

        public Boolean InParalogContainsGene(String geneId)
        {
            foreach (InParalog inparalog in InParalogs)
                if (inparalog.OrthologGene.GeneId == geneId)
                    return true;
            return false;
        }

        public Boolean SeedOrthologContainsProtein(String proteinId)
        {
            foreach (SeedOrtholog ortholog in SeedOrthologs)
                if (ortholog.OrthologGene.ProteinId == proteinId)
                    return true;
            return false;
        }

        public Boolean InParalogContainsProtein(String proteinId)
        {
            foreach (InParalog inparalog in InParalogs)
                if (inparalog.OrthologGene.ProteinId == proteinId)
                    return true;
            return false;
        }

        public OrthologGroup(XElement xinfo, params Species[] allspecies)
        {
            BitScore = (Int32)xinfo.Element("{http://orthoXML.org/2011/}score").Attribute("value");

            foreach (XElement xgeneorthology in xinfo.Elements("{http://orthoXML.org/2011/}geneRef"))
            {
                if (xgeneorthology.Elements("{http://orthoXML.org/2011/}score").ToList()
                    .Find(delegate(XElement xgenescore)
                    {
                        if ((String)xgenescore.Attribute("id") == "bootstrap") return true; else return false;
                    }) != null)
                    SeedOrthologs.Add(new SeedOrtholog(xgeneorthology, allspecies));
                else
                    InParalogs.Add(new InParalog(xgeneorthology, allspecies));
            }
        }
    }

    public class SeedOrtholog
    {
        public Gene OrthologGene { get; private set; }
        public Species Origin { get; private set; }
        public Nullable<Double> BootstrapScore { get; private set; }

        public SeedOrtholog(XElement xorthology, params Species[] allspecies)
        {
            var scores = xorthology.Elements("{http://orthoXML.org/2011/}score").ToList();
            var bootstrap = scores.Find(delegate(XElement score) { if ((String)score.Attribute("id") == "bootstrap") return true; else return false; });
            if (bootstrap != null)
                BootstrapScore = (Double)bootstrap.Attribute("value");

            var id = (UInt32)xorthology.Attribute("id");
            foreach (Species species in allspecies)
            {
                if (species.dictionary.ContainsKey(id))
                {
                    OrthologGene = species.dictionary[id];
                    Origin = species;
                    break;
                }
            }
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", Origin.Name, OrthologGene.GeneId);
        }
    }

    public class InParalog
    {
        public Gene OrthologGene { get; private set; }
        public Species Origin { get; private set; }
        public Nullable<Double> InParalogScore { get; private set; }
        public Nullable<Double> BootstrapScore { get; private set; }

        public InParalog(XElement xorthology, params Species[] allspecies)
        {
            var scores = xorthology.Elements("{http://orthoXML.org/2011/}score").ToList();
            var paralog = scores.Find(delegate(XElement score) { if ((String)score.Attribute("id") == "inparalog") return true; else return false; });
            if (paralog != null)
                InParalogScore = (Double)paralog.Attribute("value");
            var bootstrap = scores.Find(delegate(XElement score) { if ((String)score.Attribute("id") == "bootstrap") return true; else return false; });
            if (bootstrap != null)
                BootstrapScore = (Double)bootstrap.Attribute("value");

            var id = (UInt32)xorthology.Attribute("id");
            foreach (Species species in allspecies)
            {
                if (species.dictionary.ContainsKey(id))
                {
                    OrthologGene = species.dictionary[id];
                    Origin = species;
                    break;
                }
            }
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", Origin.Name, OrthologGene.GeneId);
        }
    }

    public class Gene
    {
        public String GeneId { get; private set; }
        public String ProteinId { get; private set; }

        public Gene(XElement xgene)
        {
            GeneId = (String)xgene.Attribute("geneId");
            ProteinId = (String)xgene.Attribute("protId");
        }

        public override string ToString()
        {
            return GeneId;
        }
    }

    public class InParanoidReader
    {
        SortedDictionary<String, Species> speciesDictionary = new SortedDictionary<String, Species>();
        OrthologDatabase Database;

        public InParanoidReader(XDocument xdoc)
        {
            var orthoXML = xdoc.Element("{http://orthoXML.org/2011/}orthoXML");
            var xspeciesList = orthoXML.Elements("{http://orthoXML.org/2011/}species");

            foreach (XElement xspecies in xspeciesList)
            {
                Species species = new Species(xspecies);
                speciesDictionary.Add(species.Name, species);
            }

            Database = new OrthologDatabase(orthoXML.Element("{http://orthoXML.org/2011/}groups"), speciesDictionary.Values.ToArray());
        }

        public SortedDictionary<String, String> MapGeneToGeneSeedOrtholog(String referenceSpeciesName, String comparisonSpeciesName)
        {
            var dictionary = new Dictionary<String, String>();
            Species referenceSpecies = speciesDictionary[referenceSpeciesName];
            Species comparisonSpecies = speciesDictionary[comparisonSpeciesName];

            Gene[] genes = referenceSpecies.dictionary.Values.ToArray();
            Parallel.ForEach(genes, delegate(Gene gene)
            {
                OrthologGroup orthologGroup = Database.FindGeneOrtholog(gene.GeneId);
                if (orthologGroup != null)
                {
                    var referenceGene = orthologGroup.SeedOrthologs.Find(delegate(SeedOrtholog seed) { if (seed.Origin.Name == referenceSpeciesName) return true; else return false; });
                    var comparisonGene = orthologGroup.SeedOrthologs.Find(delegate(SeedOrtholog seed) { if (seed.Origin.Name == comparisonSpeciesName) return true; else return false; });
                    String referenceGeneId = referenceGene.OrthologGene.GeneId;
                    String comparisonProteinId = comparisonGene.OrthologGene.ProteinId;
                    //if (referenceSpeciesGeneMapper != null)
                    //    referenceGeneId = referenceSpeciesGeneMapper[referenceGeneId];
                    //if (comparisonSpeciesProteinMapper != null)
                    //    comparisonProteinId = comparisonSpeciesProteinMapper[comparisonProteinId];
                    lock (dictionary)
                    {
                        //SortedSet<String> orthologSet;
                        //if (!dictionary.TryGetValue(referenceGeneId, out orthologSet))
                        //{
                        //    orthologSet = new SortedSet<String>();
                        //    dictionary.Add(referenceGeneId, orthologSet);
                        //}
                        //orthologSet.Add(comparisonProteinId);
                        dictionary[referenceGeneId] = comparisonProteinId;
                    }
                }
                //orthologGroup.GeneOrthologDatabase.
            });

            return new SortedDictionary<String,String>(dictionary);
        }

        //MapGeneToGeneTotalOrtholog: 선형으로 만들 순 없고 네트워크 불러와서 노드 연결해주는 식이 필요함.

        public class GeneDictionary
        {
            List<Gene> genelist = new List<Gene>();

            //앙상블 진 아이디 및 그에 맞는 프로테인 아이디를 갖고 있다.
            //앙상블 진 아이디와 그에 맞는 NP 아이디를 일부 갖고 있다.
            //앙상블 프로테인 아이디와 그에 맞는 NP 아이디를 일부 갖고 있다.
            //이를 조합해 앙상블 진 아이디와 NP 아이디를 매핑할 수 있을 것
            //라고 생각했는데 조교님이 안되는건 그냥 넘기래요
            //ENSP쓰라는데 무슨말이지
        }
    }
}
