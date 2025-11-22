using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Reflection;

// NOTE: NO añadimos "using ArbolGenealogico.Modelos;" aquí porque existe un MiembroFamilia
// en el namespace ArbolGenealogicoWPF que es el tipo real de la colección que recibe la ventana.
// Mantendremos el namespace actual para referirnos al tipo local.
namespace ArbolGenealogicoWPF
{
    public partial class EstadisticasWindow : WindowBaseLogica
    {
        // Observa: este MiembroFamilia es el del namespace ArbolGenealogicoWPF (la clase que pegaste).
        private readonly ObservableCollection<MiembroFamilia> familiares;

        public EstadisticasWindow(ObservableCollection<MiembroFamilia> ListaFamiliares)
        {
            InitializeComponent();

            // case-sensitive: listaFamiliares vs ListaFamiliares
            familiares = ListaFamiliares ?? new ObservableCollection<MiembroFamilia>();

            CalcularYMostrarEstadisticas();
        }

        // ============================
        //   LÓGICA PRINCIPAL
        // ============================
        private void CalcularYMostrarEstadisticas()
        {
            // Convertimos los nodos UI (ArbolGenealogicoWPF.MiembroFamilia) a instancias
            // del tipo que espera GrafoGeografico (si existe) usando reflexión.
            var tipoDestino = FindExternalMiembroFamiliaType(); // puede ser null si no existe
            if (tipoDestino == null)
            {
                // Si no existe un tipo externo, intentamos usar nuestra clase local directamente
                // y asumir que GrafoGeografico acepta estas instancias (caso raro).
                // Para que la UI no se bloquee, comprobamos coords válidas y construimos una lista simple.
                var miembrosLocales = ConvertirMiembrosLocalesValidos();
                if (miembrosLocales.Count < 2)
                {
                    var msg = "Se requieren al menos 2 familiares con coordenadas válidas.";
                    Farthest.Text = msg; Closest.Text = msg; Promedio.Text = "-";
                    return;
                }

                // Intentamos invocar GrafoGeografico con nuestras instancias si existe tipo GrafoGeografico que acepte este tipo
                if (!TryBuildAndComputeGrafo(miembrosLocales.Cast<object>().ToList(), miembrosLocales.First().GetType()))
                {
                    // fallback: mostrar mensaje con conteo
                    Farthest.Text = "No se pudo construir el grafo geográfico (tipo destino no encontrado).";
                    Closest.Text = "-";
                    Promedio.Text = "-";
                }

                return;
            }

            // Si encontramos tipo destino, creamos instancias del tipo destino copiando datos relevantes
            var listaDestino = CrearInstanciasTipoDestino(tipoDestino);

            if (listaDestino == null || listaDestino.Count == 0)
            {
                var msg = "Se requieren al menos 2 familiares con coordenadas válidas (no se pudieron convertir).";
                Farthest.Text = msg; Closest.Text = msg; Promedio.Text = "-";
                return;
            }

            // Construimos una List<TDestino> fuertemente tipada y llamamos al grafo por reflexión
            bool ok = TryBuildAndComputeGrafo(listaDestino, tipoDestino);
            if (!ok)
            {
                Farthest.Text = "Error al crear el grafo geográfico (revisa la construcción por reflexión).";
                Closest.Text = "-";
                Promedio.Text = "-";
            }
        }

        // ============================
        // Helpers: localizar el tipo "MiembroFamilia" que exista fuera del namespace actual
        // ============================
        private Type? FindExternalMiembroFamiliaType()
        {
            // Buscar un tipo llamado "MiembroFamilia" en los assemblies cargados,
            // preferiblemente en 'ArbolGenealogico.Geografia' o 'ArbolGenealogico.Modelos'
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t.Name == "MiembroFamilia" && t.Namespace != typeof(MiembroFamilia).Namespace)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        // ============================
        // Convertir nodos locales (ArbolGenealogicoWPF.MiembroFamilia) a objetos del tipo destino
        // ============================
        private List<object>? CrearInstanciasTipoDestino(Type tipoDestino)
        {
            var lista = new List<object>();

            foreach (var f in familiares)
            {
                if (f == null) continue;

                // Tomar coordenadas desde tu clase real
                double lat = f.CoordenadasResidencia.Latitud;
                double lon = f.CoordenadasResidencia.Longitud;

                // Si quieres filtrar coordenadas inválidas, hazlo aquí (por ejemplo lat==0 && lon==0)
                // if (lat == 0 && lon == 0) continue;

                object? creado = CrearInstanciaDestinoDesdeFuente(tipoDestino, f, lat, lon);
                if (creado != null) lista.Add(creado);
            }

            return lista;
        }

