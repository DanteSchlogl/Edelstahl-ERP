using System.Collections.Generic;

namespace Edelstahl.BLL.DTOs
{
    public class ResultadoImportacion
    {
        public int Total { get; set; }
        public int Importados { get; set; }
        public int Omitidos { get; set; }
        public List<string> Errores { get; private set; }

        public ResultadoImportacion()
        {
            Errores = new List<string>();
        }
    }
}
