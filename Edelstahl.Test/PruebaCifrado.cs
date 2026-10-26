using System;
using System.Diagnostics;
using Edelstahl.Services.Cryptography;

namespace Edelstahl.Test
{
    /// <summary>
    /// Pruebas del cifrado simétrico AES: formato actual, compatibilidad con
    /// el formato anterior y detección de alteraciones.
    /// </summary>
    public static class PruebaCifrado
    {
        public static void Ejecutar()
        {
            Verificador.Seccion("Cifrado AES");

            const string cuit = "30-66778899-2";
            AesEncryptionService servicio = new AesEncryptionService("Edelstahl-Clave-Segura-2026");

            string cifrado = servicio.Encrypt(cuit);
            Verificador.Verificar(cifrado.StartsWith(AesEncryptionService.PrefijoV2), "El formato actual lleva el prefijo v2");
            Verificador.Igual(cuit, servicio.Decrypt(cifrado), "Se descifra lo que se cifró");
            Verificador.Verificar(cifrado != servicio.Encrypt(cuit), "Dos cifrados del mismo texto son distintos (sal e IV aleatorios)");

            string anterior = servicio.EncryptLegacy(cuit);
            Verificador.Verificar(!anterior.StartsWith(AesEncryptionService.PrefijoV2), "El formato anterior no lleva prefijo");
            Verificador.Igual(cuit, servicio.Decrypt(anterior), "Los datos guardados con el formato anterior siguen leyéndose");

            string alterado = cifrado.Substring(0, cifrado.Length - 6) + "AAAAA=";
            Verificador.Falla<System.Security.Cryptography.CryptographicException>(
                () => servicio.Decrypt(alterado), "Un texto alterado se rechaza");

            Stopwatch reloj = Stopwatch.StartNew();

            for (int i = 0; i < 200; i++)
            {
                servicio.Decrypt(servicio.Encrypt("20-12345678-" + (i % 10)));
            }

            Verificador.Verificar(reloj.ElapsedMilliseconds < 2000,
                "200 cifrados y descifrados tardan " + reloj.ElapsedMilliseconds + " ms");
        }
    }
}