        // Intenta varias firmas de constructor de forma segura
        private object? CrearInstanciaDestinoDesdeFuente(Type tipoDestino, MiembroFamilia fuente, double lat, double lon)
        {
            try
            {
                // Intento 1: (string nombre, int cedula, bool estaVivo, int? edad, DateTime fechaNacimiento, string fotografiaRuta, double latitud, double longitud)
                var ctor1 = tipoDestino.GetConstructor(new Type[] {
                    typeof(string), typeof(int), typeof(bool), typeof(int?), typeof(DateTime), typeof(string), typeof(double), typeof(double)
                });
                if (ctor1 != null)
                {
                    return ctor1.Invoke(new object[] {
                        fuente.Nombre ?? string.Empty,
                        fuente.Cedula,
                        fuente.EstaVivo,
                        fuente.Edad,
                        fuente.FechaNacimiento,
                        fuente.FotografiaRuta ?? string.Empty,
                        lat,
                        lon
                    });
                }

                // Intento 2: (string, int, bool?, int?, DateTime, string, double, double)
                var ctor2 = tipoDestino.GetConstructor(new Type[] {
                    typeof(string), typeof(int), typeof(bool?), typeof(int?), typeof(DateTime), typeof(string), typeof(double), typeof(double)
                });
                if (ctor2 != null)
                {
                    return ctor2.Invoke(new object[] {
                        fuente.Nombre ?? string.Empty,
                        fuente.Cedula,
                        (bool?)fuente.EstaVivo,
                        fuente.Edad,
                        fuente.FechaNacimiento,
                        fuente.FotografiaRuta ?? string.Empty,
                        lat,
                        lon
                    });
                }

                // Intento 3: (string nombre, double lat, double lon)
                var ctorMin = tipoDestino.GetConstructor(new Type[] { typeof(string), typeof(double), typeof(double) });
                if (ctorMin != null)
                {
                    return ctorMin.Invoke(new object[] { fuente.Nombre ?? "(sin nombre)", lat, lon });
                }

                // Intento 4: constructor vacío + setear propiedades si existen
                var ctorDefault = tipoDestino.GetConstructor(Type.EmptyTypes);
                if (ctorDefault != null)
                {
                    var inst = ctorDefault.Invoke(null);
                    TrySetIfExists(tipoDestino, inst, "Nombre", fuente.Nombre);
                    TrySetIfExists(tipoDestino, inst, "Cedula", fuente.Cedula);
                    TrySetIfExists(tipoDestino, inst, "FechaNacimiento", fuente.FechaNacimiento);
                    TrySetIfExists(tipoDestino, inst, "EstaVivo", fuente.EstaVivo);
                    TrySetIfExists(tipoDestino, inst, "FotografiaRuta", fuente.FotografiaRuta);
                    // Soportar tuple o propiedades lat/long
                    TrySetIfExists(tipoDestino, inst, "Latitud", lat);
                    TrySetIfExists(tipoDestino, inst, "Longitud", lon);
                    TrySetIfExists(tipoDestino, inst, "CoordenadasResidencia", (lat, lon));
                    return inst;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando instancia destino: {ex.Message}");
            }

            return null;
        }

        private void TrySetIfExists(Type tipo, object target, string propName, object? value)
        {
            try
            {
                var prop = tipo.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (prop != null && value != null)
                {
                    if (prop.PropertyType.IsAssignableFrom(value.GetType()))
                    {
                        prop.SetValue(target, value);
                    }
                    else
                    {
                        // Intentamos conversión simple
                        try { prop.SetValue(target, Convert.ChangeType(value, prop.PropertyType)); } catch { /* ignore */ }
                    }
                }
            }
            catch { /* ignore */ }
        }

