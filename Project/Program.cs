using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

public class Program
{
    public static void Main(string[] args) {

        DirectoryInfo root = new DirectoryInfo(Directory.GetCurrentDirectory());
        if(root.FullName.Contains("\\bin")) {
            for(int i=0;i<4;i++) if (root.Parent != null) root = root.Parent;
        }
        
        Console.WriteLine($"Path: {root.FullName}");

        List<FileInfo> files = new List<FileInfo>();

        files.AddRange(root.GetFiles("*.log"));

        string[] regex = new string[] {
            "PAGAMENTO DE BOLETO EFETUADO - "  , "",
            "TRANSFERÊNCIA ENVIADA PELO PIX - ", "",
            ".*NUBANK REWARDS .*"              , "",
            ".*PAGAMENTO DE FATURA .*"         , "",
            ".*TRANSFERÊNCIA RECEBIDA .*"      , "",
            ".*IOF.*"                          , "",
            "\\+.*"                            , "",
            ".*PAGAMENTO DE FATURA .*"         , "",
            ".*PAGAMENTO EM .*"                , "",
            ".*TOTAL A PAGAR.*"                , "",
            "BRL.*"   , "",
            "USD.*"   , "",
            "CONVER.*", "",
            "\\*","",
            " ([a-zA-Z]{0,64}),"              ," $1 - ",
            "(.*)\t(.*)\t(.*)\t(.*)", "$1,$3,$4,$2",
            ",-", ",", 
            " JAN ", "/01/2023,",
            " FEV ", "/02/2023,",
            " MAR ", "/03/2023,",
            " ABR ", "/04/2023,",
            " MAI ", "/05/2023,",
            " JUN ", "/06/2023,",
            " JUL ", "/07/2023,",
            " AGO ", "/08/2023,",
            " SET ", "/09/2023,",
            " OUT ", "/10/2023,",
            " NOV ", "/11/2023,",
            " DEZ ", "/12/2023,",
            "(\n)([0-9]{1,4},[0-9]{0,4})"," $2",
            " ([0-9]{1,3}),"   , ",$1.",
            "\n,", ",",
            "\n\n{1,10}", "\n",
            "([0-9]{2}/[0-9]{2}/2023,)", "$1,",
            ",,([0-9A-Z]{1,8}-[0-9A-Z]{1,8}-[0-9A-Z]{1,8}-[0-9A-Z]{1,8}-[0-9A-Z]{1,12})",",$1",
            "(.*),(.*),(.*),(.*)", "$1,$2,$3,,$4",
            "\\n([0-9]{1,2}\\.{0,1}[0-9]{1,3},.*)", "$1"
        };

        foreach(FileInfo fi in files) {
            if (fi.Directory == null) continue;
            string fn = fi.Name.Replace(fi.Extension,"");
            Console.WriteLine($"Processing: {fi.FullName}");
            string content = File.ReadAllText(fi.FullName);
            content  = content.Replace("\r","");
            content  = content.ToUpper();            
            for (int i=1;i<regex.Length;i+=2) {
                string s = regex[i-1];
                string v = regex[i];                
                content = Regex.Replace(content,s,v, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
            }

            content = content.Replace("\n,",",");

            List<string> tks = new List<string>(content.Split("\n"));

            tks.RemoveAll(delegate(string s) { return s.Trim().Length <= 0; });

            File.WriteAllLines($"{fi.Directory.FullName}\\raw-{fn}.csv",tks);

            tks.Sort(delegate(string a,string b) { 
                string dsa = a.Split(",")[0];
                string dsb = b.Split(",")[0];
                DateTime da = DateTime.ParseExact(dsa,"dd/MM/yyyy",CultureInfo.InvariantCulture);
                DateTime db = DateTime.ParseExact(dsb,"dd/MM/yyyy",CultureInfo.InvariantCulture);
                return da.CompareTo(db);
            });            

            for(int m=1;m<=12;m++) {  
                List<string> tkm = new List<string>();  
                for(int i=0;i<tks.Count;i++) {
                    string   ds = tks[i].Split(",")[0];
                    DateTime d  = DateTime.ParseExact(ds,"dd/MM/yyyy",CultureInfo.InvariantCulture);
                    if (d.Month != m) continue;
                    tkm.Add(tks[i]);
                }
                if(tkm.Count<=0) continue;
                File.WriteAllLines($"{fi.Directory.FullName}\\processed-{fn}-{m.ToString("00")}.csv",tkm);
            }
            
            
        }

    }
}