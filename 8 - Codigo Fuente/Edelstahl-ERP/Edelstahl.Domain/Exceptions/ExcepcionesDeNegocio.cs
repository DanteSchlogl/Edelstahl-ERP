using System;

namespace Edelstahl.Domain.Exceptions
{
    /// <summary>
    /// Se produce cuando una operación incumple una regla del negocio.
    /// El mensaje ya está traducido al idioma activo.
    /// </summary>
    public sealed class ReglaDeNegocioException : BusinessRuleException
    {
        public ReglaDeNegocioException(string mensaje)
            : base(mensaje, "REGLA_NEGOCIO")
        {
        }

        public ReglaDeNegocioException(string mensaje, string codigo)
            : base(mensaje, codigo)
        {
        }
    }

    /// <summary>
    /// Se produce cuando el usuario no tiene permiso para la operación.
    /// </summary>
    public sealed class AccesoNoAutorizadoException : BusinessRuleException
    {
        public AccesoNoAutorizadoException(string mensaje)
            : base(mensaje, "ACCESO_NO_AUTORIZADO")
        {
        }
    }

    /// <summary>
    /// Se produce cuando un dato obligatorio falta o no tiene un valor
    /// válido (identificador vacío, importe negativo, texto vacío, etc.).
    /// </summary>
    public sealed class DatoInvalidoException : BusinessRuleException
    {
        public DatoInvalidoException(string mensaje)
            : base(mensaje, "DATO_INVALIDO")
        {
        }
    }

    /// <summary>
    /// Se produce cuando el registro buscado no existe.
    /// </summary>
    public sealed class RegistroNoEncontradoException : BusinessRuleException
    {
        public RegistroNoEncontradoException(string mensaje)
            : base(mensaje, "REGISTRO_NO_ENCONTRADO")
        {
        }
    }

    /// <summary>
    /// Se produce cuando ya existe otro registro con la misma clave
    /// (número, nombre, CUIT, correo) y la operación lo duplicaría.
    /// </summary>
    public sealed class RegistroDuplicadoException : BusinessRuleException
    {
        public RegistroDuplicadoException(string mensaje)
            : base(mensaje, "REGISTRO_DUPLICADO")
        {
        }
    }

    /// <summary>
    /// Se produce cuando no se puede completar una operación con la base
    /// de datos (conexión, permisos, consulta rechazada). Conserva la
    /// excepción técnica original para la bitácora.
    /// </summary>
    public sealed class AccesoADatosException : BusinessRuleException
    {
        public AccesoADatosException(string mensaje, Exception causa)
            : base(mensaje, "ACCESO_A_DATOS", causa)
        {
        }
    }

    /// <summary>
    /// Se produce cuando falla la creación o la restauración de un
    /// respaldo de las bases de datos.
    /// </summary>
    public sealed class RespaldoException : BusinessRuleException
    {
        public RespaldoException(string mensaje)
            : base(mensaje, "RESPALDO")
        {
        }

        public RespaldoException(string mensaje, Exception causa)
            : base(mensaje, "RESPALDO", causa)
        {
        }
    }

    /// <summary>
    /// Se produce cuando la configuración de la aplicación es inválida:
    /// por ejemplo, la clase de repositorio indicada para la Factory no
    /// existe o no implementa la interfaz esperada.
    /// </summary>
    public sealed class ConfiguracionInvalidaException : BusinessRuleException
    {
        public ConfiguracionInvalidaException(string mensaje)
            : base(mensaje, "CONFIGURACION_INVALIDA")
        {
        }

        public ConfiguracionInvalidaException(string mensaje, Exception causa)
            : base(mensaje, "CONFIGURACION_INVALIDA", causa)
        {
        }
    }

    /// <summary>
    /// Se produce cuando los dígitos verificadores (DVH o DVV) indican que
    /// los datos fueron alterados fuera del sistema.
    /// </summary>
    public sealed class IntegridadVulneradaException : BusinessRuleException
    {
        public IntegridadVulneradaException(string mensaje)
            : base(mensaje, "INTEGRIDAD_VULNERADA")
        {
        }
    }

    /// <summary>
    /// Se produce cuando el cliente ya existe con el CUIT informado.
    /// </summary>
    public sealed class ClienteDuplicadoException : BusinessRuleException
    {
        public string CUIT { get; }

        public ClienteDuplicadoException(string cuit, string mensaje)
            : base(mensaje, "CLIENTE_DUPLICADO")
        {
            CUIT = cuit;
        }
    }

    /// <summary>
    /// Se produce cuando el CUIT ingresado no cumple las reglas establecidas.
    /// </summary>
    public sealed class CUITInvalidoException : BusinessRuleException
    {
        public string CUIT { get; }

        public CUITInvalidoException(string cuit, string mensaje)
            : base(mensaje, "CLIENTE_CUIT_INVALIDO")
        {
            CUIT = cuit;
        }
    }
}
