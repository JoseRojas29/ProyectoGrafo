using System;
using System.Collections.Generic;
using System.Linq;

namespace ArbolGenealogico.Modelos
{
    public class MiembroFamilia
    {
        // ==========================================
        // DATOS PERSONALES
        // ==========================================
        public string Nombre { get; set; }
        public string Cedula { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public bool EstaVivo { get; set; }
        public int Edad { get; private set; }

        // Datos adicionales
        public string FotografiaRuta { get; set; }
        public (double Latitud, double Longitud) CoordenadasResidencia { get; set; }

        // ==========================================
        // RELACIONES FAMILIARES
        // ==========================================
        public MiembroFamilia? Padre { get; private set; }
        public MiembroFamilia? Madre { get; private set; }
        public List<MiembroFamilia> Hijos { get; private set; }

        // Pareja detectada o asignada
        public MiembroFamilia? Pareja { get; private set; }

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
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
        // ASIGNAR PADRE
        // ==========================================
        public void AsignarPadre(MiembroFamilia padre)
        {
            if (padre == null)
                throw new ArgumentNullException(nameof(padre));

            if (Padre != null && Padre != padre)
                throw new InvalidOperationException($"{Nombre} ya tiene un padre asignado.");

            Padre = padre;

            if (!padre.Hijos.Contains(this))
                padre.Hijos.Add(this);
        }

        // ==========================================
        // ASIGNAR MADRE
        // ==========================================
        public void AsignarMadre(MiembroFamilia madre)
        {
            if (madre == null)
                throw new ArgumentNullException(nameof(madre));

            if (Madre != null && Madre != madre)
                throw new InvalidOperationException($"{Nombre} ya tiene una madre asignada.");

            Madre = madre;

            if (!madre.Hijos.Contains(this))
                madre.Hijos.Add(this);
        }

        // ==========================================
        // ASIGNAR PAREJA
        // ==========================================
        public void AsignarPareja(MiembroFamilia pareja)
        {
            if (pareja == null)
                throw new ArgumentNullException(nameof(pareja));

            Pareja = pareja;
            pareja.Pareja = this;
        }

        // ==========================================
        // OBTENER HERMANOS (por PADRE y MADRE)
        // ==========================================
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
        // MOSTRAR ÁRBOL EN CONSOLA
        // ==========================================
        public void MostrarArbol(string prefijo = "")
        {
            string estado = EstaVivo ? "Vivo" : "Fallecido";
            Console.WriteLine($"{prefijo}- {Nombre} ({Edad} años, {estado})");

            foreach (var hijo in Hijos)
                hijo.MostrarArbol(prefijo + "   ");
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

        // ==========================================
        // OBTENER TODOS LOS ANCESTROS RAÍZ
        // (Padre, madre o ambos)
        // ==========================================
        public List<MiembroFamilia> ObtenerAncestrosRaiz()
        {
            var ancestros = new List<MiembroFamilia>();

            if (Padre != null)
                ancestros.AddRange(Padre.ObtenerAncestrosRaiz());

            if (Madre != null)
                ancestros.AddRange(Madre.ObtenerAncestrosRaiz());

            // Si no tiene padres → es raíz en esta línea
            if (Padre == null && Madre == null)
                ancestros.Add(this);

            return ancestros.Distinct().ToList();
        }
    }
}
