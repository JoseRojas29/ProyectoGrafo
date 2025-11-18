using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace ArbolGenealogicoWPF.Modelos
{
    public class MiembroFamilia
    {
        // ==========================================
        // DATOS PERSONALES (inmutables)
        // ==========================================
        public string Nombre { get; private set; }
        public int Cedula { get; private set; }
        public DateTime FechaNacimiento { get; private set; }
        public int? Edad { get; private set; }
        public bool EstaVivo { get; private set; }
        public string FotografiaRuta { get; private set; }
        public (double Latitud, double Longitud) CoordenadasResidencia { get; private set; }

        // ==========================================
        // RELACIONES FAMILIARES
        // ==========================================
        public MiembroFamilia? Padre { get; private set; }
        public MiembroFamilia? Madre { get; private set; }
        public MiembroFamilia? Pareja { get; private set; }
        public List<MiembroFamilia> Hijos { get; private set; }
        public List<MiembroFamilia> Hermanos { get; private set; }

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public MiembroFamilia(
            string nombre,
            string cedula,
            bool estaVivo,
            int? edad,
            DateTime fechaNacimiento,
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
            Hermanos = new List<MiembroFamilia>();

            if (EstaVivo)
            {
                Edad = CalcularEdad();
            }
            else
            {
                Edad = edad;
            }
        }

        // ==========================================
        // CALCULAR EDAD
        // ==========================================
        private int CalcularEdad()
        {
            var hoy = DateTime.Now;
            int edad = hoy.Year - FechaNacimiento.Year;

            if (hoy < FechaNacimiento.AddYears(edad))
                edad--;

            return edad;
        }

        // ==========================================
        // RELACIONES
        // ==========================================
        public void AsignarPadre(MiembroFamilia padre)
        {
            if (padre == null)
                throw new ArgumentNullException(nameof(padre));

            if (Padre != null && Padre != padre)
                throw new InvalidOperationException($"{Nombre} ya tiene un padre asignado.");

            Padre = padre;

            // Usar Any en lugar de Contains
            if (!padre.Hijos.Any(h => h.Cedula == this.Cedula))
                padre.Hijos.Add(this);
        }

        public void AsignarMadre(MiembroFamilia madre)
        {
            if (madre == null)
                throw new ArgumentNullException(nameof(madre));

            if (Madre != null && Madre != madre)
                throw new InvalidOperationException($"{Nombre} ya tiene una madre asignada.");

            Madre = madre;

            // Usar Any en lugar de Contains
            if (!madre.Hijos.Any(h => h.Cedula == this.Cedula))
                madre.Hijos.Add(this);
        }

        public void AsignarPareja(MiembroFamilia pareja)
        {
            if (pareja == null)
                throw new ArgumentNullException(nameof(pareja));

            if (Pareja != null && Pareja != pareja)
                throw new InvalidOperationException($"{Nombre} ya tiene una pareja asignada.");

            Pareja = pareja;
            pareja.Pareja = this;
        }

        public void AsignarHijoComoPadre(MiembroFamilia hijo)
        {
            if (hijo == null)
                throw new ArgumentNullException(nameof(hijo));

            if (Hijos.Any(h => h.Cedula == hijo.Cedula))
                throw new InvalidOperationException($"{Nombre} ya tiene un hijo con esa cédula.");

            Hijos.Add(hijo);

            // Solo asigna si aún no tiene padre
            if (hijo.Padre == null)
                hijo.Padre = this;
        }

        public void AsignarHijoComoMadre(MiembroFamilia hijo)
        {
            if (hijo == null)
                throw new ArgumentNullException(nameof(hijo));

            if (Hijos.Any(h => h.Cedula == hijo.Cedula))
                throw new InvalidOperationException($"{Nombre} ya tiene un hijo con esa cédula.");

            Hijos.Add(hijo);

            // Solo asigna si aún no tiene madre
            if (hijo.Madre == null)
                hijo.Madre = this;
        }

        public void AsignarHermano(MiembroFamilia hermano)
        {
            if (hermano == null)
                throw new ArgumentNullException(nameof(hermano));

            if (Hermanos.Any(h => h.Cedula == hermano.Cedula))
                throw new InvalidOperationException($"{Nombre} ya tiene un hermano con esa cédula.");

            Hermanos.Add(hermano);

            if (!hermano.Hermanos.Any(h => h.Cedula == this.Cedula))
                hermano.Hermanos.Add(this);
        }

        public List<MiembroFamilia> ObtenerHermanos()
        {
            var hermanos = new HashSet<MiembroFamilia>();

            // Hermanos por parte del padre
            if (Padre != null)
            {
                foreach (var h in Padre.Hijos)
                    if (h != this)
                        hermanos.Add(h);
            }

            // Hermanos por parte de la madre
            if (Madre != null)
            {
                foreach (var h in Madre.Hijos)
                    if (h != this)
                        hermanos.Add(h);
            }

            return hermanos.ToList();
        }

        // ==========================================
        // OBTENER DESCENDIENTES (lista plana)
        // ==========================================
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
    }
}