        // ============================
        // Intenta construir GrafoGeografico via reflexión y ejecutar sus métodos
        // miembros: lista de objetos del tipo destino o de tu tipo local; tipoDestino indica el tipo de los items
        // ============================
        private bool TryBuildAndComputeGrafo(List<object> miembros, Type tipoItem)
        {
            try
            {
                // Buscar tipo GrafoGeografico
                Type? grafoTipo = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
                        try { return a.GetTypes(); } catch { return new Type[0]; }
                    })
                    .FirstOrDefault(t => t.Name == "GrafoGeografico");

                if (grafoTipo == null) return false;

                // --- Crear List<tipoItem> y llenarla con los objetos en 'miembros' ---
                var listGenericType = typeof(List<>).MakeGenericType(tipoItem);
                // Crear una instancia de List<tipoItem>
                var listaFuertementeTipada = Activator.CreateInstance(listGenericType);
                // Obtener el método Add de List<tipoItem>
                var addMethod = listGenericType.GetMethod("Add", new Type[] { tipoItem });

                if (listaFuertementeTipada == null || addMethod == null)
                    return false;

                foreach (var item in miembros)
                {
                    // Si el item ya es del tipo destino lo añadimos; si no lo es, intentamos convertir/crear por reflexión.
                    object itemToAdd = item;
                    if (!tipoItem.IsInstanceOfType(item))
                    {
                        // Intentar crear una instancia del tipo destino a partir del objeto de origen
                        // (Esto debería haber sido hecho antes; si no, podrías intentar aquí, pero normalmente 'miembros'
                        // ya contiene instancias del tipo destino).
                        // Para seguridad, intentamos asignar directamente; si no es compatible, añadiremos null (o saltamos).
                        // Aquí optamos por saltar items no-instanciables:
                        continue;
                    }

                    addMethod.Invoke(listaFuertementeTipada, new object[] { itemToAdd });
                }

                // --- Buscar constructor de GrafoGeografico que acepte IEnumerable<tipoItem> o List<tipoItem> ---
                ConstructorInfo? ctor = grafoTipo.GetConstructors()
                    .FirstOrDefault(c =>
                    {
                        var ps = c.GetParameters();
                        if (ps.Length != 1) return false;
                        var pt = ps[0].ParameterType;
                        if (pt.IsGenericType)
                        {
                            var genDef = pt.GetGenericTypeDefinition();
                            return (genDef == typeof(IEnumerable<>) || genDef == typeof(List<>)) &&
                                   pt.GetGenericArguments()[0] == tipoItem;
                        }
                        return false;
                    });

                object? grafoInstance = null;
                if (ctor != null)
                {
                    // Pasar la lista fuertemente tipada al constructor
                    grafoInstance = ctor.Invoke(new object[] { listaFuertementeTipada });
                }
                else
                {
                    // Si no hay constructor que acepte la lista, intentar constructor vacío y setear una propiedad (si existe)
                    var ctorDefault = grafoTipo.GetConstructor(Type.EmptyTypes);
                    if (ctorDefault == null) return false;
                    grafoInstance = ctorDefault.Invoke(null);

                    // Intentar setear una propiedad pública tipo List<tipoItem> o IEnumerable<tipoItem>
                    var prop = grafoTipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .FirstOrDefault(p =>
                                 {
                                     if (!p.CanWrite) return false;
                                     var t = p.PropertyType;
                                     if (!t.IsGenericType) return false;
                                     var gd = t.GetGenericTypeDefinition();
                                     return (gd == typeof(IEnumerable<>) || gd == typeof(List<>)) && t.GetGenericArguments()[0] == tipoItem;
                                 });
                    if (prop != null)
                    {
                        prop.SetValue(grafoInstance, listaFuertementeTipada);
                    }
                    else
                    {
                        // no se pudo setear la lista; abortar
                        return false;
                    }
                }

