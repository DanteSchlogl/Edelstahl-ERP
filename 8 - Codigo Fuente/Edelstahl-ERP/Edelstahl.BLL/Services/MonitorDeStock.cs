using System;
using System.Collections.Generic;
using System.Threading;
using Edelstahl.BLL.DTOs;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Revisa periódicamente el stock en un hilo propio y avisa cuando cambia
    /// la lista de productos por debajo del mínimo (patrón Observer).
    /// </summary>
    public class MonitorDeStock : IDisposable
    {
        private readonly InventarioService _inventario;
        private readonly TimeSpan _intervalo;
        private readonly object _candado = new object();

        private Thread _hilo;
        private ManualResetEventSlim _detener;
        private int _ultimaCantidad = -1;

        public event EventHandler<AlertasStockEventArgs> AlertasActualizadas;

        public MonitorDeStock(TimeSpan intervalo)
            : this(new InventarioService(), intervalo)
        {
        }

        public MonitorDeStock(InventarioService inventario, TimeSpan intervalo)
        {
            _inventario = inventario ?? throw new ArgumentNullException(nameof(inventario));
            _intervalo = intervalo < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : intervalo;
        }

        public bool EnEjecucion
        {
            get
            {
                lock (_candado)
                {
                    return _hilo != null && _hilo.IsAlive;
                }
            }
        }

        public void Iniciar()
        {
            lock (_candado)
            {
                if (_hilo != null && _hilo.IsAlive)
                {
                    return;
                }

                _detener = new ManualResetEventSlim(false);
                _hilo = new Thread(Ciclo)
                {
                    IsBackground = true,
                    Name = "MonitorDeStock"
                };
                _hilo.Start();
            }
        }

        public void Detener()
        {
            Thread hilo;
            ManualResetEventSlim senal;

            lock (_candado)
            {
                hilo = _hilo;
                senal = _detener;
                _hilo = null;
            }

            if (senal != null)
            {
                senal.Set();
            }

            if (hilo != null && hilo.IsAlive)
            {
                hilo.Join(2000);
            }
        }

        /// <summary>
        /// Fuerza una revisión inmediata, por ejemplo después de registrar un movimiento.
        /// </summary>
        public void Revisar()
        {
            try
            {
                List<AlertaReposicionDto> alertas = _inventario.ObtenerAlertas();
                bool cambio = Interlocked.Exchange(ref _ultimaCantidad, alertas.Count) != alertas.Count;

                if (cambio)
                {
                    EventHandler<AlertasStockEventArgs> manejador = AlertasActualizadas;

                    if (manejador != null)
                    {
                        manejador(this, new AlertasStockEventArgs(alertas));
                    }
                }
            }
            catch
            {
                // Un fallo de lectura no debe detener el monitor.
            }
        }

        private void Ciclo()
        {
            ManualResetEventSlim senal = _detener;

            while (!senal.IsSet)
            {
                Revisar();
                senal.Wait(_intervalo);
            }
        }

        public void Dispose()
        {
            Detener();
        }
    }

    public class AlertasStockEventArgs : EventArgs
    {
        public AlertasStockEventArgs(List<AlertaReposicionDto> alertas)
        {
            Alertas = alertas;
        }

        public List<AlertaReposicionDto> Alertas { get; private set; }
    }
}
