using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Globalization;

namespace ArbolGenealogicoWPF
{
    /// <summary>
    /// Clase que define a los nodos del grafo del árbol genealógico
    /// El grafo vive en los nodos porque cada nodo conoce a sus relaciones mediante listas de adyacencia
    /// </summary>
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
        public string CoordenadasResidencia { get; private set; }
        public double Latitud { get; private set; }
        public double Longitud { get; private set; }

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
            int cedula,
            bool estaVivo,
            int? edad,
            DateTime fechaNacimiento,
            string fotografiaRuta,
            string coordenadasResidencia)
        {
            Nombre = nombre;
            Cedula = cedula;
            FechaNacimiento = fechaNacimiento;
            EstaVivo = estaVivo;

            FotografiaRuta = fotografiaRuta;
            CoordenadasResidencia = coordenadasResidencia;
            TryParseCoordenadas(coordenadasResidencia, out double lat, out double lon);
            Latitud = lat;
            Longitud = lon;

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
        // RELACIONES INICIALES
        // ==========================================
        public void AsignarPadre(MiembroFamilia padre)
        {
            if (padre == null)
                throw new ArgumentNullException(nameof(padre));

            if (Padre != null && Padre != padre)
                throw new InvalidOperationException($"{Nombre} ya tiene un padre asignado.");

            Padre = padre;
            padre.Hijos.Add(this);
        }

        public void AsignarMadre(MiembroFamilia madre)
        {
            if (madre == null)
                throw new ArgumentNullException(nameof(madre));

            if (Madre != null && Madre != madre)
                throw new InvalidOperationException($"{Nombre} ya tiene una madre asignada.");

            Madre = madre;
            madre.Hijos.Add(this);
        }

        public void AsignarPareja(MiembroFamilia pareja)
        {
            if (pareja == null)
                throw new ArgumentNullException(nameof(pareja));

            if (Pareja != null && Pareja != pareja)
                throw new InvalidOperationException($"{Nombre} ya tiene una pareja asignada.");

            if (pareja.Pareja != null && pareja.Pareja != this)
                throw new InvalidOperationException($"{pareja.Nombre} ya tiene otra pareja asignada.");

            Pareja = pareja;
            pareja.Pareja = this;
        }

        public void AsignarHijoComoPadre(MiembroFamilia hijo)
        {
            if (hijo == null)
                throw new ArgumentNullException(nameof(hijo));

            // Si ya tengo hijos, verificar mi rol
            if (Hijos.Any())
            {
                var hijoReferencia = Hijos.First();
                bool soyPadre = hijoReferencia.Padre != null && hijoReferencia.Padre.Cedula == this.Cedula;
                bool soyMadre = hijoReferencia.Madre != null && hijoReferencia.Madre.Cedula == this.Cedula;

                if (soyMadre)
                    throw new InvalidOperationException($"{Nombre} ya es madre en otra relación, no puede ser padre.");
            }

            if (hijo.Padre != null && hijo.Padre != this)
                throw new InvalidOperationException("El hijo ya tiene otro padre asignado.");

            Hijos.Add(hijo);
            hijo.Padre = this;
        }

        public void AsignarHijoComoMadre(MiembroFamilia hijo)
        {
            if (hijo == null)
                throw new ArgumentNullException(nameof(hijo));

            // Si ya tengo hijos, verificar mi rol
            if (Hijos.Any())
            {
                var hijoReferencia = Hijos.First();
                bool soyPadre = hijoReferencia.Padre != null && hijoReferencia.Padre.Cedula == this.Cedula;
                bool soyMadre = hijoReferencia.Madre != null && hijoReferencia.Madre.Cedula == this.Cedula;

                if (soyPadre)
                    throw new InvalidOperationException($"{Nombre} ya es padre en otra relación, no puede ser madre.");
            }

            if (hijo.Madre != null && hijo.Madre != this)
                throw new InvalidOperationException("El hijo ya tiene otra madre asignada.");

            Hijos.Add(hijo);
            hijo.Madre = this;
        }

        public void AsignarHermano(MiembroFamilia hermano)
        {
            if (hermano == null)
                throw new ArgumentNullException(nameof(hermano));

            Hermanos.Add(hermano);
            hermano.Hermanos.Add(this);
        }

        // ==========================================
        // CONEXIONES FAMILIARES
        // ==========================================
        public void LigarHermanos()
        {
            // Caso 1: usar hijos de padre/madre si existen
            var fuente = Padre != null ? Padre.Hijos : Madre?.Hijos;

            if (fuente == null || !fuente.Any())
            {
                // Caso 2: si no hay padre/madre, usar la lista de hermanos de uno de mis hermanos
                if (Hermanos.Any())
                {
                    var hermanoReferencia = Hermanos.First();
                    fuente = hermanoReferencia.Hermanos;
                }
                else
                {
                    fuente = Hermanos; // fallback: mi propia lista (vacía al inicio)
                }
            }

            foreach (var h in fuente)
            {
                if (h.Cedula == this.Cedula)
                    continue;

                // Agregar a mi lista si no existe por cédula
                if (!Hermanos.Any(x => x.Cedula == h.Cedula))
                    Hermanos.Add(h);

                // Reciprocidad: asegurar que el otro también me tenga
                if (!h.Hermanos.Any(x => x.Cedula == this.Cedula))
                    h.Hermanos.Add(this);
            }
        }

        /// <summary>
        /// Ligar los hijos de un padre con la madre correspondiente
        /// Es decir, agregar los hijos del padre a la madre y asignar la madre a cada hijo
        /// </summary>
        public void LigarHijosConPadre(MiembroFamilia madre, MiembroFamilia padre)
        {
            foreach (var hijo in padre.Hijos)
            {
                if (!madre.Hijos.Any(h => h.Cedula == hijo.Cedula))
                    madre.Hijos.Add(hijo);

                if (hijo.Madre != madre)
                    hijo.Madre = madre;
            }
        }

        /// <summary>
        /// Ligar los hijos de una madre con el padre correspondiente
        /// Es decir, agregar los hijos de la madre al padre y asignar el padre a cada hijo
        /// </summary>
        public void LigarHijosConMadre(MiembroFamilia madre, MiembroFamilia padre)
        {
            foreach (var hijo in madre.Hijos)
            {
                if (!padre.Hijos.Any(h => h.Cedula == hijo.Cedula))
                    padre.Hijos.Add(hijo);

                if (hijo.Padre != padre)
                    hijo.Padre = padre;
            }
        }

        /// <summary>
        /// Ligar automáticamente a dos miembros como pareja si comparten al menos un hijo.
        /// </summary>
        public void LigarPareja(MiembroFamilia madre, MiembroFamilia padre)
        {
            if (madre == null || padre == null)
                return;

            // Buscar si hay al menos un hijo en común
            bool tienenHijoEnComun = madre.Hijos.Any(h => padre.Hijos.Any(p => p.Cedula == h.Cedula));

            if (tienenHijoEnComun)
            {
                // Asignar pareja recíproca si aún no lo están
                if (madre.Pareja == null)
                    madre.Pareja = padre;

                if (padre.Pareja == null)
                    padre.Pareja = madre;
            }
        }

        /// <summary>
        /// Se asegura de que todos los hermanos estén ligados a sus respectivos padres
        /// </summary>
        public void LigarHermanosConPadres()
        {
            // Si tengo padre, asegurar que todos mis hermanos también estén en su lista de hijos
            if (Padre != null)
            {
                foreach (var hermano in Hermanos)
                {
                    if (!Padre.Hijos.Any(h => h.Cedula == hermano.Cedula))
                        Padre.Hijos.Add(hermano);

                    if (hermano.Padre != Padre)
                        hermano.Padre = Padre;
                }
            }

            // Si tengo madre, asegurar que todos mis hermanos también estén en su lista de hijos
            if (Madre != null)
            {
                foreach (var hermano in Hermanos)
                {
                    if (!Madre.Hijos.Any(h => h.Cedula == hermano.Cedula))
                        Madre.Hijos.Add(hermano);

                    if (hermano.Madre != Madre)
                        hermano.Madre = Madre;
                }
            }
        }

        /// <summary>
        /// Se asegura de que toda pareja comparta los hijos
        /// </summary>
        public void LigarHijosAPareja()
        {
            if (Pareja == null || !Hijos.Any())
                return;

            // Tomar un hijo de referencia
            var hijoReferencia = Hijos.First();

            bool soyPadre = hijoReferencia.Padre != null && hijoReferencia.Padre.Cedula == this.Cedula;
            bool soyMadre = hijoReferencia.Madre != null && hijoReferencia.Madre.Cedula == this.Cedula;

            foreach (var hijo in Hijos)
            {
                if (!Pareja.Hijos.Any(h => h.Cedula == hijo.Cedula))
                    Pareja.Hijos.Add(hijo);

                // Asignar referencias en el hijo
                if (soyPadre && hijo.Madre != Pareja)
                    hijo.Madre = Pareja;

                if (soyMadre && hijo.Padre != Pareja)
                    hijo.Padre = Pareja;
            }
        }

        public void QuitarPadre()
        {
            if (Padre != null)
                Padre = null;
        }

        public void QuitarMadre()
        {
            if (Madre != null)
                Madre = null;
        }

        public void QuitarParejaEnElOtroLado()
        {
            if (Pareja != null)
                Pareja.Pareja = null;
        }

        private bool TryParseCoordenadas(string? texto, out double latitud, out double longitud)
        {
            latitud = 0;
            longitud = 0;

            if (string.IsNullOrWhiteSpace(texto))
                return false;

            // Esperamos algo como: "9.93,-84.08"
            var partes = texto.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2)
                return false;

            var style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;

            if (!double.TryParse(partes[0].Trim(), style, culture, out latitud))
                return false;

            if (!double.TryParse(partes[1].Trim(), style, culture, out longitud))
                return false;

            return true;
        }
    }
}
