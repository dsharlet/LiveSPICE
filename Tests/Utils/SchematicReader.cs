using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Circuit;
using LiveSPICE.Cli.Utils;
using Util;

namespace LiveSPICE.CLI.Utils
{
    internal class SchematicReader
    {
        private readonly ILog log;

        public SchematicReader(ILog log)
        {
            this.log = log;
        }

        public IEnumerable<Schematic> GetSchematics(string glob) => Globber.Glob(glob).Select(filename => GetSchematic(filename));

        public Schematic GetSchematic(string filename)
        {
            log.WriteLine(MessageType.Info, $"Opening [blue]{filename}[/blue]");
            var circuit = Schematic.Load(filename, log);
            //circuit.Name = Path.GetFileNameWithoutExtension(filename);
            return circuit;
        }
    }
}
