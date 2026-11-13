using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Ubicacion aproximada de localidades argentinas para dibujar el mapa de recorrido.
    /// Es un mapa esquematico y ficticio: no usa servicios externos ni claves de API.
    /// </summary>
    public sealed class Lugar
    {
        public Lugar(string nombre, double latitud, double longitud, bool principal)
        {
            Nombre = nombre;
            Latitud = latitud;
            Longitud = longitud;
            Principal = principal;
        }

        public string Nombre { get; private set; }
        public double Latitud { get; private set; }
        public double Longitud { get; private set; }
        public bool Principal { get; private set; }
        public bool Estimado { get; set; }
    }

    public static class GeoFicticio
    {
        public static readonly Lugar Sede = new Lugar("Edelstahl - Sede central (Capital)", -34.6358, -58.4000, true);

        private static readonly Dictionary<string, Lugar> Lugares = Cargar();

        public static IEnumerable<Lugar> Referencias
        {
            get { return Lugares.Values; }
        }

        /// <summary>
        /// Contorno aproximado de la costa del Rio de la Plata (latitud, longitud),
        /// de oeste a este; el agua queda al norte y al este de esa linea.
        /// </summary>
        public static readonly double[][] Costa =
        {
            new[] { -34.40, -58.62 }, new[] { -34.43, -58.58 }, new[] { -34.47, -58.50 }, new[] { -34.52, -58.47 },
            new[] { -34.56, -58.42 }, new[] { -34.61, -58.36 }, new[] { -34.64, -58.34 }, new[] { -34.70, -58.25 },
            new[] { -34.76, -58.18 }, new[] { -34.80, -58.10 }, new[] { -34.82, -57.98 }
        };

        private static Dictionary<string, Lugar> Cargar()
        {
            Dictionary<string, Lugar> d = new Dictionary<string, Lugar>();
            Action<string, double, double, bool> a = (n, lat, lon, p) => d[Normalizar(n)] = new Lugar(n, lat, lon, p);

            a("Capital Federal", -34.6037, -58.3816, true);
            a("Avellaneda", -34.6626, -58.3650, false);
            a("Lanus", -34.7059, -58.3912, false);
            a("Banfield", -34.7430, -58.3930, false);
            a("Lomas de Zamora", -34.7609, -58.4017, false);
            a("Temperley", -34.7840, -58.3960, false);
            a("Adrogue", -34.7971, -58.3929, false);
            a("Quilmes", -34.7242, -58.2526, true);
            a("Berazategui", -34.7654, -58.2128, false);
            a("Florencio Varela", -34.8167, -58.2833, false);
            a("Monte Grande", -34.8167, -58.4667, false);
            a("Ezeiza", -34.8550, -58.5214, false);
            a("La Matanza", -34.7700, -58.6300, false);
            a("San Justo", -34.6800, -58.5600, false);
            a("Moron", -34.6534, -58.6198, false);
            a("Haedo", -34.6430, -58.5920, false);
            a("Hurlingham", -34.5890, -58.6370, false);
            a("Ituzaingo", -34.6602, -58.6679, false);
            a("Merlo", -34.6643, -58.7277, false);
            a("Moreno", -34.6340, -58.7916, false);
            a("Caseros", -34.6090, -58.5620, false);
            a("Tres de Febrero", -34.6040, -58.5600, false);
            a("San Martin", -34.5790, -58.5365, false);
            a("San Miguel", -34.5439, -58.7122, false);
            a("Jose C. Paz", -34.5100, -58.7600, false);
            a("Vicente Lopez", -34.5261, -58.4730, false);
            a("San Isidro", -34.4708, -58.5286, true);
            a("San Fernando", -34.4420, -58.5590, false);
            a("Tigre", -34.4260, -58.5797, false);
            a("Pilar", -34.4587, -58.9142, true);
            a("Escobar", -34.3480, -58.7940, false);
            a("Campana", -34.1625, -58.9594, false);
            a("Zarate", -34.0981, -59.0243, false);
            a("Canuelas", -35.0514, -58.7581, false);
            a("La Plata", -34.9205, -57.9536, true);
            a("Mar del Plata", -38.0055, -57.5426, true);
            a("Bahia Blanca", -38.7196, -62.2724, true);
            a("Rosario", -32.9468, -60.6393, true);
            a("Santa Fe", -31.6333, -60.7000, false);
            a("Parana", -31.7333, -60.5333, false);
            a("Cordoba", -31.4201, -64.1888, true);
            a("Mendoza", -32.8895, -68.8458, true);
            a("San Juan", -31.5375, -68.5364, false);
            a("San Luis", -33.2950, -66.3356, false);
            a("Tucuman", -26.8083, -65.2176, true);
            a("Salta", -24.7821, -65.4232, false);
            a("Neuquen", -38.9516, -68.0591, true);
            a("Resistencia", -27.4606, -58.9839, false);
            a("Corrientes", -27.4692, -58.8306, false);
            a("Posadas", -27.3671, -55.8961, false);
            a("Ushuaia", -54.8019, -68.3030, false);
            a("Comodoro Rivadavia", -45.8641, -67.4966, true);
            a("Trelew", -43.2489, -65.3051, false);
            a("Rawson", -43.3002, -65.1023, false);
            a("Puerto Madryn", -42.7692, -65.0385, false);
            a("Esquel", -42.9108, -71.3194, false);
            a("Rio Gallegos", -51.6226, -69.2181, false);
            a("San Carlos de Bariloche", -41.1335, -71.3103, true);
            a("Bariloche", -41.1335, -71.3103, false);
            a("Viedma", -40.8135, -62.9967, false);
            a("Santa Rosa", -36.6203, -64.2906, false);
            a("Rio Cuarto", -33.1307, -64.3499, false);
            a("San Rafael", -34.6177, -68.3301, false);
            a("Rio Grande", -53.7877, -67.7094, false);
            a("Jujuy", -24.1858, -65.2995, false);
            a("San Salvador de Jujuy", -24.1858, -65.2995, false);
            a("Santiago del Estero", -27.7951, -64.2615, false);
            a("Formosa", -26.1775, -58.1781, false);
            a("Catamarca", -28.4696, -65.7852, false);
            a("La Rioja", -29.4131, -66.8558, false);

            // Nombres alternativos
            d[Normalizar("caba")] = d[Normalizar("Capital Federal")];
            d[Normalizar("ciudad autonoma")] = d[Normalizar("Capital Federal")];
            d[Normalizar("ciudad de buenos aires")] = d[Normalizar("Capital Federal")];
            d[Normalizar("remedios de escalada")] = d[Normalizar("Lanus")];
            d[Normalizar("jose c paz")] = d[Normalizar("Jose C. Paz")];
            d[Normalizar("vicente lopez")] = d[Normalizar("Vicente Lopez")];
            return d;
        }

        public static string Normalizar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return string.Empty;
            }

            string descompuesto = texto.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString().Replace(".", string.Empty).Trim();
        }

        /// <summary>
        /// Busca en la direccion (y el nombre del cliente) alguna localidad conocida.
        /// Si no encuentra ninguna, ubica el punto cerca de la Capital con un desplazamiento
        /// fijo calculado a partir del texto, marcandolo como estimado.
        /// </summary>
        public static Lugar Ubicar(string direccion, string detalle)
        {
            string texto = Normalizar((direccion ?? string.Empty) + " " + (detalle ?? string.Empty));
            Lugar mejor = null;
            int largo = 0;

            foreach (KeyValuePair<string, Lugar> par in Lugares)
            {
                if (par.Key.Length > largo && texto.Contains(par.Key))
                {
                    mejor = par.Value;
                    largo = par.Key.Length;
                }
            }

            if (mejor == null)
            {
                // Errores de tipeo en la direccion (por ejemplo "Vicnete Lopez").
                foreach (KeyValuePair<string, Lugar> par in Lugares)
                {
                    if (par.Key.Length > largo && Parecido(texto, par.Key))
                    {
                        mejor = par.Value;
                        largo = par.Key.Length;
                    }
                }
            }

            if (mejor == null && texto.Contains("buenos aires"))
            {
                mejor = Lugares[Normalizar("Capital Federal")];
            }

            int semilla = Math.Abs((texto.Length == 0 ? "x" : texto).GetHashCode());
            double dx = ((semilla % 1000) / 1000.0 - 0.5) * 0.045;
            double dy = (((semilla / 1000) % 1000) / 1000.0 - 0.5) * 0.045;

            if (mejor == null)
            {
                double angulo = (semilla % 360) * Math.PI / 180.0;
                double radio = 0.12 + (semilla % 7) * 0.035;
                Lugar cerca = new Lugar(string.IsNullOrWhiteSpace(direccion) ? "Ubicacion estimada" : direccion.Trim(),
                    -34.62 + Math.Sin(angulo) * radio * 0.7, -58.45 + Math.Cos(angulo) * radio, false);
                cerca.Estimado = true;
                return cerca;
            }

            return new Lugar(string.IsNullOrWhiteSpace(direccion) ? mejor.Nombre : direccion.Trim(),
                mejor.Latitud + dy, mejor.Longitud + dx, false);
        }

        private static bool Parecido(string texto, string clave)
        {
            int tolerancia = clave.Length >= 12 ? 2 : (clave.Length >= 6 ? 1 : 0);

            if (tolerancia == 0)
            {
                return false;
            }

            for (int largo = clave.Length - tolerancia; largo <= clave.Length + tolerancia; largo++)
            {
                for (int i = 0; i + largo <= texto.Length; i++)
                {
                    if (Distancia(texto.Substring(i, largo), clave, tolerancia) <= tolerancia)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Distancia de edicion (con transposicion de letras contiguas).
        private static int Distancia(string a, string b, int tope)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int costo = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + costo);

                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    {
                        d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
                    }
                }
            }

            return d[a.Length, b.Length];
        }

        public static double Kilometros(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double p1 = lat1 * Math.PI / 180.0, p2 = lat2 * Math.PI / 180.0;
            double dp = (lat2 - lat1) * Math.PI / 180.0, dl = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dp / 2) * Math.Sin(dp / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
