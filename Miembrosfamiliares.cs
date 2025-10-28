using System;
using System.Collections.Generic;

namespace ArbolGenealogico.Modelos
{
   
    public class MiembroFamilia
    {
        // ==========================
        // Atributos básicos
        // ==========================
        public string Nombre { get; set; }
        public string Cedula { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public bool EstaVivo { get; set; }
        public int Edad { get; private set; }

        // ==========================
        // Atributos avanzados
        // ==========================
        public string FotografiaRuta { get; set; }
        public (double Latitud, double Longitud) CoordenadasResidencia { get; set; }

        // ==========================
        // Relaciones familiares
        // ==========================
        public MiembroFamilia? Padre { get; private set; }
        public List<MiembroFamilia> Hijos { get; private set; }

        // ==========================
        // Constructor
        // ==========================
        public MiembroFamilia(
            string nombre,
            string cedula,
            DateTime fechaNacimiento,
            bool estaVivo,
            string fotografiaRuta = "",
            double latitud = 0,
            double longitud = 0)
        {
            Nombre = nombre;
            Cedula = cedula;
            FechaNacimiento = fechaNacimiento;
            EstaVivo = estaVivo;
            FotografiaRuta = fotografiaRuta;
            CoordenadasResidencia = (latitud, longitud);
            Hijos = new List<MiembroFamilia>();
            Edad = CalcularEdad();
        }

        // ==========================
        // Métodos principales
        // ==========================

        /// <summary>
        /// Calcula la edad actual o la edad al fallecer.
        /// </summary>
        private int CalcularEdad()
        {
            var hoy = DateTime.Now;
            int edad = hoy.Year - FechaNacimiento.Year;
            if (hoy < FechaNacimiento.AddYears(edad))
                edad--;
            return edad;
        }

        /// <summary>
        /// Agrega un hijo a este miembro, validando que no tenga otro padre.
        /// </summary>
        public void AgregarHijo(MiembroFamilia hijo)
        {
            if (hijo == null)
                throw new ArgumentNullException(nameof(hijo), "El hijo no puede ser nulo.");

            if (hijo.Padre != null)
                throw new InvalidOperationException($"El miembro {hijo.Nombre} ya tiene un padre asignado.");

            hijo.Padre = this;
            Hijos.Add(hijo);
        }

        /// <summary>
        /// Devuelve una representación jerárquica del linaje en texto.
        /// </summary>
        public void MostrarArbol(string prefijo = "")
        {
            string estado = EstaVivo ? "Vivo" : "Fallecido";
            Console.WriteLine($"{prefijo}- {Nombre} ({Edad} años, {estado})");

            foreach (var hijo in Hijos)
                hijo.MostrarArbol(prefijo + "   ");
        }

        
        /// Devuelve la lista completa de descendientes de este miembro.
        
        public List<MiembroFamilia> ObtenerDescendientes()
        {
            var descendientes = new List<MiembroFamilia>();
            foreach (var hijo in Hijos)
            {
                descendientes.Add(hijo);
                descendientes.AddRange(hijo.ObtenerDescendientes());
            }
            return descendientes;
        }

        /// 
        /// Devuelve la raíz del árbol (el ancestro más antiguo).
        /// 
        public MiembroFamilia ObtenerAncestroRaiz()
        {
            return Padre == null ? this : Padre.ObtenerAncestroRaiz();
        }
    }
}
