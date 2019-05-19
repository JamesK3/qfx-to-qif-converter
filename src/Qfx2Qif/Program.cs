using System;
using System.IO;
using System.Text;
using System.Xml;
using  Sgml;

namespace Qfx2Qif
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                return;
            }

            var qfxFile = new FileInfo(args[0]);
            //Console.WriteLine(qfxFile.FullName);


            var qfxSgml = string.Empty;
            using (var qfxReader = new StreamReader(qfxFile.FullName))
            {
                while (!qfxReader.EndOfStream)
                {
                    var line = qfxReader.ReadLine();
                    if (!line.StartsWith("<OFX>", StringComparison.OrdinalIgnoreCase)) continue;
                    qfxSgml = line;
                    break;
                }
            }

            var doc = new XmlDocument
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };

            var qif = new StringBuilder().AppendLine("!Type:CCard");

            using (var qfxStringReader = new StringReader(qfxSgml))
            {
                var sgmlReader = new SgmlReader
                {
                    WhitespaceHandling = WhitespaceHandling.All,
                    InputStream = qfxStringReader
                };


                doc.Load(sgmlReader);
                var transactions = doc.FirstChild["CREDITCARDMSGSRSV1"].GetElementsByTagName("STMTTRN");
                for (var i = 0; i < transactions.Count; i++)
                {
                    var xaction = transactions[i];
                    //var isDebit = xaction["TRNTYPE"].FirstChild.Value;
                    var amount = Double.Parse(xaction["TRNTYPE"]["DTPOSTED"]["TRNAMT"].FirstChild.Value);
                    var date = xaction["TRNTYPE"]["DTPOSTED"]["TRNAMT"]["FITID"].FirstChild.Value.Substring(0, 8);
                    var payee = xaction["TRNTYPE"]["DTPOSTED"]["TRNAMT"]["FITID"]["SIC"]["NAME"].FirstChild.Value;
                    qif.AppendLine(string.Format("D{0}", DateTime.Parse(date).ToString("MM-dd-yyyy")))
                        .AppendLine("P" + payee)
                        .AppendLine(string.Format("T{0:f}", amount))
                        .AppendLine("C*")
                        .AppendLine("^");
                }
                qif.AppendLine();
            }
            var qifFile = qfxFile.DirectoryName + "\\" +
                          qfxFile.Name.Substring(0, qfxFile.Name.LastIndexOf(".qfx", StringComparison.OrdinalIgnoreCase)) +
                          ".qif";
            File.WriteAllText(qifFile, qif.ToString());
            //Console.ReadKey();
        }
    }
}