                // --- Invocar los métodos del grafo por reflexión (misma lógica que antes) ---
                var metodoCercano = grafoTipo.GetMethod("ObtenerParMasCercano");
                var metodoLejano = grafoTipo.GetMethod("ObtenerParMasLejano");
                var metodoPromedio = grafoTipo.GetMethod("ObtenerDistanciaPromedio");

                // Par más cercano
                if (metodoCercano != null)
                {
                    var res = metodoCercano.Invoke(grafoInstance, null);
                    if (res != null)
                        ExtractAndShowPar(res, Closest);
                    else
                        Closest.Text = "No hay suficientes datos para calcular el par más cercano.";
                }

                // Par más lejano
                if (metodoLejano != null)
                {
                    var res = metodoLejano.Invoke(grafoInstance, null);
                    if (res != null)
                        ExtractAndShowPar(res, Farthest);
                    else
                        Farthest.Text = "No hay suficientes datos para calcular el par más lejano.";
                }

                // Promedio
                if (metodoPromedio != null)
                {
                    var val = metodoPromedio.Invoke(grafoInstance, null);
                    if (val is double d)
                        Promedio.Text = $"{d:F2} km";
                    else
                        Promedio.Text = "-";
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TryBuildAndComputeGrafo error: " + ex.Message);
                return false;
            }
        }


        // Extrae A.Nombre, B.Nombre y DistanciaKm de la estructura retornada por ObtenerPar... 
        // y escribe en el TextBlock provisto (Closest o Farthest).
        private void ExtractAndShowPar(object parStruct, System.Windows.Controls.TextBlock destino)
        {
            try
            {
                // intentamos obtener propiedades A, B y DistanciaKm (nombres comunes)
                var tipo = parStruct.GetType();

                var propA = tipo.GetProperty("A") ?? tipo.GetProperty("Item1");
                var propB = tipo.GetProperty("B") ?? tipo.GetProperty("Item2");
                var propDist = tipo.GetProperty("DistanciaKm") ?? tipo.GetProperty("DistanceKm") ?? tipo.GetProperty("Distancia");

                string nombreA = "(sin nombre)";
                string nombreB = "(sin nombre)";
                double distancia = 0;

                if (propA != null)
                {
                    var aVal = propA.GetValue(parStruct);
                    if (aVal != null)
                    {
                        var nProp = aVal.GetType().GetProperty("Nombre");
                        if (nProp != null) nombreA = (nProp.GetValue(aVal)?.ToString()) ?? nombreA;
                    }
                }

                if (propB != null)
                {
                    var bVal = propB.GetValue(parStruct);
                    if (bVal != null)
                    {
                        var nProp = bVal.GetType().GetProperty("Nombre");
                        if (nProp != null) nombreB = (nProp.GetValue(bVal)?.ToString()) ?? nombreB;
                    }
                }

                if (propDist != null)
                {
                    var dVal = propDist.GetValue(parStruct);
                    if (dVal != null && double.TryParse(dVal.ToString(), out double parsed)) distancia = parsed;
                }

                destino.Text = $"{nombreA} y {nombreB} - {distancia:F2} km";
            }
            catch
            {
                destino.Text = "Resultado no disponible (estructura inesperada).";
            }
        }

        // ============================
        // Convertir nuestros miembros locales a una lista simple que pueda usar la UI si no hay grafo externo
        // ============================
        private List<MiembroFamilia> ConvertirMiembrosLocalesValidos()
        {
            var lista = new List<MiembroFamilia>();
            foreach (var f in familiares)
            {
                if (f == null) continue;
                // si quieres validar coordenadas (por ejemplo no 0,0) hazlo aquí
                lista.Add(f);
            }
            return lista;
        }

        // ============================
        // Utilitario: intentar parsear coordenadas desde string (no usado si usamos CoordenadasResidencia tuple)
        // ============================
        private bool TryParseCoordenadas(string? texto, out double latitud, out double longitud)
        {
            latitud = 0; longitud = 0;
            if (string.IsNullOrWhiteSpace(texto)) return false;
            var partes = texto.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2) return false;
            var style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;
            if (!double.TryParse(partes[0].Trim(), style, culture, out latitud)) return false;
            if (!double.TryParse(partes[1].Trim(), style, culture, out longitud)) return false;
            return true;
        }
    }
}
